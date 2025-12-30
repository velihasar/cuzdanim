using Core.Aspects.Autofac.Performance;
using Core.Enums;
using Core.Extensions;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos.Transaction;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Transactions.Queries
{
    public class GetExpensePredictionQuery
        : IRequest<IDataResult<ExpensePredictionDto>>
    {
        public class GetExpensePredictionQueryHandler
            : IRequestHandler<GetExpensePredictionQuery, IDataResult<ExpensePredictionDto>>
        {
            private readonly ITransactionRepository _transactionRepository;

            public GetExpensePredictionQueryHandler(ITransactionRepository transactionRepository)
            {
                _transactionRepository = transactionRepository;
            }

            [PerformanceAspect(5)]
            public async Task<IDataResult<ExpensePredictionDto>> Handle(
                GetExpensePredictionQuery request,
                CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();
                var now = DateTime.Now;

                // Son 3 ayın harcamalarını hesapla
                var monthsToAnalyze = 3;
                var startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-monthsToAnalyze);
                var endDate = new DateTime(now.Year, now.Month, 1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

                // Son 3 ayın expense transaction'larını getir
                var transactions = await _transactionRepository.FindAllAsync(
                    filter: x => x.UserId == userId &&
                                 x.Type == TransactionType.Expense &&
                                 x.Date >= startDate &&
                                 x.Date <= endDate
                );

                // Aylara göre grupla ve toplam hesapla
                var monthlyTotals = transactions
                    .GroupBy(t => new { t.Date.Year, t.Date.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Total = g.Sum(t => t.Amount)
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToList();

                decimal predictedAmount = 0;
                decimal averageMonthlyExpense = 0;

                if (monthlyTotals.Count > 0)
                {
                    // Basit ortalama tahmini
                    averageMonthlyExpense = monthlyTotals.Average(x => x.Total);
                    predictedAmount = averageMonthlyExpense;
                }
                else
                {
                    // Eğer geçmiş veri yoksa, bu ayki harcamaları kullan
                    var currentMonthStart = new DateTime(now.Year, now.Month, 1);
                    var currentMonthEnd = now;

                    var currentMonthTransactions = await _transactionRepository.FindAllAsync(
                        filter: x => x.UserId == userId &&
                                     x.Type == TransactionType.Expense &&
                                     x.Date >= currentMonthStart &&
                                     x.Date <= currentMonthEnd
                    );

                    var currentMonthTotal = currentMonthTransactions.Sum(t => t.Amount);
                    
                    // Bu ayki harcamayı, ayın kaçta kaçı geçtiğine göre tahmin et
                    var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
                    var daysPassed = now.Day;
                    
                    if (daysPassed > 0)
                    {
                        predictedAmount = (currentMonthTotal / daysPassed) * daysInMonth;
                        averageMonthlyExpense = currentMonthTotal;
                    }
                    else
                    {
                        predictedAmount = 0;
                        averageMonthlyExpense = 0;
                    }
                }

                var result = new ExpensePredictionDto
                {
                    PredictedAmount = predictedAmount,
                    BasedOnMonths = monthlyTotals.Count > 0 ? monthlyTotals.Count : 1,
                    AverageMonthlyExpense = averageMonthlyExpense
                };

                return new SuccessDataResult<ExpensePredictionDto>(result);
            }
        }
    }
}

