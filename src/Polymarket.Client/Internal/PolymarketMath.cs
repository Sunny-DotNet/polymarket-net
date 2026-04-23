using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Polymarket.Client.Internal;

internal static class PolymarketMath
{
    public static readonly IReadOnlyDictionary<string, RoundConfig> RoundingConfig = new Dictionary<string, RoundConfig>(StringComparer.Ordinal)
    {
        ["0.1"] = new RoundConfig(1, 2, 3),
        ["0.01"] = new RoundConfig(2, 2, 4),
        ["0.001"] = new RoundConfig(3, 2, 5),
        ["0.0001"] = new RoundConfig(4, 2, 6),
    };

    public static decimal RoundNormal(decimal value, int decimals) => Math.Round(value, decimals, MidpointRounding.AwayFromZero);

    public static decimal RoundDown(decimal value, int decimals)
    {
        decimal scale = Pow10(decimals);
        return Math.Floor(value * scale) / scale;
    }

    public static decimal RoundUp(decimal value, int decimals)
    {
        decimal scale = Pow10(decimals);
        return Math.Ceiling(value * scale) / scale;
    }

    public static bool PriceValid(decimal price, string tickSize)
    {
        decimal tick = decimal.Parse(tickSize, CultureInfo.InvariantCulture);
        return price >= tick && price <= 1m - tick;
    }

    public static string GenerateOrderBookHash(OrderBookSummary orderBook)
    {
        OrderBookSummary hashless = orderBook with { Hash = string.Empty };
        string serialized = PolymarketJson.Serialize(hashless);
        byte[] bytes = SHA1.HashData(Encoding.UTF8.GetBytes(serialized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string GenerateSalt() =>
        Random.Shared.NextInt64(1, Math.Max(2, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())).ToString(CultureInfo.InvariantCulture);

    public static (Side Side, string MakerAmount, string TakerAmount) GetOrderAmounts(Side side, decimal size, decimal price, RoundConfig config)
    {
        decimal rawPrice = RoundNormal(price, config.Price);

        if (side == Side.Buy)
        {
            decimal rawTakerAmount = RoundDown(size, config.Size);
            decimal rawMakerAmount = rawTakerAmount * rawPrice;
            rawMakerAmount = NormalizeAmountPrecision(rawMakerAmount, config.Amount);
            return (Side.Buy, ToTokenDecimals(rawMakerAmount), ToTokenDecimals(rawTakerAmount));
        }

        decimal sellMakerAmount = RoundDown(size, config.Size);
        decimal sellTakerAmount = sellMakerAmount * rawPrice;
        sellTakerAmount = NormalizeAmountPrecision(sellTakerAmount, config.Amount);
        return (Side.Sell, ToTokenDecimals(sellMakerAmount), ToTokenDecimals(sellTakerAmount));
    }

    public static (Side Side, string MakerAmount, string TakerAmount) GetMarketOrderAmounts(Side side, decimal amount, decimal price, RoundConfig config)
    {
        decimal rawPrice = RoundNormal(price, config.Price);
        if (side == Side.Buy)
        {
            decimal rawMakerAmount = RoundDown(amount, config.Size);
            decimal rawTakerAmount = rawMakerAmount / rawPrice;
            rawTakerAmount = NormalizeAmountPrecision(rawTakerAmount, config.Amount);
            return (Side.Buy, ToTokenDecimals(rawMakerAmount), ToTokenDecimals(rawTakerAmount));
        }

        decimal rawSellMakerAmount = RoundDown(amount, config.Size);
        decimal rawSellTakerAmount = rawSellMakerAmount * rawPrice;
        rawSellTakerAmount = NormalizeAmountPrecision(rawSellTakerAmount, config.Amount);
        return (Side.Sell, ToTokenDecimals(rawSellMakerAmount), ToTokenDecimals(rawSellTakerAmount));
    }

    public static decimal CalculateBuyMarketPrice(IReadOnlyList<OrderSummary> positions, decimal amountToMatch, OrderType orderType)
    {
        if (positions.Count == 0)
        {
            throw new InvalidOperationException("no match");
        }

        decimal sum = 0;
        for (int index = positions.Count - 1; index >= 0; index--)
        {
            OrderSummary position = positions[index];
            sum += decimal.Parse(position.Size, CultureInfo.InvariantCulture) * decimal.Parse(position.Price, CultureInfo.InvariantCulture);
            if (sum >= amountToMatch)
            {
                return decimal.Parse(position.Price, CultureInfo.InvariantCulture);
            }
        }

        if (orderType == OrderType.Fok)
        {
            throw new InvalidOperationException("no match");
        }

        return decimal.Parse(positions[0].Price, CultureInfo.InvariantCulture);
    }

    public static decimal CalculateSellMarketPrice(IReadOnlyList<OrderSummary> positions, decimal amountToMatch, OrderType orderType)
    {
        if (positions.Count == 0)
        {
            throw new InvalidOperationException("no match");
        }

        decimal sum = 0;
        for (int index = positions.Count - 1; index >= 0; index--)
        {
            OrderSummary position = positions[index];
            sum += decimal.Parse(position.Size, CultureInfo.InvariantCulture);
            if (sum >= amountToMatch)
            {
                return decimal.Parse(position.Price, CultureInfo.InvariantCulture);
            }
        }

        if (orderType == OrderType.Fok)
        {
            throw new InvalidOperationException("no match");
        }

        return decimal.Parse(positions[0].Price, CultureInfo.InvariantCulture);
    }

    public static decimal AdjustBuyAmountForFees(
        decimal amount,
        decimal price,
        decimal userUsdcBalance,
        decimal feeRate,
        int feeExponent,
        decimal builderTakerFeeRate)
    {
        decimal platformFeeRate = feeRate * (decimal)Math.Pow((double)(price * (1m - price)), feeExponent);
        decimal platformFee = (amount / price) * platformFeeRate;
        decimal totalCost = amount + platformFee + (amount * builderTakerFeeRate);
        if (userUsdcBalance <= totalCost)
        {
            return userUsdcBalance / (1m + (platformFeeRate / price) + builderTakerFeeRate);
        }

        return amount;
    }

    private static decimal NormalizeAmountPrecision(decimal amount, int decimals)
    {
        if (GetDecimalPlaces(amount) <= decimals)
        {
            return amount;
        }

        decimal roundedUp = RoundUp(amount, decimals + 4);
        if (GetDecimalPlaces(roundedUp) <= decimals)
        {
            return roundedUp;
        }

        return RoundDown(roundedUp, decimals);
    }

    private static string ToTokenDecimals(decimal value)
    {
        decimal scaled = value * 1_000_000m;
        decimal integral = decimal.Truncate(scaled);
        return integral.ToString(CultureInfo.InvariantCulture);
    }

    private static int GetDecimalPlaces(decimal value)
    {
        int[] bits = decimal.GetBits(value);
        return (bits[3] >> 16) & 0x7F;
    }

    private static decimal Pow10(int exponent)
    {
        decimal value = 1m;
        for (int index = 0; index < exponent; index++)
        {
            value *= 10m;
        }

        return value;
    }
}
