using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

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
            // Lazy initialization - Firebase sadece notification gönderilirken initialize edilecek
            // Bu sayede Firebase hatası olsa bile API ayağa kalkabilir
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
                                var cleanedBase64 = firebaseJsonContent.Trim().Replace("\n", "").Replace("\r", "").Replace(" ", "").Replace("\t", "");
                                Console.WriteLine($"Base64 string length: {cleanedBase64.Length}");
                                
                                // Base64'ten JSON'a decode et
                                byte[] jsonBytes;
                                try
                                {
                                    jsonBytes = Convert.FromBase64String(cleanedBase64);
                                }
                                catch (FormatException ex)
                                {
                                    Console.WriteLine($"ERROR: Invalid base64 string: {ex.Message}");
                                    throw new Exception($"FIREBASE_ADMIN_JSON_CONTENT contains invalid base64 data: {ex.Message}", ex);
                                }
                                
                                var jsonContent = System.Text.Encoding.UTF8.GetString(jsonBytes);
                                
                                // JSON içeriğini kontrol et (debug için)
                                Console.WriteLine($"JSON content length: {jsonContent.Length}");
                                if (jsonContent.Length > 50)
                                {
                                    Console.WriteLine($"JSON starts with: {jsonContent.Substring(0, 50)}");
                                }
                                
                                // JSON'u parse et, private_key'i düzelt ve yeniden serialize et
                                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                                {
                                    var root = doc.RootElement;
                                    var jsonDict = new Dictionary<string, object>();
                                    
                                    // Tüm property'leri kopyala
                                    foreach (var prop in root.EnumerateObject())
                                    {
                                        if (prop.Name == "private_key")
                                        {
                                            var privateKeyValue = prop.Value.GetString();
                                            if (string.IsNullOrEmpty(privateKeyValue))
                                            {
                                                throw new Exception("'private_key' field is null or empty");
                                            }
                                            
                                            Console.WriteLine($"Private key length: {privateKeyValue.Length}");
                                            var keyStart = privateKeyValue.TrimStart();
                                            if (keyStart.Length > 30)
                                            {
                                                Console.WriteLine($"Private key starts with: {keyStart.Substring(0, 30)}");
                                            }
                                            
                                            // Private key'in başlangıcını kontrol et
                                            if (!keyStart.StartsWith("-----BEGIN PRIVATE KEY-----"))
                                            {
                                                throw new Exception($"Private key does not start with expected header. Starts with: {keyStart.Substring(0, Math.Min(50, keyStart.Length))}");
                                            }
                                            
                                            // Eğer gerçek newline karakterleri varsa, bunları koru
                                            // JsonSerializer otomatik olarak bunları \n olarak escape edecek
                                            // Normalize newlines to \n
                                            if (privateKeyValue.Contains("\r\n") || privateKeyValue.Contains("\r"))
                                            {
                                                privateKeyValue = privateKeyValue
                                                    .Replace("\r\n", "\n")
                                                    .Replace("\r", "\n");
                                                Console.WriteLine("Normalized newlines in private_key (CRLF/CR -> LF)");
                                            }
                                            
                                            // Eğer hiç newline yoksa, bu bir sorun olabilir
                                            if (!privateKeyValue.Contains("\n"))
                                            {
                                                Console.WriteLine("WARNING: Private key has no newlines - key might be malformed");
                                            }
                                            
                                            jsonDict[prop.Name] = privateKeyValue;
                                        }
                                        else
                                        {
                                            // Diğer property'leri kopyala
                                            jsonDict[prop.Name] = prop.Value.ValueKind switch
                                            {
                                                JsonValueKind.String => prop.Value.GetString(),
                                                JsonValueKind.Number => double.TryParse(prop.Value.GetRawText(), out var d) ? (object)d : prop.Value.GetRawText(),
                                                JsonValueKind.True => true,
                                                JsonValueKind.False => false,
                                                JsonValueKind.Null => null,
                                                _ => prop.Value.GetRawText()
                                            };
                                        }
                                    }
                                    
                                    // JSON'u serialize et - JsonSerializer newline'ları otomatik olarak \n olarak escape eder
                                    var serializerOptions = new JsonSerializerOptions
                                    {
                                        WriteIndented = false
                                    };
                                    
                                    var fixedJsonContent = JsonSerializer.Serialize(jsonDict, serializerOptions);
                                    Console.WriteLine($"Fixed JSON prepared (length: {fixedJsonContent.Length})");
                                    
                                    // Önce düzeltilmiş JSON'u dene
                                    try
                                    {
                                        credential = GoogleCredential.FromJson(fixedJsonContent);
                                        Console.WriteLine("Firebase credential loaded successfully from FIREBASE_ADMIN_JSON_CONTENT (using fixed JSON)");
                                    }
                                    catch (Exception credEx)
                                    {
                                        Console.WriteLine($"Failed to load credential from fixed JSON: {credEx.Message}");
                                        Console.WriteLine("Attempting to use original JSON as fallback...");
                                        
                                        // Fallback: Orijinal JSON'u dene (belki zaten doğru formatta)
                                        try
                                        {
                                            credential = GoogleCredential.FromJson(jsonContent);
                                            Console.WriteLine("Firebase credential loaded successfully from original JSON (fallback)");
                                        }
                                        catch (Exception fallbackEx)
                                        {
                                            Console.WriteLine($"Failed to load credential from original JSON: {fallbackEx.Message}");
                                            throw new Exception($"Failed to load Firebase credential from both fixed and original JSON. Fixed JSON error: {credEx.Message}. Original JSON error: {fallbackEx.Message}. The private key in FIREBASE_ADMIN_JSON_CONTENT may be corrupted. Please regenerate your Firebase service account key.", credEx);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing FIREBASE_ADMIN_JSON_CONTENT: {ex.Message}");
                                if (ex.InnerException != null)
                                {
                                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                                }
                                Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
                    // Exception fırlatma - sadece logla
                    // Bu sayede Firebase hatası olsa bile API ayağa kalkabilir
                    // _isInitialized false kalacak, notification gönderilemeyecek ama API çalışacak
                }
            }
        }

        public async Task<bool> SendNotificationAsync(string fcmToken, string title, string body, object data = null)
        {
            if (string.IsNullOrWhiteSpace(fcmToken))
            {
                return false;
            }

            // Lazy initialization - Firebase sadece ilk kullanımda initialize edilir
            if (!_isInitialized)
            {
                try
                {
                    InitializeFirebase();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Firebase init failed in SendNotificationAsync: {ex.Message}");
                    return false; // API çalışsın, sadece push notification düşsün
                }
            }

            // Firebase initialize edilemediyse, notification gönderilemez
            if (!_isInitialized || FirebaseApp.DefaultInstance == null)
            {
                Console.WriteLine("Firebase not initialized, cannot send notification");
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

            // Lazy initialization - Firebase sadece ilk kullanımda initialize edilir
            if (!_isInitialized)
            {
                try
                {
                    InitializeFirebase();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Firebase init failed in SendNotificationToMultipleAsync: {ex.Message}");
                    return false; // API çalışsın, sadece push notification düşsün
                }
            }

            // Firebase initialize edilemediyse, notification gönderilemez
            if (!_isInitialized || FirebaseApp.DefaultInstance == null)
            {
                Console.WriteLine("Firebase not initialized, cannot send notification");
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

