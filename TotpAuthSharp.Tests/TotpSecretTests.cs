using System;
using System.Linq;
using Xunit;

namespace TotpAuthSharp.Tests;

public class TotpSecretTests
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    [Fact]
    public void Generate_Default_Is160BitsAsBase32()
    {
        var secret = TotpSecret.Generate();

        Assert.Equal(32, secret.Length); // 20 bytes = 160 bits = 32 Base32 characters
        Assert.All(secret, c => Assert.Contains(c, Base32Alphabet));
    }

    [Theory]
    [InlineData(16, 26)]
    [InlineData(32, 52)]
    [InlineData(64, 103)]
    public void Generate_WithByteLength_ProducesExpectedLength(int byteLength, int expectedLength)
    {
        Assert.Equal(expectedLength, TotpSecret.Generate(byteLength).Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    public void Generate_WithTooFewBytes_Throws(int byteLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TotpSecret.Generate(byteLength));
    }

    [Fact]
    public void Generate_IsRandom()
    {
        var secrets = Enumerable.Range(0, 1000).Select(_ => TotpSecret.Generate()).ToHashSet();

        Assert.Equal(1000, secrets.Count);
    }

    [Fact]
    public void Generate_ProducesASecretTheRestOfTheLibraryAccepts()
    {
        var secret = TotpSecret.Generate();
        var generator = new TotpGenerator();

        Assert.True(new TotpValidator(generator).Validate(secret, generator.Generate(secret)));
        Assert.False(string.IsNullOrEmpty(new TotpSetupGenerator().Generate("TACS UAT", "Jane", secret).ManualSetupKey));
    }
}
