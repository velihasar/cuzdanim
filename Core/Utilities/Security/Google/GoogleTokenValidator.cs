using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;

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
                // Token'ı decode et ve 'aud' claim'ini kontrol et
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(idToken);
                
                // 'aud' claim'ini kontrol et (Web Application Client ID olmalı)
                var audClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "aud")?.Value;
                if (string.IsNullOrWhiteSpace(audClaim) || audClaim != clientId)
                {
                    throw new UnauthorizedAccessException(
                        $"Google token 'aud' claim'i beklenen Client ID ile eşleşmiyor. " +
                        $"Beklenen: {clientId}, Token'da: {audClaim}");
                }

                // Token'ı Google'ın kütüphanesi ile doğrula (imza ve diğer kontroller için)
                // Audience kontrolünü atla çünkü zaten yukarıda kontrol ettik
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

        /// <summary>
        /// Google OAuth authorization code'u ID token'a çevirir
        /// </summary>
        /// <param name="authorizationCode">Google OAuth authorization code</param>
        /// <param name="configuration">Configuration (Google:ClientId ve Google:ClientSecret için)</param>
        /// <returns>Google ID token</returns>
        public static async Task<string> ExchangeAuthorizationCodeAsync(string authorizationCode, IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(authorizationCode))
            {
                throw new ArgumentException("Authorization code boş olamaz.");
            }

            var clientId = configuration["Google:ClientId"];
            var clientSecret = configuration["Google:ClientSecret"];
            var redirectUri = configuration["Google:RedirectUri"];

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException("Google ClientId appsettings.json'da tanımlı değil. Lütfen 'Google:ClientId' ayarını ekleyin.");
            }

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new InvalidOperationException("Google ClientSecret appsettings.json'da tanımlı değil. Lütfen 'Google:ClientSecret' ayarını ekleyin.");
            }

            try
            {
                using var httpClient = new HttpClient();
                var tokenEndpoint = "https://oauth2.googleapis.com/token";
                
                var requestBody = new
                {
                    code = authorizationCode,
                    client_id = clientId,
                    client_secret = clientSecret,
                    redirect_uri = redirectUri,
                    grant_type = "authorization_code"
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(tokenEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Token exchange failed: {responseContent}");
                }

                var tokenResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                var idToken = tokenResponse?.id_token?.ToString();

                if (string.IsNullOrWhiteSpace(idToken))
                {
                    throw new Exception("ID token token response'unda bulunamadı.");
                }

                return idToken;
            }
            catch (Exception ex)
            {
                throw new Exception($"Authorization code ID token'a çevrilemedi: {ex.Message}", ex);
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

