using Core.Entities;
using Core.Enums;
using System;

namespace Core.Entities.Concrete
{
    public class Transaction:BaseEntity,IEntity
    {
        // Tüm finansal işlemleri temsil eder: gelir, gider veya varlık transferi.
        // Gelir ve gider kategorileri opsiyonel olabilir.
        public int UserId { get; set; }
        public int? AssetId { get; set; }
        public int? IncomeCategoryId { get; set; }
        public int? ExpenseCategoryId { get; set; }

        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Description { get; set; }
        public TransactionType Type { get; set; } // Income / Expense / Transfer
        public bool IsMonthlyRecurring { get; set; } = false;
        public bool IsBalanceCarriedOver { get; set; }
        public int? DayOfMonth { get; set; } // 1-31

        // Navigation
        public virtual User User { get; set; }
        public virtual Asset Asset { get; set; }
        public virtual IncomeCategory IncomeCategory { get; set; }
        public virtual ExpenseCategory ExpenseCategory { get; set; }
    }
}

