using System;
using TotpAuthSharp.Interface;

namespace TotpAuthSharp.Models;

public class QrCodeImage(byte[] bytes) : IQrCodeImage
{
    public string DataUri => @"data:image/png;base64," + Convert.ToBase64String(Bytes);
    public byte[] Bytes { get; } = bytes;
}