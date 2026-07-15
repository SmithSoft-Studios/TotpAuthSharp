namespace TotpAuthSharp.Interface;

/// <summary>
///     Generates a QR code image locally from the given content.
/// </summary>
public interface IQrCodeGenerator
{
    /// <summary>
    ///     Generates a PNG QR code for the supplied content.
    /// </summary>
    /// <param name="content">The payload to encode (for example an otpauth:// provisioning URL).</param>
    /// <param name="width">Width of the QR code in pixels. Default is 300px.</param>
    /// <param name="height">Height of the QR code in pixels. Default is 300px.</param>
    /// <returns>The QR code encoded as PNG bytes.</returns>
    byte[] Generate(string content, int width = 300, int height = 300);
}
