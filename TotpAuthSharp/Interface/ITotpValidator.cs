using System;
using TotpAuthSharp.Helper;

namespace TotpAuthSharp.Interface;

/// <summary>
///     Validates time-based one-time passwords entered by a user.
/// </summary>
public interface ITotpValidator
{
    /// <summary>
    /// Validates a given TOTP.
    /// </summary>
    /// <param name="accountSecretKey">User's secret key. Same as used to create the setup.</param>
    /// <param name="clientTotp">Number provided by the user which has to be validated.</param>
    /// <param name="timeToleranceInSeconds">How far either side of now to accept codes, to allow for clock drift.
    /// Default is 60, which accepts the current 30-second window and two either side.</param>
    /// <returns>True or False if the validation was successful.</returns>
    bool Validate(string accountSecretKey, int clientTotp, int timeToleranceInSeconds = 60);

    /// <summary>
    /// Validates a TOTP typed by the user as text. Spaces are ignored and the code must be exactly six digits,
    /// so leading zeros are kept (for example "012 345").
    /// </summary>
    /// <param name="accountSecretKey">User's secret key. Same as used to create the setup.</param>
    /// <param name="clientTotp">The code as entered by the user.</param>
    /// <param name="timeToleranceInSeconds">How far either side of now to accept codes. Default is 60.</param>
    /// <returns>True if the code is six digits and valid; otherwise false.</returns>
    bool Validate(string accountSecretKey, string? clientTotp, int timeToleranceInSeconds = 60) =>
        TotpCodeFormat.TryParse(clientTotp, out var code) && Validate(accountSecretKey, code, timeToleranceInSeconds);

    /// <summary>
    /// Validates a TOTP and reports which time step it belongs to, so you can reject a code that has already been
    /// used (RFC 6238 section 5.2). Store <paramref name="matchedTimeStep" /> after a successful sign-in and reject
    /// later codes whose time step is not greater than the stored one.
    /// </summary>
    /// <param name="accountSecretKey">User's secret key. Same as used to create the setup.</param>
    /// <param name="clientTotp">Number provided by the user which has to be validated.</param>
    /// <param name="matchedTimeStep">The time step of the matching code, or -1 when the code is not valid.</param>
    /// <param name="timeToleranceInSeconds">How far either side of now to accept codes. Default is 60.</param>
    /// <returns>True if the code is valid; otherwise false.</returns>
    /// <exception cref="NotSupportedException">The implementation does not support time steps.</exception>
    bool TryValidate(string accountSecretKey, int clientTotp, out long matchedTimeStep, int timeToleranceInSeconds = 60) =>
        throw new NotSupportedException($"{GetType().Name} does not implement {nameof(TryValidate)}.");
}
