using Core.Entities;

namespace Core.Entities.Concrete
{
    /// <summary>
    /// System settings stored in database (like API URLs, configuration values)
    /// </summary>
    public class SystemSetting : BaseEntity, IEntity
    {
        public string Key { get; set; }           // Örn: "CurrencyApiUrl", "GoldApiUrl"
        public string Value { get; set; }           // Örn: "https://finans.truncgil.com/v4/today.json"
        public string Description { get; set; }     // Açıklama
        public string Category { get; set; }       // Örn: "ApiUrls", "ExternalServices"
    }
}

