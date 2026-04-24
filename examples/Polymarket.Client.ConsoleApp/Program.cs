using Polymarket.Client;

var host = Environment.GetEnvironmentVariable("POLYMARKET_CLOB_HOST") ?? "https://clob.polymarket.com";
var gammaHost = Environment.GetEnvironmentVariable("POLYMARKET_GAMMA_HOST") ?? "https://gamma-api.polymarket.com";
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

if (!string.IsNullOrWhiteSpace(privateKey))
{
    ClobClientOptions options = new()
    {
        Host = new Uri(host),
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
