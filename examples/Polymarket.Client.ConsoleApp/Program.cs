using Polymarket.Client;

var host = Environment.GetEnvironmentVariable("POLYMARKET_CLOB_HOST") ?? PolymarketHosts.Clob;
var gammaHost = Environment.GetEnvironmentVariable("POLYMARKET_GAMMA_HOST") ?? PolymarketHosts.Gamma;
var privateKey = Environment.GetEnvironmentVariable("POLYMARKET_PRIVATE_KEY");

await using ClobClient clobClient = new(host, Chain.Polygon);
await using GammaClient gammaClient = new(gammaHost);
ClobWebSocketClient webSocketClient = new();

Console.WriteLine($"Using CLOB host: {clobClient.Host}");
Console.WriteLine($"Using Gamma host: {gammaClient.Host}");
Console.WriteLine($"Using CLOB websocket host: {webSocketClient.Host}");
Console.WriteLine($"CLOB version: {await clobClient.GetVersionAsync()}");
Console.WriteLine($"CLOB server time: {await clobClient.GetServerTimeAsync()}");
Console.WriteLine($"Gamma status: {await gammaClient.GetStatusAsync()}");



//104272深圳,102923香港，104590广州

var events = await gammaClient.GetEventsAsync(new GammaEventQueryParameters
{
    TagSlug = ["highest-temperature"],
    TagId = [104272, 102923, 104590],
    Active = true,
    Closed = false,
    Order = "endDate",
    Limit = 100,
    Ascending = false,
    Locale="zh"
});






if (!string.IsNullOrWhiteSpace(privateKey))
{
    ClobClientOptions options = new()
    {
        Host = host,
        Chain = Chain.Polygon,
        PrivateKey = privateKey,
    };

    await using ClobClient authedClient = new(options);
    Console.WriteLine($"Authenticated CLOB mode available: {authedClient.Mode}");
}

using CancellationTokenSource shutdown = new();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

BtcUpDownFiveMinuteWebSocketWatcher watcher = new(gammaClient, webSocketClient, Console.Out);

Console.WriteLine();
Console.WriteLine("Watching the latest BTC 5-minute market via Gamma discovery + CLOB websocket. Press Ctrl+C to stop.");

await watcher.RunAsync(shutdown.Token);
