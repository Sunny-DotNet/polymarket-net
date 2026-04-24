using System.Globalization;

internal sealed class ConsoleDashboardRenderer(TextWriter writer)
{
    private readonly TextWriter _writer = writer;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly bool _interactive = ReferenceEquals(writer, Console.Out) && !Console.IsOutputRedirected;
    private int? _top;
    private int _lastLineCount;

    public async Task RenderAsync(BtcUpDownDashboardSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        DashboardRenderLine[] lines = BuildLines(snapshot);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_interactive)
            {
                await RenderInteractiveAsync(lines, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await RenderPlainTextAsync(lines, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RenderInteractiveAsync(DashboardRenderLine[] lines, CancellationToken cancellationToken)
    {
        int width = Math.Max(60, Console.BufferWidth - 1);
        _top ??= Console.CursorTop;
        Console.SetCursorPosition(0, _top.Value);

        foreach (DashboardRenderLine line in lines)
        {
            Console.ForegroundColor = line.Color ?? ConsoleColor.Gray;
            string normalized = NormalizeLine(line.Text, width);
            await _writer.WriteAsync($"{normalized}{Environment.NewLine}".AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        Console.ResetColor();

        for (int index = lines.Length; index < _lastLineCount; index++)
        {
            string blank = new(' ', width);
            await _writer.WriteAsync($"{blank}{Environment.NewLine}".AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        _lastLineCount = lines.Length;
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        Console.SetCursorPosition(0, _top.Value + lines.Length);
    }

    private async Task RenderPlainTextAsync(DashboardRenderLine[] lines, CancellationToken cancellationToken)
    {
        string block = string.Join(Environment.NewLine, lines.Select(static line => line.Text));
        await _writer.WriteAsync($"{block}{Environment.NewLine}{Environment.NewLine}".AsMemory(), cancellationToken).ConfigureAwait(false);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static DashboardRenderLine[] BuildLines(BtcUpDownDashboardSnapshot snapshot)
    {
        List<DashboardRenderLine> lines =
        [
            Plain($"Window : {snapshot.WindowStart:yyyy-MM-dd HH:mm:ss}Z - {snapshot.WindowEnd:HH:mm:ss}Z"),
            Plain($"Slug   : {snapshot.Slug}"),
            Plain($"Market : {snapshot.Question ?? "Waiting for market..."}"),
            Plain($"Gamma  : {snapshot.GammaPrices ?? "-"}"),
            Plain(string.Empty),
        ];

        string header = string.Format(
            CultureInfo.InvariantCulture,
            "{0,-8} {1,9} {2,9} {3,11} {4,8} {5,8}",
            "Outcome",
            "Bid",
            "Ask",
            "Last",
            "Spread",
            "Updated");

        lines.Add(Plain(header));
        lines.Add(Plain(new string('-', header.Length)));

        foreach (BtcUpDownDashboardRowSnapshot row in snapshot.Rows)
        {
            lines.Add(new DashboardRenderLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0,-8} {1,9} {2,9} {3,11} {4,8} {5,8}",
                    row.Label,
                    FormatPrice(row.BestBid, row.Direction),
                    FormatPrice(row.BestAsk, row.Direction),
                    FormatPrice(row.LastTrade, row.Direction),
                    FormatPlainNumber(row.Spread),
                    row.Updated),
                GetRowColor(row.Direction)));
        }

        if (snapshot.Rows.Count == 0)
        {
            lines.Add(Plain("Waiting for websocket data..."));
        }

        lines.Add(Plain(string.Empty));
        lines.Add(Plain($"Status : {snapshot.Status}"));
        return [.. lines];
    }

    private static DashboardRenderLine Plain(string text) => new(text, null);

    private static string FormatPrice(string value, BtcPriceDirection direction)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
        {
            return value;
        }

        string marker = direction switch
        {
            BtcPriceDirection.Up => "+",
            BtcPriceDirection.Down => "-",
            _ => string.Empty,
        };

        return $"{marker}{parsed:0.000}";
    }

    private static string FormatPlainNumber(string value)
    {
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
        {
            return value;
        }

        return parsed.ToString("0.000", CultureInfo.InvariantCulture);
    }

    private static ConsoleColor? GetRowColor(BtcPriceDirection direction) => direction switch
    {
        BtcPriceDirection.Up => ConsoleColor.Green,
        BtcPriceDirection.Down => ConsoleColor.Red,
        _ => null,
    };

    private static string NormalizeLine(string line, int width)
    {
        if (line.Length > width)
        {
            return line[..width];
        }

        return line.PadRight(width);
    }
}

internal sealed record BtcUpDownDashboardSnapshot(
    DateTimeOffset WindowStart,
    DateTimeOffset WindowEnd,
    string Slug,
    string? Question,
    string? GammaPrices,
    IReadOnlyList<BtcUpDownDashboardRowSnapshot> Rows,
    string Status);

internal sealed record BtcUpDownDashboardRowSnapshot(
    string Label,
    string BestBid,
    string BestAsk,
    string LastTrade,
    string Spread,
    string Updated,
    BtcPriceDirection Direction);

internal readonly record struct DashboardRenderLine(string Text, ConsoleColor? Color);

internal enum BtcPriceDirection
{
    Neutral,
    Up,
    Down,
}
