namespace Polymarket.Client;

public sealed record ApiCredentials(
    string Key,
    string Secret,
    string Passphrase);
