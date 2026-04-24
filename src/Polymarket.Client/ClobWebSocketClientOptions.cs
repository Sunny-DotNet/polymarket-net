namespace Polymarket.Client;

public sealed record ClobWebSocketClientOptions
{
    public Uri Host { get; init; } = new("wss://ws-subscriptions-clob.polymarket.com/ws/", UriKind.Absolute);

    public Uri SportsHost { get; init; } = new("wss://sports-api.polymarket.com/", UriKind.Absolute);

    public TimeSpan PingInterval { get; init; } = TimeSpan.FromSeconds(10);

    public bool AutoReconnect { get; init; } = true;

    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(3);

    public int? MaxReconnectAttempts { get; init; }

    public int ReceiveBufferSize { get; init; } = 16 * 1024;
}
