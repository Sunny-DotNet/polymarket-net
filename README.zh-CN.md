# Polymarket.Client

[ English ](README.md) | [ **中文** ](README.zh-CN.md) | [ 日本語 ](README.ja-JP.md) | [ Français ](README.fr-FR.md)

---

`Polymarket.Client` 是一个面向 Polymarket CLOB API 和 Gamma API 的 .NET SDK。

当前版本已经覆盖 CLOB 主能力面和公开 Gamma 内容查询能力：

- 公共 health、version、time、markets、order books、price、spread、last trade price、price history
- 公共 Gamma status、teams、tags、events、markets、series、comments、profiles、sports、search
- 通过 `ClobWebSocketClient` 提供 market、user、sports 三类 websocket session
- L1 钱包签名鉴权，用于 API key 创建与派生
- L2 HMAC 鉴权，用于账户、成交、通知、余额授权、奖励、builder API key 和下单接口
- V1/V2 订单创建、市价单创建、下单、撤单、评分、order book hash
- Polygon / Amoy 链的合约地址解析

## 项目结构

- `src/Polymarket.Client` - 核心 SDK
- `tests/Polymarket.Client.Tests` - xUnit 测试
- `examples/Polymarket.Client.ConsoleApp` - 控制台示例

## 构建与测试

```powershell
dotnet restore Polymarket.Client.slnx
dotnet build Polymarket.Client.slnx --configuration Release --no-restore
dotnet test tests\Polymarket.Client.Tests\Polymarket.Client.Tests.csproj --configuration Release
```

运行单个测试：

```powershell
dotnet test tests\Polymarket.Client.Tests\Polymarket.Client.Tests.csproj --filter "FullyQualifiedName~Polymarket.Client.Tests.ClobClientTests.CreateApiKeyAsync_UsesReferenceL1Signature"
```

## 示例

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

Gamma 调用：

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

WebSocket 调用：

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

鉴权调用：

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

## 说明

- 包名：`Polymarket.Client`
- 目标框架：`.NET 10`
- 公开 API 命名遵循 .NET 习惯，同时尽量贴近官方 SDK 语义
- `ClobClient` 负责交易/CLOB，`GammaClient` 负责目录/内容查询
- `ClobWebSocketClient` 负责实时 market、user、sports websocket session
- 支持注入 `HttpClient`，便于测试和自定义宿主场景

## Console 示例

现在的控制台示例演示的是 **Gamma 发现最新 BTC 5 分钟市场 + CLOB websocket 实时订阅**：

```powershell
dotnet run --project examples\Polymarket.Client.ConsoleApp\Polymarket.Client.ConsoleApp.csproj
```

## 发布

- CI 运行在 GitHub Actions `windows-latest`
- NuGet 发布由 tag 触发，并从 tag 推导版本号
- 发布流程参考 `Sunny-DotNet/open-hub-agent` 的 Trusted Publishing 方式
