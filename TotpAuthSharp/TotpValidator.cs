using System;
using TotpAuthSharp.Helper;
using TotpAuthSharp.Interface;

namespace TotpAuthSharp;

/// <summary>
///     Validates time-based one-time passwords entered by a user.
/// </summary>
public class TotpValidator : ITotpValidator
{
    private readonly ITotpGenerator _totpGenerator;

    /// <summary>
    ///     Creates a validator that checks codes against <paramref name="totpGenerator" />.
    /// </summary>
    public TotpValidator(ITotpGenerator totpGenerator)
    {
        _totpGenerator = totpGenerator ?? throw new ArgumentNullException(nameof(totpGenerator));
    }

    /// <inheritdoc />
    public bool Validate(string accountSecretKey, int clientTotp, int timeToleranceInSeconds = 60)
    {
        var tolerance = TimeSpan.FromSeconds(timeToleranceInSeconds);
        if (_totpGenerator is TotpGenerator generator)
            return generator.TryMatch(accountSecretKey, clientTotp, tolerance, out _);

        // Custom ITotpGenerator implementations: compare against every valid code without stopping early.
        Guard.NotNull(accountSecretKey);
        var matched = false;
        foreach (var code in _totpGenerator.GetValidTotps(accountSecretKey, tolerance))
            matched |= code == clientTotp;

        return matched;
    }

    /// <inheritdoc />
    public bool Validate(string accountSecretKey, string? clientTotp, int timeToleranceInSeconds = 60)
    {
        Guard.NotNull(accountSecretKey);
        return TotpCodeFormat.TryParse(clientTotp, out var code) && Validate(accountSecretKey, code, timeToleranceInSeconds);
    }

    /// <inheritdoc />
    public bool TryValidate(string accountSecretKey, int clientTotp, out long matchedTimeStep, int timeToleranceInSeconds = 60)
    {
        var tolerance = TimeSpan.FromSeconds(timeToleranceInSeconds);
        if (_totpGenerator is TotpGenerator generator)
            return generator.TryMatch(accountSecretKey, clientTotp, tolerance, out matchedTimeStep);

        Guard.NotNull(accountSecretKey);
        var windows = TotpGenerator.GetWindowCount(tolerance);
        var currentStep = _totpGenerator.GetCurrentTimeStep();

        matchedTimeStep = -1;
        for (var step = currentStep - windows; step <= currentStep + windows; step++)
        {
            if (_totpGenerator.Generate(accountSecretKey, step) == clientTotp)
                matchedTimeStep = step;
        }

        return matchedTimeStep != -1;
    }
}
