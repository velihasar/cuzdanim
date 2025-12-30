using Core.Entities;
using System;
using System.Collections.Generic;

namespace Core.Entities.Concrete
{
    public class Asset: BaseEntity, IEntity
    {
        // Kullanıcının sahip olduğu varlıkları (hesap, nakit, kripto vb.) temsil eder.
        public int UserId { get; set; }
        public int AssetTypeId { get; set; }
        public string Name { get; set; } // Örn: "Garanti TL Hesabı"
        public int Piece { get; set; } //Varlık Miktarı
        public bool IsDebt { get; set; } = false; // Borç mu yoksa varlık mı?

        // Navigation
        public virtual User User { get; set; }
        public virtual AssetType AssetType { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}

