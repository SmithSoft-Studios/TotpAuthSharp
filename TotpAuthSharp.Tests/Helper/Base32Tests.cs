using Xunit;
using TotpAuthSharp.Helper;
namespace TotpAuthSharp.Tests.Helper;

public class Base32Tests
{
    [Fact]
    public void Encode_ShouldCreateCorrectEncodedString()
    {
        var encoded = Base32.Encode(TotpAuthTests.AccountSecretKey);
        Assert.Equal(TotpAuthTests.AccountSecretKeyEncoded, encoded);
    }

    [Fact]
    public void Decode_ShouldCreateCorrectDecodedString()
    {
        var decoded = Base32.Decode(TotpAuthTests.AccountSecretKeyEncoded);
        Assert.Equal(TotpAuthTests.AccountSecretKey, decoded);
    }

    [Fact]
    public void Encode_Decode_WithDash_Succeed()
    {
        var encoded = Base32.Encode("12345-123");
        Assert.Equal("GEZDGNBVFUYTEMY", encoded);

        var decoded = Base32.Decode("GEZDGNBVFUYTEMY");
        Assert.Equal("12345-123", decoded);
    }

    [Fact]
    public void Encode_Decode_WithDollar_Succeed()
    {
        var encoded = Base32.Encode("12345$123");
        Assert.Equal("GEZDGNBVEQYTEMY", encoded);

        var decoded = Base32.Decode("GEZDGNBVEQYTEMY");
        Assert.Equal("12345$123", decoded);
    }

    [Fact]
    public void Encode_Decode_Succeed()
    {
        var encoded = Base32.Encode("12345");
        Assert.Equal("GEZDGNBV", encoded);

        var decoded = Base32.Decode("GEZDGNBV");
        Assert.Equal("12345", decoded);
    }
}