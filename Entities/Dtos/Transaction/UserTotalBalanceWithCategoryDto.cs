using Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Dtos.Transaction
{
    public class UserTotalBalanceWithCategoryDto:IDto
    {
        public int? IncomeCategoryId { get; set; }
        public string Category { get; set; }
        public decimal TotalBalance { get; set; }
    }
}
