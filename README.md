# TotpAuthSharp
.net8.0 library for generating and validating timed based one time password authentication.

# Based On Library
https://github.com/damirkusar/AspNetCore.Totp AspNetCore.Totp

# What's New

## 2.1.0

- **Choice of local QR generators.** New `ZXingQrCodeGenerator` (ZXing.Net) alongside the default `SkiaQrCodeGenerator` (SkiaSharp.QrCode). Both render on your server, so the shared secret never leaves it.
- **Verified QR accuracy.** Each generator's output is scanned by the *other* library in the test suite (SkiaSharp.QrCode codes are read by ZXing, and ZXing codes by SkiaSharp.QrCode). The tests confirm the secret, issuer and account come back exactly, and that the scanned secret produces codes `TotpValidator` accepts.
- **Linux and Alpine support out of the box.** The package now includes SkiaSharp's Linux native library (`SkiaSharp.NativeAssets.Linux.NoDependencies`). No extra packages or system libraries are needed, including on `aspnet:8.0-alpine`.
- **International issuer and account names fixed.** Non-ASCII text such as "Café" or "Zoë Müller" is now encoded correctly (UTF-8), so it displays properly in authenticator apps.
- **Special characters in account names fixed.** Characters such as `:`, `?`, `#`, `/` and `@` in the account name are now encoded instead of corrupting the QR payload.
- **Non-square QR sizes fixed.** A `qrCodeWidth` different from `qrCodeHeight` used to stretch the code so it could not be scanned reliably. The code is now drawn square and centred. Square sizes, including the 300x300 default, are unchanged.
- **`GenerateFromWeb` is obsolete.** It sends the shared secret to quickchart.io. Use `Generate` instead. `GenerateFromWeb` still works but will be removed in 3.0.
- **Dependencies:** SkiaSharp.QrCode upgraded from 1.0.0 to 1.2.0 (faster encoding; fixes a binary incompatibility with apps that use SkiaSharp.QrCode 1.1 or later). ZXing.Net 0.16.11 and ZXing.Net.Bindings.SkiaSharp 0.16.24 added.

### Upgrading from 2.0.x

- Calls to `GenerateFromWeb` now produce an obsolete warning (CS0618). Projects that treat warnings as errors must switch to `Generate`.
- Account names are now percent-encoded in the QR payload, so `jane@example.com` is written as `jane%40example.com`. This follows the otpauth URL format, and authenticator apps decode it for display. Users who have already enrolled are unaffected, because the QR code is only used during setup.

## 2.0.0

- Upgraded to SkiaSharp.QrCode 1.0.0.
- Added `IQrCodeGenerator` and `IQrCodeDownloader` so QR generation can be injected or mocked.

# Getting Started

## Installing the package

Open up an existing project, or create a new one. Add a reference to the TotpAuthSharp library. 

.NET Core CLI
```  
dotnet add package TotpAuthSharp
```

PowerShell (Nuget Package Manager)
```
Install-Package TotpAuthSharp
```

Manual entry (.csproj) 
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    ...
    <ItemGroup>
        <PackageReference Include="TotpAuthSharp" Version="x.x.x" />
    </ItemGroup>
</Project>
```

## Platform support

The package targets .NET 8 and runs on Windows, macOS and Linux. Linux support includes Alpine (musl) and Debian/Ubuntu (glibc) on x64 and ARM64.

No extra NuGet packages or system libraries are needed. The Linux native library is included and does not require `libfontconfig`. Version 2.1.0 was tested on the `mcr.microsoft.com/dotnet/aspnet:8.0-alpine`, `sdk:8.0-alpine` and `sdk:8.0` images.

## Public Namespace Structure

TotpAuthSharp
- `CLASS` TotpGenerator
- `CLASS` TotpValidator
- `CLASS` TotpSetupGenerator

- TotpAuthSharp.Helper
- `CLASS` Base32
- `CLASS` Guard
- `CLASS` SkiaQrCodeGenerator (implements `IQrCodeGenerator`)
- `CLASS` ZXingQrCodeGenerator (implements `IQrCodeGenerator`)
- `CLASS` HttpQrCodeDownloader (implements `IQrCodeDownloader`)
- `CLASS` TotpHasher
- `CLASS` UrlEncoder

TotpAuthSharp.Models
- `CLASS` TotpSetup
- `CLASS` QrCodeImage

TotpAuthSharp.Interface
- `INTERFACE` IQrCodeImage
- `INTERFACE` IQrCodeGenerator
- `INTERFACE` IQrCodeDownloader
- `INTERFACE` ITotpGenerator
- `INTERFACE` ITotpSetup
- `INTERFACE` ITotpSetupGenerator
- `INTERFACE` ITotpValidator

## Using the package

__Class: TotpGenerator__

Constructor Parameters: `None`

Description: Used for generating the TOTP code, using a super secret code for your app. 

Example
```C#
var generator = new TotpGenerator();
var code = generator.Generate(_userIdentity.AccountSecretKey);
```

__TotpValidator__

Constructor Parameters: `TotpGenerator`

Description: Generates a new token and compares against a given TOTP code to check validity.

Example
```C#
var generator = new TotpGenerator();
var validator = new TotpValidator(generator);
var code = validator.Validate(_userIdentity.AccountSecretKey, code);
```

__TotpSetupGenerator__

Constructor Parameters: `None` (default), or `IQrCodeGenerator, IQrCodeDownloader` for custom composition / testing

Description: Generates the setup details a user needs to add your app to an authenticator app (Google Authenticator, Microsoft Authenticator and so on). It returns a `TotpSetup` containing the QR code image (PNG bytes and a `data:` URI) and the manual setup key.

The parameterless constructor wires the default `SkiaQrCodeGenerator` (local QR generation via SkiaSharp.QrCode) and `HttpQrCodeDownloader` (used only by the obsolete `GenerateFromWeb`). A second constructor accepts these dependencies so you can inject your own implementations or mocks:

```C#
// Default
var qrGenerator = new TotpSetupGenerator();

// Injected (IoC / testing)
var qrGenerator = new TotpSetupGenerator(myQrCodeGenerator, myQrCodeDownloader);

// Local generation with ZXing.Net instead of SkiaSharp.QrCode (using TotpAuthSharp.Helper;)
var qrGenerator = new TotpSetupGenerator(new ZXingQrCodeGenerator(), new HttpQrCodeDownloader());
```

__Choosing a QR generator__

| Generator | Library | Notes |
|---|---|---|
| `SkiaQrCodeGenerator` | SkiaSharp.QrCode | Default. |
| `ZXingQrCodeGenerator` | ZXing.Net | Error correction level M. |

Both render the QR code locally, so the shared secret never leaves your server. Both produce a PNG of the requested size and work on every supported platform. The test suite checks each one against the other library's decoder. You can also supply your own implementation of `IQrCodeGenerator`.

__Generate__ (recommended)

Renders the QR code locally with the configured `IQrCodeGenerator`.

- `issuer` is written to the otpauth `issuer` parameter, percent-encoded as UTF-8, so spaces and non-ASCII text are kept (for example "TACS UAT").
- `accountIdentity` has its spaces removed, then is percent-encoded.
- `qrCodeWidth` and `qrCodeHeight` default to 300px. If they differ, the code is drawn square at the smaller size and centred.

Example
```C#
var qrGenerator = new TotpSetupGenerator();
var qrCode = qrGenerator.Generate(
	issuer: "TestCo",
	accountIdentity: _userIdentity.Id.ToString(),
	accountSecretKey: _userIdentity.AccountSecretKey
);
```

__GenerateFromWeb__ (obsolete)

Description: Fetches the QR code image from quickchart.io and returns it as a TotpSetup class containing the image.

> **Obsolete since 2.1.0:** `GenerateFromWeb` sends the whole otpauth URL, including the shared secret, to quickchart.io. Use `Generate`, which renders locally. `GenerateFromWeb` will be removed in 3.0.

Example
```C#
var qrGenerator = new TotpSetupGenerator();
var qrCode = qrGenerator.GenerateFromWeb(
	issuer: "TestCo",
	accountIdentity: _userIdentity.Id.ToString(),
	accountSecretKey: _userIdentity.AccountSecretKey
);
```

## Usage Samples

### 1. Register the services (ASP.NET Core dependency injection)

All the classes are stateless, so singletons are safe.

```C#
using TotpAuthSharp;
using TotpAuthSharp.Helper;
using TotpAuthSharp.Interface;

builder.Services.AddSingleton<ITotpGenerator, TotpGenerator>();
builder.Services.AddSingleton<ITotpValidator, TotpValidator>();

// Pick the local QR generator: SkiaQrCodeGenerator (default) or ZXingQrCodeGenerator.
builder.Services.AddSingleton<IQrCodeGenerator, ZXingQrCodeGenerator>();

// Register TotpSetupGenerator with a factory so it uses the generator above.
builder.Services.AddSingleton<ITotpSetupGenerator>(sp =>
    new TotpSetupGenerator(sp.GetRequiredService<IQrCodeGenerator>(), new HttpQrCodeDownloader()));
```

> **Why the factory?** `TotpSetupGenerator` has a parameterless constructor. If you register it with `AddSingleton<ITotpSetupGenerator, TotpSetupGenerator>()` without also registering `IQrCodeDownloader`, the container picks the parameterless constructor and silently uses `SkiaQrCodeGenerator`, ignoring your `IQrCodeGenerator` registration.

### 2. Enrol a user

Create a random secret per user and store it with the user record. Treat it like a password: encrypt it at rest and never log it. Then return the QR code and the manual setup key.

```C#
using System.Security.Cryptography;

app.MapPost("/2fa/setup", (ITotpSetupGenerator setupGenerator) =>
{
    var accountSecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(20));
    // Save accountSecretKey against the user here (encrypted), marked as "not yet confirmed".

    var setup = setupGenerator.Generate(
        issuer: "TACS UAT",
        accountIdentity: "jane.doe@example.co.za",
        accountSecretKey: accountSecretKey);

    return Results.Ok(new
    {
        qrCodeImage = setup.QrCodeImage,       // data:image/png;base64,... ready for an <img> tag
        manualSetupKey = setup.ManualSetupKey  // for users who cannot scan
    });
});
```

### 3. Confirm enrolment with the first code

Only switch two-factor authentication on once the user proves their app works. This catches a mistyped manual key or a failed scan.

```C#
app.MapPost("/2fa/confirm", (ITotpValidator validator, ConfirmRequest request) =>
{
    var accountSecretKey = "..."; // load the unconfirmed secret for the current user

    if (!validator.Validate(accountSecretKey, request.Code))
        return Results.BadRequest("That code did not match. Check your device clock and try again.");

    // Mark two-factor authentication as enabled for the user here.
    return Results.Ok();
});

public record ConfirmRequest(int Code);
```

### 4. Show the QR code

As a `data:` URI in a Razor page or view, with no extra endpoint needed:

```html
<img src="@Model.QrCodeImage" width="300" height="300" alt="Scan this QR code with your authenticator app" />
<p>Can't scan it? Enter this key manually: <code>@Model.ManualSetupKey</code></p>
```

Or as a PNG from an endpoint:

```C#
app.MapGet("/2fa/qr.png", (ITotpSetupGenerator setupGenerator) =>
{
    var setup = setupGenerator.Generate("TACS UAT", "jane.doe@example.co.za", "the user's stored secret");
    return Results.File(setup.QrCodeImageBytes, "image/png");
});
```

### 5. Validate a code at sign-in

```C#
app.MapPost("/2fa/verify", (ITotpValidator validator, ConfirmRequest request) =>
{
    var accountSecretKey = "..."; // load the confirmed secret for the current user

    // The third argument is the allowed clock drift in seconds (default 60).
    return validator.Validate(accountSecretKey, request.Code, timeToleranceInSeconds: 30)
        ? Results.Ok()
        : Results.Unauthorized();
});
```

### 6. Without dependency injection (console app or script)

```C#
using TotpAuthSharp;
using TotpAuthSharp.Helper;

var setupGenerator = new TotpSetupGenerator(new ZXingQrCodeGenerator(), new HttpQrCodeDownloader());
var setup = setupGenerator.Generate("TACS UAT", "Jane Doe", "the user's secret", qrCodeWidth: 400, qrCodeHeight: 400);

File.WriteAllBytes("totp-qr.png", setup.QrCodeImageBytes);
Console.WriteLine($"Manual setup key: {setup.ManualSetupKey}");
```

### 7. Unit testing your code

Every service has an interface, so you can mock it (example uses Moq):

```C#
var validator = new Mock<ITotpValidator>();
validator.Setup(v => v.Validate(It.IsAny<string>(), 123456, It.IsAny<int>())).Returns(true);

var qrCodeGenerator = new Mock<IQrCodeGenerator>();
qrCodeGenerator.Setup(g => g.Generate(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
    .Returns(new byte[] { 1, 2, 3 });
var setupGenerator = new TotpSetupGenerator(qrCodeGenerator.Object, Mock.Of<IQrCodeDownloader>());
```

### Example Implementation

```C#
using System;
using TotpAuthSharp;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers
{
    internal struct UserIdentity
    {
        public int Id { get; set; }
        public string AccountSecretKey { get; set; }
    }
    
    internal static class AuthProvider
    {
        public static UserIdentity GetUserIdentity()
        {
            return new UserIdentity()
            {
                Id = new Random().Next(0, 999),
                AccountSecretKey = Guid.NewGuid().ToString()
            };
        }
    }
    
    [ApiController]
    [Route("[controller]")]
    public class TotpController : ControllerBase
    {
        private readonly ITotpGenerator _totpGenerator;
        private readonly ITotpSetupGenerator _totpQrGenerator;
        private readonly ITotpValidator _totpValidator;
        private readonly UserIdentity _userIdentity;

        public TotpController()
        {
            _totpGenerator = new TotpGenerator();
            _totpValidator = new TotpValidator(_totpGenerator);
            _totpQrGenerator = new TotpSetupGenerator();
            _userIdentity = AuthProvider.GetUserIdentity();
        }

        [HttpGet("code")]
        public int GetCode()
        {
            return _totpGenerator.Generate(_userIdentity.AccountSecretKey);
        }

        [HttpGet("qr-code")]
        public IActionResult GetQr()
        {
            var qrCode = _totpQrGenerator.Generate(
                "TestCo",
                _userIdentity.Id.ToString(),
                _userIdentity.AccountSecretKey
            );
            return File(qrCode.QrCodeImageBytes, "image/png");
        }

        [HttpPost("validate")]
        public bool Validate([FromBody] int code)
        {
            return _totpValidator.Validate(_userIdentity.AccountSecretKey, code);
        }
    }
}
```

# License
[MIT License](License.md)
