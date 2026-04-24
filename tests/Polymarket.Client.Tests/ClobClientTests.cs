using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Polymarket.Client.Tests;

public sealed class ClobClientTests
{
    private const string KnownPrivateKey = "0xac0974bec39a17e36ba4a6b4d238ff944bacb478cbed5efcae784d7bf4f2ff80";
    private const string KnownAddress = "0xf39fd6e51aad88f6f4ce6ab8827279cfffb92266";

    [Fact]
    public async Task Constructor_UsesDefaultClobHostConstant()
    {
        await using ClobClient client = new();

        Assert.Equal(PolymarketHosts.Clob, client.Host.AbsoluteUri);
    }

    [Fact]
    public async Task GetVersionAsync_ReturnsVersionFromPayload()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(_ => CreateJsonResponse("""{"version":3}""")));
        await using ClobClient client = new("https://clob.polymarket.com", Chain.Polygon, httpClient);

        int version = await client.GetVersionAsync();

        Assert.Equal(3, version);
    }

    [Fact]
    public async Task CreateApiKeyAsync_UsesReferenceL1Signature()
    {
        HttpRequestMessage? authRequest = null;
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/time" => CreateJsonResponse("""{"time":10000000}"""),
                "/auth/api-key" => Capture(request, """{"apiKey":"k","secret":"s","passphrase":"p"}""", out authRequest),
                _ => throw new InvalidOperationException($"Unexpected path {request.RequestUri!.AbsolutePath}."),
            };
        }));

        ClobClientOptions options = new()
        {
            Host = new Uri("https://clob.polymarket.com"),
            Chain = Chain.Amoy,
            PrivateKey = KnownPrivateKey,
            UseServerTime = true,
        };

        await using ClobClient client = new(options, httpClient);
        ApiCredentials credentials = await client.CreateApiKeyAsync(23);

        Assert.Equal("k", credentials.Key);
        Assert.NotNull(authRequest);
        Assert.Equal(KnownAddress, authRequest!.Headers.GetValues("POLY_ADDRESS").Single().ToLowerInvariant());
        Assert.Equal("10000000", authRequest.Headers.GetValues("POLY_TIMESTAMP").Single());
        Assert.Equal("23", authRequest.Headers.GetValues("POLY_NONCE").Single());
        Assert.Equal(
            "0xf62319a987514da40e57e2f4d7529f7bac38f0355bd88bb5adbb3768d80de6c1682518e0af677d5260366425f4361e7b70c25ae232aff0ab2331e2b164a1aedc1b",
            authRequest.Headers.GetValues("POLY_SIGNATURE").Single());
    }

    [Fact]
    public async Task GetApiKeysAsync_UsesExpectedL2Headers()
    {
        byte[] secretBytes = Encoding.UTF8.GetBytes("secret-for-tests");
        string apiSecret = Convert.ToBase64String(secretBytes).Replace('+', '-').Replace('/', '_');
        HttpRequestMessage? apiKeysRequest = null;

        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/time" => CreateJsonResponse("""{"time":1713929974}"""),
                "/auth/api-keys" => Capture(request, """{"apiKeys":[{"key":"abc","secret":"def","passphrase":"ghi"}]}""", out apiKeysRequest),
                _ => throw new InvalidOperationException($"Unexpected path {request.RequestUri!.AbsolutePath}."),
            };
        }));

        ClobClientOptions options = new()
        {
            Host = new Uri("https://clob.polymarket.com"),
            Chain = Chain.Polygon,
            PrivateKey = KnownPrivateKey,
            Credentials = new ApiCredentials("key-1", apiSecret, "pass-1"),
            UseServerTime = true,
        };

        await using ClobClient client = new(options, httpClient);
        ApiKeysResponse response = await client.GetApiKeysAsync();

        Assert.Single(response.ApiKeys);
        Assert.NotNull(apiKeysRequest);
        Assert.Equal(KnownAddress, apiKeysRequest!.Headers.GetValues("POLY_ADDRESS").Single().ToLowerInvariant());
        Assert.Equal("1713929974", apiKeysRequest.Headers.GetValues("POLY_TIMESTAMP").Single());
        Assert.Equal("key-1", apiKeysRequest.Headers.GetValues("POLY_API_KEY").Single());
        Assert.Equal("pass-1", apiKeysRequest.Headers.GetValues("POLY_PASSPHRASE").Single());
        Assert.Equal(
            ComputeExpectedHmac(apiSecret, 1713929974, "GET", "/auth/api-keys", null),
            apiKeysRequest.Headers.GetValues("POLY_SIGNATURE").Single());
    }

    [Fact]
    public async Task DeleteApiKeyAsync_ParsesStructuredResponse()
    {
        byte[] secretBytes = Encoding.UTF8.GetBytes("secret-for-tests");
        string apiSecret = Convert.ToBase64String(secretBytes).Replace('+', '-').Replace('/', '_');
        HttpRequestMessage? deleteRequest = null;

        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/time" => CreateJsonResponse("""{"time":1713929974}"""),
                "/auth/api-key" => Capture(request, """{"success":true,"status":"ok","message":"deleted"}""", out deleteRequest),
                _ => throw new InvalidOperationException($"Unexpected path {request.RequestUri!.AbsolutePath}."),
            };
        }));

        ClobClientOptions options = new()
        {
            Host = new Uri("https://clob.polymarket.com"),
            Chain = Chain.Polygon,
            PrivateKey = KnownPrivateKey,
            Credentials = new ApiCredentials("key-1", apiSecret, "pass-1"),
            UseServerTime = true,
        };

        await using ClobClient client = new(options, httpClient);
        ApiOperationResult response = await client.DeleteApiKeyAsync();

        Assert.True(response.Success);
        Assert.Equal("ok", response.Status);
        Assert.Equal("deleted", response.Message);
        Assert.NotNull(deleteRequest);
        Assert.Equal(
            ComputeExpectedHmac(apiSecret, 1713929974, "DELETE", "/auth/api-key", null),
            deleteRequest!.Headers.GetValues("POLY_SIGNATURE").Single());
    }

    [Fact]
    public async Task DeleteReadonlyApiKeyAsync_SendsKeyPayload_AndParsesBooleanResponse()
    {
        const string keyToDelete = "readonly-key";
        byte[] secretBytes = Encoding.UTF8.GetBytes("secret-for-tests");
        string apiSecret = Convert.ToBase64String(secretBytes).Replace('+', '-').Replace('/', '_');
        HttpRequestMessage? deleteRequest = null;
        string? requestBody = null;

        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/time" => CreateJsonResponse("""{"time":1713929974}"""),
                "/auth/readonly-api-key" => Capture(
                    request,
                    "true",
                    out deleteRequest,
                    capturedBody: requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult()),
                _ => throw new InvalidOperationException($"Unexpected path {request.RequestUri!.AbsolutePath}."),
            };
        }));

        ClobClientOptions options = new()
        {
            Host = new Uri("https://clob.polymarket.com"),
            Chain = Chain.Polygon,
            PrivateKey = KnownPrivateKey,
            Credentials = new ApiCredentials("key-1", apiSecret, "pass-1"),
            UseServerTime = true,
        };

        await using ClobClient client = new(options, httpClient);
        ApiOperationResult response = await client.DeleteReadonlyApiKeyAsync(keyToDelete);

        Assert.True(response.Success);
        Assert.Equal("""{"key":"readonly-key"}""", requestBody);
        Assert.NotNull(deleteRequest);
        Assert.Equal(
            ComputeExpectedHmac(apiSecret, 1713929974, "DELETE", "/auth/readonly-api-key", requestBody),
            deleteRequest!.Headers.GetValues("POLY_SIGNATURE").Single());
    }

    [Fact]
    public async Task CreateOrderAsync_ReturnsSignedOrderV2()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/version" => CreateJsonResponse("""{"version":2}"""),
                _ => throw new InvalidOperationException($"Unexpected path {request.RequestUri!.AbsolutePath}."),
            };
        }));

        ClobClientOptions options = new()
        {
            Host = new Uri("https://clob.polymarket.com"),
            Chain = Chain.Polygon,
            PrivateKey = KnownPrivateKey,
        };

        await using ClobClient client = new(options, httpClient);
        SignedOrderBase order = await client.CreateOrderAsync(
            new OrderArguments("123", 0.45m, 10m, Side.Buy),
            new PartialCreateOrderOptions("0.01", false));

        SignedOrderV2 typedOrder = Assert.IsType<SignedOrderV2>(order);
        Assert.Equal(KnownAddress, typedOrder.Maker.ToLowerInvariant());
        Assert.Equal(KnownAddress, typedOrder.Signer.ToLowerInvariant());
        Assert.Equal("123", typedOrder.TokenId);
        Assert.False(string.IsNullOrWhiteSpace(typedOrder.Signature));
        Assert.Equal(SignatureTypeV2.Eoa, typedOrder.SignatureType);
    }

    [Fact]
    public void GetOrderBookHash_MatchesSha1OfHashlessPayload()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(_ => CreateJsonResponse("""{"version":2}""")));
        using ClobClient client = new("https://clob.polymarket.com", Chain.Polygon, httpClient);

        OrderBookSummary orderBook = new(
            "m1",
            "a1",
            "1713929974",
            [new OrderSummary("0.45", "10")],
            [new OrderSummary("0.55", "10")],
            "1",
            "0.01",
            false,
            "existing",
            "0.50");

        string expectedPayload = JsonSerializer.Serialize(orderBook with { Hash = string.Empty });
        string expectedHash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(expectedPayload))).ToLowerInvariant();

        string hash = client.GetOrderBookHash(orderBook);

        Assert.Equal(expectedHash, hash);
        Assert.Equal("existing", orderBook.Hash);
    }

    [Fact]
    public async Task GetOrderAsync_ReturnsStronglyTypedOrder()
    {
        byte[] secretBytes = Encoding.UTF8.GetBytes("secret-for-tests");
        string apiSecret = Convert.ToBase64String(secretBytes).Replace('+', '-').Replace('/', '_');
        HttpRequestMessage? getOrderRequest = null;

        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/time" => CreateJsonResponse("""{"time":1713929974}"""),
                "/data/order/o1" => Capture(
                    request,
                    """{"id":"o1","status":"LIVE","owner":"0x1","maker_address":"0x2","market":"c1","asset_id":"t1","side":"BUY","original_size":"10","size_matched":"2","price":"0.45","associate_trades":["tr1"],"outcome":"Yes","created_at":1713929000,"expiration":"0","order_type":"GTC"}""",
                    out getOrderRequest),
                _ => throw new InvalidOperationException($"Unexpected path {request.RequestUri!.AbsolutePath}."),
            };
        }));

        ClobClientOptions options = new()
        {
            Host = new Uri("https://clob.polymarket.com"),
            Chain = Chain.Polygon,
            PrivateKey = KnownPrivateKey,
            Credentials = new ApiCredentials("key-1", apiSecret, "pass-1"),
            UseServerTime = true,
        };

        await using ClobClient client = new(options, httpClient);
        OpenOrder order = await client.GetOrderAsync("o1");

        Assert.Equal("o1", order.Id);
        Assert.Equal("LIVE", order.Status);
        Assert.Equal("t1", order.AssetId);
        Assert.Equal("GTC", order.OrderType);
        Assert.NotNull(getOrderRequest);
        Assert.Equal(
            ComputeExpectedHmac(apiSecret, 1713929974, "GET", "/data/order/o1", null),
            getOrderRequest!.Headers.GetValues("POLY_SIGNATURE").Single());
    }

    [Fact]
    public async Task MarketListQueryOverloads_SerializeTypedAndAdditionalParameters()
    {
        List<HttpRequestMessage> requests = [];
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            requests.Add(request);
            return request.RequestUri!.AbsolutePath switch
            {
                "/markets" or "/simplified-markets" or "/sampling-markets" or "/sampling-simplified-markets"
                    => CreateJsonResponse("""{"limit":1,"count":0,"next_cursor":"LTE=","data":[]}"""),
                _ => throw new InvalidOperationException($"Unexpected path {request.RequestUri!.AbsolutePath}."),
            };
        }));

        await using ClobClient client = new("https://clob.polymarket.com", Chain.Polygon, httpClient);
        MarketQueryParameters queryParameters = new()
        {
            NextCursor = "MA==",
            Limit = 1,
            AdditionalParameters = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["active"] = "true",
            },
        };

        await client.GetMarketsAsync(queryParameters);
        await client.GetSimplifiedMarketsAsync(queryParameters);
        await client.GetSamplingMarketsAsync(queryParameters);
        await client.GetSamplingSimplifiedMarketsAsync(queryParameters);

        Assert.Collection(
            requests,
            request => AssertMarketQuery(request, "/markets", "MA==", "1", "true"),
            request => AssertMarketQuery(request, "/simplified-markets", "MA==", "1", "true"),
            request => AssertMarketQuery(request, "/sampling-markets", "MA==", "1", "true"),
            request => AssertMarketQuery(request, "/sampling-simplified-markets", "MA==", "1", "true"));
    }

    [Fact]
    public async Task MarketListNextCursorOverloads_ForwardToTypedQueryParameters()
    {
        List<HttpRequestMessage> requests = [];
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            requests.Add(request);
            return request.RequestUri!.AbsolutePath switch
            {
                "/markets" or "/simplified-markets" or "/sampling-markets" or "/sampling-simplified-markets"
                    => CreateJsonResponse("""{"limit":1,"count":0,"next_cursor":"LTE=","data":[]}"""),
                _ => throw new InvalidOperationException($"Unexpected path {request.RequestUri!.AbsolutePath}."),
            };
        }));

        await using ClobClient client = new("https://clob.polymarket.com", Chain.Polygon, httpClient);

        await client.GetMarketsAsync("MA==");
        await client.GetSimplifiedMarketsAsync("MA==");
        await client.GetSamplingMarketsAsync("MA==");
        await client.GetSamplingSimplifiedMarketsAsync("MA==");

        Assert.Collection(
            requests,
            request => AssertMarketQuery(request, "/markets", "MA==", null, null),
            request => AssertMarketQuery(request, "/simplified-markets", "MA==", null, null),
            request => AssertMarketQuery(request, "/sampling-markets", "MA==", null, null),
            request => AssertMarketQuery(request, "/sampling-simplified-markets", "MA==", null, null));
    }

    [Fact]
    public async Task GetMarketsAsync_ReturnsStronglyTypedMarkets()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/markets" => CreateJsonResponse(
                    """
                    {
                      "limit": 1,
                      "count": 1,
                      "next_cursor": "LTE=",
                      "data": [
                        {
                          "enable_order_book": true,
                          "active": true,
                          "closed": false,
                          "archived": false,
                          "accepting_orders": true,
                          "accepting_order_timestamp": "2025-01-01T00:00:00Z",
                          "minimum_order_size": 5,
                          "minimum_tick_size": 0.01,
                          "condition_id": "c1",
                          "question_id": "q1",
                          "question": "Will it rain?",
                          "description": "Rain market",
                          "market_slug": "will-it-rain",
                          "end_date_iso": "2025-01-02T00:00:00Z",
                          "game_start_time": null,
                          "seconds_delay": 0,
                          "fpmm": "0xabc",
                          "maker_base_fee": 0,
                          "taker_base_fee": 0,
                          "notifications_enabled": true,
                          "neg_risk": false,
                          "neg_risk_market_id": null,
                          "neg_risk_request_id": null,
                          "icon": "https://example.com/icon.png",
                          "image": "https://example.com/image.png",
                          "rewards": {
                            "rates": null,
                            "min_size": 10,
                            "max_spread": 3
                          },
                          "is_50_50_outcome": true,
                          "tokens": [
                            {
                              "token_id": "t1",
                              "outcome": "Yes",
                              "price": 0.42,
                              "winner": false
                            }
                          ],
                          "tags": [
                            "weather"
                          ],
                          "custom_field": "kept"
                        }
                      ]
                    }
                    """),
                _ => throw new InvalidOperationException($"Unexpected path {request.RequestUri!.AbsolutePath}."),
            };
        }));

        await using ClobClient client = new("https://clob.polymarket.com", Chain.Polygon, httpClient);

        PaginationPayload<Market> markets = await client.GetMarketsAsync();

        Market market = Assert.Single(markets.Data);
        Assert.Equal("c1", market.ConditionId);
        Assert.Equal("Will it rain?", market.Question);
        Assert.Single(market.Tokens);
        Assert.Equal("t1", market.Tokens[0].TokenId);
        Assert.True(market.IsFiftyFiftyOutcome);
        Assert.Equal("kept", market.ExtensionData["custom_field"].GetString());
    }

    [Fact]
    public async Task GetSimplifiedMarketsAsync_ReturnsStronglyTypedMarkets()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/simplified-markets" => CreateJsonResponse(
                    """
                    {
                      "limit": 1,
                      "count": 1,
                      "next_cursor": "LTE=",
                      "data": [
                        {
                          "condition_id": "c1",
                          "rewards": {
                            "rates": null,
                            "min_size": 1,
                            "max_spread": 2
                          },
                          "tokens": [
                            {
                              "token_id": "t1",
                              "outcome": "Yes",
                              "price": 0.51,
                              "winner": false
                            }
                          ],
                          "active": true,
                          "closed": false,
                          "archived": false,
                          "accepting_orders": true,
                          "category": "weather"
                        }
                      ]
                    }
                    """),
                _ => throw new InvalidOperationException($"Unexpected path {request.RequestUri!.AbsolutePath}."),
            };
        }));

        await using ClobClient client = new("https://clob.polymarket.com", Chain.Polygon, httpClient);

        PaginationPayload<SimplifiedMarket> markets = await client.GetSimplifiedMarketsAsync();

        SimplifiedMarket market = Assert.Single(markets.Data);
        Assert.Equal("c1", market.ConditionId);
        Assert.True(market.AcceptingOrders);
        Assert.Equal(1, market.Rewards!.MinSize);
        Assert.Equal("weather", market.ExtensionData["category"].GetString());
    }

    [Fact]
    public async Task GetMarketAsync_AndGetMarketByTokenAsync_ReturnStronglyTypedResponses()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsolutePath switch
            {
                "/markets/c1" => CreateJsonResponse(
                    """
                    {
                      "enable_order_book": true,
                      "active": true,
                      "closed": false,
                      "archived": false,
                      "accepting_orders": true,
                      "accepting_order_timestamp": "2025-01-01T00:00:00Z",
                      "minimum_order_size": 5,
                      "minimum_tick_size": 0.01,
                      "condition_id": "c1",
                      "question_id": "q1",
                      "question": "Will it rain?",
                      "description": "Rain market",
                      "market_slug": "will-it-rain",
                      "end_date_iso": "2025-01-02T00:00:00Z",
                      "game_start_time": null,
                      "seconds_delay": 0,
                      "fpmm": "0xabc",
                      "maker_base_fee": 0,
                      "taker_base_fee": 0,
                      "notifications_enabled": true,
                      "neg_risk": false,
                      "neg_risk_market_id": null,
                      "neg_risk_request_id": null,
                      "icon": null,
                      "image": null,
                      "rewards": {
                        "rates": null,
                        "min_size": 10,
                        "max_spread": 3
                      },
                      "is_50_50_outcome": true,
                      "tokens": [
                        {
                          "token_id": "t1",
                          "outcome": "Yes",
                          "price": 0.42,
                          "winner": false
                        }
                      ],
                      "tags": [
                        "weather"
                      ]
                    }
                    """),
                "/markets-by-token/t1" => CreateJsonResponse("""{"condition_id":"c1","primary_token_id":"t1","secondary_token_id":"t2"}"""),
                _ => throw new InvalidOperationException($"Unexpected path {request.RequestUri!.AbsolutePath}."),
            };
        }));

        await using ClobClient client = new("https://clob.polymarket.com", Chain.Polygon, httpClient);

        Market market = await client.GetMarketAsync("c1");
        MarketByTokenResponse marketByToken = await client.GetMarketByTokenAsync("t1");

        Assert.Equal("c1", market.ConditionId);
        Assert.Equal("t1", market.Tokens[0].TokenId);
        Assert.Equal("c1", marketByToken.ConditionId);
        Assert.Equal("t1", marketByToken.PrimaryTokenId);
        Assert.Equal("t2", marketByToken.SecondaryTokenId);
    }

    [Fact]
    public void ContractConfig_ReturnsPolygonAddresses()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(_ => CreateJsonResponse("""{"version":2}""")));
        using ClobClient client = new("https://clob.polymarket.com", Chain.Polygon, httpClient);

        Assert.Equal("0x4bFb41d5B3570DeFd03C39a9A4D8dE6Bd8B8982E", client.ContractConfig.Exchange);
    }

    private static HttpResponseMessage CreateJsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private static HttpResponseMessage Capture(HttpRequestMessage request, string json, out HttpRequestMessage capturedRequest, string? capturedBody = null)
    {
        capturedRequest = request;
        return CreateJsonResponse(json);
    }

    private static string ComputeExpectedHmac(string base64UrlSecret, long timestamp, string method, string requestPath, string? body)
    {
        string padded = base64UrlSecret.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        byte[] secret = Convert.FromBase64String(padded);
        string payload = string.Concat(
            timestamp.ToString(CultureInfo.InvariantCulture),
            method,
            requestPath,
            body ?? string.Empty);

        using HMACSHA256 hmac = new(secret);
        byte[] digest = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(digest).Replace('+', '-').Replace('/', '_');
    }

    private static void AssertMarketQuery(HttpRequestMessage request, string expectedPath, string expectedCursor, string? expectedLimit, string? expectedActive)
    {
        Assert.Equal(expectedPath, request.RequestUri!.AbsolutePath);

        Dictionary<string, string?> query = ParseQuery(request.RequestUri.Query);
        Assert.Equal(expectedCursor, query["next_cursor"]);

        if (expectedLimit is null)
        {
            Assert.False(query.ContainsKey("limit"));
        }
        else
        {
            Assert.Equal(expectedLimit, query["limit"]);
        }

        if (expectedActive is null)
        {
            Assert.False(query.ContainsKey("active"));
        }
        else
        {
            Assert.Equal(expectedActive, query["active"]);
        }
    }

    private static Dictionary<string, string?> ParseQuery(string query)
    {
        Dictionary<string, string?> result = new(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(query))
        {
            return result;
        }

        foreach (string part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = part.Split('=', 2);
            string key = Uri.UnescapeDataString(pair[0]);
            string? value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : null;
            result[key] = value;
        }

        return result;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
