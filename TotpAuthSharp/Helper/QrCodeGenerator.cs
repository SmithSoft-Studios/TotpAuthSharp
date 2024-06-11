using System.IO;
using SkiaSharp;
using SkiaSharp.QrCode.Image;

namespace TotpAuthSharp.Helper;

public static class QrCodeGenerator
{
    public static byte[] GenerateQrCode(this string content, int qrCodeWidth = 300,int qrCodeHeight = 300)
    {
        using var output = new MemoryStream();

        // generate QRCode
        var qrCode = new QrCode(content, new Vector2Slim(qrCodeWidth, qrCodeHeight), SKEncodedImageFormat.Png);

        // output to stream
        qrCode.GenerateImage(output);

        return output.ToArray();
    }
}