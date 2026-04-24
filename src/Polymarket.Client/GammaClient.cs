using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Polymarket.Client.Internal;

namespace Polymarket.Client;

public sealed class GammaClient : IDisposable, IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;

    public GammaClient(HttpClient? httpClient = null)
        : this(new GammaClientOptions(), httpClient)
    {
    }

    public GammaClient(string host, HttpClient? httpClient = null)
        : this(new GammaClientOptions
        {
            Host = host,
        }, httpClient)
    {
    }

    public GammaClient(Uri host, HttpClient? httpClient = null)
        : this(new GammaClientOptions
        {
            Host = host.AbsoluteUri,
        }, httpClient)
    {
    }

    public GammaClient(GammaClientOptions options, HttpClient? httpClient = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        string normalizedHost = NormalizeHost(options.Host);
        Options = options with { Host = normalizedHost };

        _httpClient = httpClient ?? new HttpClient();
        _disposeHttpClient = httpClient is null;
        _httpClient.BaseAddress = new Uri(normalizedHost, UriKind.Absolute);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
    }

    public GammaClientOptions Options { get; private set; }

    public string Host => Options.Host;

    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default) =>
        SendAsync<string>(HttpMethod.Get, GammaEndpoints.Status, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<GammaTeam>> GetTeamsAsync(GammaTeamQueryParameters? queryParameters = null, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaTeam>>(HttpMethod.Get, GammaEndpoints.Teams, query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);

    public Task<GammaTeam> GetTeamAsync(long id, CancellationToken cancellationToken = default) =>
        SendAsync<GammaTeam>(HttpMethod.Get, $"{GammaEndpoints.Teams}/{id}", cancellationToken: cancellationToken);

    public Task<IReadOnlyList<GammaTag>> GetTagsAsync(GammaTagQueryParameters? queryParameters = null, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaTag>>(HttpMethod.Get, GammaEndpoints.Tags, query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);

    public Task<GammaTag> GetTagAsync(long id, bool? includeTemplate = null, CancellationToken cancellationToken = default) =>
        SendAsync<GammaTag>(
            HttpMethod.Get,
            $"{GammaEndpoints.Tags}/{id}",
            query: GammaQuery.FromOptional(("include_template", includeTemplate)),
            cancellationToken: cancellationToken);

    public Task<GammaTag> GetTagBySlugAsync(string slug, bool? includeTemplate = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        return SendAsync<GammaTag>(
            HttpMethod.Get,
            $"{GammaEndpoints.Tags}/slug/{Uri.EscapeDataString(slug)}",
            query: GammaQuery.FromOptional(("include_template", includeTemplate)),
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<GammaRelatedTag>> GetRelatedTagRelationshipsAsync(long tagId, GammaRelatedTagQueryParameters? queryParameters = null, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaRelatedTag>>(HttpMethod.Get, $"{GammaEndpoints.Tags}/{tagId}/related-tags", query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);

    public Task<IReadOnlyList<GammaRelatedTag>> GetRelatedTagRelationshipsBySlugAsync(string slug, GammaRelatedTagQueryParameters? queryParameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        return SendAsync<IReadOnlyList<GammaRelatedTag>>(HttpMethod.Get, $"{GammaEndpoints.Tags}/slug/{Uri.EscapeDataString(slug)}/related-tags", query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<GammaTag>> GetRelatedTagsForTagAsync(long tagId, GammaRelatedTagQueryParameters? queryParameters = null, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaTag>>(HttpMethod.Get, $"{GammaEndpoints.Tags}/{tagId}/related-tags/tags", query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);

    public Task<IReadOnlyList<GammaTag>> GetRelatedTagsForTagBySlugAsync(string slug, GammaRelatedTagQueryParameters? queryParameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        return SendAsync<IReadOnlyList<GammaTag>>(HttpMethod.Get, $"{GammaEndpoints.Tags}/slug/{Uri.EscapeDataString(slug)}/related-tags/tags", query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<GammaEvent>> GetEventsAsync(GammaEventQueryParameters? queryParameters = null, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaEvent>>(HttpMethod.Get, GammaEndpoints.Events, query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);

    public Task<GammaEventsPaginationResponse> GetEventsPaginationAsync(GammaEventPaginationQueryParameters? queryParameters = null, CancellationToken cancellationToken = default) =>
        SendAsync<GammaEventsPaginationResponse>(HttpMethod.Get, $"{GammaEndpoints.Events}/pagination", query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);

    public Task<IReadOnlyList<GammaEvent>> GetEventResultsAsync(GammaPaginationQueryParameters? queryParameters = null, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaEvent>>(HttpMethod.Get, $"{GammaEndpoints.Events}/results", query: GammaQuery.FromOptional(
            ("limit", queryParameters?.Limit),
            ("offset", queryParameters?.Offset),
            ("order", queryParameters?.Order),
            ("ascending", queryParameters?.Ascending)), cancellationToken: cancellationToken);

    public Task<GammaEvent> GetEventAsync(long id, bool? includeChat = null, bool? includeTemplate = null, CancellationToken cancellationToken = default) =>
        SendAsync<GammaEvent>(
            HttpMethod.Get,
            $"{GammaEndpoints.Events}/{id}",
            query: GammaQuery.FromOptional(("include_chat", includeChat), ("include_template", includeTemplate)),
            cancellationToken: cancellationToken);

    public Task<GammaEventTweetCount> GetEventTweetCountAsync(long id, CancellationToken cancellationToken = default) =>
        SendAsync<GammaEventTweetCount>(HttpMethod.Get, $"{GammaEndpoints.Events}/{id}/tweet-count", cancellationToken: cancellationToken);

    public Task<GammaCount> GetEventCommentsCountAsync(long id, CancellationToken cancellationToken = default) =>
        SendAsync<GammaCount>(HttpMethod.Get, $"{GammaEndpoints.Events}/{id}/comments/count", cancellationToken: cancellationToken);

    public Task<IReadOnlyList<GammaTag>> GetEventTagsAsync(long id, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaTag>>(HttpMethod.Get, $"{GammaEndpoints.Events}/{id}/tags", cancellationToken: cancellationToken);

    public Task<GammaEvent> GetEventBySlugAsync(string slug, bool? includeChat = null, bool? includeTemplate = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        return SendAsync<GammaEvent>(
            HttpMethod.Get,
            $"{GammaEndpoints.Events}/slug/{Uri.EscapeDataString(slug)}",
            query: GammaQuery.FromOptional(("include_chat", includeChat), ("include_template", includeTemplate)),
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<GammaEventCreator>> GetEventCreatorsAsync(GammaEventCreatorQueryParameters? queryParameters = null, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaEventCreator>>(HttpMethod.Get, $"{GammaEndpoints.Events}/creators", query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);

    public Task<GammaEventCreator> GetEventCreatorAsync(long id, CancellationToken cancellationToken = default) =>
        SendAsync<GammaEventCreator>(HttpMethod.Get, $"{GammaEndpoints.Events}/creators/{id}", cancellationToken: cancellationToken);

    public Task<IReadOnlyList<GammaMarket>> GetMarketsAsync(GammaMarketQueryParameters? queryParameters = null, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaMarket>>(HttpMethod.Get, GammaEndpoints.Markets, query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);

    public Task<GammaMarket> GetMarketAsync(long id, bool? includeTag = null, CancellationToken cancellationToken = default) =>
        SendAsync<GammaMarket>(
            HttpMethod.Get,
            $"{GammaEndpoints.Markets}/{id}",
            query: GammaQuery.FromOptional(("include_tag", includeTag)),
            cancellationToken: cancellationToken);

    public Task<GammaMarketDescription> GetMarketDescriptionAsync(long id, CancellationToken cancellationToken = default) =>
        SendAsync<GammaMarketDescription>(HttpMethod.Get, $"{GammaEndpoints.Markets}/{id}/description", cancellationToken: cancellationToken);

    public Task<IReadOnlyList<GammaTag>> GetMarketTagsAsync(long id, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaTag>>(HttpMethod.Get, $"{GammaEndpoints.Markets}/{id}/tags", cancellationToken: cancellationToken);

    public Task<GammaMarket> GetMarketBySlugAsync(string slug, bool? includeTag = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        return SendAsync<GammaMarket>(
            HttpMethod.Get,
            $"{GammaEndpoints.Markets}/slug/{Uri.EscapeDataString(slug)}",
            query: GammaQuery.FromOptional(("include_tag", includeTag)),
            cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<GammaMarket>> GetMarketsInformationAsync(GammaMarketsInformationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync<IReadOnlyList<GammaMarket>>(HttpMethod.Post, $"{GammaEndpoints.Markets}/information", body: request, cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<GammaMarket>> GetAbridgedMarketsAsync(GammaMarketsInformationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SendAsync<IReadOnlyList<GammaMarket>>(HttpMethod.Post, $"{GammaEndpoints.Markets}/abridged", body: request, cancellationToken: cancellationToken);
    }

    public Task<GammaKeysetMarketsResponse> GetMarketsKeysetAsync(GammaMarketKeysetQueryParameters? queryParameters = null, CancellationToken cancellationToken = default) =>
        SendAsync<GammaKeysetMarketsResponse>(HttpMethod.Get, $"{GammaEndpoints.Markets}/keyset", query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);

    public Task<GammaKeysetEventsResponse> GetEventsKeysetAsync(GammaEventKeysetQueryParameters? queryParameters = null, CancellationToken cancellationToken = default) =>
        SendAsync<GammaKeysetEventsResponse>(HttpMethod.Get, $"{GammaEndpoints.Events}/keyset", query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);

    public Task<IReadOnlyList<GammaSeries>> GetSeriesAsync(GammaSeriesQueryParameters? queryParameters = null, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaSeries>>(HttpMethod.Get, GammaEndpoints.Series, query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);

    public Task<GammaSeries> GetSeriesAsync(long id, bool? includeChat = null, CancellationToken cancellationToken = default) =>
        SendAsync<GammaSeries>(
            HttpMethod.Get,
            $"{GammaEndpoints.Series}/{id}",
            query: GammaQuery.FromOptional(("include_chat", includeChat)),
            cancellationToken: cancellationToken);

    public Task<GammaCount> GetSeriesCommentsCountAsync(long id, CancellationToken cancellationToken = default) =>
        SendAsync<GammaCount>(HttpMethod.Get, $"{GammaEndpoints.Series}/{id}/comments/count", cancellationToken: cancellationToken);

    public Task<GammaSeriesSummary> GetSeriesSummaryAsync(long id, CancellationToken cancellationToken = default) =>
        SendAsync<GammaSeriesSummary>(HttpMethod.Get, $"{GammaEndpoints.SeriesSummary}/{id}", cancellationToken: cancellationToken);

    public Task<GammaSeriesSummary> GetSeriesSummaryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        return SendAsync<GammaSeriesSummary>(HttpMethod.Get, $"{GammaEndpoints.SeriesSummary}/slug/{Uri.EscapeDataString(slug)}", cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<GammaComment>> GetCommentsAsync(GammaCommentQueryParameters? queryParameters = null, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaComment>>(HttpMethod.Get, GammaEndpoints.Comments, query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);

    public Task<IReadOnlyList<GammaComment>> GetCommentsByIdAsync(long id, bool? getPositions = null, CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaComment>>(
            HttpMethod.Get,
            $"{GammaEndpoints.Comments}/{id}",
            query: GammaQuery.FromOptional(("get_positions", getPositions)),
            cancellationToken: cancellationToken);

    public Task<IReadOnlyList<GammaComment>> GetCommentsByUserAddressAsync(string userAddress, GammaCommentsByUserQueryParameters? queryParameters = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userAddress);
        return SendAsync<IReadOnlyList<GammaComment>>(HttpMethod.Get, $"{GammaEndpoints.Comments}/user_address/{Uri.EscapeDataString(userAddress)}", query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);
    }

    public Task<GammaPublicProfileResponse> GetPublicProfileAsync(string address, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        return SendAsync<GammaPublicProfileResponse>(HttpMethod.Get, GammaEndpoints.PublicProfile, query: GammaQuery.FromOptional(("address", address)), cancellationToken: cancellationToken);
    }

    public Task<GammaProfile> GetProfileByUserAddressAsync(string userAddress, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userAddress);
        return SendAsync<GammaProfile>(HttpMethod.Get, $"{GammaEndpoints.Profiles}/user_address/{Uri.EscapeDataString(userAddress)}", cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<GammaSportsMetadata>> GetSportsMetadataAsync(CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<GammaSportsMetadata>>(HttpMethod.Get, GammaEndpoints.Sports, cancellationToken: cancellationToken);

    public Task<GammaSportsMarketTypesResponse> GetSportsMarketTypesAsync(CancellationToken cancellationToken = default) =>
        SendAsync<GammaSportsMarketTypesResponse>(HttpMethod.Get, $"{GammaEndpoints.Sports}/market-types", cancellationToken: cancellationToken);

    public Task<GammaSearchResponse> PublicSearchAsync(GammaPublicSearchQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryParameters.Query);
        return SendAsync<GammaSearchResponse>(HttpMethod.Get, GammaEndpoints.PublicSearch, query: GammaQuery.From(queryParameters), cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string endpoint, object? body = null, IReadOnlyList<KeyValuePair<string, string?>>? query = null, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(method, GammaQuery.Append(endpoint, query));
        if (body is not null)
        {
            request.Content = PolymarketJson.CreateJsonContent(body);
        }

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            ThrowHttpError(response.StatusCode, payload);
        }

        if (typeof(T) == typeof(string))
        {
            return (T)(object)payload;
        }

        if (typeof(T) == typeof(object) || string.IsNullOrWhiteSpace(payload))
        {
            return default!;
        }

        return PolymarketJson.Deserialize<T>(payload);
    }

    private static string NormalizeHost(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        return host.TrimEnd('/') + "/";
    }

    private static void ThrowHttpError(HttpStatusCode statusCode, string responseBody)
    {
        string message = $"Polymarket Gamma API request failed with status code {(int)statusCode}.";

        try
        {
            GammaErrorResponseBody? error = PolymarketJson.Deserialize<GammaErrorResponseBody>(responseBody);
            if (!string.IsNullOrWhiteSpace(error.Error))
            {
                message = error.Error;
            }
        }
        catch
        {
        }

        throw new GammaApiException(message, statusCode, responseBody);
    }

    private sealed record GammaErrorResponseBody
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }
}
