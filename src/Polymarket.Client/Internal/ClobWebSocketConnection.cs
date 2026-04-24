using System.Buffers;
using System.Net.WebSockets;
using System.Text;

namespace Polymarket.Client.Internal;

internal interface IClobWebSocketConnectionFactory
{
    IClobWebSocketConnection Create(ClobWebSocketClientOptions options);
}

internal interface IClobWebSocketConnection : IAsyncDisposable
{
    WebSocketCloseStatus? CloseStatus { get; }

    string? CloseStatusDescription { get; }

    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);

    Task SendTextAsync(string message, CancellationToken cancellationToken);

    Task<string?> ReceiveTextAsync(CancellationToken cancellationToken);
}

internal sealed class ClientWebSocketConnectionFactory : IClobWebSocketConnectionFactory
{
    public IClobWebSocketConnection Create(ClobWebSocketClientOptions options) =>
        new ClientWebSocketConnection(options);
}

internal sealed class ClientWebSocketConnection(ClobWebSocketClientOptions options) : IClobWebSocketConnection
{
    private readonly ClientWebSocket _socket = new();

    public WebSocketCloseStatus? CloseStatus => _socket.CloseStatus;

    public string? CloseStatusDescription => _socket.CloseStatusDescription;

    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        _socket.Options.KeepAliveInterval = Timeout.InfiniteTimeSpan;
        _socket.Options.SetBuffer(options.ReceiveBufferSize, options.ReceiveBufferSize);
        return _socket.ConnectAsync(uri, cancellationToken);
    }

    public async Task SendTextAsync(string message, CancellationToken cancellationToken)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> ReceiveTextAsync(CancellationToken cancellationToken)
    {
        ArrayPool<byte> pool = ArrayPool<byte>.Shared;
        byte[] buffer = pool.Rent(options.ReceiveBufferSize);
        try
        {
            using MemoryStream stream = new();
            while (true)
            {
                WebSocketReceiveResult result = await _socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return null;
                }

                if (result.Count > 0)
                {
                    await stream.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken).ConfigureAwait(false);
                }

                if (result.EndOfMessage)
                {
                    break;
                }
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    public ValueTask DisposeAsync()
    {
        _socket.Dispose();
        return ValueTask.CompletedTask;
    }
}
