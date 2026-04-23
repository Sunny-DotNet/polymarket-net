using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Polymarket.Client.Internal;

namespace Polymarket.Client;

public sealed class ClobClient : IDisposable, IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;
    private readonly PolymarketSigner? _signer;
    private readonly Dictionary<string, string> _tickSizes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _negRisk = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _feeRates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FeeInfo> _feeInfos = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BuilderFeeRate> _builderFeeRates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _tokenConditionMap = new(StringComparer.Ordinal);
    private ApiCredentials? _credentials;
    private int? _cachedVersion;

    public ClobClient(string host, Chain chain, HttpClient? httpClient = null)
        : this(new ClobClientOptions
        {
            Host = new Uri(host, UriKind.Absolute),
            Chain = chain,
        }, httpClient)
    {
    }

    public ClobClient(Uri host, Chain chain, HttpClient? httpClient = null)
        : this(new ClobClientOptions
        {
            Host = host,
            Chain = chain,
        }, httpClient)
    {
    }

    public ClobClient(ClobClientOptions options, HttpClient? httpClient = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        Uri normalizedHost = NormalizeHost(options.Host);

        Options = options with
        {
            Host = normalizedHost,
        };

        _credentials = options.Credentials;
        _signer = string.IsNullOrWhiteSpace(options.PrivateKey) ? null : new PolymarketSigner(options.PrivateKey);
        _httpClient = httpClient ?? new HttpClient();
        _disposeHttpClient = httpClient is null;
        _httpClient.BaseAddress = normalizedHost;
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public ClobClientOptions Options { get; private set; }

    public Uri Host => Options.Host;

    public Chain Chain => Options.Chain;

    public ContractConfig ContractConfig => ContractRegistry.Get(Chain);

    public ApiCredentials? Credentials => _credentials;

    public ClientMode Mode => _signer is null
        ? ClientMode.L0
        : _credentials is null
            ? ClientMode.L1
            : ClientMode.L2;

    public void SetApiCredentials(ApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        _credentials = credentials;
        Options = Options with { Credentials = credentials };
    }

    public Task<string> GetOkAsync(CancellationToken cancellationToken = default) =>
        SendAsync<string>(HttpMethod.Get, ClobEndpoints.Ok, cancellationToken: cancellationToken);

    public async Task<HeartbeatResponse> PostHeartbeatAsync(string heartbeatId = "", CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();

        var payload = new { heartbeat_id = heartbeatId };
        string serializedBody = PolymarketJson.Serialize(payload);
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Post, ClobEndpoints.Heartbeat, serializedBody, cancellationToken).ConfigureAwait(false);
        return await SendAsync<HeartbeatResponse>(HttpMethod.Post, ClobEndpoints.Heartbeat, payload, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        VersionResponse? response = await SendAsync<VersionResponse?>(HttpMethod.Get, ClobEndpoints.Version, cancellationToken: cancellationToken).ConfigureAwait(false);
        _cachedVersion = response?.Version ?? 2;
        return _cachedVersion.Value;
    }

    public async Task<long> GetServerTimeAsync(CancellationToken cancellationToken = default)
    {
        string payload = await SendAsync<string>(HttpMethod.Get, ClobEndpoints.Time, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (long.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out long numeric))
        {
            return numeric;
        }

        ServerTimeResponse? parsed = PolymarketJson.Deserialize<ServerTimeResponse>(payload);
        if (parsed.Time is long time)
        {
            return time;
        }

        if (parsed.Timestamp is long timestamp)
        {
            return timestamp;
        }

        throw new InvalidOperationException("The server time payload could not be parsed.");
    }

    public Task<PaginationPayload<SimplifiedMarket>> GetSamplingSimplifiedMarketsAsync(string nextCursor = PolymarketConstants.InitialCursor, CancellationToken cancellationToken = default) =>
        SendAsync<PaginationPayload<SimplifiedMarket>>(HttpMethod.Get, ClobEndpoints.GetSamplingSimplifiedMarkets, query: new Dictionary<string, string?> { ["next_cursor"] = nextCursor }, cancellationToken: cancellationToken);

    public Task<PaginationPayload<Market>> GetSamplingMarketsAsync(string nextCursor = PolymarketConstants.InitialCursor, CancellationToken cancellationToken = default) =>
        SendAsync<PaginationPayload<Market>>(HttpMethod.Get, ClobEndpoints.GetSamplingMarkets, query: new Dictionary<string, string?> { ["next_cursor"] = nextCursor }, cancellationToken: cancellationToken);

    public Task<PaginationPayload<SimplifiedMarket>> GetSimplifiedMarketsAsync(string nextCursor = PolymarketConstants.InitialCursor, CancellationToken cancellationToken = default) =>
        SendAsync<PaginationPayload<SimplifiedMarket>>(HttpMethod.Get, ClobEndpoints.GetSimplifiedMarkets, query: new Dictionary<string, string?> { ["next_cursor"] = nextCursor }, cancellationToken: cancellationToken);

    public Task<PaginationPayload<Market>> GetMarketsAsync(string nextCursor = PolymarketConstants.InitialCursor, CancellationToken cancellationToken = default) =>
        SendAsync<PaginationPayload<Market>>(HttpMethod.Get, ClobEndpoints.GetMarkets, query: new Dictionary<string, string?> { ["next_cursor"] = nextCursor }, cancellationToken: cancellationToken);

    public Task<Market> GetMarketAsync(string conditionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionId);
        return SendAsync<Market>(HttpMethod.Get, $"{ClobEndpoints.GetMarket}{conditionId}", cancellationToken: cancellationToken);
    }

    public Task<MarketByTokenResponse> GetMarketByTokenAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenId);
        return SendAsync<MarketByTokenResponse>(HttpMethod.Get, $"{ClobEndpoints.GetMarketByToken}{tokenId}", cancellationToken: cancellationToken);
    }

    public async Task<MarketDetails> GetClobMarketInfoAsync(string conditionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionId);
        MarketDetails result = await SendAsync<MarketDetails>(HttpMethod.Get, $"{ClobEndpoints.GetClobMarket}{conditionId}", cancellationToken: cancellationToken).ConfigureAwait(false);
        if (result.Tokens.Count == 0)
        {
            throw new InvalidOperationException($"Failed to fetch market info for condition id {conditionId}.");
        }

        foreach (ClobToken? token in result.Tokens)
        {
            if (token is null)
            {
                continue;
            }

            _tokenConditionMap[token.TokenId] = conditionId;
            _tickSizes[token.TokenId] = result.MinimumTickSize.ToString(CultureInfo.InvariantCulture);
            _negRisk[token.TokenId] = result.NegRisk;
            _feeInfos[token.TokenId] = new FeeInfo(result.FeeDetails?.Rate ?? 0, result.FeeDetails?.Exponent ?? 0);
        }

        return result;
    }

    public Task<OrderBookSummary> GetOrderBookAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenId);
        return SendAsync<OrderBookSummary>(HttpMethod.Get, ClobEndpoints.GetOrderBook, query: new Dictionary<string, string?> { ["token_id"] = tokenId }, cancellationToken: cancellationToken);
    }

    public Task<IReadOnlyList<OrderBookSummary>> GetOrderBooksAsync(IReadOnlyList<BookParameters> parameters, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        IReadOnlyList<object> payload = parameters.Select(static parameter => new Dictionary<string, object?>
        {
            ["token_id"] = parameter.TokenId,
            ["side"] = parameter.Side?.ToApiString(),
        }).Cast<object>().ToArray();
        return SendAsync<IReadOnlyList<OrderBookSummary>>(HttpMethod.Post, ClobEndpoints.GetOrderBooks, payload, cancellationToken: cancellationToken);
    }

    public async Task<string> GetTickSizeAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        if (_tickSizes.TryGetValue(tokenId, out string? cached))
        {
            return cached;
        }

        if (_tokenConditionMap.TryGetValue(tokenId, out string? conditionId))
        {
            await GetClobMarketInfoAsync(conditionId, cancellationToken).ConfigureAwait(false);
            return _tickSizes[tokenId];
        }

        TickSizeResponse response = await SendAsync<TickSizeResponse>(HttpMethod.Get, ClobEndpoints.GetTickSize, query: new Dictionary<string, string?> { ["token_id"] = tokenId }, cancellationToken: cancellationToken).ConfigureAwait(false);
        string tickSize = response.MinimumTickSize.ToString(CultureInfo.InvariantCulture);
        _tickSizes[tokenId] = tickSize;
        return tickSize;
    }

    public async Task<bool> GetNegRiskAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        if (_negRisk.TryGetValue(tokenId, out bool cached))
        {
            return cached;
        }

        if (_tokenConditionMap.TryGetValue(tokenId, out string? conditionId))
        {
            await GetClobMarketInfoAsync(conditionId, cancellationToken).ConfigureAwait(false);
            return _negRisk[tokenId];
        }

        NegRiskResponse response = await SendAsync<NegRiskResponse>(HttpMethod.Get, ClobEndpoints.GetNegRisk, query: new Dictionary<string, string?> { ["token_id"] = tokenId }, cancellationToken: cancellationToken).ConfigureAwait(false);
        _negRisk[tokenId] = response.NegRisk;
        return response.NegRisk;
    }

    public async Task<int> GetFeeRateBpsAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        if (_feeRates.TryGetValue(tokenId, out int cached))
        {
            return cached;
        }

        FeeRateResponse response = await SendAsync<FeeRateResponse>(HttpMethod.Get, ClobEndpoints.GetFeeRate, query: new Dictionary<string, string?> { ["token_id"] = tokenId }, cancellationToken: cancellationToken).ConfigureAwait(false);
        _feeRates[tokenId] = response.BaseFee;
        return response.BaseFee;
    }

    public async Task<int> GetFeeExponentAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        if (_feeInfos.TryGetValue(tokenId, out FeeInfo? feeInfo))
        {
            return feeInfo.Exponent;
        }

        await EnsureMarketInfoCachedAsync(tokenId, cancellationToken).ConfigureAwait(false);
        return _feeInfos[tokenId].Exponent;
    }

    public string GetOrderBookHash(OrderBookSummary orderBook)
    {
        ArgumentNullException.ThrowIfNull(orderBook);
        return PolymarketMath.GenerateOrderBookHash(orderBook);
    }

    public Task<JsonElement> GetMidpointAsync(string tokenId, CancellationToken cancellationToken = default) =>
        SendAsync<JsonElement>(HttpMethod.Get, ClobEndpoints.GetMidpoint, query: new Dictionary<string, string?> { ["token_id"] = tokenId }, cancellationToken: cancellationToken);

    public Task<JsonElement> GetMidpointsAsync(IReadOnlyList<BookParameters> parameters, CancellationToken cancellationToken = default) =>
        SendAsync<JsonElement>(HttpMethod.Post, ClobEndpoints.GetMidpoints, BuildBookParametersPayload(parameters), cancellationToken: cancellationToken);

    public Task<JsonElement> GetPriceAsync(string tokenId, Side side, CancellationToken cancellationToken = default) =>
        SendAsync<JsonElement>(HttpMethod.Get, ClobEndpoints.GetPrice, query: new Dictionary<string, string?> { ["token_id"] = tokenId, ["side"] = side.ToApiString() }, cancellationToken: cancellationToken);

    public Task<JsonElement> GetPricesAsync(IReadOnlyList<BookParameters> parameters, CancellationToken cancellationToken = default) =>
        SendAsync<JsonElement>(HttpMethod.Post, ClobEndpoints.GetPrices, BuildBookParametersPayload(parameters), cancellationToken: cancellationToken);

    public Task<JsonElement> GetSpreadAsync(string tokenId, CancellationToken cancellationToken = default) =>
        SendAsync<JsonElement>(HttpMethod.Get, ClobEndpoints.GetSpread, query: new Dictionary<string, string?> { ["token_id"] = tokenId }, cancellationToken: cancellationToken);

    public Task<JsonElement> GetSpreadsAsync(IReadOnlyList<BookParameters> parameters, CancellationToken cancellationToken = default) =>
        SendAsync<JsonElement>(HttpMethod.Post, ClobEndpoints.GetSpreads, BuildBookParametersPayload(parameters), cancellationToken: cancellationToken);

    public Task<JsonElement> GetLastTradePriceAsync(string tokenId, CancellationToken cancellationToken = default) =>
        SendAsync<JsonElement>(HttpMethod.Get, ClobEndpoints.GetLastTradePrice, query: new Dictionary<string, string?> { ["token_id"] = tokenId }, cancellationToken: cancellationToken);

    public Task<JsonElement> GetLastTradesPricesAsync(IReadOnlyList<BookParameters> parameters, CancellationToken cancellationToken = default) =>
        SendAsync<JsonElement>(HttpMethod.Post, ClobEndpoints.GetLastTradesPrices, BuildBookParametersPayload(parameters), cancellationToken: cancellationToken);

    public Task<IReadOnlyList<MarketPricePoint>> GetPricesHistoryAsync(PriceHistoryParameters parameters, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (parameters.Interval is null && (parameters.StartTimestamp is null || parameters.EndTimestamp is null))
        {
            throw new ArgumentException("GetPricesHistoryAsync requires either Interval or both StartTimestamp and EndTimestamp.", nameof(parameters));
        }

        return SendAsync<IReadOnlyList<MarketPricePoint>>(HttpMethod.Get, ClobEndpoints.GetPricesHistory, query: PolymarketQuery.FromPriceHistoryParameters(parameters), cancellationToken: cancellationToken);
    }

    public async Task<ApiCredentials> CreateApiKeyAsync(long? nonce = null, CancellationToken cancellationToken = default)
    {
        EnsureL1Auth();
        Dictionary<string, string> headers = await CreateLevel1HeadersAsync(nonce, cancellationToken).ConfigureAwait(false);
        ApiCredentialsRaw raw = await SendAsync<ApiCredentialsRaw>(HttpMethod.Post, ClobEndpoints.CreateApiKey, headers: headers, cancellationToken: cancellationToken).ConfigureAwait(false);
        ApiCredentials apiCredentials = new(raw.ApiKey, raw.Secret, raw.Passphrase);
        SetApiCredentials(apiCredentials);
        return apiCredentials;
    }

    public async Task<ApiCredentials> DeriveApiKeyAsync(long? nonce = null, CancellationToken cancellationToken = default)
    {
        EnsureL1Auth();
        Dictionary<string, string> headers = await CreateLevel1HeadersAsync(nonce, cancellationToken).ConfigureAwait(false);
        ApiCredentialsRaw raw = await SendAsync<ApiCredentialsRaw>(HttpMethod.Get, ClobEndpoints.DeriveApiKey, headers: headers, cancellationToken: cancellationToken).ConfigureAwait(false);
        ApiCredentials apiCredentials = new(raw.ApiKey, raw.Secret, raw.Passphrase);
        SetApiCredentials(apiCredentials);
        return apiCredentials;
    }

    public async Task<ApiCredentials> CreateOrDeriveApiKeyAsync(long? nonce = null, CancellationToken cancellationToken = default)
    {
        ApiCredentials created = await CreateApiKeyAsync(nonce, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(created.Key)
            ? await DeriveApiKeyAsync(nonce, cancellationToken).ConfigureAwait(false)
            : created;
    }

    public async Task<ApiKeysResponse> GetApiKeysAsync(CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Get, ClobEndpoints.GetApiKeys, null, cancellationToken).ConfigureAwait(false);
        return await SendAsync<ApiKeysResponse>(HttpMethod.Get, ClobEndpoints.GetApiKeys, headers: headers, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<BanStatus> GetClosedOnlyModeAsync(CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Get, ClobEndpoints.ClosedOnly, null, cancellationToken).ConfigureAwait(false);
        return await SendAsync<BanStatus>(HttpMethod.Get, ClobEndpoints.ClosedOnly, headers: headers, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<DynamicPayload> DeleteApiKeyAsync(CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Delete, ClobEndpoints.DeleteApiKey, null, cancellationToken).ConfigureAwait(false);
        return await SendAsync<DynamicPayload>(HttpMethod.Delete, ClobEndpoints.DeleteApiKey, headers: headers, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReadonlyApiKeyResponse> CreateReadonlyApiKeyAsync(long? nonce = null, CancellationToken cancellationToken = default)
    {
        EnsureL1Auth();
        Dictionary<string, string> headers = await CreateLevel1HeadersAsync(nonce, cancellationToken).ConfigureAwait(false);
        return await SendAsync<ReadonlyApiKeyResponse>(HttpMethod.Post, ClobEndpoints.CreateReadonlyApiKey, headers: headers, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ReadonlyApiKeyResponse>> GetReadonlyApiKeysAsync(CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Get, ClobEndpoints.GetReadonlyApiKeys, null, cancellationToken).ConfigureAwait(false);
        return await SendAsync<IReadOnlyList<ReadonlyApiKeyResponse>>(HttpMethod.Get, ClobEndpoints.GetReadonlyApiKeys, headers: headers, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<DynamicPayload> DeleteReadonlyApiKeyAsync(CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Delete, ClobEndpoints.DeleteReadonlyApiKey, null, cancellationToken).ConfigureAwait(false);
        return await SendAsync<DynamicPayload>(HttpMethod.Delete, ClobEndpoints.DeleteReadonlyApiKey, headers: headers, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<SignedOrderBase> CreateOrderAsync(OrderArgumentsV1 orderArguments, PartialCreateOrderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderArguments);
        EnsureL1Auth();

        string tickSize = await ResolveTickSizeAsync(orderArguments.TokenId, options?.TickSize, cancellationToken).ConfigureAwait(false);
        if (!PolymarketMath.PriceValid(orderArguments.Price, tickSize))
        {
            throw new InvalidOperationException($"Invalid price ({orderArguments.Price.ToString(CultureInfo.InvariantCulture)}), min: {tickSize} - max: {(1m - decimal.Parse(tickSize, CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture)}");
        }

        bool negRisk = options?.NegRisk ?? await GetNegRiskAsync(orderArguments.TokenId, cancellationToken).ConfigureAwait(false);
        int version = await ResolveVersionAsync(false, cancellationToken).ConfigureAwait(false);
        int feeRate = await ResolveFeeRateBpsAsync(orderArguments.TokenId, orderArguments.FeeRateBps, cancellationToken).ConfigureAwait(false);

        return BuildSignedOrder(orderArguments, tickSize, negRisk, version, feeRate);
    }

    public async Task<SignedOrderBase> CreateOrderAsync(OrderArgumentsV2 orderArguments, PartialCreateOrderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderArguments);
        EnsureL1Auth();

        string builderCode = orderArguments.BuilderCode ?? Options.BuilderConfig?.BuilderCode ?? PolymarketConstants.Bytes32Zero;
        string tickSize = await ResolveTickSizeAsync(orderArguments.TokenId, options?.TickSize, cancellationToken).ConfigureAwait(false);
        if (!PolymarketMath.PriceValid(orderArguments.Price, tickSize))
        {
            throw new InvalidOperationException($"Invalid price ({orderArguments.Price.ToString(CultureInfo.InvariantCulture)}), min: {tickSize} - max: {(1m - decimal.Parse(tickSize, CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture)}");
        }

        bool negRisk = options?.NegRisk ?? await GetNegRiskAsync(orderArguments.TokenId, cancellationToken).ConfigureAwait(false);
        int version = await ResolveVersionAsync(false, cancellationToken).ConfigureAwait(false);
        int feeRate = version == 1
            ? await ResolveFeeRateBpsAsync(orderArguments.TokenId, null, cancellationToken).ConfigureAwait(false)
            : 0;

        OrderArgumentsV1 fallbackV1 = new(orderArguments.TokenId, orderArguments.Price, orderArguments.Size, orderArguments.Side, feeRate, 0, orderArguments.Expiration, PolymarketConstants.ZeroAddress, builderCode);
        return BuildSignedOrder(version == 1 ? fallbackV1 : orderArguments with { BuilderCode = builderCode }, tickSize, negRisk, version, feeRate);
    }

    public async Task<SignedOrderBase> CreateMarketOrderAsync(MarketOrderArgumentsV1 marketOrderArguments, PartialCreateOrderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(marketOrderArguments);
        EnsureL1Auth();

        await EnsureMarketInfoCachedAsync(marketOrderArguments.TokenId, cancellationToken).ConfigureAwait(false);
        decimal price = marketOrderArguments.Price ?? await CalculateMarketPriceAsync(marketOrderArguments.TokenId, marketOrderArguments.Side, marketOrderArguments.Amount, marketOrderArguments.OrderType ?? OrderType.Fok, cancellationToken).ConfigureAwait(false);
        string tickSize = await ResolveTickSizeAsync(marketOrderArguments.TokenId, options?.TickSize, cancellationToken).ConfigureAwait(false);
        if (!PolymarketMath.PriceValid(price, tickSize))
        {
            throw new InvalidOperationException($"Invalid price ({price.ToString(CultureInfo.InvariantCulture)}), min: {tickSize} - max: {(1m - decimal.Parse(tickSize, CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture)}");
        }

        bool negRisk = options?.NegRisk ?? await GetNegRiskAsync(marketOrderArguments.TokenId, cancellationToken).ConfigureAwait(false);
        int version = await ResolveVersionAsync(false, cancellationToken).ConfigureAwait(false);
        int feeRate = await ResolveFeeRateBpsAsync(marketOrderArguments.TokenId, marketOrderArguments.FeeRateBps, cancellationToken).ConfigureAwait(false);

        MarketOrderArgumentsV1 resolved = marketOrderArguments with { Price = price, FeeRateBps = feeRate };
        return BuildSignedMarketOrder(resolved, tickSize, negRisk, version, feeRate);
    }

    public async Task<SignedOrderBase> CreateMarketOrderAsync(MarketOrderArgumentsV2 marketOrderArguments, PartialCreateOrderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(marketOrderArguments);
        EnsureL1Auth();

        await EnsureMarketInfoCachedAsync(marketOrderArguments.TokenId, cancellationToken).ConfigureAwait(false);

        decimal price = marketOrderArguments.Price ?? await CalculateMarketPriceAsync(marketOrderArguments.TokenId, marketOrderArguments.Side, marketOrderArguments.Amount, marketOrderArguments.OrderType ?? OrderType.Fok, cancellationToken).ConfigureAwait(false);
        string tickSize = await ResolveTickSizeAsync(marketOrderArguments.TokenId, options?.TickSize, cancellationToken).ConfigureAwait(false);
        if (!PolymarketMath.PriceValid(price, tickSize))
        {
            throw new InvalidOperationException($"Invalid price ({price.ToString(CultureInfo.InvariantCulture)}), min: {tickSize} - max: {(1m - decimal.Parse(tickSize, CultureInfo.InvariantCulture)).ToString(CultureInfo.InvariantCulture)}");
        }

        string builderCode = marketOrderArguments.BuilderCode ?? Options.BuilderConfig?.BuilderCode ?? PolymarketConstants.Bytes32Zero;
        await EnsureBuilderFeeRateCachedAsync(builderCode, cancellationToken).ConfigureAwait(false);

        decimal amount = marketOrderArguments.Amount;
        if (marketOrderArguments.Side == Side.Buy && marketOrderArguments.UserUsdcBalance is decimal userUsdcBalance)
        {
            decimal builderTakerFeeRate = builderCode != PolymarketConstants.Bytes32Zero && _builderFeeRates.TryGetValue(builderCode, out BuilderFeeRate? builderRate)
                ? builderRate.Taker
                : 0m;
            FeeInfo feeInfo = _feeInfos[marketOrderArguments.TokenId];
            amount = PolymarketMath.AdjustBuyAmountForFees(amount, price, userUsdcBalance, feeInfo.Rate, feeInfo.Exponent, builderTakerFeeRate);
        }

        bool negRisk = options?.NegRisk ?? await GetNegRiskAsync(marketOrderArguments.TokenId, cancellationToken).ConfigureAwait(false);
        int version = await ResolveVersionAsync(false, cancellationToken).ConfigureAwait(false);
        int feeRate = version == 1
            ? await ResolveFeeRateBpsAsync(marketOrderArguments.TokenId, null, cancellationToken).ConfigureAwait(false)
            : 0;

        MarketOrderArgumentsV1 fallbackV1 = new(marketOrderArguments.TokenId, amount, marketOrderArguments.Side, price, feeRate, 0, PolymarketConstants.ZeroAddress, marketOrderArguments.OrderType, builderCode);
        MarketOrderArgumentsV2 resolvedV2 = marketOrderArguments with { Amount = amount, Price = price, BuilderCode = builderCode };
        return BuildSignedMarketOrder(version == 1 ? fallbackV1 : resolvedV2, tickSize, negRisk, version, feeRate);
    }

    public async Task<JsonElement> PostOrderAsync(SignedOrderBase order, OrderType orderType = OrderType.Gtc, bool postOnly = false, bool deferExecution = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        EnsureL2Auth();
        if (postOnly && (orderType == OrderType.Fok || orderType == OrderType.Fak))
        {
            throw new InvalidOperationException("postOnly is not supported for FOK/FAK orders.");
        }

        object payload = BuildOrderPayload(order, orderType, postOnly, deferExecution);
        string serializedBody = PolymarketJson.Serialize(payload);
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Post, ClobEndpoints.PostOrder, serializedBody, cancellationToken).ConfigureAwait(false);
        JsonElement response = await SendAsync<JsonElement>(HttpMethod.Post, ClobEndpoints.PostOrder, payload, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
        await RefreshVersionOnMismatchAsync(response, cancellationToken).ConfigureAwait(false);
        return HandleJsonResponse(response);
    }

    public async Task<JsonElement> PostOrdersAsync(IReadOnlyList<PostOrderArguments> arguments, bool postOnly = false, bool deferExecution = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        EnsureL2Auth();
        if (postOnly && arguments.Any(static x => x.OrderType is OrderType.Fok or OrderType.Fak))
        {
            throw new InvalidOperationException("postOnly is not supported for FOK/FAK orders.");
        }

        object[] payload = arguments.Select(arg => BuildOrderPayload(arg.Order, arg.OrderType, postOnly, deferExecution)).ToArray();
        string serializedBody = PolymarketJson.Serialize(payload);
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Post, ClobEndpoints.PostOrders, serializedBody, cancellationToken).ConfigureAwait(false);
        JsonElement response = await SendAsync<JsonElement>(HttpMethod.Post, ClobEndpoints.PostOrders, payload, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
        await RefreshVersionOnMismatchAsync(response, cancellationToken).ConfigureAwait(false);
        return HandleJsonResponse(response);
    }

    public async Task<JsonElement> CreateAndPostOrderAsync(OrderArgumentsV1 orderArguments, PartialCreateOrderOptions? options = null, OrderType orderType = OrderType.Gtc, bool postOnly = false, bool deferExecution = false, CancellationToken cancellationToken = default)
    {
        JsonElement response = default;
        await RetryOnVersionUpdateAsync(async () =>
        {
            SignedOrderBase order = await CreateOrderAsync(orderArguments, options, cancellationToken).ConfigureAwait(false);
            response = await PostOrderAsync(order, orderType, postOnly, deferExecution, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JsonElement> CreateAndPostOrderAsync(OrderArgumentsV2 orderArguments, PartialCreateOrderOptions? options = null, OrderType orderType = OrderType.Gtc, bool postOnly = false, bool deferExecution = false, CancellationToken cancellationToken = default)
    {
        JsonElement response = default;
        await RetryOnVersionUpdateAsync(async () =>
        {
            SignedOrderBase order = await CreateOrderAsync(orderArguments, options, cancellationToken).ConfigureAwait(false);
            response = await PostOrderAsync(order, orderType, postOnly, deferExecution, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JsonElement> CreateAndPostMarketOrderAsync(MarketOrderArgumentsV1 marketOrderArguments, PartialCreateOrderOptions? options = null, OrderType orderType = OrderType.Fok, bool deferExecution = false, CancellationToken cancellationToken = default)
    {
        JsonElement response = default;
        await RetryOnVersionUpdateAsync(async () =>
        {
            SignedOrderBase order = await CreateMarketOrderAsync(marketOrderArguments, options, cancellationToken).ConfigureAwait(false);
            response = await PostOrderAsync(order, orderType, false, deferExecution, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<JsonElement> CreateAndPostMarketOrderAsync(MarketOrderArgumentsV2 marketOrderArguments, PartialCreateOrderOptions? options = null, OrderType orderType = OrderType.Fok, bool deferExecution = false, CancellationToken cancellationToken = default)
    {
        JsonElement response = default;
        await RetryOnVersionUpdateAsync(async () =>
        {
            SignedOrderBase order = await CreateMarketOrderAsync(marketOrderArguments, options, cancellationToken).ConfigureAwait(false);
            response = await PostOrderAsync(order, orderType, false, deferExecution, cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<IReadOnlyList<OpenOrder>> GetOpenOrdersAsync(OpenOrderParameters? parameters = null, bool onlyFirstPage = false, string? nextCursor = null, CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Get, ClobEndpoints.GetOpenOrders, null, cancellationToken).ConfigureAwait(false);
        return await GetAllPagesAsync<OpenOrder>(ClobEndpoints.GetOpenOrders, PolymarketQuery.FromOpenOrderParameters(parameters), headers, onlyFirstPage, nextCursor, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OpenOrder>> GetPreMigrationOrdersAsync(bool onlyFirstPage = false, string? nextCursor = null, CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Get, ClobEndpoints.GetPreMigrationOrders, null, cancellationToken).ConfigureAwait(false);
        return await GetAllPagesAsync<OpenOrder>(ClobEndpoints.GetPreMigrationOrders, null, headers, onlyFirstPage, nextCursor, cancellationToken).ConfigureAwait(false);
    }

    public Task<DynamicPayload> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();
        ArgumentException.ThrowIfNullOrWhiteSpace(orderId);
        return SendAuthedDynamicAsync(HttpMethod.Get, $"{ClobEndpoints.GetOrder}{orderId}", null, null, cancellationToken);
    }

    public async Task<IReadOnlyList<Trade>> GetTradesAsync(TradeParameters? parameters = null, bool onlyFirstPage = false, string? nextCursor = null, CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Get, ClobEndpoints.GetTrades, null, cancellationToken).ConfigureAwait(false);
        return await GetAllPagesAsync<Trade>(ClobEndpoints.GetTrades, PolymarketQuery.FromTradeParameters(parameters), headers, onlyFirstPage, nextCursor, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TradesPaginatedResponse> GetTradesPaginatedAsync(TradeParameters? parameters = null, string? nextCursor = null, CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Get, ClobEndpoints.GetTrades, null, cancellationToken).ConfigureAwait(false);
        Dictionary<string, string?> query = PolymarketQuery.FromTradeParameters(parameters);
        query["next_cursor"] = nextCursor ?? PolymarketConstants.InitialCursor;
        PaginatedData<Trade> result = await SendAsync<PaginatedData<Trade>>(HttpMethod.Get, ClobEndpoints.GetTrades, query: query, headers: headers, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new TradesPaginatedResponse(result.Data, result.NextCursor, result.Limit, result.Count);
    }

    public async Task<BuilderTradesResponse> GetBuilderTradesAsync(BuilderTradeParameters parameters, string? nextCursor = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (string.IsNullOrWhiteSpace(parameters.BuilderCode) || parameters.BuilderCode == PolymarketConstants.Bytes32Zero)
        {
            throw new ArgumentException("BuilderCode is required and cannot be zero.", nameof(parameters));
        }

        Dictionary<string, string?> query = new()
        {
            ["builder_code"] = parameters.BuilderCode,
            ["id"] = parameters.Id,
            ["maker_address"] = parameters.MakerAddress,
            ["market"] = parameters.Market,
            ["asset_id"] = parameters.AssetId,
            ["before"] = parameters.Before,
            ["after"] = parameters.After,
            ["next_cursor"] = nextCursor ?? PolymarketConstants.InitialCursor,
        };

        PaginatedData<BuilderTrade> result = await SendAsync<PaginatedData<BuilderTrade>>(HttpMethod.Get, ClobEndpoints.GetBuilderTrades, query: query, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new BuilderTradesResponse(result.Data, result.NextCursor, result.Limit, result.Count);
    }

    public Task<IReadOnlyList<Notification>> GetNotificationsAsync(CancellationToken cancellationToken = default) =>
        SendAuthedAsync<IReadOnlyList<Notification>>(HttpMethod.Get, ClobEndpoints.GetNotifications, query: new Dictionary<string, string?> { ["signature_type"] = ((int)Options.SignatureType).ToString(CultureInfo.InvariantCulture) }, cancellationToken: cancellationToken);

    public Task DropNotificationsAsync(DropNotificationParameters? parameters = null, CancellationToken cancellationToken = default) =>
        SendAuthedAsync<object?>(HttpMethod.Delete, ClobEndpoints.DropNotifications, query: PolymarketQuery.FromDropNotifications(parameters), cancellationToken: cancellationToken);

    public Task<BalanceAllowanceResponse> GetBalanceAllowanceAsync(BalanceAllowanceParameters? parameters = null, CancellationToken cancellationToken = default) =>
        SendAuthedAsync<BalanceAllowanceResponse>(HttpMethod.Get, ClobEndpoints.GetBalanceAllowance, query: PolymarketQuery.FromBalanceAllowanceParameters(parameters, Options.SignatureType), cancellationToken: cancellationToken);

    public Task UpdateBalanceAllowanceAsync(BalanceAllowanceParameters? parameters = null, CancellationToken cancellationToken = default) =>
        SendAuthedAsync<object?>(HttpMethod.Get, ClobEndpoints.UpdateBalanceAllowance, query: PolymarketQuery.FromBalanceAllowanceParameters(parameters, Options.SignatureType), cancellationToken: cancellationToken);

    public Task<JsonElement> CancelOrderAsync(OrderPayload payload, CancellationToken cancellationToken = default) =>
        SendAuthedJsonAsync(HttpMethod.Delete, ClobEndpoints.CancelOrder, payload, cancellationToken);

    public Task<JsonElement> CancelOrdersAsync(IReadOnlyList<string> orderHashes, CancellationToken cancellationToken = default) =>
        SendAuthedJsonAsync(HttpMethod.Delete, ClobEndpoints.CancelOrders, orderHashes, cancellationToken);

    public Task<JsonElement> CancelAllAsync(CancellationToken cancellationToken = default) =>
        SendAuthedJsonAsync(HttpMethod.Delete, ClobEndpoints.CancelAll, null, cancellationToken);

    public Task<JsonElement> CancelMarketOrdersAsync(OrderMarketCancelParameters parameters, CancellationToken cancellationToken = default) =>
        SendAuthedJsonAsync(HttpMethod.Delete, ClobEndpoints.CancelMarketOrders, parameters, cancellationToken);

    public Task<OrderScoringResponse> IsOrderScoringAsync(OrderScoringParameters? parameters = null, CancellationToken cancellationToken = default) =>
        SendAuthedAsync<OrderScoringResponse>(HttpMethod.Get, ClobEndpoints.IsOrderScoring, query: parameters is null ? null : new Dictionary<string, string?> { ["order_id"] = parameters.OrderId }, cancellationToken: cancellationToken);

    public async Task<IReadOnlyDictionary<string, bool>> AreOrdersScoringAsync(OrdersScoringParameters? parameters = null, CancellationToken cancellationToken = default)
    {
        JsonElement response = await SendAuthedJsonAsync(HttpMethod.Post, ClobEndpoints.AreOrdersScoring, parameters?.OrderIds, cancellationToken).ConfigureAwait(false);
        Dictionary<string, bool> result = new(StringComparer.Ordinal);
        if (response.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in response.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.True || property.Value.ValueKind == JsonValueKind.False)
                {
                    result[property.Name] = property.Value.GetBoolean();
                }
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<UserEarning>> GetEarningsForUserForDayAsync(string date, CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Get, ClobEndpoints.GetEarningsForUserForDay, null, cancellationToken).ConfigureAwait(false);
        return await GetAllPagesAsync<UserEarning>(
            ClobEndpoints.GetEarningsForUserForDay,
            new Dictionary<string, string?> { ["date"] = date, ["signature_type"] = ((int)Options.SignatureType).ToString(CultureInfo.InvariantCulture) },
            headers,
            onlyFirstPage: false,
            nextCursor: null,
            cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<TotalUserEarning>> GetTotalEarningsForUserForDayAsync(string date, CancellationToken cancellationToken = default) =>
        SendAuthedAsync<IReadOnlyList<TotalUserEarning>>(HttpMethod.Get, ClobEndpoints.GetTotalEarningsForUserForDay, query: new Dictionary<string, string?> { ["date"] = date, ["signature_type"] = ((int)Options.SignatureType).ToString(CultureInfo.InvariantCulture) }, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<UserRewardsEarning>> GetUserEarningsAndMarketsConfigAsync(string date, string orderBy = "", string position = "", bool noCompetition = false, CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(HttpMethod.Get, ClobEndpoints.GetRewardsEarningsPercentages, null, cancellationToken).ConfigureAwait(false);
        return await GetAllPagesAsync<UserRewardsEarning>(
            ClobEndpoints.GetRewardsEarningsPercentages,
            new Dictionary<string, string?>
            {
                ["date"] = date,
                ["signature_type"] = ((int)Options.SignatureType).ToString(CultureInfo.InvariantCulture),
                ["order_by"] = orderBy,
                ["position"] = position,
                ["no_competition"] = noCompetition.ToString().ToLowerInvariant(),
            },
            headers,
            onlyFirstPage: false,
            nextCursor: null,
            cancellationToken).ConfigureAwait(false);
    }

    public Task<Dictionary<string, decimal>> GetRewardPercentagesAsync(CancellationToken cancellationToken = default) =>
        SendAuthedAsync<Dictionary<string, decimal>>(HttpMethod.Get, ClobEndpoints.GetLiquidityRewardPercentages, query: new Dictionary<string, string?> { ["signature_type"] = ((int)Options.SignatureType).ToString(CultureInfo.InvariantCulture) }, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<MarketReward>> GetCurrentRewardsAsync(CancellationToken cancellationToken = default) =>
        GetPublicPagedAsync<MarketReward>(ClobEndpoints.GetRewardsMarketsCurrent, cancellationToken);

    public Task<IReadOnlyList<MarketReward>> GetRawRewardsForMarketAsync(string conditionId, CancellationToken cancellationToken = default) =>
        GetPublicPagedAsync<MarketReward>($"{ClobEndpoints.GetRewardsMarkets}{conditionId}", cancellationToken);

    public async Task<decimal> CalculateMarketPriceAsync(string tokenId, Side side, decimal amount, OrderType orderType = OrderType.Fok, CancellationToken cancellationToken = default)
    {
        OrderBookSummary orderBook = await GetOrderBookAsync(tokenId, cancellationToken).ConfigureAwait(false);
        return side == Side.Buy
            ? PolymarketMath.CalculateBuyMarketPrice(orderBook.Asks, amount, orderType)
            : PolymarketMath.CalculateSellMarketPrice(orderBook.Bids, amount, orderType);
    }

    public Task<BuilderApiKey> CreateBuilderApiKeyAsync(CancellationToken cancellationToken = default) =>
        SendAuthedAsync<BuilderApiKey>(HttpMethod.Post, ClobEndpoints.CreateBuilderApiKey, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<BuilderApiKeyResponse>> GetBuilderApiKeysAsync(CancellationToken cancellationToken = default) =>
        SendAuthedAsync<IReadOnlyList<BuilderApiKeyResponse>>(HttpMethod.Get, ClobEndpoints.GetBuilderApiKeys, cancellationToken: cancellationToken);

    public Task<JsonElement> RevokeBuilderApiKeyAsync(CancellationToken cancellationToken = default) =>
        SendAuthedJsonAsync(HttpMethod.Delete, ClobEndpoints.RevokeBuilderApiKey, null, cancellationToken);

    public Task<IReadOnlyList<MarketTradeEvent>> GetMarketTradesEventsAsync(string conditionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionId);
        return SendAsync<IReadOnlyList<MarketTradeEvent>>(HttpMethod.Get, $"{ClobEndpoints.GetMarketTradesEvents}{conditionId}", cancellationToken: cancellationToken);
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
        Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task<T> SendAuthedAsync<T>(HttpMethod method, string endpoint, object? body = null, IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
    {
        EnsureL2Auth();
        string? serializedBody = body is null ? null : PolymarketJson.Serialize(body);
        Dictionary<string, string> headers = await CreateLevel2HeadersAsync(method, endpoint, serializedBody, cancellationToken).ConfigureAwait(false);
        return await SendAsync<T>(method, endpoint, body, headers, query, cancellationToken).ConfigureAwait(false);
    }

    private async Task<JsonElement> SendAuthedJsonAsync(HttpMethod method, string endpoint, object? body, CancellationToken cancellationToken)
    {
        JsonElement response = await SendAuthedAsync<JsonElement>(method, endpoint, body, cancellationToken: cancellationToken).ConfigureAwait(false);
        return HandleJsonResponse(response);
    }

    private Task<DynamicPayload> SendAuthedDynamicAsync(HttpMethod method, string endpoint, object? body = null, IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default) =>
        SendAuthedAsync<DynamicPayload>(method, endpoint, body, query, cancellationToken);

    private async Task<IReadOnlyList<T>> GetPublicPagedAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        List<T> results = [];
        string cursor = PolymarketConstants.InitialCursor;
        while (cursor != PolymarketConstants.EndCursor)
        {
            PaginatedData<T> page = await SendAsync<PaginatedData<T>>(HttpMethod.Get, endpoint, query: new Dictionary<string, string?> { ["next_cursor"] = cursor }, cancellationToken: cancellationToken).ConfigureAwait(false);
            results.AddRange(page.Data);
            cursor = page.NextCursor;
        }

        return results;
    }

    private async Task<IReadOnlyList<T>> GetAllPagesAsync<T>(string endpoint, Dictionary<string, string?>? baseQuery, IReadOnlyDictionary<string, string> headers, bool onlyFirstPage, string? nextCursor, CancellationToken cancellationToken)
    {
        List<T> results = [];
        string cursor = nextCursor ?? PolymarketConstants.InitialCursor;
        while (cursor != PolymarketConstants.EndCursor && (cursor == PolymarketConstants.InitialCursor || !onlyFirstPage))
        {
            Dictionary<string, string?> query = baseQuery is null ? [] : new Dictionary<string, string?>(baseQuery, StringComparer.Ordinal);
            query["next_cursor"] = cursor;
            PaginatedData<T> response = await SendAsync<PaginatedData<T>>(HttpMethod.Get, endpoint, query: query, headers: headers, cancellationToken: cancellationToken).ConfigureAwait(false);
            results.AddRange(response.Data);
            cursor = response.NextCursor;
        }

        return results;
    }

    private async Task RetryOnVersionUpdateAsync(Func<Task> callback, CancellationToken cancellationToken)
    {
        try
        {
            await callback().ConfigureAwait(false);
        }
        catch (ClobApiException ex) when (Options.RetryOnError && (ex.ResponseBody?.Contains(PolymarketConstants.OrderVersionMismatchError, StringComparison.OrdinalIgnoreCase) ?? false))
        {
            await ResolveVersionAsync(true, cancellationToken).ConfigureAwait(false);
            await callback().ConfigureAwait(false);
        }
    }

    private async Task<int> ResolveVersionAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && _cachedVersion.HasValue)
        {
            return _cachedVersion.Value;
        }

        return await GetVersionAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureMarketInfoCachedAsync(string tokenId, CancellationToken cancellationToken)
    {
        if (_feeInfos.ContainsKey(tokenId))
        {
            return;
        }

        if (!_tokenConditionMap.TryGetValue(tokenId, out string? conditionId))
        {
            MarketByTokenResponse response = await SendAsync<MarketByTokenResponse>(HttpMethod.Get, $"{ClobEndpoints.GetMarketByToken}{tokenId}", cancellationToken: cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(response.ConditionId))
            {
                throw new InvalidOperationException($"Failed to resolve condition id for token {tokenId}.");
            }

            conditionId = response.ConditionId;
            _tokenConditionMap[tokenId] = conditionId;
        }

        await GetClobMarketInfoAsync(conditionId, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureBuilderFeeRateCachedAsync(string? builderCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(builderCode) || builderCode == PolymarketConstants.Bytes32Zero || _builderFeeRates.ContainsKey(builderCode))
        {
            return;
        }

        BuilderFeeRateResponse response = await SendAsync<BuilderFeeRateResponse>(HttpMethod.Get, $"{ClobEndpoints.GetBuilderFees}{builderCode}", cancellationToken: cancellationToken).ConfigureAwait(false);
        _builderFeeRates[builderCode] = new BuilderFeeRate(
            response.BuilderMakerFeeRateBps / (decimal)PolymarketConstants.BuilderFeesBps,
            response.BuilderTakerFeeRateBps / (decimal)PolymarketConstants.BuilderFeesBps);
    }

    private async Task<int> ResolveFeeRateBpsAsync(string tokenId, int? requested, CancellationToken cancellationToken) =>
        requested ?? await GetFeeRateBpsAsync(tokenId, cancellationToken).ConfigureAwait(false);

    private async Task<string> ResolveTickSizeAsync(string tokenId, string? requested, CancellationToken cancellationToken) =>
        requested ?? await GetTickSizeAsync(tokenId, cancellationToken).ConfigureAwait(false);

    private SignedOrderBase BuildSignedOrder(object orderArguments, string tickSize, bool negRisk, int version, int feeRateBps)
    {
        if (_signer is null)
        {
            throw new InvalidOperationException("L1 authentication is unavailable.");
        }

        if (!PolymarketMath.RoundingConfig.TryGetValue(tickSize, out RoundConfig? roundConfig))
        {
            throw new InvalidOperationException($"Unsupported tick size '{tickSize}'.");
        }

        string maker = Options.FunderAddress ?? _signer.Address;
        string exchangeAddress = ResolveExchangeAddress(negRisk, version);

        if (orderArguments is OrderArgumentsV1 v1)
        {
            (Side side, string makerAmount, string takerAmount) = PolymarketMath.GetOrderAmounts(v1.Side, v1.Size, v1.Price, roundConfig);
            SignedOrderV1 order = new()
            {
                Salt = PolymarketMath.GenerateSalt(),
                Maker = maker,
                Signer = _signer.Address,
                Taker = v1.Taker ?? PolymarketConstants.ZeroAddress,
                TokenId = v1.TokenId,
                MakerAmount = makerAmount,
                TakerAmount = takerAmount,
                Side = side,
                Expiration = (v1.Expiration ?? 0).ToString(CultureInfo.InvariantCulture),
                Nonce = (v1.Nonce ?? 0).ToString(CultureInfo.InvariantCulture),
                FeeRateBps = (v1.FeeRateBps ?? feeRateBps).ToString(CultureInfo.InvariantCulture),
                SignatureType = (SignatureTypeV1)Math.Min((int)Options.SignatureType, (int)SignatureTypeV1.PolyGnosisSafe),
                Signature = string.Empty,
            };
            order = order with { Signature = _signer.SignOrderV1(Chain, exchangeAddress, order) };
            return order;
        }

        OrderArgumentsV2 v2 = (OrderArgumentsV2)orderArguments;
        (Side sideV2, string makerAmountV2, string takerAmountV2) = PolymarketMath.GetOrderAmounts(v2.Side, v2.Size, v2.Price, roundConfig);
        SignedOrderV2 orderV2 = new()
        {
            Salt = PolymarketMath.GenerateSalt(),
            Maker = maker,
            Signer = _signer.Address,
            Taker = PolymarketConstants.ZeroAddress,
            TokenId = v2.TokenId,
            MakerAmount = makerAmountV2,
            TakerAmount = takerAmountV2,
            Side = sideV2,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            Metadata = v2.Metadata ?? PolymarketConstants.Bytes32Zero,
            Builder = v2.BuilderCode ?? PolymarketConstants.Bytes32Zero,
            Expiration = (v2.Expiration ?? 0).ToString(CultureInfo.InvariantCulture),
            SignatureType = Options.SignatureType,
            Signature = string.Empty,
        };
        orderV2 = orderV2 with { Signature = _signer.SignOrderV2(Chain, exchangeAddress, orderV2) };
        return orderV2;
    }

    private SignedOrderBase BuildSignedMarketOrder(object marketOrderArguments, string tickSize, bool negRisk, int version, int feeRateBps)
    {
        if (_signer is null)
        {
            throw new InvalidOperationException("L1 authentication is unavailable.");
        }

        if (!PolymarketMath.RoundingConfig.TryGetValue(tickSize, out RoundConfig? roundConfig))
        {
            throw new InvalidOperationException($"Unsupported tick size '{tickSize}'.");
        }

        string maker = Options.FunderAddress ?? _signer.Address;
        string exchangeAddress = ResolveExchangeAddress(negRisk, version);

        if (marketOrderArguments is MarketOrderArgumentsV1 v1)
        {
            decimal price = v1.Price ?? throw new InvalidOperationException("Market order price must be resolved before signing.");
            (Side side, string makerAmount, string takerAmount) = PolymarketMath.GetMarketOrderAmounts(v1.Side, v1.Amount, price, roundConfig);
            SignedOrderV1 order = new()
            {
                Salt = PolymarketMath.GenerateSalt(),
                Maker = maker,
                Signer = _signer.Address,
                Taker = v1.Taker ?? PolymarketConstants.ZeroAddress,
                TokenId = v1.TokenId,
                MakerAmount = makerAmount,
                TakerAmount = takerAmount,
                Side = side,
                Expiration = "0",
                Nonce = (v1.Nonce ?? 0).ToString(CultureInfo.InvariantCulture),
                FeeRateBps = (v1.FeeRateBps ?? feeRateBps).ToString(CultureInfo.InvariantCulture),
                SignatureType = (SignatureTypeV1)Math.Min((int)Options.SignatureType, (int)SignatureTypeV1.PolyGnosisSafe),
                Signature = string.Empty,
            };
            order = order with { Signature = _signer.SignOrderV1(Chain, exchangeAddress, order) };
            return order;
        }

        MarketOrderArgumentsV2 v2 = (MarketOrderArgumentsV2)marketOrderArguments;
        decimal priceV2 = v2.Price ?? throw new InvalidOperationException("Market order price must be resolved before signing.");
        (Side sideV2, string makerAmountV2, string takerAmountV2) = PolymarketMath.GetMarketOrderAmounts(v2.Side, v2.Amount, priceV2, roundConfig);
        SignedOrderV2 orderV2 = new()
        {
            Salt = PolymarketMath.GenerateSalt(),
            Maker = maker,
            Signer = _signer.Address,
            Taker = PolymarketConstants.ZeroAddress,
            TokenId = v2.TokenId,
            MakerAmount = makerAmountV2,
            TakerAmount = takerAmountV2,
            Side = sideV2,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            Metadata = v2.Metadata ?? PolymarketConstants.Bytes32Zero,
            Builder = v2.BuilderCode ?? PolymarketConstants.Bytes32Zero,
            Expiration = "0",
            SignatureType = Options.SignatureType,
            Signature = string.Empty,
        };
        orderV2 = orderV2 with { Signature = _signer.SignOrderV2(Chain, exchangeAddress, orderV2) };
        return orderV2;
    }

    private object BuildOrderPayload(SignedOrderBase order, OrderType orderType, bool postOnly, bool deferExecution)
    {
        string owner = _credentials?.Key ?? string.Empty;
        long salt = long.Parse(order.Salt, CultureInfo.InvariantCulture);
        if (order is SignedOrderV1 v1)
        {
            return new
            {
                deferExec = deferExecution,
                postOnly = postOnly,
                order = new
                {
                    salt,
                    maker = v1.Maker,
                    signer = v1.Signer,
                    taker = v1.Taker,
                    tokenId = v1.TokenId,
                    makerAmount = v1.MakerAmount,
                    takerAmount = v1.TakerAmount,
                    expiration = v1.Expiration,
                    nonce = v1.Nonce,
                    feeRateBps = v1.FeeRateBps,
                    side = v1.Side.ToApiString(),
                    signatureType = (int)v1.SignatureType,
                    signature = v1.Signature,
                },
                owner,
                orderType = orderType.ToApiString(),
            };
        }

        SignedOrderV2 v2 = (SignedOrderV2)order;
        return new
        {
            deferExec = deferExecution,
            postOnly = postOnly,
            order = new
            {
                salt,
                maker = v2.Maker,
                signer = v2.Signer,
                taker = v2.Taker,
                tokenId = v2.TokenId,
                makerAmount = v2.MakerAmount,
                takerAmount = v2.TakerAmount,
                side = v2.Side.ToApiString(),
                signatureType = (int)v2.SignatureType,
                timestamp = v2.Timestamp,
                expiration = v2.Expiration,
                metadata = v2.Metadata,
                builder = v2.Builder,
                signature = v2.Signature,
            },
            owner,
            orderType = orderType.ToApiString(),
        };
    }

    private async Task<Dictionary<string, string>> CreateLevel1HeadersAsync(long? nonce, CancellationToken cancellationToken)
    {
        EnsureL1Auth();
        long timestamp = await ResolveTimestampAsync(cancellationToken).ConfigureAwait(false);
        long resolvedNonce = nonce ?? 0;
        string signature = _signer!.SignClobAuthMessage(Chain, timestamp, resolvedNonce);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["POLY_ADDRESS"] = _signer.Address,
            ["POLY_SIGNATURE"] = signature,
            ["POLY_TIMESTAMP"] = timestamp.ToString(CultureInfo.InvariantCulture),
            ["POLY_NONCE"] = resolvedNonce.ToString(CultureInfo.InvariantCulture),
        };
    }

    private async Task<Dictionary<string, string>> CreateLevel2HeadersAsync(HttpMethod method, string endpoint, string? serializedBody, CancellationToken cancellationToken)
    {
        EnsureL2Auth();
        long timestamp = await ResolveTimestampAsync(cancellationToken).ConfigureAwait(false);
        string signature = PolymarketSigner.BuildL2Signature(_credentials!.Secret, timestamp, method, $"/{endpoint.TrimStart('/')}", serializedBody);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["POLY_ADDRESS"] = _signer!.Address,
            ["POLY_SIGNATURE"] = signature,
            ["POLY_TIMESTAMP"] = timestamp.ToString(CultureInfo.InvariantCulture),
            ["POLY_API_KEY"] = _credentials.Key,
            ["POLY_PASSPHRASE"] = _credentials.Passphrase,
        };
    }

    private async Task<long> ResolveTimestampAsync(CancellationToken cancellationToken) =>
        Options.UseServerTime
            ? await GetServerTimeAsync(cancellationToken).ConfigureAwait(false)
            : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private static IReadOnlyList<object> BuildBookParametersPayload(IReadOnlyList<BookParameters> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        return parameters.Select(static parameter => new Dictionary<string, object?>
        {
            ["token_id"] = parameter.TokenId,
            ["side"] = parameter.Side?.ToApiString(),
        }).Cast<object>().ToArray();
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string endpoint, object? body = null, IReadOnlyDictionary<string, string>? headers = null, IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(method, PolymarketQuery.Append(endpoint, query));
        if (headers is not null)
        {
            foreach ((string key, string value) in headers)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

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

        if (typeof(T) == typeof(object))
        {
            return default!;
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            return default!;
        }

        return PolymarketJson.Deserialize<T>(payload);
    }

    private JsonElement HandleJsonResponse(JsonElement element)
    {
        if (!Options.ThrowOnError)
        {
            return element;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("success", out JsonElement success) && success.ValueKind == JsonValueKind.False)
            {
                string? errorMessage = TryGetString(element, "errorMsg") ?? TryGetString(element, "error") ?? "Request returned success=false.";
                throw new ClobApiException(errorMessage, responseBody: element.GetRawText());
            }

            if (element.TryGetProperty("error", out JsonElement error) && error.ValueKind == JsonValueKind.String)
            {
                throw new ClobApiException(error.GetString() ?? "Request failed.", responseBody: element.GetRawText());
            }
        }

        return element;
    }

    private async Task RefreshVersionOnMismatchAsync(JsonElement element, CancellationToken cancellationToken)
    {
        if (ContainsOrderVersionMismatch(element))
        {
            await ResolveVersionAsync(true, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool ContainsOrderVersionMismatch(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        string? errorMessage = TryGetString(element, "errorMsg") ?? TryGetString(element, "error");
        return errorMessage?.Contains(PolymarketConstants.OrderVersionMismatchError, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private void ThrowHttpError(HttpStatusCode statusCode, string responseBody)
    {
        string message = $"Polymarket API request failed with status code {(int)statusCode}.";
        try
        {
            ClobErrorResponseBody? error = PolymarketJson.Deserialize<ClobErrorResponseBody>(responseBody);
            if (!string.IsNullOrWhiteSpace(error.Error))
            {
                message = error.Error;
            }
        }
        catch
        {
        }

        throw new ClobApiException(message, statusCode, responseBody);
    }

    private void EnsureL1Auth()
    {
        if (_signer is null)
        {
            throw new InvalidOperationException("L1 authentication is unavailable. Configure PrivateKey first.");
        }
    }

    private void EnsureL2Auth()
    {
        EnsureL1Auth();
        if (_credentials is null)
        {
            throw new InvalidOperationException("L2 authentication is unavailable. Configure API credentials first.");
        }
    }

    private string ResolveExchangeAddress(bool negRisk, int version) => version switch
    {
        1 when negRisk => ContractConfig.NegRiskExchange,
        1 => ContractConfig.Exchange,
        2 when negRisk => ContractConfig.NegRiskExchangeV2,
        2 => ContractConfig.ExchangeV2,
        _ => throw new InvalidOperationException($"Unsupported order version {version}."),
    };

    private static Uri NormalizeHost(Uri host)
    {
        ArgumentNullException.ThrowIfNull(host);
        string normalized = host.AbsoluteUri.TrimEnd('/') + "/";
        return new Uri(normalized, UriKind.Absolute);
    }
}
