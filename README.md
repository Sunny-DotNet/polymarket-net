# Polymarket.Client

[ **English** ](README.md) | [ 中文 ](README.zh-CN.md) | [ 日本語 ](README.ja-JP.md) | [ Français ](README.fr-FR.md)

---

`Polymarket.Client` is a .NET SDK for the Polymarket CLOB and Gamma APIs.

The current SDK covers the main CLOB client surface and the public Gamma content/discovery surface:

- public health, version, time, markets, order books, price, spread, trade price, and price history endpoints
- public Gamma status, teams, tags, events, markets, series, comments, profiles, sports, and search endpoints
- websocket sessions for market, user, and sports channels via `ClobWebSocketClient`
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

await using ClobClient client = new(Chain.Polygon);

int version = await client.GetVersionAsync();
long serverTime = await client.GetServerTimeAsync();
PaginationPayload<Market> markets = await client.GetMarketsAsync(new MarketQueryParameters
{
    NextCursor = PolymarketConstants.InitialCursor,
    Limit = 20,
});
OrderBookSummary book = await client.GetOrderBookAsync("TOKEN_ID");
```

Gamma usage:

```csharp
using Polymarket.Client;

await using GammaClient gammaClient = new();

string status = await gammaClient.GetStatusAsync();
IReadOnlyList<GammaMarket> gammaMarkets = await gammaClient.GetMarketsAsync(new GammaMarketQueryParameters
{
    Limit = 20,
    Closed = false,
    IncludeTag = true,
});

GammaSearchResponse search = await gammaClient.PublicSearchAsync(new GammaPublicSearchQueryParameters
{
    Query = "election",
    SearchTags = true,
    SearchProfiles = true,
});
```

WebSocket usage:

```csharp
using Polymarket.Client;

ClobWebSocketClient webSocketClient = new();

await using ClobMarketWebSocketSession session = await webSocketClient.ConnectMarketAsync(
    new ClobMarketSubscriptionRequest
    {
        AssetIds = ["YES_TOKEN_ID", "NO_TOKEN_ID"],
        InitialDump = true,
        Level = ClobMarketSubscriptionLevel.Level2,
        CustomFeatureEnabled = true,
    });

await foreach (ClobMarketChannelMessage message in session.ReadAllAsync())
{
    Console.WriteLine(message.EventType);
}
```

Authenticated usage:

```csharp
using Polymarket.Client;

ClobClientOptions options = new()
{
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
- `ClobClient` targets trading/CLOB endpoints, while `GammaClient` targets catalog/content endpoints
- `ClobWebSocketClient` targets realtime market, user, and sports websocket sessions
- `HttpClient` is injectable for tests and custom hosting scenarios

## Console sample

The console sample now demonstrates **Gamma discovery + CLOB websocket streaming** for the latest BTC 5-minute market:

```powershell
dotnet run --project examples\Polymarket.Client.ConsoleApp\Polymarket.Client.ConsoleApp.csproj
```

