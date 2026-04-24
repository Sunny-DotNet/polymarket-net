using Polymarket.Client.Internal;

namespace Polymarket.Client;

public abstract class ClobWebSocketSession<TMessage> : IAsyncDisposable
{
    private protected ClobWebSocketSessionController<TMessage> Controller { get; }

    internal ClobWebSocketSession(ClobWebSocketSessionController<TMessage> controller)
    {
        Controller = controller;
    }

    public IAsyncEnumerable<TMessage> ReadAllAsync(CancellationToken cancellationToken = default) =>
        Controller.ReadAllAsync(cancellationToken);

    public ValueTask DisposeAsync() => Controller.DisposeAsync();

    internal Task StartAsync(CancellationToken cancellationToken) => Controller.StartAsync(cancellationToken);
}

public sealed class ClobMarketWebSocketSession : ClobWebSocketSession<ClobMarketChannelMessage>
{
    private readonly ControllerImpl _controller;

    internal ClobMarketWebSocketSession(
        ClobWebSocketClientOptions options,
        IClobWebSocketConnectionFactory connectionFactory,
        ClobMarketSubscriptionRequest request)
        : this(new ControllerImpl(options, connectionFactory, request))
    {
    }

    private ClobMarketWebSocketSession(ControllerImpl controller)
        : base(controller)
    {
        _controller = controller;
    }

    public Task UpdateSubscriptionAsync(
        ClobMarketSubscriptionUpdate update,
        CancellationToken cancellationToken = default) =>
        _controller.UpdateSubscriptionAsync(update, cancellationToken);

    public Task SubscribeAsync(
        IEnumerable<string> assetIds,
        ClobMarketSubscriptionLevel? level = null,
        bool? customFeatureEnabled = null,
        CancellationToken cancellationToken = default) =>
        UpdateSubscriptionAsync(
            new ClobMarketSubscriptionUpdate
            {
                Operation = ClobWebSocketSubscriptionOperation.Subscribe,
                AssetIds = NormalizeValues(assetIds, nameof(assetIds)),
                Level = level,
                CustomFeatureEnabled = customFeatureEnabled,
            },
            cancellationToken);

    public Task UnsubscribeAsync(
        IEnumerable<string> assetIds,
        CancellationToken cancellationToken = default) =>
        UpdateSubscriptionAsync(
            new ClobMarketSubscriptionUpdate
            {
                Operation = ClobWebSocketSubscriptionOperation.Unsubscribe,
                AssetIds = NormalizeValues(assetIds, nameof(assetIds)),
            },
            cancellationToken);

    private static string[] NormalizeValues(IEnumerable<string> values, string paramName) =>
        values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray() switch
            {
                [] => throw new ArgumentException("At least one asset id must be provided.", paramName),
                string[] normalized => normalized,
            };

    private sealed class ControllerImpl(
        ClobWebSocketClientOptions options,
        IClobWebSocketConnectionFactory connectionFactory,
        ClobMarketSubscriptionRequest request)
        : ClobWebSocketSessionController<ClobMarketChannelMessage>(options, connectionFactory)
    {
        private readonly object _gate = new();
        private ClobMarketSubscriptionRequest _request = request with
        {
            AssetIds = [.. request.AssetIds],
        };

        protected override Uri Endpoint => new(new Uri(Options.Host, UriKind.Absolute), "market");

        protected override bool HasReconnectableState
        {
            get
            {
                lock (_gate)
                {
                    return _request.AssetIds.Count > 0;
                }
            }
        }

        protected override IReadOnlyList<ClobMarketChannelMessage> DeserializeMessages(string payload) =>
            ClobWebSocketJson.DeserializeMany(
                payload,
                static json => PolymarketJson.Deserialize<ClobMarketChannelMessage>(json));

        protected override Task SendInitialAsync(IClobWebSocketConnection connection, CancellationToken cancellationToken)
        {
            ClobMarketSubscriptionRequest snapshot;
            lock (_gate)
            {
                snapshot = _request with
                {
                    AssetIds = [.. _request.AssetIds],
                };
            }

            return SendJsonAsync(connection, snapshot, cancellationToken);
        }

        protected override async Task RunHeartbeatLoopAsync(IClobWebSocketConnection connection, CancellationToken cancellationToken)
        {
            if (Options.PingInterval <= TimeSpan.Zero)
            {
                return;
            }

            using PeriodicTimer timer = new(Options.PingInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await SendTextAsync(connection, "PING", cancellationToken).ConfigureAwait(false);
            }
        }

        protected override async ValueTask<bool> HandleControlMessageAsync(
            string payload,
            IClobWebSocketConnection connection,
            CancellationToken cancellationToken)
        {
            if (string.Equals(payload, "PONG", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(payload, "PING", StringComparison.OrdinalIgnoreCase))
            {
                await SendTextAsync(connection, "PONG", cancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        public async Task UpdateSubscriptionAsync(ClobMarketSubscriptionUpdate update, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(update);

            if (update.AssetIds.Count == 0)
            {
                throw new ArgumentException("At least one asset id must be provided.", nameof(update));
            }

            await SendJsonCommandAsync(update, cancellationToken).ConfigureAwait(false);

            lock (_gate)
            {
                HashSet<string> assetIds = new(_request.AssetIds, StringComparer.Ordinal);
                foreach (string assetId in update.AssetIds)
                {
                    if (update.Operation == ClobWebSocketSubscriptionOperation.Subscribe)
                    {
                        assetIds.Add(assetId);
                    }
                    else
                    {
                        assetIds.Remove(assetId);
                    }
                }

                _request = _request with
                {
                    AssetIds = [.. assetIds],
                    Level = update.Level ?? _request.Level,
                    CustomFeatureEnabled = update.CustomFeatureEnabled ?? _request.CustomFeatureEnabled,
                };
            }
        }
    }
}

public sealed class ClobUserWebSocketSession : ClobWebSocketSession<ClobUserChannelMessage>
{
    private readonly ControllerImpl _controller;

    internal ClobUserWebSocketSession(
        ClobWebSocketClientOptions options,
        IClobWebSocketConnectionFactory connectionFactory,
        ClobUserSubscriptionRequest request)
        : this(new ControllerImpl(options, connectionFactory, request))
    {
    }

    private ClobUserWebSocketSession(ControllerImpl controller)
        : base(controller)
    {
        _controller = controller;
    }

    public Task UpdateSubscriptionAsync(
        ClobUserSubscriptionUpdate update,
        CancellationToken cancellationToken = default) =>
        _controller.UpdateSubscriptionAsync(update, cancellationToken);

    public Task SubscribeAsync(
        IEnumerable<string> markets,
        CancellationToken cancellationToken = default) =>
        UpdateSubscriptionAsync(
            new ClobUserSubscriptionUpdate
            {
                Operation = ClobWebSocketSubscriptionOperation.Subscribe,
                Markets = NormalizeValues(markets, nameof(markets)),
            },
            cancellationToken);

    public Task UnsubscribeAsync(
        IEnumerable<string> markets,
        CancellationToken cancellationToken = default) =>
        UpdateSubscriptionAsync(
            new ClobUserSubscriptionUpdate
            {
                Operation = ClobWebSocketSubscriptionOperation.Unsubscribe,
                Markets = NormalizeValues(markets, nameof(markets)),
            },
            cancellationToken);

    private static string[] NormalizeValues(IEnumerable<string> values, string paramName) =>
        values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray() switch
            {
                [] => throw new ArgumentException("At least one market id must be provided.", paramName),
                string[] normalized => normalized,
            };

    private sealed class ControllerImpl(
        ClobWebSocketClientOptions options,
        IClobWebSocketConnectionFactory connectionFactory,
        ClobUserSubscriptionRequest request)
        : ClobWebSocketSessionController<ClobUserChannelMessage>(options, connectionFactory)
    {
        private readonly object _gate = new();
        private ClobUserSubscriptionRequest _request = request with
        {
            Markets = request.Markets is null ? null : [.. request.Markets],
        };

        protected override Uri Endpoint => new(new Uri(Options.Host, UriKind.Absolute), "user");

        protected override bool HasReconnectableState => true;

        protected override IReadOnlyList<ClobUserChannelMessage> DeserializeMessages(string payload) =>
            ClobWebSocketJson.DeserializeMany(
                payload,
                static json => PolymarketJson.Deserialize<ClobUserChannelMessage>(json));

        protected override Task SendInitialAsync(IClobWebSocketConnection connection, CancellationToken cancellationToken)
        {
            ClobUserSubscriptionRequest snapshot;
            lock (_gate)
            {
                snapshot = _request with
                {
                    Markets = _request.Markets is null ? null : [.. _request.Markets],
                };
            }

            return SendJsonAsync(connection, snapshot, cancellationToken);
        }

        protected override async Task RunHeartbeatLoopAsync(IClobWebSocketConnection connection, CancellationToken cancellationToken)
        {
            if (Options.PingInterval <= TimeSpan.Zero)
            {
                return;
            }

            using PeriodicTimer timer = new(Options.PingInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await SendTextAsync(connection, "PING", cancellationToken).ConfigureAwait(false);
            }
        }

        protected override async ValueTask<bool> HandleControlMessageAsync(
            string payload,
            IClobWebSocketConnection connection,
            CancellationToken cancellationToken)
        {
            if (string.Equals(payload, "PONG", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(payload, "PING", StringComparison.OrdinalIgnoreCase))
            {
                await SendTextAsync(connection, "PONG", cancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        public async Task UpdateSubscriptionAsync(ClobUserSubscriptionUpdate update, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(update);

            if (update.Markets.Count == 0)
            {
                throw new ArgumentException("At least one market id must be provided.", nameof(update));
            }

            lock (_gate)
            {
                if (_request.Markets is null)
                {
                    throw new InvalidOperationException("Dynamic market updates require the session to start with an explicit market filter.");
                }
            }

            await SendJsonCommandAsync(update, cancellationToken).ConfigureAwait(false);

            lock (_gate)
            {
                HashSet<string> markets = new(_request.Markets!, StringComparer.Ordinal);
                foreach (string market in update.Markets)
                {
                    if (update.Operation == ClobWebSocketSubscriptionOperation.Subscribe)
                    {
                        markets.Add(market);
                    }
                    else
                    {
                        markets.Remove(market);
                    }
                }

                _request = _request with
                {
                    Markets = [.. markets],
                };
            }
        }
    }
}

public sealed class SportsWebSocketSession : ClobWebSocketSession<SportsResultUpdateMessage>
{
    internal SportsWebSocketSession(
        ClobWebSocketClientOptions options,
        IClobWebSocketConnectionFactory connectionFactory)
        : base(new ControllerImpl(options, connectionFactory))
    {
    }

    private sealed class ControllerImpl(
        ClobWebSocketClientOptions options,
        IClobWebSocketConnectionFactory connectionFactory)
        : ClobWebSocketSessionController<SportsResultUpdateMessage>(options, connectionFactory)
    {
        protected override Uri Endpoint => new(new Uri(Options.SportsHost, UriKind.Absolute), "ws");

        protected override bool HasReconnectableState => true;

        protected override IReadOnlyList<SportsResultUpdateMessage> DeserializeMessages(string payload) =>
            ClobWebSocketJson.DeserializeMany(
                payload,
                static json => PolymarketJson.Deserialize<SportsResultUpdateMessage>(json));

        protected override Task SendInitialAsync(IClobWebSocketConnection connection, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        protected override async ValueTask<bool> HandleControlMessageAsync(
            string payload,
            IClobWebSocketConnection connection,
            CancellationToken cancellationToken)
        {
            if (string.Equals(payload, "pong", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(payload, "ping", StringComparison.OrdinalIgnoreCase))
            {
                await SendTextAsync(connection, "pong", cancellationToken).ConfigureAwait(false);
                return true;
            }

            return false;
        }
    }
}
