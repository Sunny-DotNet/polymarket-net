namespace Polymarket.Client;

public sealed record GammaClientOptions
{
    public Uri Host { get; init; } = new(PolymarketHosts.Gamma, UriKind.Absolute);
}
