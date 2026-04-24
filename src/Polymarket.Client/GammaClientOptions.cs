namespace Polymarket.Client;

public sealed record GammaClientOptions
{
    public Uri Host { get; init; } = new("https://gamma-api.polymarket.com/", UriKind.Absolute);
}
