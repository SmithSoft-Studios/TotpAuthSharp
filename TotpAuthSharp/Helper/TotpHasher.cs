using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace TotpAuthSharp.Helper;

internal static class TotpHasher
{
    // 10^digits for 0-9 digits, so the modulus needs no floating-point Math.Pow.
    private static readonly int[] PowersOfTen = [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000];

    internal static int Hash(string secret, long iterationNumber, int digits = 6)
    {
        return Hash(GetKey(secret), iterationNumber, digits);
    }

    internal static byte[] GetKey(string secret)
    {
        return Encoding.UTF8.GetBytes(secret);
    }

    internal static int Hash(ReadOnlySpan<byte> key, long iterationNumber, int digits = 6)
    {
        Span<byte> counter = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(counter, iterationNumber);

        // Static one-shot HMAC: no HMACSHA1 instance to allocate or dispose on every call.
        Span<byte> hash = stackalloc byte[HMACSHA1.HashSizeInBytes];
        HMACSHA1.HashData(key, counter, hash);

        var offset = hash[^1] & 0xf;

        // Convert the 4 bytes into an integer, ignoring the sign.
        var binary =
            ((hash[offset] & 0x7f) << 24)
            | (hash[offset + 1] << 16)
            | (hash[offset + 2] << 8)
            | hash[offset + 3];

        return binary % PowersOfTen[digits];
    }
}
