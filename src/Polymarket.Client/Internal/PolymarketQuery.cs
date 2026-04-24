using System.Globalization;

namespace Polymarket.Client.Internal;

internal static class PolymarketQuery
{
    public static string Append(string path, IReadOnlyDictionary<string, string?>? parameters)
    {
        if (parameters is null || parameters.Count == 0)
        {
            return path;
        }

        List<string> pairs = [];
        foreach ((string key, string? value) in parameters)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            pairs.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }

        if (pairs.Count == 0)
        {
            return path;
        }

        return $"{path}?{string.Join("&", pairs)}";
    }

    public static Dictionary<string, string?> FromTradeParameters(TradeParameters? parameters)
    {
        Dictionary<string, string?> query = [];
        if (parameters is null)
        {
            return query;
        }

        query["id"] = parameters.Id;
        query["maker_address"] = parameters.MakerAddress;
        query["market"] = parameters.Market;
        query["asset_id"] = parameters.AssetId;
        query["before"] = parameters.Before;
        query["after"] = parameters.After;
        return query;
    }

    public static Dictionary<string, string?> FromMarketQueryParameters(MarketQueryParameters? parameters)
    {
        Dictionary<string, string?> query = [];
        if (parameters is null)
        {
            return query;
        }

        foreach ((string key, string? value) in parameters.AdditionalParameters)
        {
            query[key] = value;
        }

        query["next_cursor"] = parameters.NextCursor;
        query["limit"] = parameters.Limit?.ToString(CultureInfo.InvariantCulture);
        return query;
    }

    public static Dictionary<string, string?> FromOpenOrderParameters(OpenOrderParameters? parameters)
    {
        Dictionary<string, string?> query = [];
        if (parameters is null)
        {
            return query;
        }

        query["id"] = parameters.Id;
        query["market"] = parameters.Market;
        query["asset_id"] = parameters.AssetId;
        return query;
    }

    public static Dictionary<string, string?> FromPriceHistoryParameters(PriceHistoryParameters parameters)
    {
        Dictionary<string, string?> query = [];
        query["market"] = parameters.Market;
        query["startTs"] = parameters.StartTimestamp?.ToString(CultureInfo.InvariantCulture);
        query["endTs"] = parameters.EndTimestamp?.ToString(CultureInfo.InvariantCulture);
        query["fidelity"] = parameters.Fidelity?.ToString(CultureInfo.InvariantCulture);
        query["interval"] = parameters.Interval?.ToApiString();
        return query;
    }

    public static Dictionary<string, string?> FromDropNotifications(DropNotificationParameters? parameters)
    {
        Dictionary<string, string?> query = [];
        if (parameters?.Ids is null)
        {
            return query;
        }

        for (int index = 0; index < parameters.Ids.Count; index++)
        {
            query[$"ids[{index}]"] = parameters.Ids[index];
        }

        return query;
    }

    public static Dictionary<string, string?> FromBalanceAllowanceParameters(BalanceAllowanceParameters? parameters, SignatureTypeV2 signatureType)
    {
        Dictionary<string, string?> query = [];
        if (parameters is not null)
        {
            query["asset_type"] = parameters.AssetType.ToApiString();
            query["token_id"] = parameters.TokenId;
            query["signature_type"] = (parameters.SignatureType ?? (int)signatureType).ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            query["signature_type"] = ((int)signatureType).ToString(CultureInfo.InvariantCulture);
        }

        return query;
    }
}
