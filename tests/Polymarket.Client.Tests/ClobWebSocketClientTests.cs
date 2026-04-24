using System.Net.WebSockets;
using System.Threading.Channels;
using Polymarket.Client;
using Polymarket.Client.Internal;

namespace Polymarket.Client.Tests;

public sealed class ClobWebSocketClientTests
{
    [Fact]
    public async Task ConnectMarketAsync_SendsDocumentedInitialRequestAndSubscriptionUpdates()
    {
        FakeWebSocketConnection connection = new();
        ClobWebSocketClient client = CreateClient(connection);

        await using ClobMarketWebSocketSession session = await client.ConnectMarketAsync(
            new ClobMarketSubscriptionRequest
            {
                AssetIds = ["yes-token"],
                InitialDump = true,
                Level = ClobMarketSubscriptionLevel.Level3,
                CustomFeatureEnabled = true,
            });

        Assert.Equal("wss://ws-subscriptions-clob.polymarket.com/ws/market", connection.ConnectedUri?.AbsoluteUri);
        Assert.Equal(
            """{"type":"market","assets_ids":["yes-token"],"initial_dump":true,"level":3,"custom_feature_enabled":true}""",
            await connection.ReadSentAsync());

        await session.SubscribeAsync(["no-token"], customFeatureEnabled: false);

        Assert.Equal(
            """{"operation":"subscribe","assets_ids":["no-token"],"custom_feature_enabled":false}""",
            await connection.ReadSentAsync());
    }

    [Fact]
    public async Task ConnectUserAsync_SendsDocumentedInitialRequestAndSubscriptionUpdates()
    {
        FakeWebSocketConnection connection = new();
        ClobWebSocketClient client = CreateClient(connection);

        await using ClobUserWebSocketSession session = await client.ConnectUserAsync(
            new ClobUserSubscriptionRequest
            {
                Auth = new ClobWebSocketAuthentication("k", "s", "p"),
                Markets = ["0xabc"],
            });

        Assert.Equal("wss://ws-subscriptions-clob.polymarket.com/ws/user", connection.ConnectedUri?.AbsoluteUri);
        Assert.Equal(
            """{"auth":{"apiKey":"k","secret":"s","passphrase":"p"},"type":"user","markets":["0xabc"]}""",
            await connection.ReadSentAsync());

        await session.UnsubscribeAsync(["0xabc"]);

        Assert.Equal(
            """{"operation":"unsubscribe","markets":["0xabc"]}""",
            await connection.ReadSentAsync());
    }

    [Fact]
    public void MarketChannelMessage_DeserializesDerivedTypes()
    {
        ClobMarketChannelMessage book = PolymarketJson.Deserialize<ClobMarketChannelMessage>(
            """{"event_type":"book","asset_id":"t1","market":"m1","bids":[{"price":"0.45","size":"10"}],"asks":[{"price":"0.55","size":"12"}],"timestamp":"1","hash":"h"}""");
        ClobMarketChannelMessage newMarket = PolymarketJson.Deserialize<ClobMarketChannelMessage>(
            """{"event_type":"new_market","id":"market-1","question":"Will it rain?","market":"m1","slug":"will-it-rain","assets_ids":["yes","no"],"outcomes":["YES","NO"],"timestamp":"2"}""");
        ClobMarketChannelMessage resolved = PolymarketJson.Deserialize<ClobMarketChannelMessage>(
            """{"event_type":"market_resolved","id":"market-1","market":"m1","assets_ids":["yes","no"],"winning_asset_id":"yes","winning_outcome":"YES","timestamp":"3"}""");
        ClobMarketChannelMessage unknown = PolymarketJson.Deserialize<ClobMarketChannelMessage>(
            """{"event_type":"future_event","foo":"bar"}""");

        ClobMarketBookMessage typedBook = Assert.IsType<ClobMarketBookMessage>(book);
        Assert.Equal("t1", typedBook.AssetId);
        Assert.Single(typedBook.Bids);

        ClobNewMarketMessage typedNewMarket = Assert.IsType<ClobNewMarketMessage>(newMarket);
        Assert.Equal("will-it-rain", typedNewMarket.Slug);

        ClobMarketResolvedMessage typedResolved = Assert.IsType<ClobMarketResolvedMessage>(resolved);
        Assert.Equal("YES", typedResolved.WinningOutcome);

        ClobUnknownMarketChannelMessage typedUnknown = Assert.IsType<ClobUnknownMarketChannelMessage>(unknown);
        Assert.Equal("bar", typedUnknown.ExtensionData["foo"].GetString());
    }

    [Fact]
    public void UserChannelMessage_DeserializesDerivedTypes()
    {
        ClobUserChannelMessage trade = PolymarketJson.Deserialize<ClobUserChannelMessage>(
            """{"event_type":"trade","type":"TRADE","id":"trade-1","taker_order_id":"o2","market":"m1","asset_id":"t1","side":"BUY","size":"10","price":"0.57","fee_rate_bps":"0","status":"MATCHED","matchtime":"1","last_update":"1","outcome":"YES","owner":"u1","trade_owner":"u1","maker_address":"0x1","transaction_hash":"0xhash","bucket_index":0,"maker_orders":[{"asset_id":"t1","matched_amount":"10","order_id":"o1","outcome":"YES","owner":"u1","maker_address":"0x2","price":"0.57","fee_rate_bps":"0","side":"SELL"}],"trader_side":"TAKER","timestamp":"1"}""");
        ClobUserChannelMessage order = PolymarketJson.Deserialize<ClobUserChannelMessage>(
            """{"event_type":"order","id":"o1","owner":"u1","market":"m1","asset_id":"t1","side":"SELL","order_owner":"u1","original_size":"10","size_matched":"0","price":"0.57","associate_trades":["trade-1"],"outcome":"YES","type":"PLACEMENT","created_at":"1","expiration":"2","order_type":"GTC","status":"LIVE","maker_address":"0x2","timestamp":"2"}""");

        ClobTradeMessage typedTrade = Assert.IsType<ClobTradeMessage>(trade);
        Assert.Equal(Side.Buy, typedTrade.Side);
        Assert.Equal("0xhash", typedTrade.TransactionHash);
        Assert.Single(typedTrade.MakerOrders);

        ClobOrderMessage typedOrder = Assert.IsType<ClobOrderMessage>(order);
        Assert.Equal("PLACEMENT", typedOrder.Type);
        Assert.Equal(OrderType.Gtc, typedOrder.OrderType);
        Assert.Equal(Side.Sell, typedOrder.Side);
    }

    [Fact]
    public async Task ConnectSportsAsync_RespondsToPingAndStreamsUpdates()
    {
        FakeWebSocketConnection connection = new();
        ClobWebSocketClient client = CreateClient(connection);

        await using SportsWebSocketSession session = await client.ConnectSportsAsync();

        Assert.Equal("wss://sports-api.polymarket.com/ws", connection.ConnectedUri?.AbsoluteUri);

        connection.EnqueueIncoming("ping");
        connection.EnqueueIncoming("""{"slug":"mci-liv-2025-02-03","live":true,"score":"2-1","period":"FT","last_update":"2025-02-03T20:15:00Z"}""");

        Assert.Equal("pong", await connection.ReadSentAsync());

        SportsResultUpdateMessage update = await ReadSingleAsync(session.ReadAllAsync());
        Assert.Equal("mci-liv-2025-02-03", update.Slug);
        Assert.True(update.Live);
        Assert.Equal("2-1", update.Score);
    }

    [Fact]
    public async Task ConnectMarketAsync_StreamsArrayPayloadAsMultipleMessages()
    {
        FakeWebSocketConnection connection = new();
        ClobWebSocketClient client = CreateClient(connection);

        await using ClobMarketWebSocketSession session = await client.ConnectMarketAsync(
            new ClobMarketSubscriptionRequest
            {
                AssetIds = ["yes-token"],
            });

        await connection.ReadSentAsync();

        connection.EnqueueIncoming(
            """
            [
              {"event_type":"book","asset_id":"yes-token","market":"m1","bids":[{"price":"0.45","size":"10"}],"asks":[{"price":"0.55","size":"12"}],"timestamp":"1","hash":"h1"},
              {"event_type":"best_bid_ask","asset_id":"yes-token","market":"m1","best_bid":"0.45","best_ask":"0.55","spread":"0.10","timestamp":"2"}
            ]
            """);

        await using IAsyncEnumerator<ClobMarketChannelMessage> enumerator = session.ReadAllAsync().GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        Assert.IsType<ClobMarketBookMessage>(enumerator.Current);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.IsType<ClobMarketBestBidAskMessage>(enumerator.Current);
    }

    private static ClobWebSocketClient CreateClient(FakeWebSocketConnection connection) =>
        new(
            new ClobWebSocketClientOptions
            {
                PingInterval = TimeSpan.Zero,
                ReconnectDelay = TimeSpan.Zero,
            },
            new FakeWebSocketConnectionFactory(connection));

    private static async Task<T> ReadSingleAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        await using IAsyncEnumerator<T> enumerator = source.GetAsyncEnumerator(cancellationToken);
        Assert.True(await enumerator.MoveNextAsync());
        return enumerator.Current;
    }

    private sealed class FakeWebSocketConnectionFactory(FakeWebSocketConnection connection) : IClobWebSocketConnectionFactory
    {
        public IClobWebSocketConnection Create(ClobWebSocketClientOptions options) => connection;
    }

    private sealed class FakeWebSocketConnection : IClobWebSocketConnection
    {
        private readonly Channel<string> _sent = Channel.CreateUnbounded<string>();
        private readonly Channel<string> _incoming = Channel.CreateUnbounded<string>();

        public WebSocketCloseStatus? CloseStatus { get; private set; }

        public string? CloseStatusDescription { get; private set; }

        public Uri? ConnectedUri { get; private set; }

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            ConnectedUri = uri;
            return Task.CompletedTask;
        }

        public Task SendTextAsync(string message, CancellationToken cancellationToken) =>
            _sent.Writer.WriteAsync(message, cancellationToken).AsTask();

        public async Task<string?> ReceiveTextAsync(CancellationToken cancellationToken)
        {
            while (await _incoming.Reader.WaitToReadAsync(cancellationToken))
            {
                if (_incoming.Reader.TryRead(out string? message))
                {
                    return message;
                }
            }

            return null;
        }

        public void EnqueueIncoming(string message)
        {
            _incoming.Writer.TryWrite(message);
        }

        public async Task<string> ReadSentAsync(CancellationToken cancellationToken = default) =>
            await _sent.Reader.ReadAsync(cancellationToken);

        public ValueTask DisposeAsync()
        {
            CloseStatus = WebSocketCloseStatus.NormalClosure;
            CloseStatusDescription = "Disposed";
            _incoming.Writer.TryComplete();
            _sent.Writer.TryComplete();
            return ValueTask.CompletedTask;
        }
    }
}
