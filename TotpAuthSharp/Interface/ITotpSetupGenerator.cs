namespace TotpAuthSharp.Interface;

/// <summary>
///     Generates what a user needs to add your app to an authenticator app.
/// </summary>
public interface ITotpSetupGenerator
{
    /// <summary>
    /// Generates an object you will need so that the user can setup his Google Authenticator to be used with your app.
    /// </summary>
    /// <param name="issuer">Your app name or company for example.</param>
    /// <param name="accountIdentity">Name, Email or Id of the user. Spaces are removed; this is shown in the authenticator app.</param>
    /// <param name="accountSecretKey">A secret key which will be used to generate one time passwords. This key is the same needed for validating a passed TOTP.</param>
    /// <param name="qrCodeWidth">Width of the QR code. Default is 300px.</param>
    /// <param name="qrCodeHeight">Height of the QR code. Default is 300px.</param>
    /// <returns>TotpSetup with ManualSetupKey and QrCode.</returns>
    ITotpSetup Generate(string issuer, string accountIdentity, string accountSecretKey, int qrCodeWidth = 300, int qrCodeHeight = 300);
}