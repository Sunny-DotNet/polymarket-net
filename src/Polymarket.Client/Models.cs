using System.Text.Json;
using System.Text.Json.Serialization;

namespace Polymarket.Client;

public enum Side
{
    Buy,
    Sell,
}

public enum OrderType
{
    Gtc,
    Fok,
    Gtd,
    Fak,
}

public enum AssetType
{
    Collateral,
    Conditional,
}

public enum PriceHistoryInterval
{
    Max,
    OneWeek,
    OneDay,
    SixHours,
    OneHour,
}

public enum SignatureTypeV1
{
    Eoa = 0,
    PolyProxy = 1,
    PolyGnosisSafe = 2,
}

public enum SignatureTypeV2
{
    Eoa = 0,
    PolyProxy = 1,
    PolyGnosisSafe = 2,
    Poly1271 = 3,
}

public sealed record DynamicPayload
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> Data { get; init; } = [];
}

public sealed record MarketRewards
{
    [JsonPropertyName("rates")]
    public JsonElement? Rates { get; init; }

    [JsonPropertyName("min_size")]
    public decimal MinSize { get; init; }

    [JsonPropertyName("max_spread")]
    public decimal MaxSpread { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record MarketToken
{
    [JsonPropertyName("token_id")]
    public string TokenId { get; init; } = string.Empty;

    [JsonPropertyName("outcome")]
    public string Outcome { get; init; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; init; }

    [JsonPropertyName("winner")]
    public bool Winner { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record SimplifiedMarket
{
    [JsonPropertyName("condition_id")]
    public string ConditionId { get; init; } = string.Empty;

    [JsonPropertyName("rewards")]
    public MarketRewards? Rewards { get; init; }

    [JsonPropertyName("tokens")]
    public IReadOnlyList<MarketToken> Tokens { get; init; } = [];

    [JsonPropertyName("active")]
    public bool Active { get; init; }

    [JsonPropertyName("closed")]
    public bool Closed { get; init; }

    [JsonPropertyName("archived")]
    public bool Archived { get; init; }

    [JsonPropertyName("accepting_orders")]
    public bool AcceptingOrders { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record Market
{
    [JsonPropertyName("enable_order_book")]
    public bool EnableOrderBook { get; init; }

    [JsonPropertyName("active")]
    public bool Active { get; init; }

    [JsonPropertyName("closed")]
    public bool Closed { get; init; }

    [JsonPropertyName("archived")]
    public bool Archived { get; init; }

    [JsonPropertyName("accepting_orders")]
    public bool AcceptingOrders { get; init; }

    [JsonPropertyName("accepting_order_timestamp")]
    public string? AcceptingOrderTimestamp { get; init; }

    [JsonPropertyName("minimum_order_size")]
    public decimal MinimumOrderSize { get; init; }

    [JsonPropertyName("minimum_tick_size")]
    public decimal MinimumTickSize { get; init; }

    [JsonPropertyName("condition_id")]
    public string ConditionId { get; init; } = string.Empty;

    [JsonPropertyName("question_id")]
    public string? QuestionId { get; init; }

    [JsonPropertyName("question")]
    public string Question { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("market_slug")]
    public string MarketSlug { get; init; } = string.Empty;

    [JsonPropertyName("end_date_iso")]
    public string? EndDateIso { get; init; }

    [JsonPropertyName("game_start_time")]
    public string? GameStartTime { get; init; }

    [JsonPropertyName("seconds_delay")]
    public int SecondsDelay { get; init; }

    [JsonPropertyName("fpmm")]
    public string? Fpmm { get; init; }

    [JsonPropertyName("maker_base_fee")]
    public decimal? MakerBaseFee { get; init; }

    [JsonPropertyName("taker_base_fee")]
    public decimal? TakerBaseFee { get; init; }

    [JsonPropertyName("notifications_enabled")]
    public bool NotificationsEnabled { get; init; }

    [JsonPropertyName("neg_risk")]
    public bool NegRisk { get; init; }

    [JsonPropertyName("neg_risk_market_id")]
    public string? NegRiskMarketId { get; init; }

    [JsonPropertyName("neg_risk_request_id")]
    public string? NegRiskRequestId { get; init; }

    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    [JsonPropertyName("image")]
    public string? Image { get; init; }

    [JsonPropertyName("rewards")]
    public MarketRewards? Rewards { get; init; }

    [JsonPropertyName("is_50_50_outcome")]
    public bool IsFiftyFiftyOutcome { get; init; }

    [JsonPropertyName("tokens")]
    public IReadOnlyList<MarketToken> Tokens { get; init; } = [];

    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record MarketByTokenResponse
{
    [JsonPropertyName("condition_id")]
    public string ConditionId { get; init; } = string.Empty;

    [JsonPropertyName("primary_token_id")]
    public string? PrimaryTokenId { get; init; }

    [JsonPropertyName("secondary_token_id")]
    public string? SecondaryTokenId { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record PaginationPayload<T>(
    [property: JsonPropertyName("limit")] int Limit,
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("next_cursor")] string NextCursor,
    [property: JsonPropertyName("data")] IReadOnlyList<T> Data);

public sealed record BookParameters(
    [property: JsonPropertyName("token_id")] string TokenId,
    [property: JsonPropertyName("side")] Side? Side = null);

public sealed record PriceHistoryParameters(
    [property: JsonPropertyName("market")] string? Market = null,
    [property: JsonPropertyName("startTs")] long? StartTimestamp = null,
    [property: JsonPropertyName("endTs")] long? EndTimestamp = null,
    [property: JsonPropertyName("fidelity")] int? Fidelity = null,
    [property: JsonPropertyName("interval")] PriceHistoryInterval? Interval = null);

public sealed record TradeParameters(
    [property: JsonPropertyName("id")] string? Id = null,
    [property: JsonPropertyName("maker_address")] string? MakerAddress = null,
    [property: JsonPropertyName("market")] string? Market = null,
    [property: JsonPropertyName("asset_id")] string? AssetId = null,
    [property: JsonPropertyName("before")] string? Before = null,
    [property: JsonPropertyName("after")] string? After = null);

public sealed record BuilderTradeParameters(
    [property: JsonPropertyName("builder_code")] string BuilderCode,
    [property: JsonPropertyName("id")] string? Id = null,
    [property: JsonPropertyName("maker_address")] string? MakerAddress = null,
    [property: JsonPropertyName("market")] string? Market = null,
    [property: JsonPropertyName("asset_id")] string? AssetId = null,
    [property: JsonPropertyName("before")] string? Before = null,
    [property: JsonPropertyName("after")] string? After = null);

public sealed record OpenOrderParameters(
    [property: JsonPropertyName("id")] string? Id = null,
    [property: JsonPropertyName("market")] string? Market = null,
    [property: JsonPropertyName("asset_id")] string? AssetId = null);

public sealed record DropNotificationParameters(
    [property: JsonPropertyName("ids")] IReadOnlyList<string> Ids);

public sealed record BalanceAllowanceParameters(
    [property: JsonPropertyName("asset_type")] AssetType AssetType,
    [property: JsonPropertyName("token_id")] string? TokenId = null,
    [property: JsonPropertyName("signature_type")] int? SignatureType = null);

public sealed record OrderScoringParameters(
    [property: JsonPropertyName("order_id")] string OrderId);

public sealed record OrdersScoringParameters(
    [property: JsonPropertyName("orderIds")] IReadOnlyList<string> OrderIds);

public sealed record CreateOrderOptions(
    [property: JsonPropertyName("tickSize")] string TickSize,
    [property: JsonPropertyName("negRisk")] bool NegRisk = false);

public sealed record PartialCreateOrderOptions(
    [property: JsonPropertyName("tickSize")] string? TickSize = null,
    [property: JsonPropertyName("negRisk")] bool? NegRisk = null);

public sealed record RoundConfig(int Price, int Size, int Amount);

public sealed record OrderArgumentsV1(
    string TokenId,
    decimal Price,
    decimal Size,
    Side Side,
    int? FeeRateBps = null,
    long? Nonce = null,
    long? Expiration = null,
    string? Taker = null,
    string? BuilderCode = null);

public record OrderArgumentsV2(
    string TokenId,
    decimal Price,
    decimal Size,
    Side Side,
    string? Metadata = null,
    string? BuilderCode = null,
    long? Expiration = null);

public sealed record OrderArguments(
    string TokenId,
    decimal Price,
    decimal Size,
    Side Side,
    string? Metadata = null,
    string? BuilderCode = null,
    long? Expiration = null)
    : OrderArgumentsV2(TokenId, Price, Size, Side, Metadata, BuilderCode, Expiration);

public sealed record MarketOrderArgumentsV1(
    string TokenId,
    decimal Amount,
    Side Side,
    decimal? Price = null,
    int? FeeRateBps = null,
    long? Nonce = null,
    string? Taker = null,
    OrderType? OrderType = null,
    string? BuilderCode = null);

public record MarketOrderArgumentsV2(
    string TokenId,
    decimal Amount,
    Side Side,
    decimal? Price = null,
    OrderType? OrderType = null,
    decimal? UserUsdcBalance = null,
    string? Metadata = null,
    string? BuilderCode = null);

public sealed record MarketOrderArguments(
    string TokenId,
    decimal Amount,
    Side Side,
    decimal? Price = null,
    OrderType? OrderType = null,
    decimal? UserUsdcBalance = null,
    string? Metadata = null,
    string? BuilderCode = null)
    : MarketOrderArgumentsV2(TokenId, Amount, Side, Price, OrderType, UserUsdcBalance, Metadata, BuilderCode);

public abstract record SignedOrderBase
{
    public required string Salt { get; init; }

    public required string Maker { get; init; }

    public required string Signer { get; init; }

    public string Taker { get; init; } = PolymarketConstants.ZeroAddress;

    public required string TokenId { get; init; }

    public required string MakerAmount { get; init; }

    public required string TakerAmount { get; init; }

    public required Side Side { get; init; }

    public required string Signature { get; init; }

    public string Expiration { get; init; } = "0";
}

public sealed record SignedOrderV1 : SignedOrderBase
{
    public required string Nonce { get; init; }

    public required string FeeRateBps { get; init; }

    public SignatureTypeV1 SignatureType { get; init; } = SignatureTypeV1.Eoa;
}

public sealed record SignedOrderV2 : SignedOrderBase
{
    public string Timestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

    public string Metadata { get; init; } = PolymarketConstants.Bytes32Zero;

    public string Builder { get; init; } = PolymarketConstants.Bytes32Zero;

    public SignatureTypeV2 SignatureType { get; init; } = SignatureTypeV2.Eoa;
}

public sealed record PostOrderArguments(
    SignedOrderBase Order,
    OrderType OrderType = OrderType.Gtc);

public sealed record OrderPayload(
    [property: JsonPropertyName("orderID")] string OrderId);

public sealed record OrderResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("errorMsg")] string? ErrorMessage,
    [property: JsonPropertyName("orderID")] string? OrderId,
    [property: JsonPropertyName("transactionsHashes")] IReadOnlyList<string>? TransactionHashes,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("takingAmount")] string? TakingAmount,
    [property: JsonPropertyName("makingAmount")] string? MakingAmount);

public sealed record OpenOrder(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("owner")] string Owner,
    [property: JsonPropertyName("maker_address")] string MakerAddress,
    [property: JsonPropertyName("market")] string Market,
    [property: JsonPropertyName("asset_id")] string AssetId,
    [property: JsonPropertyName("side")] string Side,
    [property: JsonPropertyName("original_size")] string OriginalSize,
    [property: JsonPropertyName("size_matched")] string SizeMatched,
    [property: JsonPropertyName("price")] string Price,
    [property: JsonPropertyName("associate_trades")] IReadOnlyList<string> AssociateTrades,
    [property: JsonPropertyName("outcome")] string Outcome,
    [property: JsonPropertyName("created_at")] long CreatedAt,
    [property: JsonPropertyName("expiration")] string Expiration,
    [property: JsonPropertyName("order_type")] string OrderType);

public sealed record MakerOrder(
    [property: JsonPropertyName("order_id")] string OrderId,
    [property: JsonPropertyName("owner")] string Owner,
    [property: JsonPropertyName("maker_address")] string MakerAddress,
    [property: JsonPropertyName("matched_amount")] string MatchedAmount,
    [property: JsonPropertyName("price")] string Price,
    [property: JsonPropertyName("fee_rate_bps")] string FeeRateBps,
    [property: JsonPropertyName("asset_id")] string AssetId,
    [property: JsonPropertyName("outcome")] string Outcome,
    [property: JsonPropertyName("side")] string Side);

public sealed record Trade(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("taker_order_id")] string TakerOrderId,
    [property: JsonPropertyName("market")] string Market,
    [property: JsonPropertyName("asset_id")] string AssetId,
    [property: JsonPropertyName("side")] string Side,
    [property: JsonPropertyName("size")] string Size,
    [property: JsonPropertyName("fee_rate_bps")] string FeeRateBps,
    [property: JsonPropertyName("price")] string Price,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("match_time")] string MatchTime,
    [property: JsonPropertyName("last_update")] string LastUpdate,
    [property: JsonPropertyName("outcome")] string Outcome,
    [property: JsonPropertyName("bucket_index")] int BucketIndex,
    [property: JsonPropertyName("owner")] string Owner,
    [property: JsonPropertyName("maker_address")] string MakerAddress,
    [property: JsonPropertyName("maker_orders")] IReadOnlyList<MakerOrder> MakerOrders,
    [property: JsonPropertyName("transaction_hash")] string TransactionHash,
    [property: JsonPropertyName("trader_side")] string TraderSide);

public sealed record ApiKeysResponse(
    [property: JsonPropertyName("apiKeys")] IReadOnlyList<ApiCredentials> ApiKeys);

public sealed record BanStatus(
    [property: JsonPropertyName("closed_only")] bool ClosedOnly);

public sealed record MarketPricePoint(
    [property: JsonPropertyName("t")] long Timestamp,
    [property: JsonPropertyName("p")] decimal Price);

public sealed record Notification(
    [property: JsonPropertyName("type")] int Type,
    [property: JsonPropertyName("owner")] string Owner,
    [property: JsonPropertyName("payload")] JsonElement Payload);

public sealed record OrderSummary(
    [property: JsonPropertyName("price")] string Price,
    [property: JsonPropertyName("size")] string Size);

public sealed record OrderBookSummary(
    [property: JsonPropertyName("market")] string Market,
    [property: JsonPropertyName("asset_id")] string AssetId,
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("bids")] IReadOnlyList<OrderSummary> Bids,
    [property: JsonPropertyName("asks")] IReadOnlyList<OrderSummary> Asks,
    [property: JsonPropertyName("min_order_size")] string MinOrderSize,
    [property: JsonPropertyName("tick_size")] string TickSize,
    [property: JsonPropertyName("neg_risk")] bool NegRisk,
    [property: JsonPropertyName("hash")] string Hash,
    [property: JsonPropertyName("last_trade_price")] string LastTradePrice);

public sealed record BalanceAllowanceResponse(
    [property: JsonPropertyName("balance")] string Balance,
    [property: JsonPropertyName("allowance")] string Allowance);

public sealed record OrderScoringResponse(
    [property: JsonPropertyName("scoring")] bool Scoring);

public sealed record FeeInfo(int Rate, int Exponent);

public sealed record BuilderFeeRate(decimal Maker, decimal Taker);

public sealed record FeeDetails(
    [property: JsonPropertyName("r")] int? Rate,
    [property: JsonPropertyName("e")] int? Exponent,
    [property: JsonPropertyName("to")] bool TakerOnly);

public sealed record ClobToken(
    [property: JsonPropertyName("t")] string TokenId,
    [property: JsonPropertyName("o")] string Outcome);

public sealed record MarketDetails(
    [property: JsonPropertyName("c")] string ConditionId,
    [property: JsonPropertyName("t")] IReadOnlyList<ClobToken?> Tokens,
    [property: JsonPropertyName("mts")] decimal MinimumTickSize,
    [property: JsonPropertyName("nr")] bool NegRisk,
    [property: JsonPropertyName("fd")] FeeDetails? FeeDetails,
    [property: JsonPropertyName("mbf")] int? MakerBaseFee,
    [property: JsonPropertyName("tbf")] int? TakerBaseFee);

public sealed record UserEarning(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("condition_id")] string ConditionId,
    [property: JsonPropertyName("asset_address")] string AssetAddress,
    [property: JsonPropertyName("maker_address")] string MakerAddress,
    [property: JsonPropertyName("earnings")] decimal Earnings,
    [property: JsonPropertyName("asset_rate")] decimal AssetRate);

public sealed record TotalUserEarning(
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("asset_address")] string AssetAddress,
    [property: JsonPropertyName("maker_address")] string MakerAddress,
    [property: JsonPropertyName("earnings")] decimal Earnings,
    [property: JsonPropertyName("asset_rate")] decimal AssetRate);

public sealed record RewardToken(
    [property: JsonPropertyName("token_id")] string TokenId,
    [property: JsonPropertyName("outcome")] string Outcome,
    [property: JsonPropertyName("price")] decimal Price);

public sealed record RewardsConfig(
    [property: JsonPropertyName("asset_address")] string AssetAddress,
    [property: JsonPropertyName("start_date")] string StartDate,
    [property: JsonPropertyName("end_date")] string EndDate,
    [property: JsonPropertyName("rate_per_day")] decimal RatePerDay,
    [property: JsonPropertyName("total_rewards")] decimal TotalRewards);

public sealed record MarketReward(
    [property: JsonPropertyName("condition_id")] string ConditionId,
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("market_slug")] string MarketSlug,
    [property: JsonPropertyName("event_slug")] string EventSlug,
    [property: JsonPropertyName("image")] string Image,
    [property: JsonPropertyName("rewards_max_spread")] decimal RewardsMaxSpread,
    [property: JsonPropertyName("rewards_min_size")] decimal RewardsMinSize,
    [property: JsonPropertyName("tokens")] IReadOnlyList<RewardToken> Tokens,
    [property: JsonPropertyName("rewards_config")] IReadOnlyList<RewardsConfig> RewardsConfig);

public sealed record Earning(
    [property: JsonPropertyName("asset_address")] string AssetAddress,
    [property: JsonPropertyName("earnings")] decimal Earnings,
    [property: JsonPropertyName("asset_rate")] decimal AssetRate);

public sealed record UserRewardsEarning(
    [property: JsonPropertyName("condition_id")] string ConditionId,
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("market_slug")] string MarketSlug,
    [property: JsonPropertyName("event_slug")] string EventSlug,
    [property: JsonPropertyName("image")] string Image,
    [property: JsonPropertyName("rewards_max_spread")] decimal RewardsMaxSpread,
    [property: JsonPropertyName("rewards_min_size")] decimal RewardsMinSize,
    [property: JsonPropertyName("market_competitiveness")] decimal MarketCompetitiveness,
    [property: JsonPropertyName("tokens")] IReadOnlyList<RewardToken> Tokens,
    [property: JsonPropertyName("rewards_config")] IReadOnlyList<RewardsConfig> RewardsConfig,
    [property: JsonPropertyName("maker_address")] string MakerAddress,
    [property: JsonPropertyName("earning_percentage")] decimal EarningPercentage,
    [property: JsonPropertyName("earnings")] IReadOnlyList<Earning> Earnings);

public sealed record BuilderTrade(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("tradeType")] string TradeType,
    [property: JsonPropertyName("takerOrderHash")] string TakerOrderHash,
    [property: JsonPropertyName("builder")] string Builder,
    [property: JsonPropertyName("market")] string Market,
    [property: JsonPropertyName("assetId")] string AssetId,
    [property: JsonPropertyName("side")] string Side,
    [property: JsonPropertyName("size")] string Size,
    [property: JsonPropertyName("sizeUsdc")] string SizeUsdc,
    [property: JsonPropertyName("price")] string Price,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("outcome")] string Outcome,
    [property: JsonPropertyName("outcomeIndex")] int OutcomeIndex,
    [property: JsonPropertyName("owner")] string Owner,
    [property: JsonPropertyName("maker")] string Maker,
    [property: JsonPropertyName("transactionHash")] string TransactionHash,
    [property: JsonPropertyName("matchTime")] string MatchTime,
    [property: JsonPropertyName("bucketIndex")] int BucketIndex,
    [property: JsonPropertyName("fee")] string Fee,
    [property: JsonPropertyName("feeUsdc")] string FeeUsdc,
    [property: JsonPropertyName("err_msg")] string? ErrorMessage,
    [property: JsonPropertyName("createdAt")] string? CreatedAt,
    [property: JsonPropertyName("updatedAt")] string? UpdatedAt);

public sealed record ReadonlyApiKeyResponse(
    [property: JsonPropertyName("apiKey")] string ApiKey);

public sealed record MarketTradeEventMarket(
    [property: JsonPropertyName("condition_id")] string ConditionId,
    [property: JsonPropertyName("asset_id")] string AssetId,
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("icon")] string Icon,
    [property: JsonPropertyName("slug")] string Slug);

public sealed record MarketTradeEventUser(
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("profile_picture")] string ProfilePicture,
    [property: JsonPropertyName("optimized_profile_picture")] string OptimizedProfilePicture,
    [property: JsonPropertyName("pseudonym")] string Pseudonym);

public sealed record MarketTradeEvent(
    [property: JsonPropertyName("event_type")] string EventType,
    [property: JsonPropertyName("market")] MarketTradeEventMarket Market,
    [property: JsonPropertyName("user")] MarketTradeEventUser User,
    [property: JsonPropertyName("side")] string Side,
    [property: JsonPropertyName("fee_rate_bps")] string FeeRateBps,
    [property: JsonPropertyName("size")] string Size,
    [property: JsonPropertyName("price")] string Price,
    [property: JsonPropertyName("outcome")] string Outcome,
    [property: JsonPropertyName("outcome_index")] int OutcomeIndex,
    [property: JsonPropertyName("transaction_hash")] string TransactionHash,
    [property: JsonPropertyName("timestamp")] string Timestamp);

public sealed record BuilderApiKey(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("secret")] string Secret,
    [property: JsonPropertyName("passphrase")] string Passphrase);

public sealed record BuilderApiKeyResponse(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("createdAt")] string? CreatedAt,
    [property: JsonPropertyName("revokedAt")] string? RevokedAt);

public sealed record TradesPaginatedResponse(
    [property: JsonPropertyName("trades")] IReadOnlyList<Trade> Trades,
    [property: JsonPropertyName("next_cursor")] string NextCursor,
    [property: JsonPropertyName("limit")] int Limit,
    [property: JsonPropertyName("count")] int Count);

public sealed record BuilderTradesResponse(
    [property: JsonPropertyName("trades")] IReadOnlyList<BuilderTrade> Trades,
    [property: JsonPropertyName("next_cursor")] string NextCursor,
    [property: JsonPropertyName("limit")] int Limit,
    [property: JsonPropertyName("count")] int Count);

public sealed record HeartbeatResponse(
    [property: JsonPropertyName("heartbeat_id")] string HeartbeatId,
    [property: JsonPropertyName("error_msg")] string? ErrorMessage);

public sealed record OrderMarketCancelParameters(
    [property: JsonPropertyName("market")] string? Market = null,
    [property: JsonPropertyName("asset_id")] string? AssetId = null);
