using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Core.Utilities.Security.Encyption
{
    public static class EmailEncryptionHelper
    {
        private static string GetEncryptionKey(IConfiguration configuration)
        {
            if (configuration == null)
            {
                // Configuration null ise default key kullan
                return "CuzdanimMasavTech2024!Key32!!"; // 32 karakter olmalı
            }

            // Önce environment variable'dan dene
            var key = Environment.GetEnvironmentVariable("EMAIL_ENCRYPTION_KEY");
            
            // Environment variable yoksa appsettings.json'dan dene
            if (string.IsNullOrWhiteSpace(key))
            {
                key = configuration["EmailEncryption:Key"];
            }
            
            // Hala yoksa default key kullan
            if (string.IsNullOrWhiteSpace(key))
            {
                // Default key (production'da mutlaka değiştirilmeli)
                key = "CuzdanimMasavTech2024!Key32!!"; // 32 karakter olmalı
            }
            return key;
        }

        public static string EncryptEmail(string email, IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var key = GetEncryptionKey(configuration);
            var keyBytes = Encoding.UTF8.GetBytes(key);
            
            // Key'i 32 byte'a tamamla (AES-256 için)
            var keyArray = new byte[32];
            Array.Copy(keyBytes, 0, keyArray, 0, Math.Min(keyBytes.Length, 32));

            using var aes = Aes.Create();
            aes.Key = keyArray;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var emailBytes = Encoding.UTF8.GetBytes(email);
            var encrypted = encryptor.TransformFinalBlock(emailBytes, 0, emailBytes.Length);

            // IV + encrypted data'yı birleştir
            var result = new byte[aes.IV.Length + encrypted.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

            // Base64 string olarak döndür
            return Convert.ToBase64String(result);
        }

        public static string DecryptEmail(string encryptedEmail, IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(encryptedEmail))
                return null;

            try
            {
                var key = GetEncryptionKey(configuration);
                var keyBytes = Encoding.UTF8.GetBytes(key);
                
                // Key'i 32 byte'a tamamla
                var keyArray = new byte[32];
                Array.Copy(keyBytes, 0, keyArray, 0, Math.Min(keyBytes.Length, 32));

                var fullCipher = Convert.FromBase64String(encryptedEmail);

                using var aes = Aes.Create();
                aes.Key = keyArray;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // IV'yi çıkar
                var iv = new byte[aes.BlockSize / 8];
                var cipher = new byte[fullCipher.Length - iv.Length];
                Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                // Şifre çözme hatası (eski format veya geçersiz data)
                return null;
            }
        }

        /// <summary>
        /// Email'i deterministik olarak şifreler (arama performansı için)
        /// Aynı email her zaman aynı şekilde şifrelenir, böylece veritabanında direkt arama yapılabilir
        /// </summary>
        public static string EncryptEmailDeterministic(string email, IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var key = GetEncryptionKey(configuration);
            var keyBytes = Encoding.UTF8.GetBytes(key);
            
            // Key'i 32 byte'a tamamla (AES-256 için)
            var keyArray = new byte[32];
            Array.Copy(keyBytes, 0, keyArray, 0, Math.Min(keyBytes.Length, 32));

            using var aes = Aes.Create();
            aes.Key = keyArray;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            // IV'yi email'den deterministik olarak türet (hash kullanmadan)
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var emailBytes = Encoding.UTF8.GetBytes(normalizedEmail);
            var iv = new byte[16]; // AES block size (128 bit = 16 byte)
            
            // Email byte'larını IV'ye kopyala, yetersizse tekrar et
            for (int i = 0; i < iv.Length; i++)
            {
                iv[i] = emailBytes[i % emailBytes.Length];
            }
            
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(emailBytes, 0, emailBytes.Length);

            // IV + encrypted data'yı birleştir
            var result = new byte[iv.Length + encrypted.Length];
            Array.Copy(iv, 0, result, 0, iv.Length);
            Array.Copy(encrypted, 0, result, iv.Length, encrypted.Length);

            return Convert.ToBase64String(result);
        }
    }
}

