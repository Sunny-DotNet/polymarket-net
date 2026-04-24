# Polymarket.Client

[ **English** ](README.md) | [ 中文 ](README.zh-CN.md) | [ 日本語 ](README.ja-JP.md) | [ Français ](README.fr-FR.md)

---

`Polymarket.Client` is a .NET SDK for the Polymarket CLOB API.

The current SDK covers the main CLOB client surface:

- public health, version, time, markets, order books, price, spread, trade price, and price history endpoints
- L1 wallet auth for API key creation/derivation
- L2 HMAC auth for account, trades, notifications, balance allowance, rewards, builder API keys, and order endpoints
- V1/V2 order creation, market order creation, posting, cancellation, scoring, and order book hashing
- chain-aware exchange contract resolution for Polygon and Amoy

## Project layout

- `src/Polymarket.Client` - core SDK package
- `tests/Polymarket.Client.Tests` - xUnit test suite
- `examples/Polymarket.Client.ConsoleApp` - minimal console sample

## Build and test

```powershell
dotnet restore Polymarket.Client.slnx
dotnet build Polymarket.Client.slnx --configuration Release --no-restore
dotnet test tests\Polymarket.Client.Tests\Polymarket.Client.Tests.csproj --configuration Release
```

Run a single test:

```powershell
dotnet test tests\Polymarket.Client.Tests\Polymarket.Client.Tests.csproj --filter "FullyQualifiedName~Polymarket.Client.Tests.ClobClientTests.GetVersionAsync_ReturnsVersionFromPayload"
```

## Example

```csharp
using Polymarket.Client;

await using ClobClient client = new("https://clob.polymarket.com", Chain.Polygon);

int version = await client.GetVersionAsync();
long serverTime = await client.GetServerTimeAsync();
PaginationPayload<Market> markets = await client.GetMarketsAsync(new MarketQueryParameters
{
    NextCursor = PolymarketConstants.InitialCursor,
    Limit = 20,
});
OrderBookSummary book = await client.GetOrderBookAsync("TOKEN_ID");
```

Authenticated usage:

```csharp
using Polymarket.Client;

ClobClientOptions options = new()
{
    Host = new Uri("https://clob.polymarket.com"),
    Chain = Chain.Polygon,
    PrivateKey = Environment.GetEnvironmentVariable("POLYMARKET_PRIVATE_KEY"),
    Credentials = new ApiCredentials(
        Environment.GetEnvironmentVariable("POLYMARKET_API_KEY")!,
        Environment.GetEnvironmentVariable("POLYMARKET_API_SECRET")!,
        Environment.GetEnvironmentVariable("POLYMARKET_API_PASSPHRASE")!),
    UseServerTime = true,
};

await using ClobClient authedClient = new(options);

SignedOrderBase order = await authedClient.CreateOrderAsync(
    new OrderArguments("TOKEN_ID", 0.45m, 10m, Side.Buy),
    new PartialCreateOrderOptions("0.01", false));

JsonElement postResult = await authedClient.PostOrderAsync(order);
```

## Notes

- Package ID: `Polymarket.Client`
- Target framework: `.NET 10`
- Naming follows .NET conventions while keeping the official SDK surface recognizable
- `HttpClient` is injectable for tests and custom hosting scenarios

## Release automation

- CI runs on GitHub Actions with `windows-latest`
- NuGet publishing is tag-driven and derives the package version from the git tag
- Publishing follows the same Trusted Publishing pattern used in `Sunny-DotNet/open-hub-agent`
