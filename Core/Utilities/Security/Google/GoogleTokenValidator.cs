using System;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace Core.Utilities.Security.Google
{
    public static class GoogleTokenValidator
    {
        /// <summary>
        /// Google ID token'ı doğrular ve kullanıcı bilgilerini döndürür
        /// </summary>
        /// <param name="idToken">Google'dan gelen ID token</param>
        /// <param name="configuration">Configuration (Google:ClientId için)</param>
        /// <returns>Google kullanıcı bilgileri (email, name, sub)</returns>
        public static async Task<GoogleUserInfo> ValidateTokenAsync(string idToken, IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                throw new ArgumentException("ID token boş olamaz.");
            }

            var clientId = configuration["Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException("Google ClientId appsettings.json'da tanımlı değil. Lütfen 'Google:ClientId' ayarını ekleyin.");
            }

            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                return new GoogleUserInfo
                {
                    Email = payload.Email,
                    Name = payload.Name,
                    GivenName = payload.GivenName,
                    FamilyName = payload.FamilyName,
                    Picture = payload.Picture,
                    GoogleId = payload.Subject, // Google kullanıcı ID'si
                    EmailVerified = payload.EmailVerified
                };
            }
            catch (Exception ex)
            {
                // Google token doğrulama hatalarını yakala ve daha açıklayıcı mesajlar ver
                string errorMessage;
                
                if (ex.Message.Contains("audience") || ex.Message.Contains("Audience"))
                {
                    errorMessage = "Google Client ID uyumsuzluğu. Lütfen uygulama yapılandırmasını kontrol edin. " +
                                   $"Beklenen Client ID: {clientId}";
                }
                else if (ex.Message.Contains("expired") || ex.Message.Contains("Expired"))
                {
                    errorMessage = "Google giriş token'ı süresi dolmuş. Lütfen tekrar giriş yapın.";
                }
                else if (ex.Message.Contains("Invalid") || 
                         ex.Message.Contains("JWT") || 
                         ex.Message.Contains("token") ||
                         ex.GetType().Name.Contains("Jwt") ||
                         ex.GetType().Name.Contains("Token"))
                {
                    errorMessage = $"Geçersiz Google token. Detay: {ex.Message}";
                }
                else
                {
                    errorMessage = $"Google token doğrulama hatası: {ex.Message}";
                }
                
                throw new UnauthorizedAccessException(errorMessage, ex);
            }
        }
    }

    public class GoogleUserInfo
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string Picture { get; set; }
        public string GoogleId { get; set; } // sub claim
        public bool EmailVerified { get; set; }
    }
}

