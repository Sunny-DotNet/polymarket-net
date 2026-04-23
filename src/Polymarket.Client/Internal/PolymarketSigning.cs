using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;

namespace Polymarket.Client.Internal;

internal sealed class PolymarketSigner
{
    private readonly EthECKey _key;

    public PolymarketSigner(string privateKey)
    {
        _key = new EthECKey(privateKey);
        Address = _key.GetPublicAddress();
    }

    public string Address { get; }

    public string SignClobAuthMessage(Chain chain, long timestamp, long nonce)
    {
        string typedDataJson = $$"""
        {
          "types": {
            "EIP712Domain": [
              { "name": "name", "type": "string" },
              { "name": "version", "type": "string" },
              { "name": "chainId", "type": "uint256" }
            ],
            "ClobAuth": [
              { "name": "address", "type": "address" },
              { "name": "timestamp", "type": "string" },
              { "name": "nonce", "type": "uint256" },
              { "name": "message", "type": "string" }
            ]
          },
          "domain": {
            "name": "{{PolymarketConstants.AuthDomainName}}",
            "version": "{{PolymarketConstants.AuthDomainVersion}}",
            "chainId": {{(int)chain}}
          },
          "primaryType": "ClobAuth",
          "message": {
            "address": "{{Address}}",
            "timestamp": "{{timestamp.ToString(CultureInfo.InvariantCulture)}}",
            "nonce": {{nonce}},
            "message": "{{PolymarketConstants.AuthMessage}}"
          }
        }
        """;

        return new Eip712TypedDataSigner().SignTypedDataV4(typedDataJson, _key);
    }

    public string SignOrderV1(Chain chain, string exchangeAddress, SignedOrderV1 order)
    {
        string typedDataJson = $$"""
        {
          "types": {
            "EIP712Domain": [
              { "name": "name", "type": "string" },
              { "name": "version", "type": "string" },
              { "name": "chainId", "type": "uint256" },
              { "name": "verifyingContract", "type": "address" }
            ],
            "Order": [
              { "name": "salt", "type": "uint256" },
              { "name": "maker", "type": "address" },
              { "name": "signer", "type": "address" },
              { "name": "taker", "type": "address" },
              { "name": "tokenId", "type": "uint256" },
              { "name": "makerAmount", "type": "uint256" },
              { "name": "takerAmount", "type": "uint256" },
              { "name": "expiration", "type": "uint256" },
              { "name": "nonce", "type": "uint256" },
              { "name": "feeRateBps", "type": "uint256" },
              { "name": "side", "type": "uint8" },
              { "name": "signatureType", "type": "uint8" }
            ]
          },
          "domain": {
            "name": "{{PolymarketConstants.ExchangeDomainName}}",
            "version": "1",
            "chainId": {{(int)chain}},
            "verifyingContract": "{{exchangeAddress}}"
          },
          "primaryType": "Order",
          "message": {
            "salt": "{{order.Salt}}",
            "maker": "{{order.Maker}}",
            "signer": "{{order.Signer}}",
            "taker": "{{order.Taker}}",
            "tokenId": "{{order.TokenId}}",
            "makerAmount": "{{order.MakerAmount}}",
            "takerAmount": "{{order.TakerAmount}}",
            "expiration": "{{order.Expiration}}",
            "nonce": "{{order.Nonce}}",
            "feeRateBps": "{{order.FeeRateBps}}",
            "side": {{(order.Side == Side.Buy ? 0 : 1)}},
            "signatureType": {{(int)order.SignatureType}}
          }
        }
        """;

        return new Eip712TypedDataSigner().SignTypedDataV4(typedDataJson, _key);
    }

    public string SignOrderV2(Chain chain, string exchangeAddress, SignedOrderV2 order)
    {
        string typedDataJson = $$"""
        {
          "types": {
            "EIP712Domain": [
              { "name": "name", "type": "string" },
              { "name": "version", "type": "string" },
              { "name": "chainId", "type": "uint256" },
              { "name": "verifyingContract", "type": "address" }
            ],
            "Order": [
              { "name": "salt", "type": "uint256" },
              { "name": "maker", "type": "address" },
              { "name": "signer", "type": "address" },
              { "name": "tokenId", "type": "uint256" },
              { "name": "makerAmount", "type": "uint256" },
              { "name": "takerAmount", "type": "uint256" },
              { "name": "side", "type": "uint8" },
              { "name": "signatureType", "type": "uint8" },
              { "name": "timestamp", "type": "uint256" },
              { "name": "metadata", "type": "bytes32" },
              { "name": "builder", "type": "bytes32" }
            ]
          },
          "domain": {
            "name": "{{PolymarketConstants.ExchangeDomainName}}",
            "version": "2",
            "chainId": {{(int)chain}},
            "verifyingContract": "{{exchangeAddress}}"
          },
          "primaryType": "Order",
          "message": {
            "salt": "{{order.Salt}}",
            "maker": "{{order.Maker}}",
            "signer": "{{order.Signer}}",
            "tokenId": "{{order.TokenId}}",
            "makerAmount": "{{order.MakerAmount}}",
            "takerAmount": "{{order.TakerAmount}}",
            "side": {{(order.Side == Side.Buy ? 0 : 1)}},
            "signatureType": {{(int)order.SignatureType}},
            "timestamp": "{{order.Timestamp}}",
            "metadata": "{{order.Metadata}}",
            "builder": "{{order.Builder}}"
          }
        }
        """;

        return new Eip712TypedDataSigner().SignTypedDataV4(typedDataJson, _key);
    }

    public static string BuildL2Signature(string secret, long timestamp, HttpMethod method, string requestPath, string? body)
    {
        byte[] decodedSecret = Base64UrlDecode(secret);
        string payload = string.Concat(
            timestamp.ToString(CultureInfo.InvariantCulture),
            method.Method.ToUpperInvariant(),
            requestPath,
            body ?? string.Empty);

        using HMACSHA256 hmac = new(decodedSecret);
        byte[] digest = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Base64UrlEncode(digest);
    }

    private static byte[] Base64UrlDecode(string value)
    {
        string padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return Convert.FromBase64String(padded);
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).Replace('+', '-').Replace('/', '_');
}
