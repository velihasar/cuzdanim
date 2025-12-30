using Core.Entities;

namespace Entities.Dtos.Transaction
{
    public class TopExpenseDto : IDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Date { get; set; }
        public string Description { get; set; }
        public string ExpenseCategoryName { get; set; }
    }
}

