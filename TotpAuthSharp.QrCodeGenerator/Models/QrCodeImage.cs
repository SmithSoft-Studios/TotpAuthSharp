using TotpAuthSharp.Interface;

namespace TotpAuthSharp.QrCodeGenerator.Models;

public class QrCodeImage(byte[] bytes) : IQrCodeImage
{
    public string DataUri => @"data:image/png;base64," + Convert.ToBase64String(Bytes);
    public byte[] Bytes { get; } = bytes;
}