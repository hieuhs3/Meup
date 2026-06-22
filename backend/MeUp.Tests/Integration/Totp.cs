using System.Net;
using System.Security.Cryptography;

namespace MeUp.Tests.Integration;

/// <summary>Sinh mã TOTP (RFC 6238, SHA1, 6 số, bước 30s) từ khóa base32 — để test 2FA.</summary>
public static class Totp
{
    public static string Compute(string base32Key)
    {
        var key = Base32Decode(base32Key);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var msg = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(counter)); // 8 byte big-endian

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(msg);

        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
                     | ((hash[offset + 1] & 0xff) << 16)
                     | ((hash[offset + 2] & 0xff) << 8)
                     | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6");
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        input = new string(input.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        var bits = 0;
        var value = 0;
        var output = new List<byte>();
        foreach (var c in input)
        {
            var idx = alphabet.IndexOf(c);
            if (idx < 0) continue;
            value = (value << 5) | idx;
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)((value >> (bits - 8)) & 0xff));
                bits -= 8;
            }
        }
        return output.ToArray();
    }
}
