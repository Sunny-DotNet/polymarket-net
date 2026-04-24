namespace Polymarket.Client;

public sealed record GammaClientOptions
{
    public string Host { get; init; } = PolymarketHosts.Gamma;
}
