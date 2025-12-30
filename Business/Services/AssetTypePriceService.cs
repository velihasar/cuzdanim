using Core.Entities.Concrete;
using Core.Enums;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.CrossCuttingConcerns.Caching;
using DataAccess.Abstract;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Business.Services
{
    public class AssetTypePriceService : IAssetTypePriceService
    {
        private readonly HttpClient _httpClient;
        private readonly FileLogger _logger;
        private readonly ISystemSettingRepository _systemSettingRepository;
        private readonly ICacheManager _cacheManager;

        public AssetTypePriceService(HttpClient httpClient, FileLogger logger, ISystemSettingRepository systemSettingRepository, ICacheManager cacheManager)
        {
            _httpClient = httpClient;
            _logger = logger;
            _systemSettingRepository = systemSettingRepository;
            _cacheManager = cacheManager;
        }

        public async Task<decimal?> GetPriceForAssetConvertTypeAsync(AssetConvertType assetConvertType)
        {
            try
            {
                switch (assetConvertType)
                {
                    case AssetConvertType.Tl:
                        return 1.0m; // TL zaten 1.0

                    case AssetConvertType.Dolar:
                    case AssetConvertType.Avro:
                    case AssetConvertType.JaponYeni:
                    case AssetConvertType.IngilizSterlini:
                        return await GetCurrencyRateAsync(assetConvertType, null);

                    case AssetConvertType.GrAltin:
                    case AssetConvertType.CeyrekAltin:
                    case AssetConvertType.YarimAltin:
                    case AssetConvertType.TamAltin:
                        return await GetGoldPriceAsync(assetConvertType, null);

                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"AssetTypePriceService: Error getting price for {assetConvertType}. {ex.Message}. StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// AssetType'a göre fiyat alır. Eğer AssetType'da ApiUrlKey varsa, o API'den veri çeker.
        /// </summary>
        public async Task<decimal?> GetPriceForAssetTypeAsync(AssetType assetType)
        {
            if (assetType == null)
                return null;

            try
            {
                // TL ise direkt 1.0 döndür
                if (assetType.ConvertedAmountType == AssetConvertType.Tl)
                {
                    return 1.0m;
                }

                // Eğer ApiUrlKey varsa, o API'den veri çek
                if (!string.IsNullOrWhiteSpace(assetType.ApiUrlKey))
                {
                    return await GetPriceFromCustomApiAsync(assetType);
                }

                // ApiUrlKey yoksa, eski mantığa göre çalış (AssetConvertType'a göre)
                return await GetPriceForAssetConvertTypeAsync(assetType.ConvertedAmountType);
            }
            catch (Exception ex)
            {
                _logger?.Error($"AssetTypePriceService: Error getting price for AssetType {assetType.Name} (Id: {assetType.Id}). {ex.Message}. StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// AssetType'ın ApiUrlKey'ine göre özel API'den fiyat alır
        /// </summary>
        private async Task<decimal?> GetPriceFromCustomApiAsync(AssetType assetType)
        {
            try
            {
                // API URL'ini SystemSetting'ten al
                var apiUrl = await GetApiUrlAsync(assetType.ApiUrlKey, null);
                if (string.IsNullOrWhiteSpace(apiUrl))
                {
                    _logger?.Error($"AssetTypePriceService: API URL not found for key '{assetType.ApiUrlKey}'");
                    return null;
                }

                // API'den veri çek
                var response = await _httpClient.GetStringAsync(apiUrl);
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(response);
                var root = jsonDoc.RootElement;

                // AssetConvertType'a göre doğru değeri bul
                switch (assetType.ConvertedAmountType)
                {
                    case AssetConvertType.Dolar:
                        return ExtractPriceFromApi(root, "USD");
                    case AssetConvertType.Avro:
                        return ExtractPriceFromApi(root, "EUR");
                    case AssetConvertType.JaponYeni:
                        return ExtractPriceFromApi(root, "JPY");
                    case AssetConvertType.IngilizSterlini:
                        return ExtractPriceFromApi(root, "GBP");
                    case AssetConvertType.GrAltin:
                        return ExtractGoldPriceFromApi(root, AssetConvertType.GrAltin);
                    case AssetConvertType.CeyrekAltin:
                        return ExtractGoldPriceFromApi(root, AssetConvertType.CeyrekAltin);
                    case AssetConvertType.YarimAltin:
                        return ExtractGoldPriceFromApi(root, AssetConvertType.YarimAltin);
                    case AssetConvertType.TamAltin:
                        return ExtractGoldPriceFromApi(root, AssetConvertType.TamAltin);
                    default:
                        // Diğer durumlar için genel bir arama yap
                        return ExtractPriceFromApi(root, null);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"AssetTypePriceService: Error getting price from custom API for AssetType {assetType.Name} (ApiUrlKey: {assetType.ApiUrlKey}). {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// API'den currency fiyatını çıkarır
        /// </summary>
        private decimal? ExtractPriceFromApi(System.Text.Json.JsonElement root, string currencyCode)
        {
            if (!string.IsNullOrWhiteSpace(currencyCode) && root.TryGetProperty(currencyCode, out var currencyElement))
            {
                return ExtractPriceFromElement(currencyElement);
            }

            // CurrencyCode yoksa, root'tan direkt fiyat çıkarmayı dene
            return ExtractPriceFromElement(root);
        }

        /// <summary>
        /// API'den altın fiyatını çıkarır
        /// </summary>
        private decimal? ExtractGoldPriceFromApi(System.Text.Json.JsonElement root, AssetConvertType assetConvertType)
        {
            decimal? gramAltinPrice = null;

            // Önce direkt property isimlerini dene
            string[] directGoldNames = { "GA", "gram-altin", "gramaltin", "gram-altın", "gramaltın", "Gram-Altin", "GramAltin", "GRAM-ALTIN", "GRAMALTIN" };
            foreach (var directName in directGoldNames)
            {
                if (root.TryGetProperty(directName, out var directElement))
                {
                    var price = ExtractPriceFromElement(directElement);
                    if (price.HasValue && price.Value > 0 && price.Value < 1000000)
                    {
                        gramAltinPrice = price;
                        break;
                    }
                }
            }

            // Eğer direkt bulunamadıysa, tüm property'leri tara
            if (!gramAltinPrice.HasValue)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    string propName = prop.Name.ToLower();
                    bool isGoldProperty = propName == "ga" ||
                                         propName == "gram-altin" ||
                                         propName == "gramaltin" ||
                                         propName == "gram-altın" ||
                                         propName == "gramaltın" ||
                                         propName == "gram" ||
                                         propName.StartsWith("ga-") ||
                                         (propName.Contains("gram") && (propName.Contains("altin") || propName.Contains("altın") || propName.Contains("gold")));

                    if (!isGoldProperty && prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (prop.Value.TryGetProperty("Type", out var typeElement))
                        {
                            string typeValue = typeElement.GetString()?.ToLower() ?? "";
                            if (typeValue.Contains("gold") || typeValue.Contains("precious") || typeValue.Contains("metal"))
                            {
                                isGoldProperty = true;
                            }
                        }

                        if (!isGoldProperty && prop.Value.TryGetProperty("Name", out var nameElement))
                        {
                            string nameValue = nameElement.GetString()?.ToLower() ?? "";
                            if (nameValue.Contains("altin") || nameValue.Contains("altın") || nameValue.Contains("gold") || nameValue.Contains("gram"))
                            {
                                isGoldProperty = true;
                            }
                        }
                    }

                    if (isGoldProperty)
                    {
                        var price = ExtractPriceFromElement(prop.Value);
                        if (price.HasValue && price.Value > 0 && price.Value < 1000000)
                        {
                            gramAltinPrice = price;
                            break;
                        }
                    }
                }
            }

            if (!gramAltinPrice.HasValue || gramAltinPrice.Value <= 0)
                return null;

            // Altın tipine göre çarpan uygula
            if (assetConvertType == AssetConvertType.GrAltin)
            {
                return gramAltinPrice;
            }
            else
            {
                decimal multiplier = assetConvertType switch
                {
                    AssetConvertType.CeyrekAltin => 1.75m,
                    AssetConvertType.YarimAltin => 3.5m,
                    AssetConvertType.TamAltin => 7.0m,
                    _ => 1.0m
                };
                return gramAltinPrice.Value * multiplier;
            }
        }

        private async Task<decimal?> GetCurrencyRateAsync(AssetConvertType assetConvertType, string apiUrlKey = null)
        {
            try
            {
                string currencyCode = assetConvertType switch
                {
                    AssetConvertType.Dolar => "USD",
                    AssetConvertType.Avro => "EUR",
                    AssetConvertType.JaponYeni => "JPY",
                    AssetConvertType.IngilizSterlini => "GBP",
                    _ => "USD"
                };
                
                // API URL'ini SystemSetting'ten al (cache ile)
                var apiUrl = await GetApiUrlAsync(apiUrlKey ?? "CurrencyApiUrl", "https://finans.truncgil.com/v4/today.json");
                
                var truncgilResponse = await _httpClient.GetStringAsync(apiUrl);
                using var truncgilJson = System.Text.Json.JsonDocument.Parse(truncgilResponse);
                var truncgilRoot = truncgilJson.RootElement;
                
                if (truncgilRoot.TryGetProperty(currencyCode, out var currencyElement))
                {
                    // Buying veya Selling değerini al (genellikle Buying kullanılır)
                    if (currencyElement.TryGetProperty("Buying", out var buyingElement))
                    {
                        if (buyingElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            return buyingElement.GetDecimal();
                        }
                        else if (buyingElement.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            if (decimal.TryParse(buyingElement.GetString(), NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out decimal buyingRate))
                            {
                                return buyingRate;
                            }
                        }
                    }
                    
                    // Buying yoksa Selling'i dene
                    if (currencyElement.TryGetProperty("Selling", out var sellingElement))
                    {
                        if (sellingElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            return sellingElement.GetDecimal();
                        }
                        else if (sellingElement.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            if (decimal.TryParse(sellingElement.GetString(), NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out decimal sellingRate))
                            {
                                return sellingRate;
                            }
                        }
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger?.Error($"AssetTypePriceService: Error getting currency rate for {assetConvertType}. {ex.Message}");
                return null;
            }
        }

        private async Task<decimal?> GetGoldPriceAsync(AssetConvertType assetConvertType, string apiUrlKey = null)
        {
            // Önce gram altın fiyatını çek, sonra diğerlerini hesapla
            decimal? gramAltinPrice = null;
            
            // 1. Truncgil Finans API'sini dene (en güvenilir ve hızlı)
            try
            {
                // API URL'ini SystemSetting'ten al (cache ile)
                var apiUrl = await GetApiUrlAsync(apiUrlKey ?? "CurrencyApiUrl", "https://finans.truncgil.com/v4/today.json");
                
                var truncgilResponse = await _httpClient.GetStringAsync(apiUrl);
                using var truncgilJson = System.Text.Json.JsonDocument.Parse(truncgilResponse);
                var truncgilRoot = truncgilJson.RootElement;
                
                // Önce tüm property'leri tara (case-insensitive)
                var allProperties = truncgilRoot.EnumerateObject().ToList();
                
                // Önce direkt property isimlerini dene (en yaygın olanlar)
                string[] directGoldNames = { "GA", "gram-altin", "gramaltin", "gram-altın", "gramaltın", "Gram-Altin", "GramAltin", "GRAM-ALTIN", "GRAMALTIN" };
                foreach (var directName in directGoldNames)
                {
                    if (truncgilRoot.TryGetProperty(directName, out var directElement))
                    {
                        var price = ExtractPriceFromElement(directElement);
                        if (price.HasValue && price.Value > 0 && price.Value < 1000000)
                        {
                            gramAltinPrice = price;
                            break;
                        }
                    }
                }
                
                // Eğer direkt bulunamadıysa, tüm property'leri tara
                if (!gramAltinPrice.HasValue)
                {
                    foreach (var prop in allProperties)
                    {
                        string propName = prop.Name.ToLower();
                        
                        // Gram altın için farklı olasılıkları kontrol et
                        bool isGoldProperty = propName == "ga" || 
                                             propName == "gram-altin" || 
                                             propName == "gramaltin" || 
                                             propName == "gram-altın" || 
                                             propName == "gramaltın" ||
                                             propName == "gram" ||
                                             propName.StartsWith("ga-") ||
                                             (propName.Contains("gram") && (propName.Contains("altin") || propName.Contains("altın") || propName.Contains("gold")));
                        
                        // Ayrıca, property'nin içinde "Type" property'si varsa ve "Gold" veya "PreciousMetal" ise
                        if (!isGoldProperty && prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            if (prop.Value.TryGetProperty("Type", out var typeElement))
                            {
                                string typeValue = typeElement.GetString()?.ToLower() ?? "";
                                if (typeValue.Contains("gold") || typeValue.Contains("precious") || typeValue.Contains("metal"))
                                {
                                    isGoldProperty = true;
                                }
                            }
                            
                            // Name property'sinde altın geçiyorsa
                            if (!isGoldProperty && prop.Value.TryGetProperty("Name", out var nameElement))
                            {
                                string nameValue = nameElement.GetString()?.ToLower() ?? "";
                                if (nameValue.Contains("altin") || nameValue.Contains("altın") || nameValue.Contains("gold") || nameValue.Contains("gram"))
                                {
                                    isGoldProperty = true;
                                }
                            }
                        }
                        
                        if (isGoldProperty)
                        {
                            var price = ExtractPriceFromElement(prop.Value);
                            if (price.HasValue && price.Value > 0 && price.Value < 1000000)
                            {
                                gramAltinPrice = price;
                                break;
                            }
                        }
                    }
                }
                
                // Eğer gram altın bulunduysa, diğer altın tiplerini hesapla
                if (gramAltinPrice.HasValue && gramAltinPrice.Value > 0)
                {
                    if (assetConvertType == AssetConvertType.GrAltin)
                    {
                        return gramAltinPrice;
                    }
                    else
                    {
                        decimal multiplier = assetConvertType switch
                        {
                            AssetConvertType.CeyrekAltin => 1.75m,
                            AssetConvertType.YarimAltin => 3.5m,
                            AssetConvertType.TamAltin => 7.0m,
                            _ => 1.0m
                        };
                        
                        return gramAltinPrice.Value * multiplier;
                    }
                }
            }
            catch (Exception truncgilEx)
            {
                _logger?.Error($"AssetTypePriceService: Truncgil API failed for gold prices. {truncgilEx.Message}");
                return null;
            }
            
            // Altın bulunamadı
            return null;
        }
        
        private decimal? ExtractPriceFromElement(System.Text.Json.JsonElement element)
        {
            // Truncgil API formatı için önce "Buying" property'sini dene
            if (element.TryGetProperty("Buying", out var buyingElement))
            {
                if (buyingElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    return buyingElement.GetDecimal();
                }
                else if (buyingElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    if (decimal.TryParse(buyingElement.GetString(), NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out decimal price))
                    {
                        return price;
                    }
                }
            }
            
            // "Selling" property'sini dene (Truncgil API)
            if (element.TryGetProperty("Selling", out var sellingElement))
            {
                if (sellingElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    return sellingElement.GetDecimal();
                }
                else if (sellingElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    if (decimal.TryParse(sellingElement.GetString(), NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out decimal price))
                    {
                        return price;
                    }
                }
            }
            
            // "TRY_Price" property'sini dene (Truncgil API - kripto paralar ve altın için)
            if (element.TryGetProperty("TRY_Price", out var tryPriceElement))
            {
                if (tryPriceElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    return tryPriceElement.GetDecimal();
                }
                else if (tryPriceElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    if (decimal.TryParse(tryPriceElement.GetString(), NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out decimal price))
                    {
                        return price;
                    }
                }
            }
            
            // Direkt number ise
            if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                return element.GetDecimal();
            }
            
            // String ise parse et
            if (element.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var strValue = element.GetString();
                if (decimal.TryParse(strValue, NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out decimal price))
                {
                    return price;
                }
            }
            
            return null;
        }

        /// <summary>
        /// SystemSetting'ten API URL'ini alır (cache ile)
        /// </summary>
        private async Task<string> GetApiUrlAsync(string key, string fallbackUrl)
        {
            try
            {
                // Cache'den kontrol et
                var cacheKey = $"SystemSetting_{key}";
                var cachedUrl = _cacheManager.Get<string>(cacheKey);
                if (!string.IsNullOrWhiteSpace(cachedUrl))
                {
                    return cachedUrl;
                }

                // DB'den çek
                var setting = await _systemSettingRepository.GetAsync(s => s.Key == key);
                if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
                {
                    // Cache'e ekle (60 dakika)
                    _cacheManager.Add(cacheKey, setting.Value, 60);
                    return setting.Value;
                }

                // Fallback: appsettings.json veya default URL
                return fallbackUrl;
            }
            catch (Exception ex)
            {
                _logger?.Error($"AssetTypePriceService: Error getting API URL for key '{key}'. {ex.Message}");
                return fallbackUrl;
            }
        }
    }
}

