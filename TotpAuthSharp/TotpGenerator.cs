using System;
using System.Collections.Generic;
using System.Globalization;
using TotpAuthSharp.Helper;
using TotpAuthSharp.Interface;

namespace TotpAuthSharp;

/// <summary>
///     Generates time-based one-time passwords (RFC 6238: HMAC-SHA1, 30-second time step, 6 digits).
/// </summary>
public class TotpGenerator : ITotpGenerator
{
    private const int TimeStepSeconds = 30;

    private readonly TimeProvider _timeProvider;

    /// <summary>
    ///     Creates a generator that uses the system clock.
    /// </summary>
    public TotpGenerator()
        : this(TimeProvider.System)
    {
    }

    /// <summary>
    ///     Creates a generator that reads the time from <paramref name="timeProvider" />, for example a fake clock in tests.
    /// </summary>
    public TotpGenerator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public int Generate(string accountSecretKey)
    {
        return Generate(accountSecretKey, GetCurrentTimeStep());
    }

    /// <inheritdoc />
    public int Generate(string accountSecretKey, long timeStep)
    {
        Guard.NotNull(accountSecretKey);
        return TotpHasher.Hash(accountSecretKey, timeStep);
    }

    /// <inheritdoc />
    public string GenerateCode(string accountSecretKey)
    {
        return Generate(accountSecretKey).ToString("D6", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public long GetCurrentTimeStep()
    {
        return _timeProvider.GetUtcNow().ToUnixTimeSeconds() / TimeStepSeconds;
    }

    /// <inheritdoc />
    public IEnumerable<int> GetValidTotps(string accountSecretKey, TimeSpan timeTolerance)
    {
        Guard.NotNull(accountSecretKey);
        var windows = GetWindowCount(timeTolerance);
        var key = TotpHasher.GetKey(accountSecretKey);
        var currentStep = GetCurrentTimeStep();

        var codes = new int[windows * 2 + 1];
        for (var i = 0; i < codes.Length; i++)
            codes[i] = TotpHasher.Hash(key, currentStep - windows + i);

        return codes;
    }

    /// <summary>
    ///     Finds <paramref name="clientTotp" /> among the codes valid now without building a list of codes.
    ///     Every window is checked, so the time taken does not reveal which window matched.
    /// </summary>
    internal bool TryMatch(string accountSecretKey, int clientTotp, TimeSpan timeTolerance, out long matchedTimeStep)
    {
        Guard.NotNull(accountSecretKey);
        var windows = GetWindowCount(timeTolerance);
        var key = TotpHasher.GetKey(accountSecretKey);
        var currentStep = GetCurrentTimeStep();

        matchedTimeStep = -1;
        for (var step = currentStep - windows; step <= currentStep + windows; step++)
        {
            if (TotpHasher.Hash(key, step) == clientTotp)
                matchedTimeStep = step;
        }

        return matchedTimeStep != -1;
    }

    /// <summary>
    ///     The number of 30-second windows either side of now that fall, at least partly, within the tolerance.
    ///     A negative tolerance is treated as zero, as in 2.x.
    /// </summary>
    internal static int GetWindowCount(TimeSpan timeTolerance)
    {
        if (timeTolerance <= TimeSpan.Zero)
            return 0;

        return (int)Math.Ceiling(timeTolerance.TotalSeconds / TimeStepSeconds);
    }
}
