using System;
using SkiaSharp;
using SkiaSharp.QrCode.Image;
using TotpAuthSharp.Interface;

namespace TotpAuthSharp.Helper;

/// <summary>
///     <see cref="IQrCodeGenerator" /> backed by SkiaSharp.QrCode.
/// </summary>
public class SkiaQrCodeGenerator : IQrCodeGenerator
{
    /// <inheritdoc />
    public byte[] Generate(string content, int width = 300, int height = 300)
    {
        // The builder stretches the symbol to fill a non-square size, and stretched modules do not scan reliably.
        // Render a square at the shorter side and centre it on a white canvas of the requested size instead.
        var size = Math.Min(width, height);
        var builder = new QRCodeImageBuilder(content).WithSize(size, size);

        if (width == height)
            return builder.ToByteArray();

        // Draw the rendered symbol straight onto the canvas, so the image is PNG-encoded once rather than
        // encoded, decoded and encoded again.
        using var qrImage = builder.ToImage();
        using var canvasBitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(canvasBitmap))
        {
            canvas.Clear(SKColors.White);
            canvas.DrawImage(qrImage, (width - size) / 2f, (height - size) / 2f, SKSamplingOptions.Default);
        }

        using var png = canvasBitmap.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }
}
