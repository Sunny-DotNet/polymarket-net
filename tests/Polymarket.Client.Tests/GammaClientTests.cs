using System.Net;
using System.Text;
using System.Text.Json;

namespace Polymarket.Client.Tests;

public sealed class GammaClientTests
{
    [Fact]
    public async Task Constructor_UsesDefaultGammaHostConstant()
    {
        await using GammaClient client = new();

        Assert.Equal(PolymarketHosts.Gamma, client.Host.AbsoluteUri);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsPlainTextPayload()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(_ => CreateTextResponse("OK")));
        await using GammaClient client = new("https://gamma-api.polymarket.com", httpClient);

        string status = await client.GetStatusAsync();

        Assert.Equal("OK", status);
    }

    [Fact]
    public async Task GetMarketsAsync_SerializesRichQueryParameters()
    {
        HttpRequestMessage? capturedRequest = null;
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return CreateJsonResponse("""[{"id":"1","question":"Will it rain?","conditionId":"c1","slug":"rain","active":true}]""");
        }));

        await using GammaClient client = new("https://gamma-api.polymarket.com", httpClient);
        GammaMarketQueryParameters parameters = new()
        {
            Limit = 10,
            Offset = 20,
            Order = "volume",
            Ascending = false,
            Id = [1, 2],
            Slug = ["rain", "snow"],
            ClobTokenIds = ["t1"],
            TagId = 7,
            IncludeTag = true,
            Closed = false,
            AdditionalParameters = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["locale"] = "en",
            },
        };

        IReadOnlyList<GammaMarket> markets = await client.GetMarketsAsync(parameters);

        Assert.Single(markets);
        Assert.NotNull(capturedRequest);
        Assert.Equal("/markets", capturedRequest!.RequestUri!.AbsolutePath);
        Dictionary<string, IReadOnlyList<string?>> query = ParseQueryValues(capturedRequest.RequestUri.Query);
        Assert.Equal(["10"], query["limit"]);
        Assert.Equal(["20"], query["offset"]);
        Assert.Equal(["volume"], query["order"]);
        Assert.Equal(["false"], query["ascending"]);
        Assert.Equal(["1", "2"], query["id"]);
        Assert.Equal(["rain", "snow"], query["slug"]);
        Assert.Equal(["t1"], query["clob_token_ids"]);
        Assert.Equal(["7"], query["tag_id"]);
        Assert.Equal(["true"], query["include_tag"]);
        Assert.Equal(["false"], query["closed"]);
        Assert.Equal(["en"], query["locale"]);
    }

    [Fact]
    public async Task GetEventsKeysetAsync_SerializesArrayAndCursorParameters()
    {
        HttpRequestMessage? capturedRequest = null;
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return CreateJsonResponse("""{"events":[{"id":"e1","title":"Election"}],"next_cursor":"abc"}""");
        }));

        await using GammaClient client = new("https://gamma-api.polymarket.com", httpClient);
        GammaEventKeysetQueryParameters parameters = new()
        {
            Limit = 25,
            Order = "volume",
            Ascending = true,
            AfterCursor = "abc",
            TagId = [1, 2],
            ExcludeTagId = [3],
            CreatedBy = ["creator-1"],
            IncludeTemplate = true,
            Locale = "en",
        };

        GammaKeysetEventsResponse response = await client.GetEventsKeysetAsync(parameters);

        Assert.Equal("abc", response.NextCursor);
        Assert.Single(response.Events);
        Assert.NotNull(capturedRequest);
        Assert.Equal("/events/keyset", capturedRequest!.RequestUri!.AbsolutePath);
        Dictionary<string, IReadOnlyList<string?>> query = ParseQueryValues(capturedRequest.RequestUri.Query);
        Assert.Equal(["25"], query["limit"]);
        Assert.Equal(["volume"], query["order"]);
        Assert.Equal(["true"], query["ascending"]);
        Assert.Equal(["abc"], query["after_cursor"]);
        Assert.Equal(["1", "2"], query["tag_id"]);
        Assert.Equal(["3"], query["exclude_tag_id"]);
        Assert.Equal(["creator-1"], query["created_by"]);
        Assert.Equal(["true"], query["include_template"]);
        Assert.Equal(["en"], query["locale"]);
    }

    [Fact]
    public async Task GetMarketsInformationAsync_SendsTypedRequestBody()
    {
        HttpRequestMessage? capturedRequest = null;
        string? requestBody = null;
        using HttpClient httpClient = new(new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return CreateJsonResponse("""[{"id":"1","question":"Will it rain?","conditionId":"c1","slug":"rain"}]""");
        }));

        await using GammaClient client = new("https://gamma-api.polymarket.com", httpClient);
        GammaMarketsInformationRequest request = new()
        {
            Id = [1, 2],
            ConditionIds = ["c1"],
            IncludeTags = true,
            RelatedTags = false,
        };

        IReadOnlyList<GammaMarket> markets = await client.GetMarketsInformationAsync(request);

        Assert.Single(markets);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("/markets/information", capturedRequest.RequestUri!.AbsolutePath);
        using JsonDocument document = JsonDocument.Parse(requestBody!);
        JsonElement root = document.RootElement;
        Assert.Equal(JsonValueKind.Array, root.GetProperty("id").ValueKind);
        Assert.Equal(2, root.GetProperty("id").GetArrayLength());
        Assert.Equal("c1", root.GetProperty("conditionIds")[0].GetString());
        Assert.False(root.GetProperty("relatedTags").GetBoolean());
        Assert.True(root.GetProperty("includeTags").GetBoolean());
    }

    [Fact]
    public async Task PublicSearchAsync_ReturnsStronglyTypedResponse()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(_ => CreateJsonResponse(
            """
            {
              "events":[{"id":"e1","title":"Election 2028","slug":"election-2028"}],
              "tags":[{"id":"1","label":"Politics","slug":"politics","event_count":12}],
              "profiles":[{"id":"p1","name":"Alice","proxyWallet":"0x1"}],
              "pagination":{"hasMore":false,"totalResults":3},
              "markets":[{"id":"m1","question":"Will X win?"}]
            }
            """)));

        await using GammaClient client = new("https://gamma-api.polymarket.com", httpClient);
        GammaSearchResponse response = await client.PublicSearchAsync(new GammaPublicSearchQueryParameters
        {
            Query = "election",
            SearchProfiles = true,
            SearchTags = true,
        });

        Assert.Single(response.Events);
        Assert.Equal("Election 2028", response.Events[0].Title);
        Assert.Single(response.Tags);
        Assert.Equal(12, response.Tags[0].EventCount);
        Assert.Single(response.Profiles);
        Assert.Equal("Alice", response.Profiles[0].Name);
        Assert.NotNull(response.Pagination);
        Assert.Equal(3, response.Pagination!.TotalResults);
        Assert.True(response.ExtensionData.ContainsKey("markets"));
    }

    [Fact]
    public async Task GetPublicProfileAsync_ThrowsGammaApiException_OnErrorResponse()
    {
        using HttpClient httpClient = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"type":"not found error","error":"profile not found"}""", Encoding.UTF8, "application/json"),
            }));

        await using GammaClient client = new("https://gamma-api.polymarket.com", httpClient);

        GammaApiException exception = await Assert.ThrowsAsync<GammaApiException>(() => client.GetPublicProfileAsync("0x7c3db723f1d4d8cb9c550095203b686cb11e5c6b"));
        Assert.Equal("profile not found", exception.Message);
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    private static HttpResponseMessage CreateJsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private static HttpResponseMessage CreateTextResponse(string payload) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "text/plain"),
        };

    private static Dictionary<string, IReadOnlyList<string?>> ParseQueryValues(string query)
    {
        Dictionary<string, List<string?>> result = new(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(query))
        {
            return result.ToDictionary(static pair => pair.Key, static pair => (IReadOnlyList<string?>)pair.Value, StringComparer.Ordinal);
        }

        foreach (string part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = part.Split('=', 2);
            string key = Uri.UnescapeDataString(pair[0]);
            string? value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : null;
            if (!result.TryGetValue(key, out List<string?>? values))
            {
                values = [];
                result[key] = values;
            }

            values.Add(value);
        }

        return result.ToDictionary(static pair => pair.Key, static pair => (IReadOnlyList<string?>)pair.Value, StringComparer.Ordinal);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
