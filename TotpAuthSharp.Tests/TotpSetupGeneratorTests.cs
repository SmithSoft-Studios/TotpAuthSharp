using System;
using Moq;
using TotpAuthSharp.Interface;
using TotpAuthSharp.Tests.Helper;
using Xunit;

// GenerateFromWeb is obsolete but still supported, so its behaviour stays under test.
#pragma warning disable CS0618

namespace TotpAuthSharp.Tests;

public class TotpSetupGeneratorTests
{
    private static readonly byte[] FakeQrBytes = "fake-qr-bytes"u8.ToArray();

    private readonly Mock<IQrCodeGenerator> _qrCodeGenerator = new();
    private readonly Mock<IQrCodeDownloader> _qrCodeDownloader = new();
    private readonly TotpSetupGenerator _totpSetupGenerator;

    public TotpSetupGeneratorTests()
    {
        _totpSetupGenerator = new TotpSetupGenerator(_qrCodeGenerator.Object, _qrCodeDownloader.Object);
    }

    [Fact]
    public void Constructor_WithNullQrCodeGenerator_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TotpSetupGenerator(null, _qrCodeDownloader.Object));
    }

    [Fact]
    public void Constructor_WithNullQrCodeDownloader_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TotpSetupGenerator(_qrCodeGenerator.Object, null));
    }

    [Fact]
    public void Generate_ReturnsSetup_WithEncodedKeyAndGeneratedQrBytes()
    {
        _qrCodeGenerator
            .Setup(x => x.Generate(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(FakeQrBytes);

        var totpSetup = _totpSetupGenerator.Generate("Totp Auth Tester", "Daniel Smith", TotpAuthTests.AccountSecretKey);

        Assert.NotNull(totpSetup);
        Assert.Equal(TotpAuthTests.AccountSecretKeyEncoded, totpSetup.ManualSetupKey);
        Assert.Equal(FakeQrBytes, totpSetup.QrCodeImageBytes);
    }

    [Fact]
    public void Generate_BuildsProvisionUrl_WithStrippedIdentityEncodedKeyAndIssuer_AndRequestedSize()
    {
        _qrCodeGenerator
            .Setup(x => x.Generate(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(FakeQrBytes);

        _totpSetupGenerator.Generate("Totp Auth Tester", "Daniel Smith", TotpAuthTests.AccountSecretKey, 250, 400);

        _qrCodeGenerator.Verify(x => x.Generate(
            It.Is<string>(url =>
                url.StartsWith("otpauth://totp/DanielSmith?") &&
                url.Contains($"secret={TotpAuthTests.AccountSecretKeyEncoded}") &&
                url.Contains("issuer=Totp%20Auth%20Tester")),
            250,
            400), Times.Once);
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
    [InlineData(null, "Account", "secret")]
    [InlineData("Issuer", null, "secret")]
    [InlineData("Issuer", "Account", null)]
    public void Generate_WithNullArgument_Throws(string issuer, string accountIdentity, string accountSecretKey)
    {
        Assert.ThrowsAny<Exception>(() => _totpSetupGenerator.Generate(issuer, accountIdentity, accountSecretKey));
    }

    [Fact]
    public void GenerateFromWeb_ReturnsSetup_WithEncodedKeyAndDownloadedBytes()
    {
        _qrCodeDownloader
            .Setup(x => x.Download(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(FakeQrBytes);

        var totpSetup = _totpSetupGenerator.GenerateFromWeb("Totp Auth Tester", "Daniel Smith", TotpAuthTests.AccountSecretKey);

        Assert.NotNull(totpSetup);
        Assert.Equal(TotpAuthTests.AccountSecretKeyEncoded, totpSetup.ManualSetupKey);
        Assert.Equal(FakeQrBytes, totpSetup.QrCodeImageBytes);
    }

    [Fact]
    public void GenerateFromWeb_UsesHttpsQuickchartUrl_WithRequestedSize_ByDefault()
    {
        _qrCodeDownloader
            .Setup(x => x.Download(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(FakeQrBytes);

        _totpSetupGenerator.GenerateFromWeb("Totp Auth Tester", "Daniel Smith", TotpAuthTests.AccountSecretKey, 250, 400);

        _qrCodeDownloader.Verify(x => x.Download(
            It.Is<string>(url =>
                url.StartsWith("https://quickchart.io/chart?") &&
                url.Contains("cht=qr") &&
                url.Contains("chs=250x400")),
            It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public void GenerateFromWeb_UsesHttp_WhenUseHttpsIsFalse()
    {
        _qrCodeDownloader
            .Setup(x => x.Download(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(FakeQrBytes);

        _totpSetupGenerator.GenerateFromWeb("Issuer", "Account", TotpAuthTests.AccountSecretKey, useHttps: false);

        _qrCodeDownloader.Verify(x => x.Download(
            It.Is<string>(url => url.StartsWith("http://quickchart.io/")),
            It.IsAny<int>()), Times.Once);
    }
}
