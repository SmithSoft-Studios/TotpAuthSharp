namespace TotpAuthSharp.Interface;

/// <summary>
///     What a user needs to add your app to an authenticator app.
/// </summary>
public interface ITotpSetup
{
    /// <summary>
    ///     Provides a Uri formatted byte string ready for data attributes
    /// </summary>
    string QrCodeImage { get; }

    /// <summary>
    ///     The byte array for the QrCode
    /// </summary>
    byte[] QrCodeImageBytes { get; }

    /// <summary>
    ///     If the QR code can not be used, this code is needed to setup Google Authenticator.
    /// </summary>
    string ManualSetupKey { get; }
}