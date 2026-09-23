using System;

namespace TotpAuthSharp.Helper;

internal static class UrlEncoder
{
    // RFC 3986 percent-encoding over UTF-8: everything except A-Z a-z 0-9 - _ . ~ is escaped, so non-ASCII text
    // (for example "Café") becomes valid UTF-8 escapes that authenticator apps decode correctly.
    internal static string Encode(string value)
    {
        return Uri.EscapeDataString(value);
    }
}
