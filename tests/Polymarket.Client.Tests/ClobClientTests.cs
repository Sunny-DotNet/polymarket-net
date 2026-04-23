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

    private static HttpResponseMessage Capture(HttpRequestMessage request, string json, out HttpRequestMessage capturedRequest)
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

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
