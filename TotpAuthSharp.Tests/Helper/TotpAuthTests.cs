﻿namespace TotpAuthSharp.Tests.Helper;

public static class TotpAuthTests
{
    public static string AccountSecretKey = "8E7BCB87-AD10-4251-A5B0-3B74A2C73162";
    public static string AccountSecretKeyEncoded = "HBCTOQSDII4DOLKBIQYTALJUGI2TCLKBGVBDALJTII3TIQJSIM3TGMJWGI";
    public static string QrImage = "data:image/png;base64,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";
}