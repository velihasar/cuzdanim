using Core.Entities;
using Core.Enums;
using System;
using System.Collections.Generic;

namespace Core.Entities.Concrete
{
    // Varlık türlerini temsil eder. Örn: Nakit, Banka Hesabı, Kripto.
    // UserId null ise bu tip globaldir, yani tüm kullanıcılar tarafından kullanılabilir.
    public class AssetType:BaseEntity,IEntity
    {
        public int? UserId { get; set; } // null → global tip
        public string Name { get; set; }
        public AssetConvertType ConvertedAmountType { get; set; }
        public decimal TlValue { get; set; }
        public string ApiUrlKey { get; set; } // SystemSetting'teki Key (hangi API'den veri alınacağını belirtir)

        // Navigation
        public virtual User User { get; set; }
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }
}

