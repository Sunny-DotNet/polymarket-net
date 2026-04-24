namespace Polymarket.Client;

public sealed record ClobClientOptions
{
    public string Host { get; init; } = PolymarketHosts.Clob;

    public Chain Chain { get; init; } = Chain.Polygon;

    public string? PrivateKey { get; init; }

    public ApiCredentials? Credentials { get; init; }

    public SignatureTypeV2 SignatureType { get; init; } = SignatureTypeV2.Eoa;

    public string? FunderAddress { get; init; }

    public bool UseServerTime { get; init; }

    public BuilderConfig? BuilderConfig { get; init; }

    public bool RetryOnError { get; init; } = true;

    public bool ThrowOnError { get; init; } = true;
}
