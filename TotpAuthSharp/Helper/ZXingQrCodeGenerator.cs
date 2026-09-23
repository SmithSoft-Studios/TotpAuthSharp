using SkiaSharp;
using TotpAuthSharp.Interface;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.SkiaSharp;

namespace TotpAuthSharp.Helper;

/// <summary>
///     <see cref="IQrCodeGenerator" /> backed by ZXing.Net. An alternative to <see cref="SkiaQrCodeGenerator" />;
///     both render locally, so the shared secret never leaves the server.
/// </summary>
public class ZXingQrCodeGenerator : IQrCodeGenerator
{
    /// <inheritdoc />
    public byte[] Generate(string content, int width = 300, int height = 300)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Width = width,
                Height = height,
                ErrorCorrection = ErrorCorrectionLevel.M,
                CharacterSet = "UTF-8",
                DisableECI = true
            }
        };

        using var bitmap = writer.Write(content);
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }
}
