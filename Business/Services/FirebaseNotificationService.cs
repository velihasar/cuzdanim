using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

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
                                    // JSON'u parse edip private_key'in durumunu kontrol et
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
                                                
                                                // Orijinal JSON string'inde private_key alanını bul
                                                var pkStartIndex = jsonContent.IndexOf("\"private_key\"");
                                                if (pkStartIndex >= 0)
                                                {
                                                    var pkValueStart = jsonContent.IndexOf("\"", pkStartIndex + 13) + 1;
                                                    var pkValueEnd = jsonContent.IndexOf("\"", pkValueStart);
                                                    
                                                    if (pkValueEnd > pkValueStart)
                                                    {
                                                        // Private key değerini al ve newline'ları \n escape karakterlerine çevir
                                                        var originalPkValue = jsonContent.Substring(pkValueStart, pkValueEnd - pkValueStart);
                                                        var fixedPkValue = originalPkValue
                                                            .Replace("\\", "\\\\")  // Önce mevcut escape'leri koru
                                                            .Replace("\"", "\\\"")
                                                            .Replace("\n", "\\n")    // Gerçek newline'ları \n escape'ine çevir
                                                            .Replace("\r", "\\r")
                                                            .Replace("\t", "\\t");
                                                        
                                                        // JSON string'ini düzelt
                                                        jsonContent = jsonContent.Substring(0, pkValueStart) + 
                                                                     fixedPkValue + 
                                                                     jsonContent.Substring(pkValueEnd);
                                                        Console.WriteLine("Fixed private_key in JSON string");
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

