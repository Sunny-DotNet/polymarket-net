namespace Polymarket.Client;

public static class PolymarketConstants
{
    public const string InitialCursor = "MA==";
    public const string EndCursor = "LTE=";
    public const string ZeroAddress = "0x0000000000000000000000000000000000000000";
    public const string Bytes32Zero = "0x0000000000000000000000000000000000000000000000000000000000000000";
    public const string OrderVersionMismatchError = "order_version_mismatch";
    public const int BuilderFeesBps = 10000;
    public const string AuthDomainName = "ClobAuthDomain";
    public const string AuthDomainVersion = "1";
    public const string AuthMessage = "This message attests that I control the given wallet";
    public const string ExchangeDomainName = "Polymarket CTF Exchange";
}
