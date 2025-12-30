using Core.Entities;
using Core.Enums;
using Core.Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Dtos.Transaction
{
    public class TransactionsWithIncomeCategoryDto:IDto
    {
        public int Id { get; set; }
        public int? IncomeCategoryId { get; set; }
        public int? ExpenseCategoryId { get; set; }
        public string IncomeCategoryName { get; set; }
        public string ExpenseCategoryName { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Description { get; set; }
        public TransactionType Type { get; set; }
    }
}
