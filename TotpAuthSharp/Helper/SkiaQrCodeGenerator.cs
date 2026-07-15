using SkiaSharp.QrCode.Image;
using TotpAuthSharp.Interface;

namespace TotpAuthSharp.Helper;

/// <summary>
///     <see cref="IQrCodeGenerator" /> backed by SkiaSharp.QrCode (1.0.0+ fluent builder API).
/// </summary>
public class SkiaQrCodeGenerator : IQrCodeGenerator
{
    public byte[] Generate(string content, int width = 300, int height = 300)
    {
        return new QRCodeImageBuilder(content)
            .WithSize(width, height)
            .ToByteArray();
    }
}
