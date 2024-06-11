using TotpAuthSharp.Interface;

namespace TotpAuthSharp.Models;

public class TotpSetup(string manualSetupKey, byte[] imageBytes) : ITotpSetup
{
    private readonly IQrCodeImage _qrCodeImage = new QrCodeImage(imageBytes);

    /// <summary>
    ///     If the QR code can not be used, this code is needed to setup Google Authenticator.
    /// </summary>
    public string ManualSetupKey { get; } = manualSetupKey;

    /// <summary>
    ///     Provides a Uri formatted byte string ready for data attributes
    /// </summary>
    public string QrCodeImage => _qrCodeImage.DataUri;

    /// <summary>
    ///     The byte array for the QrCode
    /// </summary>
    public byte[] QrCodeImageBytes => _qrCodeImage.Bytes;
}