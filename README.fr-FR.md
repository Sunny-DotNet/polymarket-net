# Polymarket.Client

[ English ](README.md) | [ 中文 ](README.zh-CN.md) | [ 日本語 ](README.ja-JP.md) | [ **Français** ](README.fr-FR.md)

---

`Polymarket.Client` est un SDK .NET pour l'API CLOB de Polymarket.

La version actuelle couvre la surface principale du client CLOB :

- endpoints publics health, version, time, markets, order books, price, spread, last trade price et price history
- authentification L1 par signature de portefeuille pour la création et la dérivation des API keys
- authentification L2 HMAC pour les endpoints account, trades, notifications, balance allowance, rewards, builder API keys et orders
- création d'ordres V1/V2, ordres au marché, envoi, annulation, scoring et order book hash
- résolution des contrats selon la chaîne pour Polygon et Amoy

## Structure du projet

- `src/Polymarket.Client` - SDK principal
- `tests/Polymarket.Client.Tests` - suite de tests xUnit
- `examples/Polymarket.Client.ConsoleApp` - exemple console minimal

## Build et tests

```powershell
dotnet restore Polymarket.Client.slnx
dotnet build Polymarket.Client.slnx --configuration Release --no-restore
dotnet test tests\Polymarket.Client.Tests\Polymarket.Client.Tests.csproj --configuration Release
```

Exécuter un seul test :

```powershell
dotnet test tests\Polymarket.Client.Tests\Polymarket.Client.Tests.csproj --filter "FullyQualifiedName~Polymarket.Client.Tests.ClobClientTests.CreateApiKeyAsync_UsesReferenceL1Signature"
```

## Exemple

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

Utilisation authentifiée :

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

- Package ID : `Polymarket.Client`
- Framework cible : `.NET 10`
- Le nommage public suit les conventions .NET tout en restant proche des SDK officiels
- `HttpClient` est injectable pour faciliter les tests et l'hébergement personnalisé

## Publication

- La CI s'exécute sur GitHub Actions avec `windows-latest`
- La publication NuGet est déclenchée par tag et la version est dérivée du tag git
- Le flux de publication suit le même modèle Trusted Publishing que `Sunny-DotNet/open-hub-agent`
