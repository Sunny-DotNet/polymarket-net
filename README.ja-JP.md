# Polymarket.Client

[ English ](README.md) | [ 中文 ](README.zh-CN.md) | [ **日本語** ](README.ja-JP.md) | [ Français ](README.fr-FR.md)

---

`Polymarket.Client` は、Polymarket CLOB API と Gamma API 向けの .NET SDK です。

現在のバージョンでは、CLOB の主要機能に加えて公開 Gamma API の検索/参照機能も実装しています。

- public health、version、time、markets、order books、price、spread、last trade price、price history
- public Gamma status、teams、tags、events、markets、series、comments、profiles、sports、search
- `ClobWebSocketClient` による market / user / sports websocket session
- API key 作成・導出のための L1 ウォレット署名認証
- account、trades、notifications、balance allowance、rewards、builder API key、order 系エンドポイント向けの L2 HMAC 認証
- V1/V2 の注文作成、成行注文作成、発注、取消、scoring、order book hash
- Polygon / Amoy のコントラクト設定解決

## プロジェクト構成

- `src/Polymarket.Client` - コア SDK
- `tests/Polymarket.Client.Tests` - xUnit テスト
- `examples/Polymarket.Client.ConsoleApp` - コンソールサンプル

## ビルドとテスト

```powershell
dotnet restore Polymarket.Client.slnx
dotnet build Polymarket.Client.slnx --configuration Release --no-restore
dotnet test tests\Polymarket.Client.Tests\Polymarket.Client.Tests.csproj --configuration Release
```

単一テストの実行:

```powershell
dotnet test tests\Polymarket.Client.Tests\Polymarket.Client.Tests.csproj --filter "FullyQualifiedName~Polymarket.Client.Tests.ClobClientTests.CreateApiKeyAsync_UsesReferenceL1Signature"
```

## 使用例

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

Gamma の利用例:

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

WebSocket の利用例:

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

認証付きの利用例:

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

## 補足

- パッケージ ID: `Polymarket.Client`
- 対象フレームワーク: `.NET 10`
- 公開 API は .NET 命名規約に合わせつつ、公式 SDK の構成に近づけています
- `ClobClient` は取引/CLOB、`GammaClient` はカタログ/コンテンツ API を担当します
- `ClobWebSocketClient` は realtime の market / user / sports websocket session を担当します
- `HttpClient` を注入できるため、テストや独自ホスティングに対応しやすい設計です

## Console サンプル

現在の console サンプルは **Gamma で最新の BTC 5 分市場を見つけ、CLOB websocket でリアルタイム購読** する流れを示します:

```powershell
dotnet run --project examples\Polymarket.Client.ConsoleApp\Polymarket.Client.ConsoleApp.csproj
```

## リリース

- CI は GitHub Actions の `windows-latest` で実行されます
- NuGet 発行は tag ベースで、バージョンは git tag から決定されます
- 発行フローは `Sunny-DotNet/open-hub-agent` と同じ Trusted Publishing パターンに合わせています
