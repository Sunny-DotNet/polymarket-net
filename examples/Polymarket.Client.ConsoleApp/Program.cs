using Polymarket.Client;

var host = Environment.GetEnvironmentVariable("POLYMARKET_CLOB_HOST") ?? "https://clob.polymarket.com";
var privateKey = Environment.GetEnvironmentVariable("POLYMARKET_PRIVATE_KEY");

await using ClobClient client = new(host, Chain.Polygon);

Console.WriteLine($"Using host: {client.Host}");
Console.WriteLine($"Version: {await client.GetVersionAsync()}");
Console.WriteLine($"Server time: {await client.GetServerTimeAsync()}");

PaginationPayload<Market> markets = await client.GetMarketsAsync(new MarketQueryParameters
{
    NextCursor = PolymarketConstants.InitialCursor,
    Limit = 20,
});
Console.WriteLine($"Fetched page with {markets.Data.Count} markets.");

foreach (var item in markets.Data)
{
    Console.WriteLine(item.Question);
}

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
