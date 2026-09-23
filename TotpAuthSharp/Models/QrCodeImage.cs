using System;
using TotpAuthSharp.Interface;

namespace TotpAuthSharp.Models;

/// <summary>
///     A PNG QR code image.
/// </summary>
public class QrCodeImage(byte[] bytes) : IQrCodeImage
{
    private string? _dataUri;

    /// <inheritdoc />
    /// <remarks>Built on first use and then reused, so reading it repeatedly is free.</remarks>
    public string DataUri => _dataUri ??= "data:image/png;base64," + Convert.ToBase64String(Bytes);

    /// <inheritdoc />
    public byte[] Bytes { get; } = bytes;
}