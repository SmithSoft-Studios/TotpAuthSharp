using TotpAuthSharp.Helper;
using Xunit;

namespace TotpAuthSharp.Tests;

public class ZXingQrCodeGeneratorTests
{
    // PNG files begin with this 8-byte signature.
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private readonly ZXingQrCodeGenerator _generator = new();

    [Fact]
    public void Generate_ProducesNonEmptyPngBytes()
    {
        var bytes = _generator.Generate("otpauth://totp/Account?secret=ABC123&issuer=Test");

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > PngSignature.Length);
        Assert.Equal(PngSignature, bytes[..PngSignature.Length]);
    }

    [Fact]
    public void Generate_WithDifferentContent_ProducesDifferentImages()
    {
        var first = _generator.Generate("content-one");
        var second = _generator.Generate("content-two");

        Assert.NotEqual(first, second);
    }
}
