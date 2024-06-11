using TotpAuthSharp.Models;
using Xunit;

namespace TotpAuthSharp.Tests;

public class QrCodeImageTests
{
  [Fact]
  public void No_Byte_Allocation_Side_Effects()
  {
    var bytes = "This is a byte test"u8.ToArray();
    var setup = new TotpSetup("IRRELEVANT", bytes);
    Assert.Equal(@"data:image/png;base64,VGhpcyBpcyBhIGJ5dGUgdGVzdA==", setup.QrCodeImage);
    Assert.Equal(bytes, setup.QrCodeImageBytes);
  }
}