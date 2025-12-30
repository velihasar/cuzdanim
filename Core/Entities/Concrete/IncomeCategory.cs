using Core.Entities;
using System;
using System.Collections.Generic;

namespace Core.Entities.Concrete
{
    public class IncomeCategory:BaseEntity,IEntity
    {
        // Gelir kategorilerini temsil eder. Kullanıcıya özel veya global olabilir.
        public int? UserId { get; set; } // null → global
        public string Name { get; set; }

        // Navigation
        public virtual User User { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}

