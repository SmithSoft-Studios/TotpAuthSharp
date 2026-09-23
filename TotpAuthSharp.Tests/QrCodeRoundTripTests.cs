using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using Moq;
using SkiaSharp;
using SkiaSharp.QrCode;
using TotpAuthSharp.Helper;
using TotpAuthSharp.Interface;
using Xunit;
using ZXing;
using ZXing.SkiaSharp;

// GenerateFromWeb is obsolete but still supported, so its behaviour stays under test.
#pragma warning disable CS0618

namespace TotpAuthSharp.Tests;

/// <summary>
///     Proves that locally rendered QR codes scan back to exactly the intended content, entirely offline.
///     Each generator is checked by the other library's decoder, so neither library marks its own homework:
///     SkiaSharp.QrCode output is read by ZXing, and ZXing output is read by SkiaSharp.QrCode.
/// </summary>
public class QrCodeRoundTripTests
{
    public const string Skia = nameof(Skia);
    public const string ZXing = nameof(ZXing);

    private const string TacsIssuer = "TACS UAT";

    private const string TypicalProvisionUrl =
        "otpauth://totp/JaneDoe?secret=HBCTOQSDII4DOLKBIQYTALJUGI2TCLKBGVBDALJTII3TIQJSIM3TGMJWGI&issuer=TACS%20UAT";

    private static readonly string[] Producers = [Skia, ZXing];

    // Strict mock: any call to the downloader throws, so these tests fail if the local path ever goes to the web.
    private readonly Mock<IQrCodeDownloader> _offlineDownloader = new(MockBehavior.Strict);

    public static IEnumerable<object[]> AllProducers() => Producers.Select(p => new object[] { p });

    private static IEnumerable<object[]> ForEachProducer(IEnumerable<object> values) =>
        Producers.SelectMany(p => values.Select(v => new object[] { p, v }));

    public static IEnumerable<object[]> Contents() => ForEachProducer(
    [
        "x",
        TypicalProvisionUrl,
        "otpauth://totp/jane.doe@example.co.za?secret=JBSWY3DPEHPK3PXP&issuer=TACS%20UAT",
        "otpauth://totp/A?secret=ABCDEFGHIJKLMNOPQRSTUVWXYZ234567ABCDEFGHIJKLMNOPQRSTUVWXYZ234567ABCDEFGHIJKLMNOPQRSTUVWXYZ234567ABCDEFGHIJKLMNOPQRSTUVWXYZ234567&issuer=A%20Long%20Issuer%20Name%20For%20Capacity"
    ]);

    public static IEnumerable<object[]> Sizes() => ForEachProducer([100, 150, 200, 300, 600]);

    public static IEnumerable<object[]> RandomSecrets()
    {
        // Fixed seed keeps the run repeatable while still covering many key lengths and byte patterns.
        var random = new Random(20260923);
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        var secrets = Enumerable.Range(0, 25)
            .Select(_ => new string(Enumerable.Range(0, random.Next(1, 65)).Select(_ => chars[random.Next(chars.Length)]).ToArray()))
            .Append("8E7BCB87-AD10-4251-A5B0-3B74A2C73162")
            .ToList();

        return ForEachProducer(secrets);
    }

    [Theory]
    [MemberData(nameof(Contents))]
    public void Generator_ContentScansBackExactly(string producer, string content)
    {
        var png = GeneratorFor(producer).Generate(content);

        Assert.Equal(content, DecodeWithOtherLibrary(producer, png));
    }

    [Theory]
    [MemberData(nameof(Sizes))]
    public void Generator_ScansAtRequestedSize_AndHonoursDimensions(string producer, int size)
    {
        var png = GeneratorFor(producer).Generate(TypicalProvisionUrl, size, size);

        using var bitmap = SKBitmap.Decode(png);
        Assert.Equal(size, bitmap.Width);
        Assert.Equal(size, bitmap.Height);
        Assert.Equal(TypicalProvisionUrl, DecodeWithOtherLibrary(producer, png));
    }

    [Theory]
    [MemberData(nameof(AllProducers))]
    public void Generator_ScansWithNonSquareSize(string producer)
    {
        var png = GeneratorFor(producer).Generate(TypicalProvisionUrl, 250, 400);

        Assert.Equal(TypicalProvisionUrl, DecodeWithOtherLibrary(producer, png));
    }

    [Theory]
    [MemberData(nameof(RandomSecrets))]
    public void Generate_QrCarriesExactSecretIssuerAndAccount(string producer, string accountSecretKey)
    {
        var setup = LocalSetupGenerator(producer).Generate(TacsIssuer, "Jane Doe", accountSecretKey);

        var uri = new Uri(DecodeWithOtherLibrary(producer, setup.QrCodeImageBytes));
        var query = HttpUtility.ParseQueryString(uri.Query);

        Assert.Equal("otpauth", uri.Scheme);
        Assert.Equal("totp", uri.Host);
        Assert.Equal("/JaneDoe", uri.AbsolutePath);
        Assert.Equal(setup.ManualSetupKey, query["secret"]);
        Assert.Equal(TacsIssuer, query["issuer"]);
    }

    [Theory]
    [MemberData(nameof(AllProducers))]
    public void Generate_EmailAccountIdentity_SurvivesInLabel(string producer)
    {
        var setup = LocalSetupGenerator(producer).Generate(TacsIssuer, "jane.doe@example.co.za", "secret");

        var uri = new Uri(DecodeWithOtherLibrary(producer, setup.QrCodeImageBytes));

        Assert.Equal("/jane.doe@example.co.za", Uri.UnescapeDataString(uri.AbsolutePath));
    }

    public static IEnumerable<object[]> AwkwardIdentities() => Producers.SelectMany(p => new[]
    {
        new object[] { p, "Café Société", "Zoë Müller" },
        new object[] { p, "日本 UAT", "田中" },
        new object[] { p, "TACS: UAT & Test", "jane:doe?x=1#frag/path" }
    });

    [Theory]
    [MemberData(nameof(AwkwardIdentities))]
    public void Generate_NonAsciiAndReservedCharacters_SurviveTheScan(string producer, string issuer, string account)
    {
        var setup = LocalSetupGenerator(producer).Generate(issuer, account, "secret");

        var uri = new Uri(DecodeWithOtherLibrary(producer, setup.QrCodeImageBytes));
        var query = HttpUtility.ParseQueryString(uri.Query);

        Assert.Equal("/" + account.Replace(" ", ""), Uri.UnescapeDataString(uri.AbsolutePath));
        Assert.Equal(setup.ManualSetupKey, query["secret"]);
        Assert.Equal(issuer, query["issuer"]);
    }

    [Theory]
    [MemberData(nameof(AllProducers))]
    public void Generate_DataUri_ScansToSameContentAsBytes(string producer)
    {
        var setup = LocalSetupGenerator(producer).Generate(TacsIssuer, "Jane Doe", "secret");

        const string prefix = "data:image/png;base64,";
        Assert.StartsWith(prefix, setup.QrCodeImage);
        var fromDataUri = Convert.FromBase64String(setup.QrCodeImage[prefix.Length..]);

        Assert.Equal(DecodeWithOtherLibrary(producer, setup.QrCodeImageBytes), DecodeWithOtherLibrary(producer, fromDataUri));
    }

    [Theory]
    [MemberData(nameof(AllProducers))]
    public void Generate_ScannedSecret_ProducesCodesTheValidatorAccepts(string producer)
    {
        const string accountSecretKey = "8E7BCB87-AD10-4251-A5B0-3B74A2C73162";
        var setup = LocalSetupGenerator(producer).Generate(TacsIssuer, "Jane Doe", accountSecretKey);

        // Act as an authenticator app: read the secret from the scanned QR and compute the current code
        // with an independent RFC 6238 implementation (HMAC-SHA1, 30 s step, 6 digits).
        var scanned = DecodeWithOtherLibrary(producer, setup.QrCodeImageBytes);
        var scannedSecret = HttpUtility.ParseQueryString(new Uri(scanned).Query)["secret"];
        var code = AuthenticatorAppCode(Rfc4648Base32Decode(scannedSecret), DateTimeOffset.UtcNow);

        var validator = new TotpValidator(new TotpGenerator());
        Assert.True(validator.Validate(accountSecretKey, code));
    }

    [Theory]
    [MemberData(nameof(AllProducers))]
    public void Generate_NeverContactsTheWeb(string producer)
    {
        LocalSetupGenerator(producer).Generate(TacsIssuer, "Jane Doe", "secret");

        _offlineDownloader.VerifyNoOtherCalls();
    }

    [Theory]
    [MemberData(nameof(AllProducers))]
    public void Generate_EncodesSamePayloadAsGenerateFromWeb(string producer)
    {
        string requestedUrl = null;
        var capturingDownloader = new Mock<IQrCodeDownloader>();
        capturingDownloader
            .Setup(x => x.Download(It.IsAny<string>(), It.IsAny<int>()))
            .Callback<string, int>((url, _) => requestedUrl = url)
            .Returns([]);
        var generator = new TotpSetupGenerator(GeneratorFor(producer), capturingDownloader.Object);

        var local = generator.Generate(TacsIssuer, "Jane Doe", "secret");
        generator.GenerateFromWeb(TacsIssuer, "Jane Doe", "secret");

        // quickchart.io URL-decodes chl once before encoding it, so decode once here to get what it would render.
        var chl = requestedUrl[(requestedUrl.IndexOf("&chl=", StringComparison.Ordinal) + "&chl=".Length)..];
        Assert.Equal(Uri.UnescapeDataString(chl), DecodeWithOtherLibrary(producer, local.QrCodeImageBytes));
    }

    private static IQrCodeGenerator GeneratorFor(string producer) => producer switch
    {
        Skia => new SkiaQrCodeGenerator(),
        ZXing => new ZXingQrCodeGenerator(),
        _ => throw new ArgumentOutOfRangeException(nameof(producer), producer, null)
    };

    private TotpSetupGenerator LocalSetupGenerator(string producer) => new(GeneratorFor(producer), _offlineDownloader.Object);

    private static string DecodeWithOtherLibrary(string producer, byte[] png) => producer switch
    {
        Skia => DecodeWithZXing(png),
        ZXing => DecodeWithSkia(png),
        _ => throw new ArgumentOutOfRangeException(nameof(producer), producer, null)
    };

    private static string DecodeWithZXing(byte[] png)
    {
        using var bitmap = SKBitmap.Decode(png);
        Assert.NotNull(bitmap);

        var reader = new BarcodeReader
        {
            AutoRotate = false,
            Options = { PossibleFormats = [BarcodeFormat.QR_CODE] }
        };

        var result = reader.Decode(bitmap);
        Assert.True(result != null, "ZXing could not find a QR code in the image.");
        return result.Text;
    }

    private static string DecodeWithSkia(byte[] png)
    {
        using var bitmap = SKBitmap.Decode(png);
        Assert.NotNull(bitmap);

        Assert.True(QRCodeDecoder.TryDecode(bitmap, out var text, out var info),
            $"SkiaSharp.QrCode could not decode the image ({info.Status}).");
        return text;
    }

    private static int AuthenticatorAppCode(byte[] key, DateTimeOffset now)
    {
        var counter = BitConverter.GetBytes(now.ToUnixTimeSeconds() / 30);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counter);

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return binary % 1_000_000;
    }

    private static byte[] Rfc4648Base32Decode(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in value.TrimEnd('=').ToUpperInvariant())
        {
            buffer = (buffer << 5) | alphabet.IndexOf(c);
            bits += 5;
            if (bits >= 8)
            {
                bytes.Add((byte)(buffer >> (bits - 8)));
                bits -= 8;
            }
        }

        return bytes.ToArray();
    }
}
