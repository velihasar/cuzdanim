using Core.Entities;

namespace Entities.Dtos.Transaction
{
    public class MonthlyExpenseByCategoryDto : IDto
    {
        public string CategoryName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
    }
}

