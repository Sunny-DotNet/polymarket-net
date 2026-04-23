using Polymarket.Client;

var host = Environment.GetEnvironmentVariable("POLYMARKET_CLOB_HOST") ?? "https://clob.polymarket.com";
var privateKey = Environment.GetEnvironmentVariable("POLYMARKET_PRIVATE_KEY");

await using ClobClient client = new(host, Chain.Polygon);

Console.WriteLine($"Using host: {client.Host}");
Console.WriteLine($"Version: {await client.GetVersionAsync()}");
Console.WriteLine($"Server time: {await client.GetServerTimeAsync()}");

PaginationPayload<DynamicPayload> markets = await client.GetMarketsAsync();
Console.WriteLine($"Fetched page with {markets.Data.Count} markets.");

if (!string.IsNullOrWhiteSpace(privateKey))
{
    ClobClientOptions options = new()
    {
        Host = new Uri(host),
        Chain = Chain.Polygon,
        PrivateKey = privateKey,
    };

    await using ClobClient authedClient = new(options);
    Console.WriteLine($"Authenticated mode available: {authedClient.Mode}");
}
