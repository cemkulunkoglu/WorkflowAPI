using System.Security.Cryptography;
using System.Text;

namespace AuthServerAPI.Helpers;

public static class HashingHelper
{
    // Şifreleme (String Dönüşlü)
    public static void CreatePasswordHash(string password, out string passwordHash, out string passwordSalt)
    {
        using (var hmac = new HMACSHA512())
        {
            // Byte dizisini Base64 String'e çeviriyoruz
            passwordSalt = Convert.ToBase64String(hmac.Key);
            passwordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }

    // Doğrulama (String Girişli)
    public static bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
    {
        // String'den tekrar Byte dizisine çeviriyoruz
        var saltBytes = Convert.FromBase64String(storedSalt);
        var hashBytes = Convert.FromBase64String(storedHash);

        using (var hmac = new HMACSHA512(saltBytes))
        {
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != hashBytes[i]) return false;
            }
        }
        return true;
    }
}