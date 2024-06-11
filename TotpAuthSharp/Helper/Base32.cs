using System.Collections.Generic;
using System.Text;

namespace TotpAuthSharp.Helper;

internal static class Base32
{
    private static readonly string _allowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    internal static string Encode(string accountSecretKey)
    {
        var data = Encoding.UTF8.GetBytes(accountSecretKey);
        var output = new StringBuilder();

        for (var bitIndex = 0; bitIndex < data.Length * 8; bitIndex += 5)
        {
            var dualByte = data[bitIndex / 8] << 8;
            if (bitIndex / 8 + 1 < data.Length)
                dualByte |= data[bitIndex / 8 + 1];
            dualByte = 0x1f & (dualByte >> (16 - bitIndex % 8 - 5));
            output.Append(_allowedCharacters[dualByte]);
        }

        return output.ToString();
    }

    internal static string Decode(string base32)
    {
        var output = new List<byte>();
        var bytes = base32.ToCharArray();
        var bitIndex = 0;

        while (bitIndex < base32.Length * 5)
        {
            var dualByte = _allowedCharacters.IndexOf(bytes[bitIndex / 5]) << 10;
            if (bitIndex / 5 + 1 < bytes.Length)
                dualByte |= _allowedCharacters.IndexOf(bytes[bitIndex / 5 + 1]) << 5;
            if (bitIndex / 5 + 2 < bytes.Length)
                dualByte |= _allowedCharacters.IndexOf(bytes[bitIndex / 5 + 2]);

            dualByte = 0xff & (dualByte >> (15 - bitIndex % 5 - 8));
            output.Add((byte)dualByte);

            bitIndex += 8;
        }

        var key = Encoding.UTF8.GetString(output.ToArray());
        var nullIndex = key.IndexOf('\0');
        if (nullIndex != -1)
        {
            key = key.Remove(nullIndex);
        }
        return key;
    }
}