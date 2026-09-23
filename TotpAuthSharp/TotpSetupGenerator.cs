using System;
using System.Net.Http;
using TotpAuthSharp.Helper;
using TotpAuthSharp.Interface;
using TotpAuthSharp.Models;

namespace TotpAuthSharp;

/// <summary>
///     Generates what a user needs to add your app to an authenticator app: a QR code rendered locally and a
///     manual setup key.
/// </summary>
public class TotpSetupGenerator : ITotpSetupGenerator
{
    private readonly IQrCodeGenerator _qrCodeGenerator;

    /// <summary>
    ///     Creates a generator that renders QR codes with <see cref="SkiaQrCodeGenerator" />.
    /// </summary>
    public TotpSetupGenerator()
        : this(new SkiaQrCodeGenerator())
    {
    }

    /// <summary>
    ///     Creates a generator that renders QR codes with <paramref name="qrCodeGenerator" />, for example
    ///     <see cref="ZXingQrCodeGenerator" />, your own implementation, or a mock in tests.
    /// </summary>
    public TotpSetupGenerator(IQrCodeGenerator qrCodeGenerator)
    {
        _qrCodeGenerator = qrCodeGenerator ?? throw new ArgumentNullException(nameof(qrCodeGenerator));
    }

    /// <summary>
    ///     Kept so code written for 2.x still compiles. The downloader is ignored: it was only used by
    ///     GenerateFromWeb, which was removed in 3.0.
    /// </summary>
    [Obsolete("The downloader is no longer used because GenerateFromWeb was removed in 3.0. Use TotpSetupGenerator(IQrCodeGenerator). This constructor will be removed in 4.0.")]
    public TotpSetupGenerator(IQrCodeGenerator qrCodeGenerator, IQrCodeDownloader? qrCodeDownloader)
        : this(qrCodeGenerator)
    {
    }

    /// <summary>
    /// Generates an object you will need so that the user can setup his Google Authenticator to be used with your app.
    /// </summary>
    /// <param name="issuer">Your app name or company for example.</param>
    /// <param name="accountIdentity">Name, Email or Id of the user. Spaces are removed; this is shown in the authenticator app.</param>
    /// <param name="accountSecretKey">A secret key which will be used to generate one time passwords. This key is the same needed for validating a passed TOTP.</param>
    /// <param name="qrCodeWidth">Width of the QR code. Default is 300px.</param>
    /// <param name="qrCodeHeight">Height of the QR code. Default is 300px.</param>
    /// <returns>TotpSetup with ManualSetupKey and QrCode.</returns>
    public ITotpSetup Generate(string issuer, string accountIdentity, string accountSecretKey, int qrCodeWidth = 300, int qrCodeHeight = 300)
    {
        Guard.NotNull(issuer);
        Guard.NotNull(accountIdentity);
        Guard.NotNull(accountSecretKey);

        accountIdentity = accountIdentity.Replace(" ", "");
        var encodedSecretKey = Base32.Encode(accountSecretKey);
        var provisionUrl = $"otpauth://totp/{UrlEncoder.Encode(accountIdentity)}?secret={encodedSecretKey}&issuer={UrlEncoder.Encode(issuer)}";

        return new TotpSetup(encodedSecretKey, _getQrImage(provisionUrl, qrCodeWidth, qrCodeHeight));
    }

    private byte[] _getQrImage(string provisionUrl, int qrCodeWidth, int qrCodeHeight)
    {
        try
        {
            return _qrCodeGenerator.Generate(provisionUrl, qrCodeWidth, qrCodeHeight);
        }
        catch (Exception exception)
        {
            // Kept as HttpRequestException for compatibility with 2.x callers that catch it.
            throw new HttpRequestException("Unexpected result from the QR generator.", exception);
        }
    }
}
