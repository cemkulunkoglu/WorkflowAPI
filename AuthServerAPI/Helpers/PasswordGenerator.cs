using System.Security.Cryptography;

namespace AuthServerAPI.Helpers;

public static class PasswordGenerator
{
    // En az 12 karakter, büyük/küçük harf, rakam, özel karakter içerir
    public static string Generate(int length = 12)
    {
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string specials = "!@#$%^&*()-_=+[]{};:,.?";

        // Her gruptan en az 1 tane garanti
        var required = new List<char>
        {
            lower[RandomNumberGenerator.GetInt32(lower.Length)],
            upper[RandomNumberGenerator.GetInt32(upper.Length)],
            digits[RandomNumberGenerator.GetInt32(digits.Length)],
            specials[RandomNumberGenerator.GetInt32(specials.Length)]
        };

        string all = lower + upper + digits + specials;

        var chars = new List<char>(required);

        for (int i = chars.Count; i < length; i++)
        {
            chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
        }

        // Shuffle
        for (int i = chars.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }
}
