using System.Net;
using System.Text.Json;
using Polymarket.Client;

internal sealed class BtcUpDownFiveMinuteWebSocketWatcher(
    GammaClient gammaClient,
    ClobWebSocketClient webSocketClient,
    TextWriter writer)
{
    private readonly ConsoleDashboardRenderer _renderer = new(writer);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            DateTimeOffset windowStart = GetCurrentWindowStart(DateTimeOffset.UtcNow);
            DateTimeOffset windowEnd = windowStart.AddMinutes(5);
            string slug = BuildSlug(windowStart);
            DashboardState dashboard = new(windowStart, windowEnd, slug)
            {
                Status = "Waiting for market to publish...",
            };

            await RenderAsync(dashboard, cancellationToken);

            GammaMarket? market = await WaitForMarketAsync(dashboard, windowEnd, cancellationToken);
            if (market is null)
            {
                continue;
            }

            IReadOnlyList<string> tokenIds = ParseStringArray(market.ClobTokenIds);
            IReadOnlyDictionary<string, string> tokenLabels = BuildTokenLabels(market, tokenIds);
            InitializeRows(dashboard, tokenIds, tokenLabels);
            dashboard.Question = market.Question;
            dashboard.GammaPrices = FormatOutcomePrices(market);
            if (tokenIds.Count == 0)
            {
                dashboard.Status = "No token ids found for the current market.";
                await RenderAsync(dashboard, cancellationToken);
                await DelayUntilNextWindowAsync(windowEnd, cancellationToken);
                continue;
            }

            using CancellationTokenSource windowCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            TimeSpan remaining = windowEnd - DateTimeOffset.UtcNow;
            if (remaining > TimeSpan.Zero)
            {
                windowCts.CancelAfter(remaining);
            }
            else
            {
                continue;
            }

            try
            {
                dashboard.Status = "Subscribing to market websocket...";
                await RenderAsync(dashboard, cancellationToken);

                await using ClobMarketWebSocketSession session = await webSocketClient.ConnectMarketAsync(
                    new ClobMarketSubscriptionRequest
                    {
                        AssetIds = tokenIds,
                        InitialDump = true,
                        Level = ClobMarketSubscriptionLevel.Level2,
                        CustomFeatureEnabled = true,
                    },
                    windowCts.Token);

                await foreach (ClobMarketChannelMessage message in session.ReadAllAsync(windowCts.Token))
                {
                    ApplyMessage(dashboard, message, tokenLabels);
                    await RenderAsync(dashboard, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (windowCts.IsCancellationRequested || cancellationToken.IsCancellationRequested)
            {
            }
        }
    }

    private async Task<GammaMarket?> WaitForMarketAsync(DashboardState dashboard, DateTimeOffset windowEnd, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && DateTimeOffset.UtcNow < windowEnd)
        {
            try
            {
                GammaMarket market = await gammaClient.GetMarketBySlugAsync(dashboard.Slug, cancellationToken: cancellationToken);
                dashboard.Question = market.Question;
                dashboard.GammaPrices = FormatOutcomePrices(market);
                dashboard.Status = "Market resolved. Waiting for websocket updates...";
                await RenderAsync(dashboard, cancellationToken);
                return market;
            }
            catch (GammaApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                dashboard.Status = $"{dashboard.Slug} is not published yet. Retrying in 5s...";
                await RenderAsync(dashboard, cancellationToken);
                TimeSpan delay = TimeSpan.FromSeconds(5);
                if (DateTimeOffset.UtcNow.Add(delay) > windowEnd)
                {
                    dashboard.Status = "Current window ended before the market was published.";
                    await RenderAsync(dashboard, cancellationToken);
                    return null;
                }

                await Task.Delay(delay, cancellationToken);
            }
        }

        return null;
    }

    private void ApplyMessage(DashboardState dashboard, ClobMarketChannelMessage message, IReadOnlyDictionary<string, string> tokenLabels)
    {
        string updated = DateTimeOffset.Now.ToString("HH:mm:ss");
        switch (message)
        {
            case ClobMarketBookMessage book:
                DashboardRowState bookRow = GetOrCreateRow(dashboard, book.AssetId, tokenLabels);
                UpdateBestPrices(bookRow, book.Bids.FirstOrDefault()?.Price, book.Asks.FirstOrDefault()?.Price, null, updated);
                dashboard.Status = $"book update: {bookRow.Label}";
                break;

            case ClobMarketBestBidAskMessage bestBidAsk:
                DashboardRowState bestBidAskRow = GetOrCreateRow(dashboard, bestBidAsk.AssetId, tokenLabels);
                UpdateBestPrices(bestBidAskRow, bestBidAsk.BestBid, bestBidAsk.BestAsk, bestBidAsk.Spread, updated);
                dashboard.Status = $"best bid/ask: {bestBidAskRow.Label}";
                break;

            case ClobMarketLastTradePriceMessage lastTrade:
                DashboardRowState lastTradeRow = GetOrCreateRow(dashboard, lastTrade.AssetId, tokenLabels);
                lastTradeRow.LastTrade = lastTrade.Price;
                lastTradeRow.Updated = updated;
                dashboard.Status = $"last trade: {lastTradeRow.Label} {lastTrade.Price}";
                break;

            case ClobMarketTickSizeChangeMessage tickSizeChange:
                dashboard.Status = $"tick size changed: {ResolveLabel(tickSizeChange.AssetId, tokenLabels)} {tickSizeChange.OldTickSize} -> {tickSizeChange.NewTickSize}";
                break;

            case ClobMarketPriceChangeMessage priceChange:
                foreach (ClobMarketPriceChange change in priceChange.PriceChanges)
                {
                    DashboardRowState priceChangeRow = GetOrCreateRow(dashboard, change.AssetId, tokenLabels);
                    UpdateBestPrices(priceChangeRow, change.BestBid, change.BestAsk, null, updated);
                }

                dashboard.Status = $"price change: {priceChange.PriceChanges.Count} update(s)";
                break;

            case ClobNewMarketMessage newMarket:
                dashboard.Status = $"new market event: {newMarket.Slug}";
                break;

            case ClobMarketResolvedMessage resolved:
                dashboard.Status = $"market resolved: {resolved.WinningOutcome}";
                break;

            case ClobUnknownMarketChannelMessage unknown:
                dashboard.Status = $"unknown event: {unknown.EventType}";
                break;
        }
    }

    private Task RenderAsync(DashboardState dashboard, CancellationToken cancellationToken) =>
        _renderer.RenderAsync(dashboard.ToSnapshot(), cancellationToken);

    private static DateTimeOffset GetCurrentWindowStart(DateTimeOffset timestamp)
    {
        long alignedSeconds = timestamp.ToUnixTimeSeconds() / 300 * 300;
        return DateTimeOffset.FromUnixTimeSeconds(alignedSeconds);
    }

    private static string BuildSlug(DateTimeOffset windowStart) =>
        $"btc-updown-5m-{windowStart.ToUnixTimeSeconds()}";

    private static async Task DelayUntilNextWindowAsync(DateTimeOffset windowEnd, CancellationToken cancellationToken)
    {
        TimeSpan delay = windowEnd - DateTimeOffset.UtcNow;
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }
    }

    private static IReadOnlyDictionary<string, string> BuildTokenLabels(GammaMarket market, IReadOnlyList<string> tokenIds)
    {
        IReadOnlyList<string> outcomes = ParseStringArray(market.Outcomes);
        Dictionary<string, string> labels = new(StringComparer.Ordinal);
        for (int index = 0; index < Math.Min(tokenIds.Count, outcomes.Count); index++)
        {
            labels[tokenIds[index]] = outcomes[index];
        }

        return labels;
    }

    private static string ResolveLabel(string assetId, IReadOnlyDictionary<string, string> labels) =>
        labels.TryGetValue(assetId, out string? label) ? label : assetId;

    private static void InitializeRows(DashboardState dashboard, IReadOnlyList<string> tokenIds, IReadOnlyDictionary<string, string> tokenLabels)
    {
        foreach (string tokenId in tokenIds)
        {
            if (dashboard.RowsByAssetId.ContainsKey(tokenId))
            {
                continue;
            }

            dashboard.RowsByAssetId[tokenId] = new DashboardRowState
            {
                AssetId = tokenId,
                Label = ResolveLabel(tokenId, tokenLabels),
                Direction = ResolveDirection(tokenId, tokenLabels),
            };
            dashboard.RowOrder.Add(tokenId);
        }
    }

    private static DashboardRowState GetOrCreateRow(DashboardState dashboard, string assetId, IReadOnlyDictionary<string, string> tokenLabels)
    {
        if (!dashboard.RowsByAssetId.TryGetValue(assetId, out DashboardRowState? row))
        {
            row = new DashboardRowState
            {
                AssetId = assetId,
                Label = ResolveLabel(assetId, tokenLabels),
                Direction = ResolveDirection(assetId, tokenLabels),
            };
            dashboard.RowsByAssetId[assetId] = row;
            dashboard.RowOrder.Add(assetId);
        }

        return row;
    }

    private static void UpdateBestPrices(DashboardRowState row, string? bestBid, string? bestAsk, string? spread, string updated)
    {
        row.BestBid = NormalizeValue(bestBid);
        row.BestAsk = NormalizeValue(bestAsk);
        row.Spread = NormalizeValue(spread) != "-" ? NormalizeValue(spread) : ComputeSpread(row.BestBid, row.BestAsk);
        row.Updated = updated;
    }

    private static string NormalizeValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static string ComputeSpread(string bestBid, string bestAsk)
    {
        if (decimal.TryParse(bestBid, out decimal bid)
            && decimal.TryParse(bestAsk, out decimal ask))
        {
            return (ask - bid).ToString("0.000");
        }

        return "-";
    }

    private static string FormatOutcomePrices(GammaMarket market)
    {
        IReadOnlyList<string> outcomes = ParseStringArray(market.Outcomes);
        IReadOnlyList<string> prices = ParseStringArray(market.OutcomePrices);
        int count = Math.Min(outcomes.Count, prices.Count);
        return count == 0
            ? $"outcomes={market.Outcomes ?? "<null>"}, prices={market.OutcomePrices ?? "<null>"}"
            : string.Join(", ", Enumerable.Range(0, count).Select(index => $"{outcomes[index]}={FormatDecimal(prices[index])}"));
    }

    private static string FormatDecimal(string value) =>
        decimal.TryParse(value, out decimal parsed)
            ? parsed.ToString("0.000")
            : value;

    private static IReadOnlyList<string> ParseStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<string[]>(json) ?? [];
    }

    private sealed class DashboardState(DateTimeOffset windowStart, DateTimeOffset windowEnd, string slug)
    {
        public DateTimeOffset WindowStart { get; } = windowStart;

        public DateTimeOffset WindowEnd { get; } = windowEnd;

        public string Slug { get; } = slug;

        public string? Question { get; set; }

        public string? GammaPrices { get; set; }

        public string Status { get; set; } = string.Empty;

        public Dictionary<string, DashboardRowState> RowsByAssetId { get; } = new(StringComparer.Ordinal);

        public List<string> RowOrder { get; } = [];

        public BtcUpDownDashboardSnapshot ToSnapshot() =>
            new(
                WindowStart,
                WindowEnd,
                Slug,
                Question,
                GammaPrices,
                [.. RowOrder.Select(assetId =>
                {
                    DashboardRowState row = RowsByAssetId[assetId];
                    return new BtcUpDownDashboardRowSnapshot(
                        row.Label,
                        row.BestBid,
                        row.BestAsk,
                        row.LastTrade,
                        row.Spread,
                        row.Updated,
                        row.Direction);
                })],
                Status);
    }

    private static BtcPriceDirection ResolveDirection(string assetId, IReadOnlyDictionary<string, string> tokenLabels)
    {
        string label = ResolveLabel(assetId, tokenLabels);
        return label.ToUpperInvariant() switch
        {
            "UP" => BtcPriceDirection.Up,
            "DOWN" => BtcPriceDirection.Down,
            _ => BtcPriceDirection.Neutral,
        };
    }

    private sealed class DashboardRowState
    {
        public string AssetId { get; init; } = string.Empty;

        public string Label { get; init; } = string.Empty;

        public BtcPriceDirection Direction { get; init; }

        public string BestBid { get; set; } = "-";

        public string BestAsk { get; set; } = "-";

        public string LastTrade { get; set; } = "-";

        public string Spread { get; set; } = "-";

        public string Updated { get; set; } = "-";
    }
}
