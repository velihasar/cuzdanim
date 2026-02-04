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
                                
                                // JSON'u parse et ve private_key'deki gerçek newline karakterlerini düzelt
                                try
                                {
                                    using (var jsonDoc = JsonDocument.Parse(jsonContent))
                                    {
                                        var root = jsonDoc.RootElement;
                                        
                                        // JSON'u Dictionary'ye çevir
                                        var jsonDict = new System.Collections.Generic.Dictionary<string, object>();
                                        
                                        foreach (var prop in root.EnumerateObject())
                                        {
                                            if (prop.Name == "private_key")
                                            {
                                                // private_key'deki gerçek newline karakterlerini al
                                                var privateKey = prop.Value.GetString();
                                                if (!string.IsNullOrEmpty(privateKey))
                                                {
                                                    // Gerçek newline karakterleri zaten var (JSON parse edildiğinde oluştu)
                                                    // JsonSerializer bunları otomatik olarak \n escape'ine çevirecek
                                                    jsonDict[prop.Name] = privateKey;
                                                    Console.WriteLine($"Private key length: {privateKey.Length}");
                                                    Console.WriteLine($"Private key starts with: {privateKey.Substring(0, Math.Min(50, privateKey.Length))}");
                                                }
                                                else
                                                {
                                                    jsonDict[prop.Name] = prop.Value.GetString();
                                                }
                                            }
                                            else if (prop.Value.ValueKind == JsonValueKind.String)
                                            {
                                                jsonDict[prop.Name] = prop.Value.GetString();
                                            }
                                            else if (prop.Value.ValueKind == JsonValueKind.Number)
                                            {
                                                // Number'ı string olarak al ve parse et
                                                if (prop.Value.TryGetInt64(out var intVal))
                                                {
                                                    jsonDict[prop.Name] = intVal;
                                                }
                                                else if (prop.Value.TryGetDouble(out var doubleVal))
                                                {
                                                    jsonDict[prop.Name] = doubleVal;
                                                }
                                                else
                                                {
                                                    jsonDict[prop.Name] = prop.Value.GetRawText();
                                                }
                                            }
                                            else if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                                            {
                                                jsonDict[prop.Name] = prop.Value.GetBoolean();
                                            }
                                            else
                                            {
                                                jsonDict[prop.Name] = prop.Value.GetRawText();
                                            }
                                        }
                                        
                                        // JSON'u serialize et - JsonSerializer otomatik olarak newline'ları \n escape'ine çevirir
                                        var options = new JsonSerializerOptions 
                                        { 
                                            WriteIndented = false,
                                            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Newline'ları \n olarak escape et
                                        };
                                        jsonContent = JsonSerializer.Serialize(jsonDict, options);
                                        Console.WriteLine("JSON private_key fixed and re-serialized using JsonSerializer");
                                        
                                        // Debug: Serialize edilen JSON'da private_key'i kontrol et
                                        try
                                        {
                                            using (var checkDoc = JsonDocument.Parse(jsonContent))
                                            {
                                                if (checkDoc.RootElement.TryGetProperty("private_key", out var checkPk))
                                                {
                                                    var serializedPk = checkPk.GetString();
                                                    Console.WriteLine($"Serialized private_key length: {serializedPk?.Length ?? 0}");
                                                    Console.WriteLine($"Serialized private_key starts with: {serializedPk?.Substring(0, Math.Min(80, serializedPk?.Length ?? 0))}");
                                                    Console.WriteLine($"Serialized private_key ends with: {serializedPk?.Substring(Math.Max(0, (serializedPk?.Length ?? 0) - 50))}");
                                                    Console.WriteLine($"Serialized private_key contains BEGIN: {serializedPk?.Contains("-----BEGIN PRIVATE KEY-----")}");
                                                    Console.WriteLine($"Serialized private_key contains END: {serializedPk?.Contains("-----END PRIVATE KEY-----")}");
                                                }
                                            }
                                        }
                                        catch (Exception debugEx)
                                        {
                                            Console.WriteLine($"Debug: Could not parse serialized JSON: {debugEx.Message}");
                                        }
                                    }
                                }
                                catch (Exception jsonEx)
                                {
                                    Console.WriteLine($"Warning: Could not parse JSON to fix private_key. Error: {jsonEx.Message}");
                                    Console.WriteLine($"Stack trace: {jsonEx.StackTrace}");
                                    // JSON parse edilemezse, exception'ı yukarı fırlat
                                    throw new Exception($"Failed to parse and fix JSON private_key: {jsonEx.Message}", jsonEx);
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

