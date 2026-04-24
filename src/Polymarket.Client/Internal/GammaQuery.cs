using System.Globalization;

namespace Polymarket.Client.Internal;

internal static class GammaQuery
{
    public static string Append(string path, IReadOnlyList<KeyValuePair<string, string?>>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
        {
            return path;
        }

        List<string> pairs = [];
        foreach ((string key, string? value) in parameters)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            pairs.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }

        if (pairs.Count == 0)
        {
            return path;
        }

        return $"{path}?{string.Join("&", pairs)}";
    }

    public static List<KeyValuePair<string, string?>> From(GammaTeamQueryParameters? parameters)
    {
        List<KeyValuePair<string, string?>> query = [];
        if (parameters is null)
        {
            return query;
        }

        AddPagination(query, parameters);
        AddMany(query, "league", parameters.League);
        AddMany(query, "name", parameters.Name);
        AddMany(query, "abbreviation", parameters.Abbreviation);
        AddAdditional(query, parameters);
        return query;
    }

    public static List<KeyValuePair<string, string?>> From(GammaTagQueryParameters? parameters)
    {
        List<KeyValuePair<string, string?>> query = [];
        if (parameters is null)
        {
            return query;
        }

        AddPagination(query, parameters);
        AddValue(query, "include_template", parameters.IncludeTemplate);
        AddValue(query, "is_carousel", parameters.IsCarousel);
        AddAdditional(query, parameters);
        return query;
    }

    public static List<KeyValuePair<string, string?>> From(GammaRelatedTagQueryParameters? parameters)
    {
        List<KeyValuePair<string, string?>> query = [];
        if (parameters is null)
        {
            return query;
        }

        AddValue(query, "omit_empty", parameters.OmitEmpty);
        AddValue(query, "status", parameters.Status);
        AddAdditional(query, parameters);
        return query;
    }

    public static List<KeyValuePair<string, string?>> From(GammaEventQueryParameters? parameters)
    {
        List<KeyValuePair<string, string?>> query = [];
        if (parameters is null)
        {
            return query;
        }

        AddPagination(query, parameters);
        AddMany(query, "id", parameters.Id);
        AddValue(query, "tag_id", parameters.TagId);
        AddMany(query, "exclude_tag_id", parameters.ExcludeTagId);
        AddMany(query, "slug", parameters.Slug);
        AddValue(query, "tag_slug", parameters.TagSlug);
        AddValue(query, "related_tags", parameters.RelatedTags);
        AddValue(query, "active", parameters.Active);
        AddValue(query, "archived", parameters.Archived);
        AddValue(query, "featured", parameters.Featured);
        AddValue(query, "cyom", parameters.Cyom);
        AddValue(query, "include_chat", parameters.IncludeChat);
        AddValue(query, "include_template", parameters.IncludeTemplate);
        AddValue(query, "recurrence", parameters.Recurrence);
        AddValue(query, "closed", parameters.Closed);
        AddValue(query, "liquidity_min", parameters.LiquidityMin);
        AddValue(query, "liquidity_max", parameters.LiquidityMax);
        AddValue(query, "volume_min", parameters.VolumeMin);
        AddValue(query, "volume_max", parameters.VolumeMax);
        AddValue(query, "start_date_min", parameters.StartDateMin);
        AddValue(query, "start_date_max", parameters.StartDateMax);
        AddValue(query, "end_date_min", parameters.EndDateMin);
        AddValue(query, "end_date_max", parameters.EndDateMax);
        AddAdditional(query, parameters);
        return query;
    }

    public static List<KeyValuePair<string, string?>> From(GammaEventPaginationQueryParameters? parameters)
    {
        List<KeyValuePair<string, string?>> query = [];
        if (parameters is null)
        {
            return query;
        }

        AddPagination(query, parameters);
        AddValue(query, "include_chat", parameters.IncludeChat);
        AddValue(query, "include_template", parameters.IncludeTemplate);
        AddValue(query, "recurrence", parameters.Recurrence);
        AddAdditional(query, parameters);
        return query;
    }

    public static List<KeyValuePair<string, string?>> From(GammaEventCreatorQueryParameters? parameters)
    {
        List<KeyValuePair<string, string?>> query = [];
        if (parameters is null)
        {
            return query;
        }

        AddPagination(query, parameters);
        AddValue(query, "creator_name", parameters.CreatorName);
        AddValue(query, "creator_handle", parameters.CreatorHandle);
        AddAdditional(query, parameters);
        return query;
    }

    public static List<KeyValuePair<string, string?>> From(GammaMarketQueryParameters? parameters)
    {
        List<KeyValuePair<string, string?>> query = [];
        if (parameters is null)
        {
            return query;
        }

        AddPagination(query, parameters);
        AddMany(query, "id", parameters.Id);
        AddMany(query, "slug", parameters.Slug);
        AddMany(query, "clob_token_ids", parameters.ClobTokenIds);
        AddMany(query, "condition_ids", parameters.ConditionIds);
        AddMany(query, "market_maker_address", parameters.MarketMakerAddress);
        AddValue(query, "liquidity_num_min", parameters.LiquidityNumMin);
        AddValue(query, "liquidity_num_max", parameters.LiquidityNumMax);
        AddValue(query, "volume_num_min", parameters.VolumeNumMin);
        AddValue(query, "volume_num_max", parameters.VolumeNumMax);
        AddValue(query, "start_date_min", parameters.StartDateMin);
        AddValue(query, "start_date_max", parameters.StartDateMax);
        AddValue(query, "end_date_min", parameters.EndDateMin);
        AddValue(query, "end_date_max", parameters.EndDateMax);
        AddValue(query, "tag_id", parameters.TagId);
        AddValue(query, "related_tags", parameters.RelatedTags);
        AddValue(query, "cyom", parameters.Cyom);
        AddValue(query, "uma_resolution_status", parameters.UmaResolutionStatus);
        AddValue(query, "game_id", parameters.GameId);
        AddMany(query, "sports_market_types", parameters.SportsMarketTypes);
        AddValue(query, "rewards_min_size", parameters.RewardsMinSize);
        AddMany(query, "question_ids", parameters.QuestionIds);
        AddValue(query, "include_tag", parameters.IncludeTag);
        AddValue(query, "closed", parameters.Closed);
        AddAdditional(query, parameters);
        return query;
    }

    public static List<KeyValuePair<string, string?>> From(GammaMarketKeysetQueryParameters? parameters)
    {
        List<KeyValuePair<string, string?>> query = [];
        if (parameters is null)
        {
            return query;
        }

        AddKeyset(query, parameters);
        AddMany(query, "id", parameters.Id);
        AddMany(query, "slug", parameters.Slug);
        AddValue(query, "closed", parameters.Closed);
        AddValue(query, "decimalized", parameters.Decimalized);
        AddMany(query, "clob_token_ids", parameters.ClobTokenIds);
        AddMany(query, "condition_ids", parameters.ConditionIds);
        AddMany(query, "question_ids", parameters.QuestionIds);
        AddMany(query, "market_maker_address", parameters.MarketMakerAddress);
        AddValue(query, "liquidity_num_min", parameters.LiquidityNumMin);
        AddValue(query, "liquidity_num_max", parameters.LiquidityNumMax);
        AddValue(query, "volume_num_min", parameters.VolumeNumMin);
        AddValue(query, "volume_num_max", parameters.VolumeNumMax);
        AddValue(query, "start_date_min", parameters.StartDateMin);
        AddValue(query, "start_date_max", parameters.StartDateMax);
        AddValue(query, "end_date_min", parameters.EndDateMin);
        AddValue(query, "end_date_max", parameters.EndDateMax);
        AddMany(query, "tag_id", parameters.TagId);
        AddValue(query, "related_tags", parameters.RelatedTags);
        AddValue(query, "tag_match", parameters.TagMatch);
        AddValue(query, "cyom", parameters.Cyom);
        AddValue(query, "rfq_enabled", parameters.RfqEnabled);
        AddValue(query, "uma_resolution_status", parameters.UmaResolutionStatus);
        AddValue(query, "game_id", parameters.GameId);
        AddMany(query, "sports_market_types", parameters.SportsMarketTypes);
        AddValue(query, "include_tag", parameters.IncludeTag);
        AddValue(query, "locale", parameters.Locale);
        AddAdditional(query, parameters);
        return query;
    }

    public static List<KeyValuePair<string, string?>> From(GammaEventKeysetQueryParameters? parameters)
    {
        List<KeyValuePair<string, string?>> query = [];
        if (parameters is null)
        {
            return query;
        }

        AddKeyset(query, parameters);
        AddMany(query, "id", parameters.Id);
        AddMany(query, "slug", parameters.Slug);
        AddValue(query, "closed", parameters.Closed);
        AddValue(query, "live", parameters.Live);
        AddValue(query, "featured", parameters.Featured);
        AddValue(query, "cyom", parameters.Cyom);
        AddValue(query, "title_search", parameters.TitleSearch);
        AddValue(query, "liquidity_min", parameters.LiquidityMin);
        AddValue(query, "liquidity_max", parameters.LiquidityMax);
        AddValue(query, "volume_min", parameters.VolumeMin);
        AddValue(query, "volume_max", parameters.VolumeMax);
        AddValue(query, "start_date_min", parameters.StartDateMin);
        AddValue(query, "start_date_max", parameters.StartDateMax);
        AddValue(query, "end_date_min", parameters.EndDateMin);
        AddValue(query, "end_date_max", parameters.EndDateMax);
        AddValue(query, "start_time_min", parameters.StartTimeMin);
        AddValue(query, "start_time_max", parameters.StartTimeMax);
        AddMany(query, "tag_id", parameters.TagId);
        AddValue(query, "tag_slug", parameters.TagSlug);
        AddMany(query, "exclude_tag_id", parameters.ExcludeTagId);
        AddValue(query, "related_tags", parameters.RelatedTags);
        AddValue(query, "tag_match", parameters.TagMatch);
        AddMany(query, "series_id", parameters.SeriesId);
        AddMany(query, "game_id", parameters.GameId);
        AddValue(query, "event_date", parameters.EventDate);
        AddValue(query, "event_week", parameters.EventWeek);
        AddValue(query, "featured_order", parameters.FeaturedOrder);
        AddValue(query, "recurrence", parameters.Recurrence);
        AddMany(query, "created_by", parameters.CreatedBy);
        AddValue(query, "parent_event_id", parameters.ParentEventId);
        AddValue(query, "include_children", parameters.IncludeChildren);
        AddValue(query, "partner_slug", parameters.PartnerSlug);
        AddValue(query, "include_chat", parameters.IncludeChat);
        AddValue(query, "include_template", parameters.IncludeTemplate);
        AddValue(query, "include_best_lines", parameters.IncludeBestLines);
        AddValue(query, "locale", parameters.Locale);
        AddAdditional(query, parameters);
        return query;
    }

    public static List<KeyValuePair<string, string?>> From(GammaSeriesQueryParameters? parameters)
    {
        List<KeyValuePair<string, string?>> query = [];
        if (parameters is null)
        {
            return query;
        }

        AddPagination(query, parameters);
        AddMany(query, "slug", parameters.Slug);
        AddMany(query, "categories_ids", parameters.CategoriesIds);
        AddMany(query, "categories_labels", parameters.CategoriesLabels);
        AddValue(query, "closed", parameters.Closed);
        AddValue(query, "include_chat", parameters.IncludeChat);
        AddValue(query, "recurrence", parameters.Recurrence);
        AddValue(query, "exclude_events", parameters.ExcludeEvents);
        AddAdditional(query, parameters);
        return query;
    }

    public static List<KeyValuePair<string, string?>> From(GammaCommentQueryParameters? parameters)
    {
        List<KeyValuePair<string, string?>> query = [];
        if (parameters is null)
        {
            return query;
        }

        AddPagination(query, parameters);
        AddValue(query, "parent_entity_type", parameters.ParentEntityType);
        AddValue(query, "parent_entity_id", parameters.ParentEntityId);
        AddValue(query, "get_positions", parameters.GetPositions);
        AddValue(query, "holders_only", parameters.HoldersOnly);
        AddAdditional(query, parameters);
        return query;
    }

    public static List<KeyValuePair<string, string?>> From(GammaCommentsByUserQueryParameters? parameters)
    {
        List<KeyValuePair<string, string?>> query = [];
        if (parameters is null)
        {
            return query;
        }

        AddPagination(query, parameters);
        AddAdditional(query, parameters);
        return query;
    }

    public static List<KeyValuePair<string, string?>> From(GammaPublicSearchQueryParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        List<KeyValuePair<string, string?>> query = [];
        AddValue(query, "q", parameters.Query);
        AddValue(query, "cache", parameters.Cache);
        AddValue(query, "events_status", parameters.EventsStatus);
        AddValue(query, "limit_per_type", parameters.LimitPerType);
        AddValue(query, "page", parameters.Page);
        AddMany(query, "events_tag", parameters.EventsTag);
        AddValue(query, "keep_closed_markets", parameters.KeepClosedMarkets);
        AddValue(query, "sort", parameters.Sort);
        AddValue(query, "ascending", parameters.Ascending);
        AddValue(query, "search_tags", parameters.SearchTags);
        AddValue(query, "search_profiles", parameters.SearchProfiles);
        AddValue(query, "recurrence", parameters.Recurrence);
        AddMany(query, "exclude_tag_id", parameters.ExcludeTagId);
        AddValue(query, "optimized", parameters.Optimized);
        AddAdditional(query, parameters);
        return query;
    }

    public static List<KeyValuePair<string, string?>> FromOptional(params (string Key, object? Value)[] parameters)
    {
        List<KeyValuePair<string, string?>> query = [];
        foreach ((string key, object? value) in parameters)
        {
            AddValue(query, key, value);
        }

        return query;
    }

    private static void AddPagination(List<KeyValuePair<string, string?>> query, GammaPaginationQueryParameters parameters)
    {
        AddValue(query, "limit", parameters.Limit);
        AddValue(query, "offset", parameters.Offset);
        AddValue(query, "order", parameters.Order);
        AddValue(query, "ascending", parameters.Ascending);
    }

    private static void AddKeyset(List<KeyValuePair<string, string?>> query, GammaKeysetQueryParameters parameters)
    {
        AddValue(query, "limit", parameters.Limit);
        AddValue(query, "order", parameters.Order);
        AddValue(query, "ascending", parameters.Ascending);
        AddValue(query, "after_cursor", parameters.AfterCursor);
    }

    private static void AddMany<T>(List<KeyValuePair<string, string?>> query, string key, IReadOnlyList<T> values)
    {
        foreach (T value in values)
        {
            AddValue(query, key, value);
        }
    }

    private static void AddAdditional(List<KeyValuePair<string, string?>> query, GammaQueryParameters parameters)
    {
        foreach ((string key, string? value) in parameters.AdditionalParameters)
        {
            AddValue(query, key, value);
        }
    }

    private static void AddValue(List<KeyValuePair<string, string?>> query, string key, object? value)
    {
        string? formatted = FormatValue(value);
        if (string.IsNullOrWhiteSpace(formatted))
        {
            return;
        }

        query.Add(new KeyValuePair<string, string?>(key, formatted));
    }

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        string text => text,
        bool boolean => boolean ? "true" : "false",
        DateTimeOffset timestamp => timestamp.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture),
    };
}
