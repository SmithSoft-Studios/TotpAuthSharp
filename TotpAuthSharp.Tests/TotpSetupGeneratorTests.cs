using TotpAuthSharp.Tests.Helper;
using Xunit;

namespace TotpAuthSharp.Tests;

public class TotpSetupGeneratorTests
{
    private readonly TotpSetupGenerator _totpSetupGenerator = new();

    [Fact]
    public void GenerateSetupCode_shouldNotBeNull_manuelTest_workWithGoogleAuthenticator()
    {
        var totpSetup =
            _totpSetupGenerator.Generate("Totp Auth Tester", "Daniel Smith", TotpAuthTests.AccountSecretKey);
        Assert.NotNull(totpSetup);
        Assert.Equal(TotpAuthTests.AccountSecretKeyEncoded, totpSetup.ManualSetupKey);
        Assert.Equal(TotpAuthTests.QrImage, totpSetup.QrCodeImage);
    }
}