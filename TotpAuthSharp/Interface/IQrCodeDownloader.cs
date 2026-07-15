namespace TotpAuthSharp.Interface;

/// <summary>
///     Downloads a QR code image from a remote endpoint (for example quickchart.io).
/// </summary>
public interface IQrCodeDownloader
{
    /// <summary>
    ///     Downloads the QR code image at the given URL.
    /// </summary>
    /// <param name="url">The absolute URL that returns a QR code image.</param>
    /// <param name="timeoutInSeconds">Request timeout in seconds. Default is 30.</param>
    /// <returns>The downloaded image bytes.</returns>
    byte[] Download(string url, int timeoutInSeconds = 30);
}
