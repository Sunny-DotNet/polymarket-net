using System.Net.WebSockets;
using System.Threading.Channels;

namespace Polymarket.Client.Internal;

internal abstract class ClobWebSocketSessionController<TMessage> : IAsyncDisposable
{
    private readonly IClobWebSocketConnectionFactory _connectionFactory;
    private readonly Channel<TMessage> _messages = Channel.CreateUnbounded<TMessage>();
    private readonly CancellationTokenSource _sessionCts = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly TaskCompletionSource _connectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _connectionGate = new();
    private Task? _pumpTask;
    private IClobWebSocketConnection? _connection;

    protected ClobWebSocketSessionController(ClobWebSocketClientOptions options, IClobWebSocketConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        Options = options;
        _connectionFactory = connectionFactory;
    }

    protected ClobWebSocketClientOptions Options { get; }

    protected abstract Uri Endpoint { get; }

    protected abstract bool HasReconnectableState { get; }

    protected abstract IReadOnlyList<TMessage> DeserializeMessages(string payload);

    protected abstract Task SendInitialAsync(IClobWebSocketConnection connection, CancellationToken cancellationToken);

    protected virtual Task RunHeartbeatLoopAsync(IClobWebSocketConnection connection, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    protected virtual ValueTask<bool> HandleControlMessageAsync(
        string payload,
        IClobWebSocketConnection connection,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(false);

    public IAsyncEnumerable<TMessage> ReadAllAsync(CancellationToken cancellationToken = default) =>
        _messages.Reader.ReadAllAsync(cancellationToken);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _pumpTask ??= Task.Run(PumpAsync, CancellationToken.None);

        Task completed = await Task.WhenAny(_connectedTcs.Task, _pumpTask).WaitAsync(cancellationToken).ConfigureAwait(false);
        if (completed == _pumpTask)
        {
            await _pumpTask.ConfigureAwait(false);
        }

        await _connectedTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    protected Task SendJsonCommandAsync(object payload, CancellationToken cancellationToken) =>
        SendTextCommandAsync(PolymarketJson.Serialize(payload), cancellationToken);

    protected async Task SendTextCommandAsync(string message, CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IClobWebSocketConnection connection = GetConnectionOrThrow();
            await connection.SendTextAsync(message, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    protected async Task SendJsonAsync(IClobWebSocketConnection connection, object payload, CancellationToken cancellationToken) =>
        await SendTextAsync(connection, PolymarketJson.Serialize(payload), cancellationToken).ConfigureAwait(false);

    protected async Task SendTextAsync(IClobWebSocketConnection connection, string message, CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await connection.SendTextAsync(message, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _sessionCts.Cancel();

        if (_pumpTask is not null)
        {
            try
            {
                await _pumpTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_sessionCts.IsCancellationRequested)
            {
            }
        }

        _sessionCts.Dispose();
        _sendLock.Dispose();
    }

    private async Task PumpAsync()
    {
        int attempt = 0;
        bool connectedOnce = false;
        Exception? terminalException = null;

        try
        {
            while (!_sessionCts.IsCancellationRequested)
            {
                await using IClobWebSocketConnection connection = _connectionFactory.Create(Options);
                using CancellationTokenSource connectionCts = CancellationTokenSource.CreateLinkedTokenSource(_sessionCts.Token);

                Task heartbeatTask = Task.CompletedTask;
                bool shouldReconnect = false;

                try
                {
                    await connection.ConnectAsync(Endpoint, connectionCts.Token).ConfigureAwait(false);
                    SetConnection(connection);

                    await SendInitialAsync(connection, connectionCts.Token).ConfigureAwait(false);
                    heartbeatTask = RunHeartbeatLoopAsync(connection, connectionCts.Token);

                    connectedOnce = true;
                    terminalException = null;
                    attempt = 0;
                    _connectedTcs.TrySetResult();

                    while (!connectionCts.IsCancellationRequested)
                    {
                        string? payload = await connection.ReceiveTextAsync(connectionCts.Token).ConfigureAwait(false);
                        if (payload is null)
                        {
                            shouldReconnect = ShouldReconnectOnClose(connection.CloseStatus);
                            break;
                        }

                        if (await HandleControlMessageAsync(payload, connection, connectionCts.Token).ConfigureAwait(false))
                        {
                            continue;
                        }

                        foreach (TMessage message in DeserializeMessages(payload))
                        {
                            await _messages.Writer.WriteAsync(message, connectionCts.Token).ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException) when (_sessionCts.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex) when (CanReconnect(++attempt))
                {
                    terminalException = ex;
                    await DelayReconnectAsync(_sessionCts.Token).ConfigureAwait(false);
                    continue;
                }
                catch (Exception ex)
                {
                    terminalException = ex;
                    break;
                }
                finally
                {
                    ClearConnection(connection);
                    connectionCts.Cancel();
                    try
                    {
                        await heartbeatTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                if (!shouldReconnect)
                {
                    break;
                }

                if (!CanReconnect(++attempt))
                {
                    terminalException = new ClobWebSocketException("The websocket connection closed and the reconnect policy was exhausted.");
                    break;
                }

                await DelayReconnectAsync(_sessionCts.Token).ConfigureAwait(false);
            }
        }
        finally
        {
            if (!connectedOnce)
            {
                if (_sessionCts.IsCancellationRequested)
                {
                    _connectedTcs.TrySetCanceled(_sessionCts.Token);
                }
                else
                {
                    _connectedTcs.TrySetException(terminalException ?? new ClobWebSocketException("The websocket session ended before the initial connection completed."));
                }
            }

            if (terminalException is null || _sessionCts.IsCancellationRequested)
            {
                _messages.Writer.TryComplete();
            }
            else
            {
                _messages.Writer.TryComplete(terminalException);
            }
        }
    }

    private bool ShouldReconnectOnClose(WebSocketCloseStatus? closeStatus) =>
        Options.AutoReconnect
        && !_sessionCts.IsCancellationRequested
        && HasReconnectableState
        && closeStatus is not WebSocketCloseStatus.NormalClosure;

    private bool CanReconnect(int attempt) =>
        Options.AutoReconnect
        && !_sessionCts.IsCancellationRequested
        && HasReconnectableState
        && (!Options.MaxReconnectAttempts.HasValue || attempt <= Options.MaxReconnectAttempts.Value);

    private Task DelayReconnectAsync(CancellationToken cancellationToken) =>
        Options.ReconnectDelay > TimeSpan.Zero
            ? Task.Delay(Options.ReconnectDelay, cancellationToken)
            : Task.CompletedTask;

    private void SetConnection(IClobWebSocketConnection connection)
    {
        lock (_connectionGate)
        {
            _connection = connection;
        }
    }

    private void ClearConnection(IClobWebSocketConnection connection)
    {
        lock (_connectionGate)
        {
            if (ReferenceEquals(_connection, connection))
            {
                _connection = null;
            }
        }
    }

    private IClobWebSocketConnection GetConnectionOrThrow()
    {
        lock (_connectionGate)
        {
            return _connection ?? throw new ClobWebSocketException("The websocket session is not connected.");
        }
    }
}
