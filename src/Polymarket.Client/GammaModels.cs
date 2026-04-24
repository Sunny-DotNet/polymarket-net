using System.Text.Json;
using System.Text.Json.Serialization;

namespace Polymarket.Client;

public record GammaQueryParameters
{
    public IReadOnlyDictionary<string, string?> AdditionalParameters { get; init; } = new Dictionary<string, string?>(StringComparer.Ordinal);
}

public record GammaPaginationQueryParameters : GammaQueryParameters
{
    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    [JsonPropertyName("offset")]
    public int? Offset { get; init; }

    [JsonPropertyName("order")]
    public string? Order { get; init; }

    [JsonPropertyName("ascending")]
    public bool? Ascending { get; init; }
}

public record GammaKeysetQueryParameters : GammaQueryParameters
{
    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    [JsonPropertyName("order")]
    public string? Order { get; init; }

    [JsonPropertyName("ascending")]
    public bool? Ascending { get; init; }

    [JsonPropertyName("after_cursor")]
    public string? AfterCursor { get; init; }
}

public sealed record GammaTeamQueryParameters : GammaPaginationQueryParameters
{
    [JsonPropertyName("league")]
    public IReadOnlyList<string> League { get; init; } = [];

    [JsonPropertyName("name")]
    public IReadOnlyList<string> Name { get; init; } = [];

    [JsonPropertyName("abbreviation")]
    public IReadOnlyList<string> Abbreviation { get; init; } = [];
}

public sealed record GammaTagQueryParameters : GammaPaginationQueryParameters
{
    [JsonPropertyName("include_template")]
    public bool? IncludeTemplate { get; init; }

    [JsonPropertyName("is_carousel")]
    public bool? IsCarousel { get; init; }
}

public sealed record GammaRelatedTagQueryParameters : GammaQueryParameters
{
    [JsonPropertyName("omit_empty")]
    public bool? OmitEmpty { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }
}

public sealed record GammaEventQueryParameters : GammaPaginationQueryParameters
{
    [JsonPropertyName("id")]
    public IReadOnlyList<long> Id { get; init; } = [];

    [JsonPropertyName("tag_id")]
    public long? TagId { get; init; }

    [JsonPropertyName("exclude_tag_id")]
    public IReadOnlyList<long> ExcludeTagId { get; init; } = [];

    [JsonPropertyName("slug")]
    public IReadOnlyList<string> Slug { get; init; } = [];

    [JsonPropertyName("tag_slug")]
    public string? TagSlug { get; init; }

    [JsonPropertyName("related_tags")]
    public bool? RelatedTags { get; init; }

    [JsonPropertyName("active")]
    public bool? Active { get; init; }

    [JsonPropertyName("archived")]
    public bool? Archived { get; init; }

    [JsonPropertyName("featured")]
    public bool? Featured { get; init; }

    [JsonPropertyName("cyom")]
    public bool? Cyom { get; init; }

    [JsonPropertyName("include_chat")]
    public bool? IncludeChat { get; init; }

    [JsonPropertyName("include_template")]
    public bool? IncludeTemplate { get; init; }

    [JsonPropertyName("recurrence")]
    public string? Recurrence { get; init; }

    [JsonPropertyName("closed")]
    public bool? Closed { get; init; }

    [JsonPropertyName("liquidity_min")]
    public decimal? LiquidityMin { get; init; }

    [JsonPropertyName("liquidity_max")]
    public decimal? LiquidityMax { get; init; }

    [JsonPropertyName("volume_min")]
    public decimal? VolumeMin { get; init; }

    [JsonPropertyName("volume_max")]
    public decimal? VolumeMax { get; init; }

    [JsonPropertyName("start_date_min")]
    public DateTimeOffset? StartDateMin { get; init; }

    [JsonPropertyName("start_date_max")]
    public DateTimeOffset? StartDateMax { get; init; }

    [JsonPropertyName("end_date_min")]
    public DateTimeOffset? EndDateMin { get; init; }

    [JsonPropertyName("end_date_max")]
    public DateTimeOffset? EndDateMax { get; init; }
}

public sealed record GammaEventPaginationQueryParameters : GammaPaginationQueryParameters
{
    [JsonPropertyName("include_chat")]
    public bool? IncludeChat { get; init; }

    [JsonPropertyName("include_template")]
    public bool? IncludeTemplate { get; init; }

    [JsonPropertyName("recurrence")]
    public string? Recurrence { get; init; }
}

public sealed record GammaEventCreatorQueryParameters : GammaPaginationQueryParameters
{
    [JsonPropertyName("creator_name")]
    public string? CreatorName { get; init; }

    [JsonPropertyName("creator_handle")]
    public string? CreatorHandle { get; init; }
}

public sealed record GammaMarketQueryParameters : GammaPaginationQueryParameters
{
    [JsonPropertyName("id")]
    public IReadOnlyList<long> Id { get; init; } = [];

    [JsonPropertyName("slug")]
    public IReadOnlyList<string> Slug { get; init; } = [];

    [JsonPropertyName("clob_token_ids")]
    public IReadOnlyList<string> ClobTokenIds { get; init; } = [];

    [JsonPropertyName("condition_ids")]
    public IReadOnlyList<string> ConditionIds { get; init; } = [];

    [JsonPropertyName("market_maker_address")]
    public IReadOnlyList<string> MarketMakerAddress { get; init; } = [];

    [JsonPropertyName("liquidity_num_min")]
    public decimal? LiquidityNumMin { get; init; }

    [JsonPropertyName("liquidity_num_max")]
    public decimal? LiquidityNumMax { get; init; }

    [JsonPropertyName("volume_num_min")]
    public decimal? VolumeNumMin { get; init; }

    [JsonPropertyName("volume_num_max")]
    public decimal? VolumeNumMax { get; init; }

    [JsonPropertyName("start_date_min")]
    public DateTimeOffset? StartDateMin { get; init; }

    [JsonPropertyName("start_date_max")]
    public DateTimeOffset? StartDateMax { get; init; }

    [JsonPropertyName("end_date_min")]
    public DateTimeOffset? EndDateMin { get; init; }

    [JsonPropertyName("end_date_max")]
    public DateTimeOffset? EndDateMax { get; init; }

    [JsonPropertyName("tag_id")]
    public long? TagId { get; init; }

    [JsonPropertyName("related_tags")]
    public bool? RelatedTags { get; init; }

    [JsonPropertyName("cyom")]
    public bool? Cyom { get; init; }

    [JsonPropertyName("uma_resolution_status")]
    public string? UmaResolutionStatus { get; init; }

    [JsonPropertyName("game_id")]
    public string? GameId { get; init; }

    [JsonPropertyName("sports_market_types")]
    public IReadOnlyList<string> SportsMarketTypes { get; init; } = [];

    [JsonPropertyName("rewards_min_size")]
    public decimal? RewardsMinSize { get; init; }

    [JsonPropertyName("question_ids")]
    public IReadOnlyList<string> QuestionIds { get; init; } = [];

    [JsonPropertyName("include_tag")]
    public bool? IncludeTag { get; init; }

    [JsonPropertyName("closed")]
    public bool? Closed { get; init; }
}

public sealed record GammaMarketKeysetQueryParameters : GammaKeysetQueryParameters
{
    [JsonPropertyName("id")]
    public IReadOnlyList<long> Id { get; init; } = [];

    [JsonPropertyName("slug")]
    public IReadOnlyList<string> Slug { get; init; } = [];

    [JsonPropertyName("closed")]
    public bool? Closed { get; init; }

    [JsonPropertyName("decimalized")]
    public bool? Decimalized { get; init; }

    [JsonPropertyName("clob_token_ids")]
    public IReadOnlyList<string> ClobTokenIds { get; init; } = [];

    [JsonPropertyName("condition_ids")]
    public IReadOnlyList<string> ConditionIds { get; init; } = [];

    [JsonPropertyName("question_ids")]
    public IReadOnlyList<string> QuestionIds { get; init; } = [];

    [JsonPropertyName("market_maker_address")]
    public IReadOnlyList<string> MarketMakerAddress { get; init; } = [];

    [JsonPropertyName("liquidity_num_min")]
    public decimal? LiquidityNumMin { get; init; }

    [JsonPropertyName("liquidity_num_max")]
    public decimal? LiquidityNumMax { get; init; }

    [JsonPropertyName("volume_num_min")]
    public decimal? VolumeNumMin { get; init; }

    [JsonPropertyName("volume_num_max")]
    public decimal? VolumeNumMax { get; init; }

    [JsonPropertyName("start_date_min")]
    public DateTimeOffset? StartDateMin { get; init; }

    [JsonPropertyName("start_date_max")]
    public DateTimeOffset? StartDateMax { get; init; }

    [JsonPropertyName("end_date_min")]
    public DateTimeOffset? EndDateMin { get; init; }

    [JsonPropertyName("end_date_max")]
    public DateTimeOffset? EndDateMax { get; init; }

    [JsonPropertyName("tag_id")]
    public IReadOnlyList<long> TagId { get; init; } = [];

    [JsonPropertyName("related_tags")]
    public bool? RelatedTags { get; init; }

    [JsonPropertyName("tag_match")]
    public string? TagMatch { get; init; }

    [JsonPropertyName("cyom")]
    public bool? Cyom { get; init; }

    [JsonPropertyName("rfq_enabled")]
    public bool? RfqEnabled { get; init; }

    [JsonPropertyName("uma_resolution_status")]
    public string? UmaResolutionStatus { get; init; }

    [JsonPropertyName("game_id")]
    public string? GameId { get; init; }

    [JsonPropertyName("sports_market_types")]
    public IReadOnlyList<string> SportsMarketTypes { get; init; } = [];

    [JsonPropertyName("include_tag")]
    public bool? IncludeTag { get; init; }

    [JsonPropertyName("locale")]
    public string? Locale { get; init; }
}

public sealed record GammaEventKeysetQueryParameters : GammaKeysetQueryParameters
{
    [JsonPropertyName("id")]
    public IReadOnlyList<long> Id { get; init; } = [];

    [JsonPropertyName("slug")]
    public IReadOnlyList<string> Slug { get; init; } = [];

    [JsonPropertyName("closed")]
    public bool? Closed { get; init; }

    [JsonPropertyName("live")]
    public bool? Live { get; init; }

    [JsonPropertyName("featured")]
    public bool? Featured { get; init; }

    [JsonPropertyName("cyom")]
    public bool? Cyom { get; init; }

    [JsonPropertyName("title_search")]
    public string? TitleSearch { get; init; }

    [JsonPropertyName("liquidity_min")]
    public decimal? LiquidityMin { get; init; }

    [JsonPropertyName("liquidity_max")]
    public decimal? LiquidityMax { get; init; }

    [JsonPropertyName("volume_min")]
    public decimal? VolumeMin { get; init; }

    [JsonPropertyName("volume_max")]
    public decimal? VolumeMax { get; init; }

    [JsonPropertyName("start_date_min")]
    public DateTimeOffset? StartDateMin { get; init; }

    [JsonPropertyName("start_date_max")]
    public DateTimeOffset? StartDateMax { get; init; }

    [JsonPropertyName("end_date_min")]
    public DateTimeOffset? EndDateMin { get; init; }

    [JsonPropertyName("end_date_max")]
    public DateTimeOffset? EndDateMax { get; init; }

    [JsonPropertyName("start_time_min")]
    public DateTimeOffset? StartTimeMin { get; init; }

    [JsonPropertyName("start_time_max")]
    public DateTimeOffset? StartTimeMax { get; init; }

    [JsonPropertyName("tag_id")]
    public IReadOnlyList<long> TagId { get; init; } = [];

    [JsonPropertyName("tag_slug")]
    public string? TagSlug { get; init; }

    [JsonPropertyName("exclude_tag_id")]
    public IReadOnlyList<long> ExcludeTagId { get; init; } = [];

    [JsonPropertyName("related_tags")]
    public bool? RelatedTags { get; init; }

    [JsonPropertyName("tag_match")]
    public string? TagMatch { get; init; }

    [JsonPropertyName("series_id")]
    public IReadOnlyList<long> SeriesId { get; init; } = [];

    [JsonPropertyName("game_id")]
    public IReadOnlyList<long> GameId { get; init; } = [];

    [JsonPropertyName("event_date")]
    public DateTimeOffset? EventDate { get; init; }

    [JsonPropertyName("event_week")]
    public int? EventWeek { get; init; }

    [JsonPropertyName("featured_order")]
    public bool? FeaturedOrder { get; init; }

    [JsonPropertyName("recurrence")]
    public string? Recurrence { get; init; }

    [JsonPropertyName("created_by")]
    public IReadOnlyList<string> CreatedBy { get; init; } = [];

    [JsonPropertyName("parent_event_id")]
    public long? ParentEventId { get; init; }

    [JsonPropertyName("include_children")]
    public bool? IncludeChildren { get; init; }

    [JsonPropertyName("partner_slug")]
    public string? PartnerSlug { get; init; }

    [JsonPropertyName("include_chat")]
    public bool? IncludeChat { get; init; }

    [JsonPropertyName("include_template")]
    public bool? IncludeTemplate { get; init; }

    [JsonPropertyName("include_best_lines")]
    public bool? IncludeBestLines { get; init; }

    [JsonPropertyName("locale")]
    public string? Locale { get; init; }
}

public sealed record GammaSeriesQueryParameters : GammaPaginationQueryParameters
{
    [JsonPropertyName("slug")]
    public IReadOnlyList<string> Slug { get; init; } = [];

    [JsonPropertyName("categories_ids")]
    public IReadOnlyList<long> CategoriesIds { get; init; } = [];

    [JsonPropertyName("categories_labels")]
    public IReadOnlyList<string> CategoriesLabels { get; init; } = [];

    [JsonPropertyName("closed")]
    public bool? Closed { get; init; }

    [JsonPropertyName("include_chat")]
    public bool? IncludeChat { get; init; }

    [JsonPropertyName("recurrence")]
    public string? Recurrence { get; init; }

    [JsonPropertyName("exclude_events")]
    public bool? ExcludeEvents { get; init; }
}

public sealed record GammaCommentQueryParameters : GammaPaginationQueryParameters
{
    [JsonPropertyName("parent_entity_type")]
    public string? ParentEntityType { get; init; }

    [JsonPropertyName("parent_entity_id")]
    public long? ParentEntityId { get; init; }

    [JsonPropertyName("get_positions")]
    public bool? GetPositions { get; init; }

    [JsonPropertyName("holders_only")]
    public bool? HoldersOnly { get; init; }
}

public sealed record GammaCommentsByUserQueryParameters : GammaPaginationQueryParameters;

public sealed record GammaPublicSearchQueryParameters : GammaQueryParameters
{
    [JsonPropertyName("q")]
    public string Query { get; init; } = string.Empty;

    [JsonPropertyName("cache")]
    public bool? Cache { get; init; }

    [JsonPropertyName("events_status")]
    public string? EventsStatus { get; init; }

    [JsonPropertyName("limit_per_type")]
    public int? LimitPerType { get; init; }

    [JsonPropertyName("page")]
    public int? Page { get; init; }

    [JsonPropertyName("events_tag")]
    public IReadOnlyList<string> EventsTag { get; init; } = [];

    [JsonPropertyName("keep_closed_markets")]
    public int? KeepClosedMarkets { get; init; }

    [JsonPropertyName("sort")]
    public string? Sort { get; init; }

    [JsonPropertyName("ascending")]
    public bool? Ascending { get; init; }

    [JsonPropertyName("search_tags")]
    public bool? SearchTags { get; init; }

    [JsonPropertyName("search_profiles")]
    public bool? SearchProfiles { get; init; }

    [JsonPropertyName("recurrence")]
    public string? Recurrence { get; init; }

    [JsonPropertyName("exclude_tag_id")]
    public IReadOnlyList<long> ExcludeTagId { get; init; } = [];

    [JsonPropertyName("optimized")]
    public bool? Optimized { get; init; }
}

public sealed record GammaMarketsInformationRequest
{
    [JsonPropertyName("id")]
    public IReadOnlyList<long> Id { get; init; } = [];

    [JsonPropertyName("slug")]
    public IReadOnlyList<string> Slug { get; init; } = [];

    [JsonPropertyName("closed")]
    public bool? Closed { get; init; }

    [JsonPropertyName("clobTokenIds")]
    public IReadOnlyList<string> ClobTokenIds { get; init; } = [];

    [JsonPropertyName("conditionIds")]
    public IReadOnlyList<string> ConditionIds { get; init; } = [];

    [JsonPropertyName("marketMakerAddress")]
    public IReadOnlyList<string> MarketMakerAddress { get; init; } = [];

    [JsonPropertyName("liquidityNumMin")]
    public decimal? LiquidityNumMin { get; init; }

    [JsonPropertyName("liquidityNumMax")]
    public decimal? LiquidityNumMax { get; init; }

    [JsonPropertyName("volumeNumMin")]
    public decimal? VolumeNumMin { get; init; }

    [JsonPropertyName("volumeNumMax")]
    public decimal? VolumeNumMax { get; init; }

    [JsonPropertyName("startDateMin")]
    public DateTimeOffset? StartDateMin { get; init; }

    [JsonPropertyName("startDateMax")]
    public DateTimeOffset? StartDateMax { get; init; }

    [JsonPropertyName("endDateMin")]
    public DateTimeOffset? EndDateMin { get; init; }

    [JsonPropertyName("endDateMax")]
    public DateTimeOffset? EndDateMax { get; init; }

    [JsonPropertyName("relatedTags")]
    public bool? RelatedTags { get; init; }

    [JsonPropertyName("tagId")]
    public long? TagId { get; init; }

    [JsonPropertyName("cyom")]
    public bool? Cyom { get; init; }

    [JsonPropertyName("umaResolutionStatus")]
    public string? UmaResolutionStatus { get; init; }

    [JsonPropertyName("gameId")]
    public string? GameId { get; init; }

    [JsonPropertyName("sportsMarketTypes")]
    public IReadOnlyList<string> SportsMarketTypes { get; init; } = [];

    [JsonPropertyName("rewardsMinSize")]
    public decimal? RewardsMinSize { get; init; }

    [JsonPropertyName("questionIds")]
    public IReadOnlyList<string> QuestionIds { get; init; } = [];

    [JsonPropertyName("includeTags")]
    public bool? IncludeTags { get; init; }
}

public sealed record GammaImageOptimization
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("imageUrlSource")]
    public string? ImageUrlSource { get; init; }

    [JsonPropertyName("imageUrlOptimized")]
    public string? ImageUrlOptimized { get; init; }

    [JsonPropertyName("imageSizeKbSource")]
    public decimal? ImageSizeKbSource { get; init; }

    [JsonPropertyName("imageSizeKbOptimized")]
    public decimal? ImageSizeKbOptimized { get; init; }

    [JsonPropertyName("imageOptimizedComplete")]
    public bool? ImageOptimizedComplete { get; init; }

    [JsonPropertyName("imageOptimizedLastUpdated")]
    public string? ImageOptimizedLastUpdated { get; init; }

    [JsonPropertyName("relID")]
    public int? RelId { get; init; }

    [JsonPropertyName("field")]
    public string? Field { get; init; }

    [JsonPropertyName("relname")]
    public string? Relname { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaTeam
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("league")]
    public string? League { get; init; }

    [JsonPropertyName("record")]
    public string? Record { get; init; }

    [JsonPropertyName("logo")]
    public string? Logo { get; init; }

    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; init; }

    [JsonPropertyName("alias")]
    public string? Alias { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaTag
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("forceShow")]
    public bool? ForceShow { get; init; }

    [JsonPropertyName("publishedAt")]
    public string? PublishedAt { get; init; }

    [JsonPropertyName("createdBy")]
    public int? CreatedBy { get; init; }

    [JsonPropertyName("updatedBy")]
    public int? UpdatedBy { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("forceHide")]
    public bool? ForceHide { get; init; }

    [JsonPropertyName("isCarousel")]
    public bool? IsCarousel { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaRelatedTag
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("tagID")]
    public int? TagId { get; init; }

    [JsonPropertyName("relatedTagID")]
    public int? RelatedTagId { get; init; }

    [JsonPropertyName("rank")]
    public int? Rank { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaMarket
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("question")]
    public string? Question { get; init; }

    [JsonPropertyName("conditionId")]
    public string? ConditionId { get; init; }

    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("resolutionSource")]
    public string? ResolutionSource { get; init; }

    [JsonPropertyName("startDate")]
    public DateTimeOffset? StartDate { get; init; }

    [JsonPropertyName("endDate")]
    public DateTimeOffset? EndDate { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("ammType")]
    public string? AmmType { get; init; }

    [JsonPropertyName("liquidity")]
    public string? Liquidity { get; init; }

    [JsonPropertyName("volume")]
    public string? Volume { get; init; }

    [JsonPropertyName("clobTokenIds")]
    public string? ClobTokenIds { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("outcomes")]
    public string? Outcomes { get; init; }

    [JsonPropertyName("outcomePrices")]
    public string? OutcomePrices { get; init; }

    [JsonPropertyName("active")]
    public bool? Active { get; init; }

    [JsonPropertyName("closed")]
    public bool? Closed { get; init; }

    [JsonPropertyName("archived")]
    public bool? Archived { get; init; }

    [JsonPropertyName("featured")]
    public bool? Featured { get; init; }

    [JsonPropertyName("marketType")]
    public string? MarketType { get; init; }

    [JsonPropertyName("formatType")]
    public string? FormatType { get; init; }

    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    [JsonPropertyName("image")]
    public string? Image { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<GammaTag> Tags { get; init; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaMarketDescription
{
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaEvent
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("ticker")]
    public string? Ticker { get; init; }

    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("resolutionSource")]
    public string? ResolutionSource { get; init; }

    [JsonPropertyName("startDate")]
    public DateTimeOffset? StartDate { get; init; }

    [JsonPropertyName("creationDate")]
    public DateTimeOffset? CreationDate { get; init; }

    [JsonPropertyName("endDate")]
    public DateTimeOffset? EndDate { get; init; }

    [JsonPropertyName("image")]
    public string? Image { get; init; }

    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    [JsonPropertyName("active")]
    public bool? Active { get; init; }

    [JsonPropertyName("closed")]
    public bool? Closed { get; init; }

    [JsonPropertyName("archived")]
    public bool? Archived { get; init; }

    [JsonPropertyName("new")]
    public bool? IsNew { get; init; }

    [JsonPropertyName("featured")]
    public bool? Featured { get; init; }

    [JsonPropertyName("restricted")]
    public bool? Restricted { get; init; }

    [JsonPropertyName("liquidity")]
    public decimal? Liquidity { get; init; }

    [JsonPropertyName("volume")]
    public decimal? Volume { get; init; }

    [JsonPropertyName("openInterest")]
    public decimal? OpenInterest { get; init; }

    [JsonPropertyName("sortBy")]
    public string? SortBy { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("subcategory")]
    public string? Subcategory { get; init; }

    [JsonPropertyName("isTemplate")]
    public bool? IsTemplate { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaEventCreator
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("creatorName")]
    public string? CreatorName { get; init; }

    [JsonPropertyName("creatorHandle")]
    public string? CreatorHandle { get; init; }

    [JsonPropertyName("creatorUrl")]
    public string? CreatorUrl { get; init; }

    [JsonPropertyName("creatorImage")]
    public string? CreatorImage { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaSeries
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("ticker")]
    public string? Ticker { get; init; }

    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; init; }

    [JsonPropertyName("seriesType")]
    public string? SeriesType { get; init; }

    [JsonPropertyName("recurrence")]
    public string? Recurrence { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("image")]
    public string? Image { get; init; }

    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    [JsonPropertyName("layout")]
    public string? Layout { get; init; }

    [JsonPropertyName("active")]
    public bool? Active { get; init; }

    [JsonPropertyName("closed")]
    public bool? Closed { get; init; }

    [JsonPropertyName("archived")]
    public bool? Archived { get; init; }

    [JsonPropertyName("new")]
    public bool? IsNew { get; init; }

    [JsonPropertyName("featured")]
    public bool? Featured { get; init; }

    [JsonPropertyName("restricted")]
    public bool? Restricted { get; init; }

    [JsonPropertyName("isTemplate")]
    public bool? IsTemplate { get; init; }

    [JsonPropertyName("publishedAt")]
    public string? PublishedAt { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("volume24hr")]
    public decimal? Volume24Hr { get; init; }

    [JsonPropertyName("volume")]
    public decimal? Volume { get; init; }

    [JsonPropertyName("liquidity")]
    public decimal? Liquidity { get; init; }

    [JsonPropertyName("startDate")]
    public DateTimeOffset? StartDate { get; init; }

    [JsonPropertyName("events")]
    public IReadOnlyList<GammaEvent> Events { get; init; } = [];

    [JsonPropertyName("tags")]
    public IReadOnlyList<GammaTag> Tags { get; init; } = [];

    [JsonPropertyName("commentCount")]
    public int? CommentCount { get; init; }

    [JsonPropertyName("chats")]
    public IReadOnlyList<GammaChat> Chats { get; init; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaSeriesSummary
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("eventDates")]
    public IReadOnlyList<string> EventDates { get; init; } = [];

    [JsonPropertyName("eventWeeks")]
    public IReadOnlyList<int> EventWeeks { get; init; } = [];

    [JsonPropertyName("earliest_open_week")]
    public int? EarliestOpenWeek { get; init; }

    [JsonPropertyName("earliest_open_date")]
    public string? EarliestOpenDate { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaCommentPosition
{
    [JsonPropertyName("tokenId")]
    public string? TokenId { get; init; }

    [JsonPropertyName("positionSize")]
    public string? PositionSize { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaCommentProfile
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("pseudonym")]
    public string? Pseudonym { get; init; }

    [JsonPropertyName("displayUsernamePublic")]
    public bool? DisplayUsernamePublic { get; init; }

    [JsonPropertyName("bio")]
    public string? Bio { get; init; }

    [JsonPropertyName("isMod")]
    public bool? IsMod { get; init; }

    [JsonPropertyName("isCreator")]
    public bool? IsCreator { get; init; }

    [JsonPropertyName("proxyWallet")]
    public string? ProxyWallet { get; init; }

    [JsonPropertyName("baseAddress")]
    public string? BaseAddress { get; init; }

    [JsonPropertyName("profileImage")]
    public string? ProfileImage { get; init; }

    [JsonPropertyName("profileImageOptimized")]
    public GammaImageOptimization? ProfileImageOptimized { get; init; }

    [JsonPropertyName("positions")]
    public IReadOnlyList<GammaCommentPosition> Positions { get; init; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaReaction
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("commentID")]
    public int? CommentId { get; init; }

    [JsonPropertyName("reactionType")]
    public string? ReactionType { get; init; }

    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    [JsonPropertyName("userAddress")]
    public string? UserAddress { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("profile")]
    public GammaCommentProfile? Profile { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaComment
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("body")]
    public string? Body { get; init; }

    [JsonPropertyName("parentEntityType")]
    public string? ParentEntityType { get; init; }

    [JsonPropertyName("parentEntityID")]
    public int? ParentEntityId { get; init; }

    [JsonPropertyName("parentCommentID")]
    public string? ParentCommentId { get; init; }

    [JsonPropertyName("userAddress")]
    public string? UserAddress { get; init; }

    [JsonPropertyName("replyAddress")]
    public string? ReplyAddress { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("profile")]
    public GammaCommentProfile? Profile { get; init; }

    [JsonPropertyName("reactions")]
    public IReadOnlyList<GammaReaction> Reactions { get; init; } = [];

    [JsonPropertyName("reportCount")]
    public int? ReportCount { get; init; }

    [JsonPropertyName("reactionCount")]
    public int? ReactionCount { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaProfile
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("user")]
    public int? User { get; init; }

    [JsonPropertyName("referral")]
    public string? Referral { get; init; }

    [JsonPropertyName("createdBy")]
    public int? CreatedBy { get; init; }

    [JsonPropertyName("updatedBy")]
    public int? UpdatedBy { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("walletActivated")]
    public bool? WalletActivated { get; init; }

    [JsonPropertyName("pseudonym")]
    public string? Pseudonym { get; init; }

    [JsonPropertyName("displayUsernamePublic")]
    public bool? DisplayUsernamePublic { get; init; }

    [JsonPropertyName("profileImage")]
    public string? ProfileImage { get; init; }

    [JsonPropertyName("bio")]
    public string? Bio { get; init; }

    [JsonPropertyName("proxyWallet")]
    public string? ProxyWallet { get; init; }

    [JsonPropertyName("profileImageOptimized")]
    public GammaImageOptimization? ProfileImageOptimized { get; init; }

    [JsonPropertyName("isCloseOnly")]
    public bool? IsCloseOnly { get; init; }

    [JsonPropertyName("isCertReq")]
    public bool? IsCertReq { get; init; }

    [JsonPropertyName("certReqDate")]
    public DateTimeOffset? CertReqDate { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaPublicProfileUser
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("creator")]
    public bool Creator { get; init; }

    [JsonPropertyName("mod")]
    public bool Mod { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaPublicProfileResponse
{
    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("proxyWallet")]
    public string? ProxyWallet { get; init; }

    [JsonPropertyName("profileImage")]
    public string? ProfileImage { get; init; }

    [JsonPropertyName("displayUsernamePublic")]
    public bool? DisplayUsernamePublic { get; init; }

    [JsonPropertyName("bio")]
    public string? Bio { get; init; }

    [JsonPropertyName("pseudonym")]
    public string? Pseudonym { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("users")]
    public IReadOnlyList<GammaPublicProfileUser> Users { get; init; } = [];

    [JsonPropertyName("xUsername")]
    public string? XUsername { get; init; }

    [JsonPropertyName("verifiedBadge")]
    public bool? VerifiedBadge { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaPublicProfileError
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaChat
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("channelId")]
    public string? ChannelId { get; init; }

    [JsonPropertyName("channelName")]
    public string? ChannelName { get; init; }

    [JsonPropertyName("channelImage")]
    public string? ChannelImage { get; init; }

    [JsonPropertyName("live")]
    public bool? Live { get; init; }

    [JsonPropertyName("startTime")]
    public DateTimeOffset? StartTime { get; init; }

    [JsonPropertyName("endTime")]
    public DateTimeOffset? EndTime { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaPagination
{
    [JsonPropertyName("hasMore")]
    public bool HasMore { get; init; }

    [JsonPropertyName("totalResults")]
    public int TotalResults { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaEventsPaginationResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<GammaEvent> Data { get; init; } = [];

    [JsonPropertyName("pagination")]
    public GammaPagination? Pagination { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaCount
{
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaEventTweetCount
{
    [JsonPropertyName("tweetCount")]
    public int TweetCount { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaSearchTag
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("event_count")]
    public int EventCount { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaSearchResponse
{
    [JsonPropertyName("events")]
    public IReadOnlyList<GammaEvent> Events { get; init; } = [];

    [JsonPropertyName("tags")]
    public IReadOnlyList<GammaSearchTag> Tags { get; init; } = [];

    [JsonPropertyName("profiles")]
    public IReadOnlyList<GammaProfile> Profiles { get; init; } = [];

    [JsonPropertyName("pagination")]
    public GammaPagination? Pagination { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaSportsMetadata
{
    [JsonPropertyName("sport")]
    public string Sport { get; init; } = string.Empty;

    [JsonPropertyName("image")]
    public string? Image { get; init; }

    [JsonPropertyName("resolution")]
    public string? Resolution { get; init; }

    [JsonPropertyName("ordering")]
    public string? Ordering { get; init; }

    [JsonPropertyName("tags")]
    public string? Tags { get; init; }

    [JsonPropertyName("series")]
    public string? Series { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaSportsMarketTypesResponse
{
    [JsonPropertyName("marketTypes")]
    public IReadOnlyList<string> MarketTypes { get; init; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaKeysetMarketsResponse
{
    [JsonPropertyName("markets")]
    public IReadOnlyList<GammaMarket> Markets { get; init; } = [];

    [JsonPropertyName("next_cursor")]
    public string? NextCursor { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}

public sealed record GammaKeysetEventsResponse
{
    [JsonPropertyName("events")]
    public IReadOnlyList<GammaEvent> Events { get; init; } = [];

    [JsonPropertyName("next_cursor")]
    public string? NextCursor { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
}
