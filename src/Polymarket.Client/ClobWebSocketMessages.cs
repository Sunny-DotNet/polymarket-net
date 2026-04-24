using System.Text.Json;
using System.Text.Json.Serialization;

namespace Polymarket.Client;

[JsonConverter(typeof(Internal.ClobMarketChannelMessageJsonConverter))]
public abstract record ClobMarketChannelMessage
{
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = string.Empty;
}

public sealed record ClobMarketBookMessage : ClobMarketChannelMessage
{
    [JsonPropertyName("asset_id")]
    public string AssetId { get; init; } = string.Empty;

    [JsonPropertyName("market")]
    public string Market { get; init; } = string.Empty;

    [JsonPropertyName("bids")]
    public IReadOnlyList<OrderSummary> Bids { get; init; } = [];

    [JsonPropertyName("asks")]
    public IReadOnlyList<OrderSummary> Asks { get; init; } = [];

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonPropertyName("hash")]
    public string Hash { get; init; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record ClobMarketPriceChange
{
    [JsonPropertyName("asset_id")]
    public string AssetId { get; init; } = string.Empty;

    [JsonPropertyName("price")]
    public string? Price { get; init; }

    [JsonPropertyName("size")]
    public string? Size { get; init; }

    [JsonPropertyName("side")]
    public Side? Side { get; init; }

    [JsonPropertyName("hash")]
    public string? Hash { get; init; }

    [JsonPropertyName("best_bid")]
    public string? BestBid { get; init; }

    [JsonPropertyName("best_ask")]
    public string? BestAsk { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record ClobMarketPriceChangeMessage : ClobMarketChannelMessage
{
    [JsonPropertyName("market")]
    public string Market { get; init; } = string.Empty;

    [JsonPropertyName("price_changes")]
    public IReadOnlyList<ClobMarketPriceChange> PriceChanges { get; init; } = [];

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record ClobMarketTickSizeChangeMessage : ClobMarketChannelMessage
{
    [JsonPropertyName("asset_id")]
    public string AssetId { get; init; } = string.Empty;

    [JsonPropertyName("market")]
    public string Market { get; init; } = string.Empty;

    [JsonPropertyName("old_tick_size")]
    public string OldTickSize { get; init; } = string.Empty;

    [JsonPropertyName("new_tick_size")]
    public string NewTickSize { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record ClobMarketLastTradePriceMessage : ClobMarketChannelMessage
{
    [JsonPropertyName("asset_id")]
    public string AssetId { get; init; } = string.Empty;

    [JsonPropertyName("fee_rate_bps")]
    public string? FeeRateBps { get; init; }

    [JsonPropertyName("market")]
    public string Market { get; init; } = string.Empty;

    [JsonPropertyName("price")]
    public string Price { get; init; } = string.Empty;

    [JsonPropertyName("side")]
    public Side Side { get; init; }

    [JsonPropertyName("size")]
    public string Size { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonPropertyName("transaction_hash")]
    public string? TransactionHash { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record ClobMarketBestBidAskMessage : ClobMarketChannelMessage
{
    [JsonPropertyName("market")]
    public string Market { get; init; } = string.Empty;

    [JsonPropertyName("asset_id")]
    public string AssetId { get; init; } = string.Empty;

    [JsonPropertyName("best_bid")]
    public string BestBid { get; init; } = string.Empty;

    [JsonPropertyName("best_ask")]
    public string BestAsk { get; init; } = string.Empty;

    [JsonPropertyName("spread")]
    public string Spread { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record ClobMarketEventSummary
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("ticker")]
    public string? Ticker { get; init; }

    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record ClobNewMarketMessage : ClobMarketChannelMessage
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("question")]
    public string Question { get; init; } = string.Empty;

    [JsonPropertyName("market")]
    public string Market { get; init; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("assets_ids")]
    public IReadOnlyList<string> AssetIds { get; init; } = [];

    [JsonPropertyName("outcomes")]
    public IReadOnlyList<string> Outcomes { get; init; } = [];

    [JsonPropertyName("event_message")]
    public ClobMarketEventSummary? EventMessage { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = [];

    [JsonPropertyName("condition_id")]
    public string? ConditionId { get; init; }

    [JsonPropertyName("active")]
    public bool? Active { get; init; }

    [JsonPropertyName("clob_token_ids")]
    public IReadOnlyList<string> ClobTokenIds { get; init; } = [];

    [JsonPropertyName("sports_market_type")]
    public string? SportsMarketType { get; init; }

    [JsonPropertyName("line")]
    public string? Line { get; init; }

    [JsonPropertyName("game_start_time")]
    public string? GameStartTime { get; init; }

    [JsonPropertyName("order_price_min_tick_size")]
    public string? OrderPriceMinTickSize { get; init; }

    [JsonPropertyName("group_item_title")]
    public string? GroupItemTitle { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record ClobMarketResolvedMessage : ClobMarketChannelMessage
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("market")]
    public string Market { get; init; } = string.Empty;

    [JsonPropertyName("assets_ids")]
    public IReadOnlyList<string> AssetIds { get; init; } = [];

    [JsonPropertyName("winning_asset_id")]
    public string WinningAssetId { get; init; } = string.Empty;

    [JsonPropertyName("winning_outcome")]
    public string WinningOutcome { get; init; } = string.Empty;

    [JsonPropertyName("event_message")]
    public ClobMarketEventSummary? EventMessage { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record ClobUnknownMarketChannelMessage : ClobMarketChannelMessage
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

[JsonConverter(typeof(Internal.ClobUserChannelMessageJsonConverter))]
public abstract record ClobUserChannelMessage
{
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = string.Empty;
}

public sealed record ClobUserMakerOrder
{
    [JsonPropertyName("asset_id")]
    public string AssetId { get; init; } = string.Empty;

    [JsonPropertyName("matched_amount")]
    public string? MatchedAmount { get; init; }

    [JsonPropertyName("order_id")]
    public string? OrderId { get; init; }

    [JsonPropertyName("outcome")]
    public string? Outcome { get; init; }

    [JsonPropertyName("owner")]
    public string? Owner { get; init; }

    [JsonPropertyName("maker_address")]
    public string? MakerAddress { get; init; }

    [JsonPropertyName("price")]
    public string? Price { get; init; }

    [JsonPropertyName("fee_rate_bps")]
    public string? FeeRateBps { get; init; }

    [JsonPropertyName("side")]
    public Side? Side { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record ClobTradeMessage : ClobUserChannelMessage
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("taker_order_id")]
    public string? TakerOrderId { get; init; }

    [JsonPropertyName("market")]
    public string Market { get; init; } = string.Empty;

    [JsonPropertyName("asset_id")]
    public string AssetId { get; init; } = string.Empty;

    [JsonPropertyName("side")]
    public Side Side { get; init; }

    [JsonPropertyName("size")]
    public string Size { get; init; } = string.Empty;

    [JsonPropertyName("price")]
    public string Price { get; init; } = string.Empty;

    [JsonPropertyName("fee_rate_bps")]
    public string? FeeRateBps { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("matchtime")]
    public string? Matchtime { get; init; }

    [JsonPropertyName("last_update")]
    public string? LastUpdate { get; init; }

    [JsonPropertyName("outcome")]
    public string? Outcome { get; init; }

    [JsonPropertyName("owner")]
    public string Owner { get; init; } = string.Empty;

    [JsonPropertyName("trade_owner")]
    public string? TradeOwner { get; init; }

    [JsonPropertyName("maker_address")]
    public string? MakerAddress { get; init; }

    [JsonPropertyName("transaction_hash")]
    public string? TransactionHash { get; init; }

    [JsonPropertyName("bucket_index")]
    public int? BucketIndex { get; init; }

    [JsonPropertyName("maker_orders")]
    public IReadOnlyList<ClobUserMakerOrder> MakerOrders { get; init; } = [];

    [JsonPropertyName("trader_side")]
    public string? TraderSide { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record ClobOrderMessage : ClobUserChannelMessage
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("owner")]
    public string Owner { get; init; } = string.Empty;

    [JsonPropertyName("market")]
    public string Market { get; init; } = string.Empty;

    [JsonPropertyName("asset_id")]
    public string AssetId { get; init; } = string.Empty;

    [JsonPropertyName("side")]
    public Side Side { get; init; }

    [JsonPropertyName("order_owner")]
    public string? OrderOwner { get; init; }

    [JsonPropertyName("original_size")]
    public string OriginalSize { get; init; } = string.Empty;

    [JsonPropertyName("size_matched")]
    public string SizeMatched { get; init; } = string.Empty;

    [JsonPropertyName("price")]
    public string Price { get; init; } = string.Empty;

    [JsonPropertyName("associate_trades")]
    public IReadOnlyList<string> AssociateTrades { get; init; } = [];

    [JsonPropertyName("outcome")]
    public string? Outcome { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; init; }

    [JsonPropertyName("expiration")]
    public string? Expiration { get; init; }

    [JsonPropertyName("order_type")]
    public OrderType? OrderType { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("maker_address")]
    public string? MakerAddress { get; init; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; init; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record ClobUnknownUserChannelMessage : ClobUserChannelMessage
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record SportsResultUpdateMessage
{
    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("live")]
    public bool? Live { get; init; }

    [JsonPropertyName("ended")]
    public bool? Ended { get; init; }

    [JsonPropertyName("score")]
    public string? Score { get; init; }

    [JsonPropertyName("period")]
    public string? Period { get; init; }

    [JsonPropertyName("elapsed")]
    public string? Elapsed { get; init; }

    [JsonPropertyName("last_update")]
    public string? LastUpdate { get; init; }

    [JsonPropertyName("finished_timestamp")]
    public string? FinishedTimestamp { get; init; }

    [JsonPropertyName("turn")]
    public string? Turn { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}
