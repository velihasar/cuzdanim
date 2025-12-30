using Core.Entities;
using System;
using System.Collections.Generic;

namespace Core.Entities.Concrete
{
    // Gider kategorilerini temsil eder. Kullanıcıya özel veya global olabilir.
    public class ExpenseCategory: BaseEntity,IEntity
    {
        public int? UserId { get; set; }
        public string Name { get; set; }

        // Navigation
        public virtual User User { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}

