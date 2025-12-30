using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Core.Utilities.Security.Turnstile
{
    public class TurnstileHelper
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public TurnstileHelper(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<bool> VerifyTokenAsync(string token, string remoteIp = null)
        {
            try
            {
                var secretKey = _configuration["Turnstile:SecretKey"];
                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    return false;
                }

                var requestBody = new
                {
                    secret = secretKey,
                    response = token,
                    remoteip = remoteIp
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<TurnstileVerifyResponse>(responseContent);

                return result != null && result.Success;
            }
            catch
            {
                return false;
            }
        }

        private class TurnstileVerifyResponse
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("challenge_ts")]
            public string ChallengeTs { get; set; }

            [JsonProperty("hostname")]
            public string Hostname { get; set; }

            [JsonProperty("error-codes")]
            public string[] ErrorCodes { get; set; }
        }
    }
}

