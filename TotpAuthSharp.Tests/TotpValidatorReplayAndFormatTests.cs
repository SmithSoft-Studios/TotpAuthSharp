using System;
using TotpAuthSharp.Tests.Helper;
using Xunit;

namespace TotpAuthSharp.Tests;

public class TotpValidatorReplayAndFormatTests
{
    private const string Secret = "12345678901234567890";
    private const long Now = 1234567890; // RFC 6238 vector: code 005924 at this instant

    private readonly FixedTimeProvider _clock = FixedTimeProvider.AtUnixSeconds(Now);
    private readonly TotpGenerator _generator;
    private readonly TotpValidator _validator;

    public TotpValidatorReplayAndFormatTests()
    {
        _generator = new TotpGenerator(_clock);
        _validator = new TotpValidator(_generator);
    }

    [Fact]
    public void Constructor_WithNullGenerator_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TotpValidator(null));
    }

    [Fact]
    public void Validate_CurrentCode_IsValid_AndWrongCodeIsNot()
    {
        Assert.True(_validator.Validate(Secret, 5924));
        Assert.False(_validator.Validate(Secret, 5925));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(29, true)]
    [InlineData(30, true)]
    [InlineData(60, true)]
    [InlineData(89, true)]
    [InlineData(90, false)]
    public void Validate_DefaultTolerance_AcceptsCodesFromTwoWindowsEitherSide(int secondsLater, bool expected)
    {
        // Generated at Now; checked later. The default 60 s tolerance accepts the current window and two either side.
        var code = _generator.Generate(Secret);
        var windowStartOffset = (int)(Now % 30);
        _clock.UtcNow = DateTimeOffset.FromUnixTimeSeconds(Now - windowStartOffset + secondsLater);

        Assert.Equal(expected, _validator.Validate(Secret, code));
    }

    [Fact]
    public void Validate_ToleranceOf30Seconds_NowAcceptsTheNeighbouringWindow()
    {
        // In 2.x a tolerance of 30 seconds or less silently meant "no tolerance".
        var code = _generator.Generate(Secret);
        _clock.UtcNow = _clock.UtcNow.AddSeconds(30);

        Assert.True(_validator.Validate(Secret, code, 30));
        Assert.False(_validator.Validate(Secret, code, 0));
    }

    [Theory]
    [InlineData("005924", true)]
    [InlineData(" 005 924 ", true)]
    [InlineData("005925", false)]
    [InlineData("5924", false)]
    [InlineData("0059245", false)]
    [InlineData("00592a", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Validate_StringCode_RequiresSixDigits(string code, bool expected)
    {
        Assert.Equal(expected, _validator.Validate(Secret, code));
    }

    [Fact]
    public void TryValidate_ReturnsTheMatchedTimeStep()
    {
        var previousWindowCode = _generator.Generate(Secret, _generator.GetCurrentTimeStep() - 1);

        Assert.True(_validator.TryValidate(Secret, previousWindowCode, out var matchedTimeStep));
        Assert.Equal(_generator.GetCurrentTimeStep() - 1, matchedTimeStep);
    }

    [Fact]
    public void TryValidate_WrongCode_ReturnsFalse()
    {
        Assert.False(_validator.TryValidate(Secret, 111111, out var matchedTimeStep));
        Assert.Equal(-1, matchedTimeStep);
    }

    [Fact]
    public void TryValidate_LetsCallersRejectReplayedCodes()
    {
        long lastUsedTimeStep = -1;

        bool SignIn(int code) =>
            _validator.TryValidate(Secret, code, out var step) && step > lastUsedTimeStep && (lastUsedTimeStep = step) == step;

        var code = _generator.Generate(Secret);
        Assert.True(SignIn(code));
        Assert.False(SignIn(code)); // same code again within its window: rejected by the caller's check
    }

    [Fact]
    public void Validate_WithNullSecret_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _validator.Validate(null, 123456));
    }
}
