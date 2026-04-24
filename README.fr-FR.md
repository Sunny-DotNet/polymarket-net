# Polymarket.Client

[ English ](README.md) | [ 中文 ](README.zh-CN.md) | [ 日本語 ](README.ja-JP.md) | [ **Français** ](README.fr-FR.md)

---

`Polymarket.Client` est un SDK .NET pour les API CLOB et Gamma de Polymarket.

La version actuelle couvre la surface principale du client CLOB ainsi que la surface publique Gamma pour la découverte de contenu :

- endpoints publics health, version, time, markets, order books, price, spread, last trade price et price history
- endpoints publics Gamma status, teams, tags, events, markets, series, comments, profiles, sports et search
- sessions websocket market, user et sports via `ClobWebSocketClient`
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

Utilisation Gamma :

```csharp
using Polymarket.Client;

await using GammaClient gammaClient = new("https://gamma-api.polymarket.com");

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

Utilisation WebSocket :

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
- `ClobClient` cible le trading/CLOB, tandis que `GammaClient` cible le catalogue et le contenu
- `ClobWebSocketClient` cible les sessions websocket temps réel market, user et sports
- `HttpClient` est injectable pour faciliter les tests et l'hébergement personnalisé

## Exemple console

L'exemple console montre maintenant **la découverte du dernier marché BTC 5 minutes via Gamma puis le streaming temps réel via le websocket CLOB** :

```powershell
dotnet run --project examples\Polymarket.Client.ConsoleApp\Polymarket.Client.ConsoleApp.csproj
```

## Publication

- La CI s'exécute sur GitHub Actions avec `windows-latest`
- La publication NuGet est déclenchée par tag et la version est dérivée du tag git
- Le flux de publication suit le même modèle Trusted Publishing que `Sunny-DotNet/open-hub-agent`
