using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TotpAuthSharp.Helper;
using TotpAuthSharp.Interface;
using TotpAuthSharp.Models;
using TotpAuthSharp.Tests.Helper;
using Xunit;

// The 2.x constructor that takes an IQrCodeDownloader is obsolete but must keep working, so it stays under test.
#pragma warning disable CS0618

namespace TotpAuthSharp.Tests;

public class TotpSetupGeneratorTests
{
    private static readonly byte[] FakeQrBytes = "fake-qr-bytes"u8.ToArray();

    private readonly Mock<IQrCodeGenerator> _qrCodeGenerator = new();
    private readonly TotpSetupGenerator _totpSetupGenerator;

    public TotpSetupGeneratorTests()
    {
        _qrCodeGenerator
            .Setup(x => x.Generate(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(FakeQrBytes);
        _totpSetupGenerator = new TotpSetupGenerator(_qrCodeGenerator.Object);
    }

    [Fact]
    public void Constructor_WithNullQrCodeGenerator_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TotpSetupGenerator((IQrCodeGenerator)null));
    }

    [Fact]
    public void LegacyConstructor_WithNullQrCodeGenerator_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TotpSetupGenerator(null, new Mock<IQrCodeDownloader>().Object));
    }

    [Fact]
    public void LegacyConstructor_StillWorks_AndNeverUsesTheDownloader()
    {
        var downloader = new Mock<IQrCodeDownloader>(MockBehavior.Strict);
        var generator = new TotpSetupGenerator(_qrCodeGenerator.Object, downloader.Object);

        var totpSetup = generator.Generate("Totp Auth Tester", "Daniel Smith", TotpAuthTests.AccountSecretKey);

        Assert.Equal(FakeQrBytes, totpSetup.QrCodeImageBytes);
        downloader.VerifyNoOtherCalls();
    }

    [Fact]
    public void LegacyConstructor_AcceptsNullDownloader()
    {
        var generator = new TotpSetupGenerator(_qrCodeGenerator.Object, null);

        Assert.Equal(FakeQrBytes, generator.Generate("Issuer", "Account", "secret").QrCodeImageBytes);
    }

    [Fact]
    public void ParameterlessConstructor_UsesSkiaQrCodeGenerator()
    {
        Assert.IsType<SkiaQrCodeGenerator>(QrCodeGeneratorOf(new TotpSetupGenerator()));
    }

    [Fact]
    public void DependencyInjection_WithOnlyAQrCodeGeneratorRegistered_UsesThatGenerator()
    {
        // In 2.x this silently fell back to the parameterless constructor and ignored the registration.
        var services = new ServiceCollection();
        services.AddSingleton<IQrCodeGenerator, ZXingQrCodeGenerator>();
        services.AddSingleton<ITotpSetupGenerator, TotpSetupGenerator>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        Assert.IsType<ZXingQrCodeGenerator>(QrCodeGeneratorOf(provider.GetRequiredService<ITotpSetupGenerator>()));
    }

    [Fact]
    public void DependencyInjection_2xStyleRegistrationWithDownloader_StillResolves()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IQrCodeGenerator, ZXingQrCodeGenerator>();
        services.AddSingleton<IQrCodeDownloader, HttpQrCodeDownloader>();
        services.AddSingleton<ITotpSetupGenerator, TotpSetupGenerator>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        Assert.IsType<ZXingQrCodeGenerator>(QrCodeGeneratorOf(provider.GetRequiredService<ITotpSetupGenerator>()));
    }

    [Fact]
    public void DependencyInjection_WithNothingRegistered_UsesTheDefaults()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITotpSetupGenerator, TotpSetupGenerator>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        Assert.IsType<SkiaQrCodeGenerator>(QrCodeGeneratorOf(provider.GetRequiredService<ITotpSetupGenerator>()));
    }

    [Fact]
    public void Generate_ReturnsSetup_WithEncodedKeyAndGeneratedQrBytes()
    {
        var totpSetup = _totpSetupGenerator.Generate("Totp Auth Tester", "Daniel Smith", TotpAuthTests.AccountSecretKey);

        Assert.NotNull(totpSetup);
        Assert.Equal(TotpAuthTests.AccountSecretKeyEncoded, totpSetup.ManualSetupKey);
        Assert.Equal(FakeQrBytes, totpSetup.QrCodeImageBytes);
    }

    [Fact]
    public void Generate_BuildsProvisionUrl_WithStrippedIdentityEncodedKeyAndIssuer_AndRequestedSize()
    {
        _totpSetupGenerator.Generate("Totp Auth Tester", "Daniel Smith", TotpAuthTests.AccountSecretKey, 250, 400);

        _qrCodeGenerator.Verify(x => x.Generate(
            It.Is<string>(url =>
                url.StartsWith("otpauth://totp/DanielSmith?") &&
                url.Contains($"secret={TotpAuthTests.AccountSecretKeyEncoded}") &&
                url.Contains("issuer=Totp%20Auth%20Tester")),
            250,
            400), Times.Once);
    }

    // Golden values: the exact payloads TotpAuthSharp 2.1.0 produces, so 3.0 enrols users identically.
    [Theory]
    [InlineData("TACS UAT", "Jane Doe", "8E7BCB87-AD10-4251-A5B0-3B74A2C73162",
        "otpauth://totp/JaneDoe?secret=HBCTOQSDII4DOLKBIQYTALJUGI2TCLKBGVBDALJTII3TIQJSIM3TGMJWGI&issuer=TACS%20UAT")]
    [InlineData("Café Société", "jane.doe@example.co.za", "secret",
        "otpauth://totp/jane.doe%40example.co.za?secret=ONSWG4TFOQ&issuer=Caf%C3%A9%20Soci%C3%A9t%C3%A9")]
    [InlineData("TACS: UAT & Test", "jane:doe?x=1#frag/path", "12345678901234567890",
        "otpauth://totp/jane%3Adoe%3Fx%3D1%23frag%2Fpath?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ&issuer=TACS%3A%20UAT%20%26%20Test")]
    public void Generate_ProducesTheSameProvisioningUrlAs21(string issuer, string account, string secret, string expected)
    {
        _totpSetupGenerator.Generate(issuer, account, secret);

        _qrCodeGenerator.Verify(x => x.Generate(expected, 300, 300), Times.Once);
    }

    [Fact]
    public void Generate_WhenQrGeneratorThrows_ThrowsHttpRequestException()
    {
        _qrCodeGenerator
            .Setup(x => x.Generate(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Throws(new InvalidOperationException("boom"));

        Assert.Throws<System.Net.Http.HttpRequestException>(() =>
            _totpSetupGenerator.Generate("Issuer", "Account", TotpAuthTests.AccountSecretKey));
    }

    [Theory]
    [InlineData(null, "Account", "secret", "issuer")]
    [InlineData("Issuer", null, "secret", "accountIdentity")]
    [InlineData("Issuer", "Account", null, "accountSecretKey")]
    public void Generate_WithNullArgument_ThrowsArgumentNullException(string issuer, string accountIdentity, string accountSecretKey, string paramName)
    {
        var exception = Assert.Throws<ArgumentNullException>(() => _totpSetupGenerator.Generate(issuer, accountIdentity, accountSecretKey));

        Assert.Equal(paramName, exception.ParamName);
    }

    [Fact]
    public void QrCodeImage_DataUri_IsBuiltOnceAndReused()
    {
        var totpSetup = new TotpSetup("KEY", FakeQrBytes);

        var first = totpSetup.QrCodeImage;

        Assert.Equal("data:image/png;base64," + Convert.ToBase64String(FakeQrBytes), first);
        Assert.Same(first, totpSetup.QrCodeImage);
    }

    [Fact]
    public void GenerateFromWeb_IsRemoved()
    {
        Assert.Null(typeof(ITotpSetupGenerator).GetMethod("GenerateFromWeb"));
        Assert.Null(typeof(TotpSetupGenerator).GetMethod("GenerateFromWeb"));
    }

    private static IQrCodeGenerator QrCodeGeneratorOf(ITotpSetupGenerator generator) =>
        (IQrCodeGenerator)typeof(TotpSetupGenerator)
            .GetField("_qrCodeGenerator", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(generator);
}
