using TotpAuthSharp.Helper;
using Xunit;

namespace TotpAuthSharp.Tests.Helper;

public class UrlEncoderTests
{
    [Theory]
    [InlineData("TACS UAT", "TACS%20UAT")]
    [InlineData("AZaz09-_.~", "AZaz09-_.~")]
    [InlineData("a&b=c?d#e:f/g@h", "a%26b%3Dc%3Fd%23e%3Af%2Fg%40h")]
    [InlineData("100%", "100%25")]
    public void Encode_Ascii_EncodesReservedCharacters(string value, string expected)
    {
        Assert.Equal(expected, UrlEncoder.Encode(value));
    }

    [Theory]
    [InlineData("Café", "Caf%C3%A9")]
    [InlineData("Zoë Müller", "Zo%C3%AB%20M%C3%BCller")]
    [InlineData("€", "%E2%82%AC")]
    [InlineData("日本", "%E6%97%A5%E6%9C%AC")]
    [InlineData("😀", "%F0%9F%98%80")]
    public void Encode_NonAscii_UsesUtf8PercentEncoding(string value, string expected)
    {
        Assert.Equal(expected, UrlEncoder.Encode(value));
    }
}
