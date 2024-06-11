namespace TotpAuthSharp.Interface;

public interface IQrCodeImage
{
    string DataUri { get; }
    byte[] Bytes { get; }
}