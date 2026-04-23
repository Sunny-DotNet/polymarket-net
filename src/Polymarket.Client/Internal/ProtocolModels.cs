using System.Text.Json.Serialization;

namespace Polymarket.Client.Internal;

internal sealed record VersionResponse
{
    [JsonPropertyName("version")]
    public int Version { get; init; }
}

internal sealed record ServerTimeResponse
{
    [JsonPropertyName("time")]
    public long? Time { get; init; }

    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; init; }
}

internal sealed record ApiCredentialsRaw
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; init; } = string.Empty;

    [JsonPropertyName("secret")]
    public string Secret { get; init; } = string.Empty;

    [JsonPropertyName("passphrase")]
    public string Passphrase { get; init; } = string.Empty;
}

internal sealed record TickSizeResponse
{
    [JsonPropertyName("minimum_tick_size")]
    public decimal MinimumTickSize { get; init; }
}

internal sealed record NegRiskResponse
{
    [JsonPropertyName("neg_risk")]
    public bool NegRisk { get; init; }
}

internal sealed record FeeRateResponse
{
    [JsonPropertyName("base_fee")]
    public int BaseFee { get; init; }
}

internal sealed record BuilderFeeRateResponse
{
    [JsonPropertyName("builder_maker_fee_rate_bps")]
    public int BuilderMakerFeeRateBps { get; init; }

    [JsonPropertyName("builder_taker_fee_rate_bps")]
    public int BuilderTakerFeeRateBps { get; init; }
}

internal sealed record MarketByTokenResponse
{
    [JsonPropertyName("condition_id")]
    public string ConditionId { get; init; } = string.Empty;
}

internal sealed record ClobErrorResponseBody
{
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;
}

internal sealed record PaginatedData<T>
{
    [JsonPropertyName("data")]
    public IReadOnlyList<T> Data { get; init; } = [];

    [JsonPropertyName("next_cursor")]
    public string NextCursor { get; init; } = PolymarketConstants.EndCursor;

    [JsonPropertyName("limit")]
    public int Limit { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }
}
