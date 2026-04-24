namespace Polymarket.Client;

public sealed record ClobWebSocketClientOptions
{
    public string Host { get; init; } = PolymarketHosts.ClobWebSocket;

    public string SportsHost { get; init; } = PolymarketHosts.SportsWebSocket;

    public TimeSpan PingInterval { get; init; } = TimeSpan.FromSeconds(10);

    public bool AutoReconnect { get; init; } = true;

    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(3);

    public int? MaxReconnectAttempts { get; init; }

    public int ReceiveBufferSize { get; init; } = 16 * 1024;
}
