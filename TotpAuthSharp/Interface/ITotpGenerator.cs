using System;
using System.Collections.Generic;
using System.Globalization;

namespace TotpAuthSharp.Interface;

/// <summary>
///     Generates time-based one-time passwords (RFC 6238: HMAC-SHA1, 30-second time step, 6 digits).
/// </summary>
public interface ITotpGenerator
{
    /// <summary>
    /// Generates the TOTP for the current time.
    /// </summary>
    /// <param name="accountSecretKey">User's secret key. Same as used to create the setup.</param>
    /// <returns>The 6 digit one time password as a number. Leading zeros are dropped, so 012345 is returned as 12345;
    /// use <see cref="GenerateCode" /> to get the code as the user sees it.</returns>
    int Generate(string accountSecretKey);

    /// <summary>
    /// Gets the TOTPs that are valid now, given the time tolerance.
    /// </summary>
    /// <param name="accountSecretKey">User's secret key. Same as used to create the setup.</param>
    /// <param name="timeTolerance">How far either side of now to accept codes. Every 30-second window that falls
    /// (even partly) within the tolerance is included, so 1 to 30 seconds accepts one window either side and the
    /// default 60 seconds accepts two.</param>
    /// <returns>The valid TOTPs, oldest window first.</returns>
    IEnumerable<int> GetValidTotps(string accountSecretKey, TimeSpan timeTolerance);

    /// <summary>
    /// Generates the TOTP for the current time as the six-digit string an authenticator app shows, including any
    /// leading zeros (for example "012345").
    /// </summary>
    /// <param name="accountSecretKey">User's secret key. Same as used to create the setup.</param>
    string GenerateCode(string accountSecretKey) => Generate(accountSecretKey).ToString("D6", CultureInfo.InvariantCulture);

    /// <summary>
    /// Generates the TOTP for a specific time step (Unix time divided by 30 seconds).
    /// </summary>
    /// <param name="accountSecretKey">User's secret key. Same as used to create the setup.</param>
    /// <param name="timeStep">The time step to generate the code for.</param>
    /// <exception cref="NotSupportedException">The implementation does not support time steps.</exception>
    int Generate(string accountSecretKey, long timeStep) =>
        throw new NotSupportedException($"{GetType().Name} does not implement {nameof(Generate)}(string, long).");

    /// <summary>
    /// Gets the current time step (Unix time divided by 30 seconds).
    /// </summary>
    /// <exception cref="NotSupportedException">The implementation does not support time steps.</exception>
    long GetCurrentTimeStep() =>
        throw new NotSupportedException($"{GetType().Name} does not implement {nameof(GetCurrentTimeStep)}.");
}
