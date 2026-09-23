using System;
using System.Runtime.CompilerServices;

namespace TotpAuthSharp.Helper;

internal static class Guard
{
    internal static void NotNull(object? testee, [CallerArgumentExpression(nameof(testee))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(testee, paramName);
    }
}
