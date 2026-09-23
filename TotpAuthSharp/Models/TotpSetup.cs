using TotpAuthSharp.Interface;

namespace TotpAuthSharp.Models;

/// <summary>
///     What a user needs to add your app to an authenticator app: the QR code and the manual setup key.
/// </summary>
/// <param name="manualSetupKey">The Base32 secret the user can type in if they cannot scan the QR code.</param>
/// <param name="imageBytes">The PNG QR code image.</param>
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