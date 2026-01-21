using System;
using System.Security.Cryptography;
using System.Text;

namespace Core.Utilities.Security.Hashing
{
    public static class HashingHelper
    {
        public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException(nameof(password), "Şifre boş olamaz.");
            }

            using var hmac = new HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        public static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                for (var i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHash[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Email için deterministik hash (SHA256)
        // Aynı email her zaman aynı hash'i verir (arama için)
        public static string HashEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            // Email'i normalize et (case-insensitive arama için)
            var normalizedEmail = email.Trim().ToLowerInvariant();
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedEmail));
            
            // Hex string olarak döndür (64 karakter)
            return Convert.ToHexString(hashBytes);
        }
    }
}