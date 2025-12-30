using Core.Entities;

namespace Entities.Dtos.Transaction
{
    public class ExpenseTrendByCategoryDto : IDto
    {
        public string CategoryName { get; set; }
        public decimal CurrentMonthAmount { get; set; }
        public decimal PreviousMonthAmount { get; set; }
        public decimal ChangePercentage { get; set; }
        public decimal ChangeAmount { get; set; }
    }
}

