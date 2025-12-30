using Core.Entities;

namespace Entities.Dtos.Transaction
{
    public class ExpensePredictionDto : IDto
    {
        public decimal PredictedAmount { get; set; }
        public int BasedOnMonths { get; set; }
        public decimal AverageMonthlyExpense { get; set; }
    }
}

