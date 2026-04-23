using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Utilities.Encoders;
using BCECPoint = Org.BouncyCastle.Math.EC.ECPoint;

namespace Polymarket.Client.Internal;

internal sealed class PolymarketSigner
{
    private static readonly X9ECParameters CurveParameters = SecNamedCurves.GetByName("secp256k1");
    private static readonly ECDomainParameters DomainParameters = new(
        CurveParameters.Curve,
        CurveParameters.G,
        CurveParameters.N,
        CurveParameters.H);
    private static readonly BigInteger HalfCurveOrder = CurveParameters.N.ShiftRight(1);

    private readonly BigInteger _privateKey;
    private readonly BCECPoint _publicKey;

    public PolymarketSigner(string privateKey)
    {
        _privateKey = new BigInteger(1, ParseHex(privateKey));
        _publicKey = DomainParameters.G.Multiply(_privateKey).Normalize();
        Address = ComputeAddress(_publicKey);
    }

    public string Address { get; }

    public string SignClobAuthMessage(Chain chain, long timestamp, long nonce)
    {
        byte[] digest = BuildTypedDataDigest(
            BuildAuthDomainFields(chain),
            "ClobAuth",
            [
                new TypedField("address", "address", Address),
                new TypedField("timestamp", "string", timestamp.ToString(CultureInfo.InvariantCulture)),
                new TypedField("nonce", "uint256", nonce.ToString(CultureInfo.InvariantCulture)),
                new TypedField("message", "string", PolymarketConstants.AuthMessage),
            ]);

        return SignDigest(digest);
    }

    public string SignOrderV1(Chain chain, string exchangeAddress, SignedOrderV1 order)
    {
        byte[] digest = BuildTypedDataDigest(
            BuildOrderDomainFields(chain, exchangeAddress, "1"),
            "Order",
            [
                new TypedField("salt", "uint256", order.Salt),
                new TypedField("maker", "address", order.Maker),
                new TypedField("signer", "address", order.Signer),
                new TypedField("taker", "address", order.Taker),
                new TypedField("tokenId", "uint256", order.TokenId),
                new TypedField("makerAmount", "uint256", order.MakerAmount),
                new TypedField("takerAmount", "uint256", order.TakerAmount),
                new TypedField("expiration", "uint256", order.Expiration),
                new TypedField("nonce", "uint256", order.Nonce),
                new TypedField("feeRateBps", "uint256", order.FeeRateBps),
                new TypedField("side", "uint8", (order.Side == Side.Buy ? 0 : 1).ToString(CultureInfo.InvariantCulture)),
                new TypedField("signatureType", "uint8", ((int)order.SignatureType).ToString(CultureInfo.InvariantCulture)),
            ]);

        return SignDigest(digest);
    }

    public string SignOrderV2(Chain chain, string exchangeAddress, SignedOrderV2 order)
    {
        byte[] digest = BuildTypedDataDigest(
            BuildOrderDomainFields(chain, exchangeAddress, "2"),
            "Order",
            [
                new TypedField("salt", "uint256", order.Salt),
                new TypedField("maker", "address", order.Maker),
                new TypedField("signer", "address", order.Signer),
                new TypedField("tokenId", "uint256", order.TokenId),
                new TypedField("makerAmount", "uint256", order.MakerAmount),
                new TypedField("takerAmount", "uint256", order.TakerAmount),
                new TypedField("side", "uint8", (order.Side == Side.Buy ? 0 : 1).ToString(CultureInfo.InvariantCulture)),
                new TypedField("signatureType", "uint8", ((int)order.SignatureType).ToString(CultureInfo.InvariantCulture)),
                new TypedField("timestamp", "uint256", order.Timestamp),
                new TypedField("metadata", "bytes32", order.Metadata),
                new TypedField("builder", "bytes32", order.Builder),
            ]);

        return SignDigest(digest);
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

    private string SignDigest(byte[] digest)
    {
        ECDsaSigner signer = new(new HMacDsaKCalculator(new Sha256Digest()));
        signer.Init(true, new ECPrivateKeyParameters(_privateKey, DomainParameters));

        BigInteger[] signature = signer.GenerateSignature(digest);
        BigInteger r = signature[0];
        BigInteger s = signature[1];
        if (s.CompareTo(HalfCurveOrder) > 0)
        {
            s = CurveParameters.N.Subtract(s);
        }

        int recoveryId = GetRecoveryId(r, s, digest);
        byte[] output = new byte[65];
        CopyFixed(r, output, 0);
        CopyFixed(s, output, 32);
        output[64] = (byte)(recoveryId + 27);
        return $"0x{Hex.ToHexString(output)}";
    }

    private int GetRecoveryId(BigInteger r, BigInteger s, byte[] digest)
    {
        for (int recoveryId = 0; recoveryId < 4; recoveryId++)
        {
            BCECPoint? recovered = RecoverPublicKey(recoveryId, r, s, digest);
            if (recovered is not null && recovered.Normalize().Equals(_publicKey))
            {
                return recoveryId;
            }
        }

        throw new InvalidOperationException("Failed to calculate the recovery id for the signature.");
    }

    private static BCECPoint? RecoverPublicKey(int recoveryId, BigInteger r, BigInteger s, byte[] digest)
    {
        BigInteger n = CurveParameters.N;
        BigInteger i = BigInteger.ValueOf(recoveryId / 2L);
        BigInteger x = r.Add(i.Multiply(n));
        FpCurve curve = (FpCurve)CurveParameters.Curve;
        if (x.CompareTo(curve.Q) >= 0)
        {
            return null;
        }

        BCECPoint rPoint = DecompressKey(x, (recoveryId & 1) == 1);
        if (!rPoint.Multiply(n).IsInfinity)
        {
            return null;
        }

        BigInteger e = new BigInteger(1, digest);
        BigInteger eInverse = BigInteger.Zero.Subtract(e).Mod(n);
        BigInteger rInverse = r.ModInverse(n);
        BigInteger srInverse = rInverse.Multiply(s).Mod(n);
        BigInteger eInvrInverse = rInverse.Multiply(eInverse).Mod(n);

        return ECAlgorithms.SumOfTwoMultiplies(DomainParameters.G, eInvrInverse, rPoint, srInverse);
    }

    private static BCECPoint DecompressKey(BigInteger x, bool yBit)
    {
        byte[] encoded = new byte[33];
        encoded[0] = (byte)(yBit ? 0x03 : 0x02);
        byte[] xBytes = ToFixedUnsigned(x);
        Buffer.BlockCopy(xBytes, 0, encoded, 33 - xBytes.Length, xBytes.Length);
        return CurveParameters.Curve.DecodePoint(encoded);
    }

    private static byte[] BuildTypedDataDigest(IReadOnlyList<TypedField> domainFields, string primaryType, IReadOnlyList<TypedField> messageFields)
    {
        byte[] domainSeparator = HashStruct("EIP712Domain", domainFields);
        byte[] messageHash = HashStruct(primaryType, messageFields);

        byte[] payload = new byte[2 + domainSeparator.Length + messageHash.Length];
        payload[0] = 0x19;
        payload[1] = 0x01;
        Buffer.BlockCopy(domainSeparator, 0, payload, 2, domainSeparator.Length);
        Buffer.BlockCopy(messageHash, 0, payload, 2 + domainSeparator.Length, messageHash.Length);
        return Keccak(payload);
    }

    private static byte[] HashStruct(string typeName, IReadOnlyList<TypedField> fields) =>
        Keccak(EncodeStruct(typeName, fields));

    private static byte[] EncodeStruct(string typeName, IReadOnlyList<TypedField> fields)
    {
        using MemoryStream stream = new();
        byte[] typeHash = Keccak(Encoding.UTF8.GetBytes(EncodeType(typeName, fields)));
        stream.Write(typeHash, 0, typeHash.Length);

        foreach (TypedField field in fields)
        {
            byte[] encodedValue = EncodeField(field);
            stream.Write(encodedValue, 0, encodedValue.Length);
        }

        return stream.ToArray();
    }

    private static string EncodeType(string typeName, IReadOnlyList<TypedField> fields) =>
        $"{typeName}({string.Join(",", fields.Select(static field => $"{field.Type} {field.Name}"))})";

    private static byte[] EncodeField(TypedField field) => field.Type switch
    {
        "string" => Keccak(Encoding.UTF8.GetBytes(field.Value)),
        "address" => LeftPad(ParseAddress(field.Value), 32),
        "uint256" => LeftPad(ToFixedUnsigned(ParseUnsigned(field.Value)), 32),
        "uint8" => LeftPad(ToFixedUnsigned(ParseUnsigned(field.Value)), 32),
        "bytes32" => ParseBytes32(field.Value),
        _ => throw new NotSupportedException($"Unsupported EIP-712 field type '{field.Type}'."),
    };

    private static IReadOnlyList<TypedField> BuildAuthDomainFields(Chain chain) =>
    [
        new TypedField("name", "string", PolymarketConstants.AuthDomainName),
        new TypedField("version", "string", PolymarketConstants.AuthDomainVersion),
        new TypedField("chainId", "uint256", ((int)chain).ToString(CultureInfo.InvariantCulture)),
    ];

    private static IReadOnlyList<TypedField> BuildOrderDomainFields(Chain chain, string exchangeAddress, string version) =>
    [
        new TypedField("name", "string", PolymarketConstants.ExchangeDomainName),
        new TypedField("version", "string", version),
        new TypedField("chainId", "uint256", ((int)chain).ToString(CultureInfo.InvariantCulture)),
        new TypedField("verifyingContract", "address", exchangeAddress),
    ];

    private static string ComputeAddress(BCECPoint publicKey)
    {
        byte[] encoded = publicKey.GetEncoded(false);
        byte[] hash = Keccak(encoded[1..]);
        byte[] address = hash[^20..];
        return $"0x{Hex.ToHexString(address)}";
    }

    private static void CopyFixed(BigInteger value, byte[] destination, int offset)
    {
        byte[] bytes = LeftPad(ToFixedUnsigned(value), 32);
        Buffer.BlockCopy(bytes, 0, destination, offset, 32);
    }

    private static byte[] ParseAddress(string value)
    {
        byte[] bytes = ParseHex(value);
        if (bytes.Length != 20)
        {
            throw new InvalidOperationException($"Address '{value}' is not 20 bytes.");
        }

        return bytes;
    }

    private static byte[] ParseBytes32(string value)
    {
        byte[] bytes = ParseHex(value);
        if (bytes.Length != 32)
        {
            throw new InvalidOperationException($"bytes32 value '{value}' is not 32 bytes.");
        }

        return bytes;
    }

    private static byte[] ParseHex(string value)
    {
        string normalized = NormalizeHex(value);
        return Hex.Decode(normalized);
    }

    private static string NormalizeHex(string value)
    {
        string normalized = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? value[2..]
            : value;

        if (normalized.Length % 2 != 0)
        {
            normalized = $"0{normalized}";
        }

        return normalized;
    }

    private static BigInteger ParseUnsigned(string value)
    {
        BigInteger parsed;
        try
        {
            parsed = new BigInteger(value);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Value '{value}' is not a valid unsigned integer.", ex);
        }

        if (parsed.SignValue < 0)
        {
            throw new InvalidOperationException($"Value '{value}' is not a valid unsigned integer.");
        }

        return parsed;
    }

    private static byte[] ToFixedUnsigned(BigInteger value)
    {
        byte[] bytes = value.ToByteArrayUnsigned();
        return bytes.Length == 0 ? [0] : bytes;
    }

    private static byte[] LeftPad(byte[] value, int length)
    {
        if (value.Length > length)
        {
            throw new InvalidOperationException($"Encoded value exceeds {length} bytes.");
        }

        byte[] padded = new byte[length];
        Buffer.BlockCopy(value, 0, padded, length - value.Length, value.Length);
        return padded;
    }

    private static byte[] Keccak(byte[] input)
    {
        KeccakDigest digest = new(256);
        digest.BlockUpdate(input, 0, input.Length);
        byte[] output = new byte[32];
        digest.DoFinal(output, 0);
        return output;
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

    private readonly record struct TypedField(string Name, string Type, string Value);
}
