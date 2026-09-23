using System;

namespace TotpAuthSharp.Tests.Helper;

/// <summary>
///     A clock pinned to a chosen instant, so time-based tests are exact and never flaky.
/// </summary>
public sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    public static FixedTimeProvider AtUnixSeconds(long seconds) => new(DateTimeOffset.FromUnixTimeSeconds(seconds));

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
