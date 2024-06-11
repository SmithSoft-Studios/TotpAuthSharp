using System;
using System.Net.Http;
using TotpAuthSharp.Helper;
using TotpAuthSharp.Interface;
using TotpAuthSharp.Models;

namespace TotpAuthSharp;
public class TotpSetupGenerator : ITotpSetupGenerator
{
    /// <summary>
    /// Generates an object you will need so that the user can setup his Google Authenticator to be used with your app.
    /// </summary>
    /// <param name="issuer">Your app name or company for example.</param>
    /// <param name="accountIdentity">Name, Email or Id of the user, without spaces, this will be shown in google authenticator.</param>
    /// <param name="accountSecretKey">A secret key which will be used to generate one time passwords. This key is the same needed for validating a passed TOTP.</param>
    /// <param name="qrCodeWidth">Height of the QR code. Default is 300px.</param>
    /// <param name="qrCodeHeight">Width of the QR code. Default is 300px.</param>
    /// <returns>TotpSetup with ManualSetupKey and QrCode.</returns>
    public ITotpSetup Generate(string issuer, string accountIdentity, string accountSecretKey, int qrCodeWidth = 300, int qrCodeHeight = 300)
    {
        Guard.NotNull(issuer);
        Guard.NotNull(accountIdentity);
        Guard.NotNull(accountSecretKey);

        accountIdentity = accountIdentity.Replace(" ", "");
        var encodedSecretKey = Base32.Encode(accountSecretKey);
        var provisionUrl = $"otpauth://totp/{accountIdentity}?secret={encodedSecretKey}&issuer={UrlEncoder.Encode(issuer)}";

        return new TotpSetup(encodedSecretKey, _getQrImage(provisionUrl, qrCodeWidth, qrCodeHeight));
    }

    private static byte[] _getQrImage(string provisionUrl, int qrCodeWidth = 300, int qrCodeHeight = 300)
    {
        try
        {
            return provisionUrl.GenerateQrCode(qrCodeWidth, qrCodeHeight);
        }
        catch (Exception exception)
        {
            throw new HttpRequestException("Unexpected result from the QR generator.", exception);
        }
    }
}