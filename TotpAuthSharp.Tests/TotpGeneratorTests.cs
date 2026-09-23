using System;
using System.Linq;
using TotpAuthSharp.Tests.Helper;
using Xunit;

namespace TotpAuthSharp.Tests;

public class TotpGeneratorTests
{
    // RFC 6238 Appendix B test vectors for HMAC-SHA1. The RFC lists 8-digit codes; the last 6 digits are the
    // 6-digit codes authenticator apps show. The RFC key is the ASCII string "12345678901234567890".
    private const string RfcSecret = "12345678901234567890";

    [Theory]
    [InlineData(59, 287082)]
    [InlineData(1111111109, 81804)]
    [InlineData(1111111111, 50471)]
    [InlineData(1234567890, 5924)]
    [InlineData(2000000000, 279037)]
    [InlineData(20000000000, 353130)]
    public void Generate_MatchesRfc6238TestVectors(long unixSeconds, int expected)
    {
        var generator = new TotpGenerator(FixedTimeProvider.AtUnixSeconds(unixSeconds));

        Assert.Equal(expected, generator.Generate(RfcSecret));
    }

    [Theory]
    [InlineData(1234567890, "005924")]
    [InlineData(1111111109, "081804")]
    [InlineData(59, "287082")]
    public void GenerateCode_KeepsLeadingZeros(long unixSeconds, string expected)
    {
        var generator = new TotpGenerator(FixedTimeProvider.AtUnixSeconds(unixSeconds));

        Assert.Equal(expected, generator.GenerateCode(RfcSecret));
    }

    [Fact]
    public void Generate_ForTimeStep_MatchesGenerateAtThatTime()
    {
        var generator = new TotpGenerator(FixedTimeProvider.AtUnixSeconds(1234567890));

        Assert.Equal(1234567890 / 30, generator.GetCurrentTimeStep());
        Assert.Equal(generator.Generate(RfcSecret), generator.Generate(RfcSecret, generator.GetCurrentTimeStep()));
    }

    [Fact]
    public void ParameterlessConstructor_UsesSystemClock()
    {
        var expectedStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

        var step = new TotpGenerator().GetCurrentTimeStep();

        Assert.InRange(step, expectedStep, expectedStep + 1);
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TotpGenerator(null));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 3)]
    [InlineData(29, 3)]
    [InlineData(30, 3)]
    [InlineData(31, 5)]
    [InlineData(45, 5)]
    [InlineData(60, 5)]
    [InlineData(61, 7)]
    [InlineData(75, 7)]
    [InlineData(90, 7)]
    public void GetValidTotps_CoversEveryWindowWithinTheTolerance(int toleranceSeconds, int expectedCodes)
    {
        var generator = new TotpGenerator(FixedTimeProvider.AtUnixSeconds(1234567890));

        var codes = generator.GetValidTotps(RfcSecret, TimeSpan.FromSeconds(toleranceSeconds)).ToArray();

        Assert.Equal(expectedCodes, codes.Length);
        var step = generator.GetCurrentTimeStep();
        var offset = expectedCodes / 2;
        Assert.Equal(Enumerable.Range(0, expectedCodes).Select(i => generator.Generate(RfcSecret, step - offset + i)), codes);
    }

    [Fact]
    public void GetValidTotps_WithNegativeTolerance_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TotpGenerator().GetValidTotps(RfcSecret, TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void Generate_WithNullSecret_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new TotpGenerator().Generate(null));

        Assert.Equal("accountSecretKey", exception.ParamName);
    }
}
