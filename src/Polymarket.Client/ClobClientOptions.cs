namespace Polymarket.Client;

public sealed record ClobClientOptions
{
    public required Uri Host { get; init; }

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
