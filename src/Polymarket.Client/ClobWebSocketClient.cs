using System.Runtime.CompilerServices;
using Polymarket.Client.Internal;

namespace Polymarket.Client;

public sealed class ClobWebSocketClient
{
    private readonly IClobWebSocketConnectionFactory _connectionFactory;

    public ClobWebSocketClient()
        : this(new ClobWebSocketClientOptions())
    {
    }

    public ClobWebSocketClient(string host)
        : this(new ClobWebSocketClientOptions
        {
            Host = new Uri(host, UriKind.Absolute),
        })
    {
    }

    public ClobWebSocketClient(Uri host)
        : this(new ClobWebSocketClientOptions
        {
            Host = host,
        })
    {
    }

    public ClobWebSocketClient(ClobWebSocketClientOptions options)
        : this(options, new ClientWebSocketConnectionFactory())
    {
    }

    internal ClobWebSocketClient(ClobWebSocketClientOptions options, IClobWebSocketConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connectionFactory);

        Options = options with
        {
            Host = NormalizeHost(options.Host),
            SportsHost = NormalizeHost(options.SportsHost),
        };
        _connectionFactory = connectionFactory;
    }

    public ClobWebSocketClientOptions Options { get; }

    public Uri Host => Options.Host;

    public Uri SportsHost => Options.SportsHost;

    public async Task<ClobMarketWebSocketSession> ConnectMarketAsync(
        ClobMarketSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateMarketRequest(request);

        ClobMarketWebSocketSession session = new(Options, _connectionFactory, request);
        try
        {
            await session.StartAsync(cancellationToken).ConfigureAwait(false);
            return session;
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async Task<ClobUserWebSocketSession> ConnectUserAsync(
        ClobUserSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateUserRequest(request);

        ClobUserWebSocketSession session = new(Options, _connectionFactory, request);
        try
        {
            await session.StartAsync(cancellationToken).ConfigureAwait(false);
            return session;
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async Task<SportsWebSocketSession> ConnectSportsAsync(CancellationToken cancellationToken = default)
    {
        SportsWebSocketSession session = new(Options, _connectionFactory);
        try
        {
            await session.StartAsync(cancellationToken).ConfigureAwait(false);
            return session;
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async IAsyncEnumerable<ClobMarketChannelMessage> SubscribeMarketAsync(
        ClobMarketSubscriptionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using ClobMarketWebSocketSession session = await ConnectMarketAsync(request, cancellationToken).ConfigureAwait(false);
        await foreach (ClobMarketChannelMessage message in session.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return message;
        }
    }

    public async IAsyncEnumerable<ClobUserChannelMessage> SubscribeUserAsync(
        ClobUserSubscriptionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using ClobUserWebSocketSession session = await ConnectUserAsync(request, cancellationToken).ConfigureAwait(false);
        await foreach (ClobUserChannelMessage message in session.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return message;
        }
    }

    public async IAsyncEnumerable<SportsResultUpdateMessage> SubscribeSportsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using SportsWebSocketSession session = await ConnectSportsAsync(cancellationToken).ConfigureAwait(false);
        await foreach (SportsResultUpdateMessage message in session.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return message;
        }
    }

    internal static void ValidateMarketRequest(ClobMarketSubscriptionRequest request)
    {
        if (request.AssetIds.Count == 0)
        {
            throw new ArgumentException("At least one asset id must be provided.", nameof(request));
        }
    }

    internal static void ValidateUserRequest(ClobUserSubscriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Auth.ApiKey)
            || string.IsNullOrWhiteSpace(request.Auth.Secret)
            || string.IsNullOrWhiteSpace(request.Auth.Passphrase))
        {
            throw new ArgumentException("User channel authentication must include api key, secret, and passphrase.", nameof(request));
        }

        if (request.Markets is not null && request.Markets.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Market filters cannot contain empty values.", nameof(request));
        }
    }

    private static Uri NormalizeHost(Uri host)
    {
        ArgumentNullException.ThrowIfNull(host);
        string normalized = host.AbsoluteUri.TrimEnd('/') + "/";
        return new Uri(normalized, UriKind.Absolute);
    }
}
