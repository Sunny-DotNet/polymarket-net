using System.Text.Json.Serialization;

namespace Polymarket.Client;

public sealed record ClobWebSocketAuthentication(
    [property: JsonPropertyName("apiKey")] string ApiKey,
    [property: JsonPropertyName("secret")] string Secret,
    [property: JsonPropertyName("passphrase")] string Passphrase)
{
    public static ClobWebSocketAuthentication FromApiCredentials(ApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return new ClobWebSocketAuthentication(credentials.Key, credentials.Secret, credentials.Passphrase);
    }
}

[JsonConverter(typeof(Internal.ClobWebSocketSubscriptionOperationJsonConverter))]
public enum ClobWebSocketSubscriptionOperation
{
    Subscribe,
    Unsubscribe,
}

[JsonConverter(typeof(Internal.ClobMarketSubscriptionLevelJsonConverter))]
public enum ClobMarketSubscriptionLevel
{
    Level1 = 1,
    Level2 = 2,
    Level3 = 3,
}

public sealed record ClobMarketSubscriptionRequest
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "market";

    [JsonPropertyName("assets_ids")]
    public IReadOnlyList<string> AssetIds { get; init; } = [];

    [JsonPropertyName("initial_dump")]
    public bool InitialDump { get; init; } = true;

    [JsonPropertyName("level")]
    [JsonConverter(typeof(Internal.ClobMarketSubscriptionLevelJsonConverter))]
    public ClobMarketSubscriptionLevel Level { get; init; } = ClobMarketSubscriptionLevel.Level2;

    [JsonPropertyName("custom_feature_enabled")]
    public bool? CustomFeatureEnabled { get; init; }
}

public sealed record ClobMarketSubscriptionUpdate
{
    [JsonPropertyName("operation")]
    [JsonConverter(typeof(Internal.ClobWebSocketSubscriptionOperationJsonConverter))]
    public ClobWebSocketSubscriptionOperation Operation { get; init; }

    [JsonPropertyName("assets_ids")]
    public IReadOnlyList<string> AssetIds { get; init; } = [];

    [JsonPropertyName("level")]
    [JsonConverter(typeof(Internal.NullableClobMarketSubscriptionLevelJsonConverter))]
    public ClobMarketSubscriptionLevel? Level { get; init; }

    [JsonPropertyName("custom_feature_enabled")]
    public bool? CustomFeatureEnabled { get; init; }
}

public sealed record ClobUserSubscriptionRequest
{
    [JsonPropertyName("auth")]
    public ClobWebSocketAuthentication Auth { get; init; } = new(string.Empty, string.Empty, string.Empty);

    [JsonPropertyName("type")]
    public string Type { get; init; } = "user";

    [JsonPropertyName("markets")]
    public IReadOnlyList<string>? Markets { get; init; }
}

public sealed record ClobUserSubscriptionUpdate
{
    [JsonPropertyName("operation")]
    [JsonConverter(typeof(Internal.ClobWebSocketSubscriptionOperationJsonConverter))]
    public ClobWebSocketSubscriptionOperation Operation { get; init; }

    [JsonPropertyName("markets")]
    public IReadOnlyList<string> Markets { get; init; } = [];
}
