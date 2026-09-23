using System;
using System.Security.Cryptography;
using TotpAuthSharp.Helper;

namespace TotpAuthSharp;

/// <summary>
///     Creates random secrets to use as the <c>accountSecretKey</c> for a new user.
/// </summary>
public static class TotpSecret
{
    /// <summary>The default secret size: 20 bytes (160 bits), as recommended by RFC 4226.</summary>
    public const int DefaultByteLength = 20;

    /// <summary>The smallest secret size accepted: 16 bytes (128 bits).</summary>
    public const int MinimumByteLength = 16;

    /// <summary>
    ///     Generates a cryptographically random secret, returned as a Base32 string (A-Z and 2-7). Store it securely
    ///     (encrypted at rest) against the user and pass it as <c>accountSecretKey</c> everywhere else.
    /// </summary>
    /// <param name="byteLength">Number of random bytes. Default is 20; at least 16.</param>
    public static string Generate(int byteLength = DefaultByteLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(byteLength, MinimumByteLength);
        return Base32.Encode(RandomNumberGenerator.GetBytes(byteLength));
    }
}
