namespace TotpAuthSharp.Interface;

/// <summary>
///     A QR code image.
/// </summary>
public interface IQrCodeImage
{
    /// <summary>
    ///     The image as a <c>data:image/png;base64,...</c> URI, ready for an <c>img</c> tag's <c>src</c>.
    /// </summary>
    string DataUri { get; }

    /// <summary>
    ///     The PNG image bytes.
    /// </summary>
    byte[] Bytes { get; }
}