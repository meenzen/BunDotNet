using System.Security.Cryptography;
using System.Text;

namespace BunDotNet;

public static class Hashing
{
    public static string Hash(this byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string Hash(this string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return bytes.Hash();
    }
}
