namespace Polymarket.Client;

public sealed record ContractConfig(
    string Exchange,
    string NegRiskAdapter,
    string NegRiskExchange,
    string Collateral,
    string ConditionalTokens,
    string ExchangeV2,
    string NegRiskExchangeV2);
