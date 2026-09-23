namespace TotpAuthSharp.Helper;

internal static class TotpCodeFormat
{
    private const int Digits = 6;

    /// <summary>
    ///     Parses a six-digit code as typed by a user; whitespace is ignored.
    /// </summary>
    internal static bool TryParse(string? text, out int code)
    {
        code = 0;
        if (text is null)
            return false;

        var digits = 0;
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
                continue;
            if (c is < '0' or > '9' || ++digits > Digits)
                return false;
            code = code * 10 + (c - '0');
        }

        return digits == Digits;
    }
}
