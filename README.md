# TotpAuthSharp
.NET 10 library for generating and validating time-based one-time password (TOTP) authentication, compatible with Google Authenticator, Microsoft Authenticator and other RFC 6238 apps.

# Based On Library
https://github.com/damirkusar/AspNetCore.Totp AspNetCore.Totp

# What's New

## 3.0.0

- **.NET 10.** The package now targets .NET 10 only. Stay on 2.1.x for .NET 8.
- **`GenerateFromWeb` removed.** It sent the shared secret to quickchart.io. Use `Generate`, which renders the QR code locally.
- **Replay protection.** `TryValidate` returns the time step of the matched code so you can reject a code that has already been used (RFC 6238 section 5.2). See [sample 5](#5-validate-a-code-at-sign-in-with-replay-protection).
- **Codes as text.** `GenerateCode` returns the code as the six-digit string the user sees, including leading zeros ("012345"). `Validate` also accepts the code as a string, ignoring spaces.
- **Secret generation.** `TotpSecret.Generate()` creates a cryptographically random 160-bit secret.
- **Testable clock.** `TotpGenerator` accepts a .NET `TimeProvider`, so tests can pin the time.
- **Tolerance fixed.** `timeToleranceInSeconds` now accepts every 30-second window that falls within it. See [Upgrading from 2.x](#upgrading-from-2x).
- **Dependency injection fixed.** Registering your own `IQrCodeGenerator` is now enough; the factory registration 2.1.0 needed is no longer required.
- **Faster, leaner.** Generating a code is about 2x faster, and validating one allocates 64 bytes instead of about 2.9 KB. Non-square QR codes render about a third faster, and the `QrCodeImage` data URI is built once instead of on every read.
- **Better packaging.** IntelliSense documentation, SourceLink and a symbols package (`.snupkg`) for debugging into the library, and nullable annotations.
- **Dependencies:** SkiaSharp 4.152.1 and `SkiaSharp.NativeAssets.Linux.NoDependencies` 4.152.1. SkiaSharp.QrCode 1.2.0, ZXing.Net 0.16.11 and ZXing.Net.Bindings.SkiaSharp 0.16.24 are unchanged (latest stable).

### Upgrading from 2.x

Code written for 2.1.0 keeps compiling and behaving the same, with these exceptions:

| Change | What to do |
|---|---|
| Requires .NET 10. | Target `net10.0`, or stay on TotpAuthSharp 2.1.x. |
| `GenerateFromWeb` is removed. | Call `Generate` instead. It takes the same arguments apart from `useHttps`. |
| `timeToleranceInSeconds` now accepts every 30-second window it reaches. 2.x ignored tolerances of 30 or less and rounded larger ones to the nearest window, so some values now accept one more window either side: for example 30 (2.x: none, now one), 40 (one, now two) and 75 (two, now three). | Nothing, unless you relied on the old rounding. The default of 60, and 0, 90, 120 and other multiples of 30 from 60 upward, behave exactly as before. A negative tolerance is still treated as zero. |
| Null arguments throw `ArgumentNullException` (with the parameter name) instead of `NullReferenceException`. | Only affects code that catches `NullReferenceException`. |
| `TotpSetupGenerator(IQrCodeGenerator, IQrCodeDownloader)`, `IQrCodeDownloader` and `HttpQrCodeDownloader` are obsolete (CS0618) and will be removed in 4.0. The downloader is ignored. | Use `new TotpSetupGenerator(qrCodeGenerator)`. Projects that treat warnings as errors must make this change. |

The codes, manual setup keys and QR code contents are identical to 2.1.0, so users who have already enrolled are unaffected. This was checked by running the same program against 2.1.0 and 3.0 and comparing 443 observations.

## 2.1.0

- **Choice of local QR generators.** New `ZXingQrCodeGenerator` (ZXing.Net) alongside the default `SkiaQrCodeGenerator` (SkiaSharp.QrCode). Both render on your server, so the shared secret never leaves it.
- **Verified QR accuracy.** Each generator's output is scanned by the *other* library in the test suite.
- **Linux and Alpine support out of the box** via `SkiaSharp.NativeAssets.Linux.NoDependencies`.
- **Fixes:** UTF-8 encoding of non-ASCII issuer and account names; special characters in account names; non-square QR sizes.
- **`GenerateFromWeb` obsolete** (removed in 3.0).
- **Dependencies:** SkiaSharp.QrCode 1.2.0; ZXing.Net 0.16.11 and ZXing.Net.Bindings.SkiaSharp 0.16.24 added.

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

The package targets .NET 10 and runs on Windows, macOS and Linux. Linux support includes Alpine (musl) and Debian/Ubuntu (glibc) on x64 and ARM64.

No extra NuGet packages or system libraries are needed. The Linux native library is included and does not require `libfontconfig`. Version 3.0.0 was tested on the `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`, `sdk:10.0-alpine` and `sdk:10.0` images.

## Public Namespace Structure

TotpAuthSharp
- `CLASS` TotpGenerator
- `CLASS` TotpValidator
- `CLASS` TotpSetupGenerator
- `CLASS` TotpSecret

TotpAuthSharp.Helper
- `CLASS` SkiaQrCodeGenerator (implements `IQrCodeGenerator`)
- `CLASS` ZXingQrCodeGenerator (implements `IQrCodeGenerator`)
- `CLASS` HttpQrCodeDownloader (obsolete)

TotpAuthSharp.Models
- `CLASS` TotpSetup
- `CLASS` QrCodeImage

TotpAuthSharp.Interface
- `INTERFACE` IQrCodeImage
- `INTERFACE` IQrCodeGenerator
- `INTERFACE` IQrCodeDownloader (obsolete)
- `INTERFACE` ITotpGenerator
- `INTERFACE` ITotpSetup
- `INTERFACE` ITotpSetupGenerator
- `INTERFACE` ITotpValidator

## Using the package

__TotpSecret__

Description: Creates a cryptographically random secret for a new user, as a Base32 string. Store it securely against the user and pass it as `accountSecretKey` everywhere else.

```C#
var accountSecretKey = TotpSecret.Generate();      // 20 random bytes (160 bits)
var longerSecret = TotpSecret.Generate(32);        // at least 16 bytes
```

__TotpGenerator__

Constructor Parameters: `None` (uses the system clock), or `TimeProvider` (for example a fake clock in tests)

Description: Generates the TOTP code for a user's secret (RFC 6238: HMAC-SHA1, 30-second time step, 6 digits).

```C#
var generator = new TotpGenerator();
int code = generator.Generate(accountSecretKey);             // 12345 for "012345"
string display = generator.GenerateCode(accountSecretKey);   // "012345", as the app shows it
```

__TotpValidator__

Constructor Parameters: `ITotpGenerator`

Description: Checks a code entered by the user against the codes valid now.

```C#
var validator = new TotpValidator(new TotpGenerator());
bool valid = validator.Validate(accountSecretKey, 12345);
bool validText = validator.Validate(accountSecretKey, "012 345");   // text: spaces ignored, six digits required
bool fresh = validator.TryValidate(accountSecretKey, 12345, out long timeStep); // for replay protection
```

`timeToleranceInSeconds` (default 60) allows for clock drift between the server and the phone. Every 30-second window that falls within the tolerance is accepted:

| Tolerance | Windows accepted |
|---|---|
| 0 (or negative) | the current window only |
| 1 to 30 | current and one either side |
| 31 to 60 (default 60) | current and two either side |
| 61 to 90 | current and three either side |

__TotpSetupGenerator__

Constructor Parameters: `None` (uses `SkiaQrCodeGenerator`), or `IQrCodeGenerator`

Description: Generates the setup details a user needs to add your app to an authenticator app. It returns a `TotpSetup` containing the QR code image (PNG bytes and a `data:` URI) and the manual setup key.

```C#
// Default: SkiaSharp.QrCode
var setupGenerator = new TotpSetupGenerator();

// ZXing.Net, your own IQrCodeGenerator, or a mock (using TotpAuthSharp.Helper;)
var setupGenerator = new TotpSetupGenerator(new ZXingQrCodeGenerator());
```

__Choosing a QR generator__

| Generator | Library | Notes |
|---|---|---|
| `SkiaQrCodeGenerator` | SkiaSharp.QrCode | Default. About 2.3 ms per 300x300 code on a desktop CPU. |
| `ZXingQrCodeGenerator` | ZXing.Net | About 3.3 ms per 300x300 code on a desktop CPU. |

Both use error correction level M, render the QR code locally so the shared secret never leaves your server, produce a PNG of the requested size, and work on every supported platform. The test suite checks each one against the other library's decoder.

__Generate__

```C#
var setup = setupGenerator.Generate(
	issuer: "TestCo",
	accountIdentity: "jane.doe@example.co.za",
	accountSecretKey: accountSecretKey
);
```

- `issuer` is written to the otpauth `issuer` parameter, percent-encoded as UTF-8, so spaces and non-ASCII text are kept (for example "TACS UAT").
- `accountIdentity` has its spaces removed, then is percent-encoded.
- `qrCodeWidth` and `qrCodeHeight` default to 300px. If they differ, the code is drawn square at the smaller size and centred.

## Usage Samples

### 1. Register the services (ASP.NET Core dependency injection)

All the classes are stateless, so singletons are safe.

```C#
using TotpAuthSharp;
using TotpAuthSharp.Helper;
using TotpAuthSharp.Interface;

builder.Services.AddSingleton<ITotpGenerator, TotpGenerator>();
builder.Services.AddSingleton<ITotpValidator, TotpValidator>();

// Optional: pick the QR generator. Without this line, SkiaQrCodeGenerator is used.
builder.Services.AddSingleton<IQrCodeGenerator, ZXingQrCodeGenerator>();

builder.Services.AddSingleton<ITotpSetupGenerator, TotpSetupGenerator>();
```

### 2. Enrol a user

Create a random secret per user and store it with the user record. Treat it like a password: encrypt it at rest and never log it. Then return the QR code and the manual setup key.

```C#
app.MapPost("/2fa/setup", (ITotpSetupGenerator setupGenerator) =>
{
    var accountSecretKey = TotpSecret.Generate();
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

Only switch two-factor authentication on once the user proves their app works. This catches a mistyped manual key or a failed scan. Taking the code as text keeps any leading zeros the user typed.

```C#
app.MapPost("/2fa/confirm", (ITotpValidator validator, CodeRequest request) =>
{
    var accountSecretKey = "..."; // load the unconfirmed secret for the current user

    if (!validator.Validate(accountSecretKey, request.Code))
        return Results.BadRequest("That code did not match. Check your device clock and try again.");

    // Mark two-factor authentication as enabled for the user here.
    return Results.Ok();
});

public record CodeRequest(string Code);
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

### 5. Validate a code at sign-in, with replay protection

A code stays valid for its whole time window, so someone who sees it could reuse it. Store the time step of the last code each user signed in with, and reject any code that is not newer.

```C#
app.MapPost("/2fa/verify", (ITotpValidator validator, CodeRequest request) =>
{
    var accountSecretKey = "...";   // load the confirmed secret for the current user
    long lastUsedTimeStep = -1;     // load the user's last used time step (-1 if none)

    if (!int.TryParse(request.Code.Replace(" ", ""), out var code)
        || !validator.TryValidate(accountSecretKey, code, out var timeStep)
        || timeStep <= lastUsedTimeStep)
        return Results.Unauthorized();

    // Save timeStep as the user's last used time step here.
    return Results.Ok();
});
```

### 6. Without dependency injection (console app or script)

```C#
using TotpAuthSharp;
using TotpAuthSharp.Helper;

var setupGenerator = new TotpSetupGenerator(new ZXingQrCodeGenerator());
var setup = setupGenerator.Generate("TACS UAT", "Jane Doe", TotpSecret.Generate(), qrCodeWidth: 400, qrCodeHeight: 400);

File.WriteAllBytes("totp-qr.png", setup.QrCodeImageBytes);
Console.WriteLine($"Manual setup key: {setup.ManualSetupKey}");
```

### 7. Unit testing your code

Every service has an interface, so you can mock it (example uses Moq). To test time-based logic against the real generator, pass a `TimeProvider` such as `FakeTimeProvider` from Microsoft.Extensions.TimeProvider.Testing.

```C#
var validator = new Mock<ITotpValidator>();
validator.Setup(v => v.Validate(It.IsAny<string>(), 123456, It.IsAny<int>())).Returns(true);

var qrCodeGenerator = new Mock<IQrCodeGenerator>();
qrCodeGenerator.Setup(g => g.Generate(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
    .Returns(new byte[] { 1, 2, 3 });
var setupGenerator = new TotpSetupGenerator(qrCodeGenerator.Object);

var clock = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(1234567890));
var generator = new TotpGenerator(clock);   // generator.GenerateCode("12345678901234567890") == "005924"
```

### Example Implementation

```C#
using System;
using TotpAuthSharp;
using TotpAuthSharp.Interface;
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
                AccountSecretKey = TotpSecret.Generate()
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
        public string GetCode()
        {
            return _totpGenerator.GenerateCode(_userIdentity.AccountSecretKey);
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
        public bool Validate([FromBody] string code)
        {
            return _totpValidator.Validate(_userIdentity.AccountSecretKey, code);
        }
    }
}
```

# License
[MIT License](License.md)
