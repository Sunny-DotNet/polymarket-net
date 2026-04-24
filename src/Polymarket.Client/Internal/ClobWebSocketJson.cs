using System.Text.Json;
using System.Text.Json.Serialization;

namespace Polymarket.Client.Internal;

internal static class ClobWebSocketJson
{
    public static IReadOnlyList<TMessage> DeserializeMany<TMessage>(string payload, Func<string, TMessage> deserialize)
    {
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            List<TMessage> messages = [];
            foreach (JsonElement item in root.EnumerateArray())
            {
                messages.Add(deserialize(item.GetRawText()));
            }

            return messages;
        }

        return [deserialize(payload)];
    }
}

internal sealed class ClobMarketChannelMessageJsonConverter : JsonConverter<ClobMarketChannelMessage>
{
    public override ClobMarketChannelMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        string? eventType = root.TryGetProperty("event_type", out JsonElement eventTypeProperty)
            ? eventTypeProperty.GetString()
            : null;

        string json = root.GetRawText();
        return eventType switch
        {
            "book" => JsonSerializer.Deserialize<ClobMarketBookMessage>(json, options)!,
            "price_change" => JsonSerializer.Deserialize<ClobMarketPriceChangeMessage>(json, options)!,
            "tick_size_change" => JsonSerializer.Deserialize<ClobMarketTickSizeChangeMessage>(json, options)!,
            "last_trade_price" => JsonSerializer.Deserialize<ClobMarketLastTradePriceMessage>(json, options)!,
            "best_bid_ask" => JsonSerializer.Deserialize<ClobMarketBestBidAskMessage>(json, options)!,
            "new_market" => JsonSerializer.Deserialize<ClobNewMarketMessage>(json, options)!,
            "market_resolved" => JsonSerializer.Deserialize<ClobMarketResolvedMessage>(json, options)!,
            _ => JsonSerializer.Deserialize<ClobUnknownMarketChannelMessage>(json, options)!,
        };
    }

    public override void Write(Utf8JsonWriter writer, ClobMarketChannelMessage value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
}

internal sealed class ClobUserChannelMessageJsonConverter : JsonConverter<ClobUserChannelMessage>
{
    public override ClobUserChannelMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        string? eventType = root.TryGetProperty("event_type", out JsonElement eventTypeProperty)
            ? eventTypeProperty.GetString()
            : null;

        string json = root.GetRawText();
        return eventType switch
        {
            "trade" => JsonSerializer.Deserialize<ClobTradeMessage>(json, options)!,
            "order" => JsonSerializer.Deserialize<ClobOrderMessage>(json, options)!,
            _ => JsonSerializer.Deserialize<ClobUnknownUserChannelMessage>(json, options)!,
        };
    }

    public override void Write(Utf8JsonWriter writer, ClobUserChannelMessage value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
}

internal sealed class ClobWebSocketSubscriptionOperationJsonConverter : JsonConverter<ClobWebSocketSubscriptionOperation>
{
    public override ClobWebSocketSubscriptionOperation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        (reader.GetString() ?? string.Empty).ToLowerInvariant() switch
        {
            "subscribe" => ClobWebSocketSubscriptionOperation.Subscribe,
            "unsubscribe" => ClobWebSocketSubscriptionOperation.Unsubscribe,
            _ => throw new JsonException($"Unsupported websocket subscription operation '{reader.GetString()}'."),
        };

    public override void Write(Utf8JsonWriter writer, ClobWebSocketSubscriptionOperation value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            ClobWebSocketSubscriptionOperation.Subscribe => "subscribe",
            ClobWebSocketSubscriptionOperation.Unsubscribe => "unsubscribe",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
        });
}

internal sealed class ClobMarketSubscriptionLevelJsonConverter : JsonConverter<ClobMarketSubscriptionLevel>
{
    public override ClobMarketSubscriptionLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int numericLevel))
        {
            return Parse(numericLevel);
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            if (int.TryParse(value, out int parsedLevel))
            {
                return Parse(parsedLevel);
            }
        }

        throw new JsonException("Websocket market subscription level must be 1, 2, or 3.");
    }

    public override void Write(Utf8JsonWriter writer, ClobMarketSubscriptionLevel value, JsonSerializerOptions options) =>
        writer.WriteNumberValue((int)value);

    private static ClobMarketSubscriptionLevel Parse(int value) => value switch
    {
        1 => ClobMarketSubscriptionLevel.Level1,
        2 => ClobMarketSubscriptionLevel.Level2,
        3 => ClobMarketSubscriptionLevel.Level3,
        _ => throw new JsonException($"Unsupported websocket market subscription level '{value}'."),
    };
}

internal sealed class NullableClobMarketSubscriptionLevelJsonConverter : JsonConverter<ClobMarketSubscriptionLevel?>
{
    private static readonly ClobMarketSubscriptionLevelJsonConverter Inner = new();

    public override ClobMarketSubscriptionLevel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null
            ? null
            : Inner.Read(ref reader, typeof(ClobMarketSubscriptionLevel), options);

    public override void Write(Utf8JsonWriter writer, ClobMarketSubscriptionLevel? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            Inner.Write(writer, value.Value, options);
            return;
        }

        writer.WriteNullValue();
    }
}
