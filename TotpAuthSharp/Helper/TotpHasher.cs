﻿using System;
using System.Security.Cryptography;
using System.Text;

namespace TotpAuthSharp.Helper;

internal static class TotpHasher
{
    internal static int Hash(string secret, long iterationNumber, int digits = 6)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        return _hash(key, iterationNumber, digits);
    }

    private static int _hash(byte[] key, long iterationNumber, int digits = 6)
    {
        var counter = BitConverter.GetBytes(iterationNumber);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counter);
        }

        var hmac = new HMACSHA1(key);

        var hash = hmac.ComputeHash(counter);

        var offset = hash[hash.Length - 1] & 0xf;

        // Convert the 4 bytes into an integer, ignoring the sign.
        var binary =
            ((hash[offset] & 0x7f) << 24)
            | (hash[offset + 1] << 16)
            | (hash[offset + 2] << 8)
            | (hash[offset + 3]);

        var password = binary % (int)Math.Pow(10, digits);
        return password;
    }
}