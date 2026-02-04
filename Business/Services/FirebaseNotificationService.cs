using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace Business.Services
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly IConfiguration _configuration;
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        public FirebaseNotificationService(IConfiguration configuration)
        {
            _configuration = configuration;
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            if (_isInitialized)
                return;

            lock (_lock)
            {
                if (_isInitialized)
                    return;

                try
                {
                    // Firebase Admin SDK'nın zaten initialize edilip edilmediğini kontrol et
                    if (FirebaseApp.DefaultInstance == null)
                    {
                        GoogleCredential credential = null;
                        var currentDir = Directory.GetCurrentDirectory();
                        Console.WriteLine($"Firebase initialization - Current directory: {currentDir}");

                        // Önce environment variable'dan base64 encoded içeriği kontrol et
                        var firebaseJsonContent = Environment.GetEnvironmentVariable("FIREBASE_ADMIN_JSON_CONTENT");
                        if (!string.IsNullOrWhiteSpace(firebaseJsonContent))
                        {
                            Console.WriteLine("Using FIREBASE_ADMIN_JSON_CONTENT from environment variable");
                            try
                            {
                                // Base64 string'deki whitespace'leri temizle (newline, space, tab vs.)
                                var cleanedBase64 = firebaseJsonContent.Replace("\n", "").Replace("\r", "").Replace(" ", "").Replace("\t", "");
                                Console.WriteLine($"Base64 string length: {cleanedBase64.Length}");
                                
                                var jsonBytes = Convert.FromBase64String(cleanedBase64);
                                var jsonContent = System.Text.Encoding.UTF8.GetString(jsonBytes);
                                
                                // JSON içeriğini kontrol et (debug için)
                                Console.WriteLine($"JSON content length: {jsonContent.Length}");
                                if (jsonContent.Length > 50)
                                {
                                    Console.WriteLine($"JSON starts with: {jsonContent.Substring(0, 50)}");
                                }
                                
                                // Orijinal JSON string'ini direkt kullan (parse etmeden)
                                // Base64'ten decode edilen JSON zaten doğru formatta olmalı
                                // Eğer private_key'de gerçek newline karakterleri varsa, bunları \n escape karakterlerine çevir
                                // Ama önce JSON'u parse edip kontrol edelim
                                try
                                {
                                    using (var jsonDoc = JsonDocument.Parse(jsonContent))
                                    {
                                        var root = jsonDoc.RootElement;
                                        if (root.TryGetProperty("private_key", out var pkElement))
                                        {
                                            var privateKey = pkElement.GetString();
                                            Console.WriteLine($"Private key length: {privateKey?.Length ?? 0}");
                                            Console.WriteLine($"Private key starts with: {privateKey?.Substring(0, Math.Min(50, privateKey?.Length ?? 0))}");
                                            
                                            // Eğer private_key'de gerçek newline karakterleri varsa (JSON parse edildiğinde oluşmuşsa)
                                            // Orijinal JSON string'inde private_key alanını bulup düzelt
                                            if (privateKey != null && privateKey.Contains("\n"))
                                            {
                                                Console.WriteLine("Private key contains real newlines, fixing in original JSON string...");
                                                
                                                // Orijinal JSON string'inde "private_key" alanını bul
                                                var pkPattern = "\"private_key\"";
                                                var pkStartIndex = jsonContent.IndexOf(pkPattern);
                                                if (pkStartIndex >= 0)
                                                {
                                                    // "private_key": " değerini bul
                                                    var valueStartIndex = jsonContent.IndexOf("\"", pkStartIndex + pkPattern.Length);
                                                    if (valueStartIndex >= 0)
                                                    {
                                                        valueStartIndex = jsonContent.IndexOf("\"", valueStartIndex + 1) + 1; // İkinci " karakterinden sonra
                                                        var valueEndIndex = jsonContent.IndexOf("\"", valueStartIndex);
                                                        
                                                        if (valueEndIndex > valueStartIndex)
                                                        {
                                                            // Private key değerini al (parse edilmiş hali - gerçek newline'lar var)
                                                            // Orijinal JSON string'inde bu değeri bul ve newline'ları \n escape'ine çevir
                                                            var originalValue = jsonContent.Substring(valueStartIndex, valueEndIndex - valueStartIndex);
                                                            
                                                            // Orijinal değerde zaten \n escape karakterleri olabilir, ama parse edildiğinde gerçek newline'lara çevrilmiş
                                                            // Şimdi gerçek newline'ları \n escape'ine çevirmemiz gerekiyor
                                                            // Ama önce orijinal JSON string'inde private_key değerini bulalım
                                                            // Regex kullanarak daha güvenli bir şekilde bulalım
                                                            var regex = new System.Text.RegularExpressions.Regex(@"""private_key""\s*:\s*""([^""]*(?:\\.[^""]*)*)""");
                                                            var match = regex.Match(jsonContent);
                                                            if (match.Success)
                                                            {
                                                                var originalPkValue = match.Groups[1].Value;
                                                                Console.WriteLine($"Original PK value length: {originalPkValue.Length}");
                                                                Console.WriteLine($"Original PK value starts with: {originalPkValue.Substring(0, Math.Min(50, originalPkValue.Length))}");
                                                                
                                                                // Eğer orijinal değerde zaten \n escape karakterleri varsa, hiçbir şey yapma
                                                                // Eğer gerçek newline karakterleri varsa, bunları \n escape'ine çevir
                                                                if (!originalPkValue.Contains("\\n") && originalPkValue.Contains("\n"))
                                                                {
                                                                    // Gerçek newline'ları \n escape'ine çevir
                                                                    var fixedPkValue = originalPkValue
                                                                        .Replace("\\", "\\\\")  // Önce mevcut escape'leri koru
                                                                        .Replace("\"", "\\\"")
                                                                        .Replace("\n", "\\n")    // Gerçek newline'ları \n escape'ine çevir
                                                                        .Replace("\r", "\\r")
                                                                        .Replace("\t", "\\t");
                                                                    
                                                                    // JSON string'ini düzelt
                                                                    jsonContent = jsonContent.Substring(0, match.Groups[1].Index) + 
                                                                                 fixedPkValue + 
                                                                                 jsonContent.Substring(match.Groups[1].Index + match.Groups[1].Length);
                                                                    Console.WriteLine("Fixed private_key in JSON string (real newlines -> \\n)");
                                                                }
                                                                else
                                                                {
                                                                    Console.WriteLine("Original PK value already has \\n escape characters or no real newlines");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                Console.WriteLine("Could not find private_key value in JSON string using regex");
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("Private key format looks correct (no real newlines found)");
                                            }
                                        }
                                    }
                                }
                                catch (Exception jsonEx)
                                {
                                    Console.WriteLine($"Warning: Could not parse JSON to check private_key. Error: {jsonEx.Message}");
                                    Console.WriteLine("Using original JSON string as-is...");
                                }
                                
                                credential = GoogleCredential.FromJson(jsonContent);
                                Console.WriteLine("Firebase credential loaded from environment variable (base64)");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing FIREBASE_ADMIN_JSON_CONTENT: {ex.Message}");
                                if (ex.InnerException != null)
                                {
                                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                                }
                                throw;
                            }
                        }
                        // Sonra environment variable'dan dosya yolunu kontrol et
                        else
                        {
                            var firebaseConfigPath = Environment.GetEnvironmentVariable("FIREBASE_ADMIN_JSON_PATH");
                            
                            if (string.IsNullOrWhiteSpace(firebaseConfigPath))
                            {
                                // Environment variable yoksa, dosya arama mantığını kullan
                                firebaseConfigPath = Path.Combine(
                                    currentDir,
                                    "cuzdanim-firebase-admin.json"
                                );
                                Console.WriteLine($"Checking path 1: {firebaseConfigPath}");

                                if (!File.Exists(firebaseConfigPath))
                                {
                                    var parentDir = Directory.GetParent(currentDir)?.FullName;
                                    Console.WriteLine($"Parent directory: {parentDir}");
                                    firebaseConfigPath = Path.Combine(
                                        parentDir ?? "",
                                        "cuzdanim-firebase-admin.json"
                                    );
                                    Console.WriteLine($"Checking path 2: {firebaseConfigPath}");
                                }

                                if (!File.Exists(firebaseConfigPath))
                                {
                                    firebaseConfigPath = Path.Combine(
                                        currentDir,
                                        "..",
                                        "cuzdanim-firebase-admin.json"
                                    );
                                    Console.WriteLine($"Checking path 3: {firebaseConfigPath}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Using FIREBASE_ADMIN_JSON_PATH: {firebaseConfigPath}");
                            }

                            if (!File.Exists(firebaseConfigPath))
                            {
                                var errorMsg = $"Firebase Admin JSON dosyası bulunamadı. Aranan yollar:\n1. {Path.Combine(currentDir, "cuzdanim-firebase-admin.json")}\n2. {Path.Combine(Directory.GetParent(currentDir)?.FullName ?? "", "cuzdanim-firebase-admin.json")}\n3. {Path.Combine(currentDir, "..", "cuzdanim-firebase-admin.json")}\n\nEnvironment variable kullanımı:\n- FIREBASE_ADMIN_JSON_PATH: Dosya yolu\n- FIREBASE_ADMIN_JSON_CONTENT: Base64 encoded JSON içeriği";
                                Console.WriteLine($"ERROR: {errorMsg}");
                                throw new FileNotFoundException(errorMsg);
                            }

                            Console.WriteLine($"Firebase config file found at: {firebaseConfigPath}");
                            credential = GoogleCredential.FromFile(firebaseConfigPath);
                        }

                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = credential
                        });
                        Console.WriteLine("Firebase Admin SDK initialized successfully");
                    }

                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Firebase initialization error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw new Exception($"Firebase Admin SDK initialize edilemedi: {ex.Message}", ex);
                }
            }
        }

        public async Task<bool> SendNotificationAsync(string fcmToken, string title, string body, object data = null)
        {
            if (string.IsNullOrWhiteSpace(fcmToken))
            {
                return false;
            }

            try
            {
                var message = new Message()
                {
                    Token = fcmToken,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "default"
                        }
                    },
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            Sound = "default",
                            Badge = 1
                        }
                    }
                };

                // Data payload ekle
                if (data != null)
                {
                    var dataDict = new System.Collections.Generic.Dictionary<string, string>();
                    foreach (var prop in data.GetType().GetProperties())
                    {
                        var value = prop.GetValue(data)?.ToString() ?? string.Empty;
                        dataDict[prop.Name] = value;
                    }
                    message.Data = dataDict;
                }

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                return !string.IsNullOrEmpty(response);
            }
            catch (FirebaseMessagingException ex)
            {
                // Invalid token veya diğer hatalar
                System.Diagnostics.Debug.WriteLine($"Firebase notification gönderme hatası: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Notification gönderme hatası: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendNotificationToMultipleAsync(string[] fcmTokens, string title, string body, object data = null)
        {
            if (fcmTokens == null || fcmTokens.Length == 0)
            {
                return false;
            }

            try
            {
                var message = new Message()
                {
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "default"
                        }
                    },
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            Sound = "default",
                            Badge = 1
                        }
                    }
                };

                // Data payload ekle
                System.Collections.Generic.Dictionary<string, string> dataDict = null;
                if (data != null)
                {
                    dataDict = new System.Collections.Generic.Dictionary<string, string>();
                    foreach (var prop in data.GetType().GetProperties())
                    {
                        var value = prop.GetValue(data)?.ToString() ?? string.Empty;
                        dataDict[prop.Name] = value;
                    }
                }

                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(
                    new MulticastMessage
                    {
                        Tokens = fcmTokens,
                        Notification = message.Notification,
                        Data = dataDict,
                        Android = message.Android,
                        Apns = message.Apns
                    }
                );

                return response.SuccessCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Multiple notification gönderme hatası: {ex.Message}");
                return false;
            }
        }
    }
}

