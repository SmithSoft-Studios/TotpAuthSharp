using System;
using System.Net.Http;
using TotpAuthSharp.Helper;
using TotpAuthSharp.Interface;
using TotpAuthSharp.Models;

namespace TotpAuthSharp;

public class TotpSetupGenerator : ITotpSetupGenerator
{
    private readonly IQrCodeGenerator _qrCodeGenerator;
    private readonly IQrCodeDownloader _qrCodeDownloader;

    /// <summary>
    ///     Creates a generator wired with the default SkiaSharp-based QR generator and HTTP downloader.
    /// </summary>
    public TotpSetupGenerator()
        : this(new SkiaQrCodeGenerator(), new HttpQrCodeDownloader())
    {
    }

    /// <summary>
    ///     Creates a generator with explicit dependencies, for testing or custom composition roots.
    /// </summary>
    public TotpSetupGenerator(IQrCodeGenerator qrCodeGenerator, IQrCodeDownloader qrCodeDownloader)
    {
        _qrCodeGenerator = qrCodeGenerator ?? throw new ArgumentNullException(nameof(qrCodeGenerator));
        _qrCodeDownloader = qrCodeDownloader ?? throw new ArgumentNullException(nameof(qrCodeDownloader));
    }

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

    private byte[] _getQrImage(string provisionUrl, int qrCodeWidth, int qrCodeHeight)
    {
        try
        {
            return _qrCodeGenerator.Generate(provisionUrl, qrCodeWidth, qrCodeHeight);
        }
        catch (Exception exception)
        {
            throw new HttpRequestException("Unexpected result from the QR generator.", exception);
        }
    }

    /// <summary>
    /// Generates an object you will need so that the user can setup his Google Authenticator to be used with your app.
    /// </summary>
    /// <param name="issuer">Your app name or company for example.</param>
    /// <param name="accountIdentity">Name, Email or Id of the user, without spaces, this will be shown in google authenticator.</param>
    /// <param name="accountSecretKey">A secret key which will be used to generate one time passwords. This key is the same needed for validating a passed TOTP.</param>
    /// <param name="qrCodeWidth">Height of the QR code. Default is 300px.</param>
    /// <param name="qrCodeHeight">Width of the QR code. Default is 300px.</param>
    /// <param name="useHttps">Use Https on quickchart.io api or not.</param>
    /// <returns>TotpSetup with ManualSetupKey and QrCode.</returns>
    public ITotpSetup GenerateFromWeb(string issuer, string accountIdentity, string accountSecretKey, int qrCodeWidth = 300, int qrCodeHeight = 300, bool useHttps = true)
    {
        Guard.NotNull(issuer);
        Guard.NotNull(accountIdentity);
        Guard.NotNull(accountSecretKey);

        accountIdentity = accountIdentity.Replace(" ", "");
        var encodedSecretKey = Base32.Encode(accountSecretKey);
        var provisionUrl = UrlEncoder.Encode($"otpauth://totp/{accountIdentity}?secret={encodedSecretKey}&issuer={UrlEncoder.Encode(issuer)}");
        var protocol = useHttps ? "https" : "http";
        var url = $"{protocol}://quickchart.io/chart?cht=qr&chs={qrCodeWidth}x{qrCodeHeight}&chl={provisionUrl}";

        return new TotpSetup(encodedSecretKey, _qrCodeDownloader.Download(url));
    }
}
