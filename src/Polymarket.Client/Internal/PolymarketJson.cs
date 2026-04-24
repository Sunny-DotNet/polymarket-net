using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Polymarket.Client.Internal;

internal static class PolymarketJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    static PolymarketJson()
    {
        Options.Converters.Add(new ApiOperationResultJsonConverter());
        Options.Converters.Add(new SideJsonConverter());
        Options.Converters.Add(new OrderTypeJsonConverter());
        Options.Converters.Add(new AssetTypeJsonConverter());
        Options.Converters.Add(new PriceHistoryIntervalJsonConverter());
        Options.Converters.Add(new JsonStringEnumConverter());
    }

    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, Options);

    public static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options)
        ?? throw new InvalidOperationException($"Failed to deserialize payload as {typeof(T).Name}.");

    public static async Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken) =>
        await JsonSerializer.DeserializeAsync<T>(stream, Options, cancellationToken).ConfigureAwait(false)
        ?? throw new InvalidOperationException($"Failed to deserialize payload as {typeof(T).Name}.");

    public static StringContent CreateJsonContent<T>(T value) =>
        new(Serialize(value), Encoding.UTF8, "application/json");

    public static string ToInvariantString(this decimal value) =>
        value.ToString(CultureInfo.InvariantCulture);

    public static string ToInvariantString(this long value) =>
        value.ToString(CultureInfo.InvariantCulture);
}

internal static class PolymarketEnumExtensions
{
    public static string ToApiString(this Side value) => value switch
    {
        Side.Buy => "BUY",
        Side.Sell => "SELL",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static string ToApiString(this OrderType value) => value switch
    {
        OrderType.Gtc => "GTC",
        OrderType.Fok => "FOK",
        OrderType.Gtd => "GTD",
        OrderType.Fak => "FAK",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static string ToApiString(this AssetType value) => value switch
    {
        AssetType.Collateral => "COLLATERAL",
        AssetType.Conditional => "CONDITIONAL",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static string ToApiString(this PriceHistoryInterval value) => value switch
    {
        PriceHistoryInterval.Max => "max",
        PriceHistoryInterval.OneWeek => "1w",
        PriceHistoryInterval.OneDay => "1d",
        PriceHistoryInterval.SixHours => "6h",
        PriceHistoryInterval.OneHour => "1h",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    public static Side ParseSide(string value) => value.ToUpperInvariant() switch
    {
        "BUY" => Side.Buy,
        "SELL" => Side.Sell,
        _ => throw new JsonException($"Unsupported side value '{value}'."),
    };

    public static OrderType ParseOrderType(string value) => value.ToUpperInvariant() switch
    {
        "GTC" => OrderType.Gtc,
        "FOK" => OrderType.Fok,
        "GTD" => OrderType.Gtd,
        "FAK" => OrderType.Fak,
        _ => throw new JsonException($"Unsupported order type value '{value}'."),
    };

    public static AssetType ParseAssetType(string value) => value.ToUpperInvariant() switch
    {
        "COLLATERAL" => AssetType.Collateral,
        "CONDITIONAL" => AssetType.Conditional,
        _ => throw new JsonException($"Unsupported asset type value '{value}'."),
    };

    public static PriceHistoryInterval ParsePriceHistoryInterval(string value) => value.ToLowerInvariant() switch
    {
        "max" => PriceHistoryInterval.Max,
        "1w" => PriceHistoryInterval.OneWeek,
        "1d" => PriceHistoryInterval.OneDay,
        "6h" => PriceHistoryInterval.SixHours,
        "1h" => PriceHistoryInterval.OneHour,
        _ => throw new JsonException($"Unsupported interval value '{value}'."),
    };
}

internal sealed class SideJsonConverter : JsonConverter<Side>
{
    public override Side Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        PolymarketEnumExtensions.ParseSide(reader.GetString() ?? string.Empty);

    public override void Write(Utf8JsonWriter writer, Side value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToApiString());
}

internal sealed class OrderTypeJsonConverter : JsonConverter<OrderType>
{
    public override OrderType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        PolymarketEnumExtensions.ParseOrderType(reader.GetString() ?? string.Empty);

    public override void Write(Utf8JsonWriter writer, OrderType value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToApiString());
}

internal sealed class AssetTypeJsonConverter : JsonConverter<AssetType>
{
    public override AssetType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        PolymarketEnumExtensions.ParseAssetType(reader.GetString() ?? string.Empty);

    public override void Write(Utf8JsonWriter writer, AssetType value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToApiString());
}

internal sealed class PriceHistoryIntervalJsonConverter : JsonConverter<PriceHistoryInterval>
{
    public override PriceHistoryInterval Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        PolymarketEnumExtensions.ParsePriceHistoryInterval(reader.GetString() ?? string.Empty);

    public override void Write(Utf8JsonWriter writer, PriceHistoryInterval value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToApiString());
}

internal sealed class ApiOperationResultJsonConverter : JsonConverter<ApiOperationResult>
{
    public override ApiOperationResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True or JsonTokenType.False => new ApiOperationResult
            {
                Success = reader.GetBoolean(),
            },
            JsonTokenType.StartObject => ReadObject(ref reader, options),
            JsonTokenType.Null => new ApiOperationResult(),
            _ => throw new JsonException($"Unsupported token {reader.TokenType} for {nameof(ApiOperationResult)}."),
        };
    }

    public override void Write(Utf8JsonWriter writer, ApiOperationResult value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.Success.HasValue)
        {
            writer.WriteBoolean("success", value.Success.Value);
        }

        if (value.Status is not null)
        {
            writer.WriteString("status", value.Status);
        }

        if (value.Message is not null)
        {
            writer.WriteString("message", value.Message);
        }

        if (value.Error is not null)
        {
            writer.WriteString("error", value.Error);
        }

        foreach ((string key, JsonElement element) in value.ExtensionData)
        {
            writer.WritePropertyName(key);
            element.WriteTo(writer);
        }

        writer.WriteEndObject();
    }

    private static ApiOperationResult ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        ApiOperationResultObject payload = JsonSerializer.Deserialize<ApiOperationResultObject>(document.RootElement.GetRawText(), options)
            ?? throw new JsonException($"Failed to deserialize {nameof(ApiOperationResult)}.");

        return new ApiOperationResult
        {
            Success = payload.Success,
            Status = payload.Status,
            Message = payload.Message,
            Error = payload.Error,
            ExtensionData = payload.ExtensionData,
        };
    }

    private sealed record ApiOperationResultObject
    {
        [JsonPropertyName("success")]
        public bool? Success { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("error")]
        public string? Error { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> ExtensionData { get; init; } = [];
    }
}
