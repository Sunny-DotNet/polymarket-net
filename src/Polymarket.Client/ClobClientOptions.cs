namespace Polymarket.Client;

public sealed record ClobClientOptions
{
    public Uri Host { get; init; } = new(PolymarketHosts.Clob, UriKind.Absolute);

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
