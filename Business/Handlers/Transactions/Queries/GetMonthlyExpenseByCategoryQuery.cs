using Core.Aspects.Autofac.Performance;
using Core.Enums;
using Core.Extensions;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos.Transaction;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Transactions.Queries
{
    public class GetMonthlyExpenseByCategoryQuery
        : IRequest<IDataResult<IEnumerable<MonthlyExpenseByCategoryDto>>>
    {
        public class GetMonthlyExpenseByCategoryQueryHandler
            : IRequestHandler<GetMonthlyExpenseByCategoryQuery, IDataResult<IEnumerable<MonthlyExpenseByCategoryDto>>>
        {
            private readonly ITransactionRepository _transactionRepository;

            public GetMonthlyExpenseByCategoryQueryHandler(ITransactionRepository transactionRepository)
            {
                _transactionRepository = transactionRepository;
            }

            [PerformanceAspect(5)]
            public async Task<IDataResult<IEnumerable<MonthlyExpenseByCategoryDto>>> Handle(
                GetMonthlyExpenseByCategoryQuery request,
                CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();
                
                // Bu ayın başlangıç ve bitiş tarihlerini hesapla
                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

                // Bu ayki expense transaction'ları getir
                var transactions = await _transactionRepository.FindAllAsync(
                    filter: x => x.UserId == userId &&
                                 x.Type == TransactionType.Expense &&
                                 x.Date >= startOfMonth &&
                                 x.Date <= endOfMonth,
                    include: i => i.Include(x => x.ExpenseCategory)
                );

                // ExpenseCategoryId'ye göre grupla ve toplam tutarları hesapla
                var groupedExpenses = transactions
                    .Where(t => t.ExpenseCategoryId.HasValue)
                    .GroupBy(t => new
                    {
                        CategoryId = t.ExpenseCategoryId.Value,
                        CategoryName = t.ExpenseCategory != null ? t.ExpenseCategory.Name : "Bilinmeyen"
                    })
                    .Select(g => new
                    {
                        g.Key.CategoryName,
                        TotalAmount = g.Sum(t => t.Amount)
                    })
                    .ToList();

                // Toplam gider tutarını hesapla
                var totalExpenseAmount = groupedExpenses.Sum(g => g.TotalAmount);

                // Yüzde hesapla ve DTO listesi oluştur
                var resultList = groupedExpenses
                    .Select(g => new MonthlyExpenseByCategoryDto
                    {
                        CategoryName = g.CategoryName,
                        TotalAmount = g.TotalAmount,
                        Percentage = totalExpenseAmount > 0 
                            ? Math.Round((g.TotalAmount / totalExpenseAmount) * 100, 2) 
                            : 0
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .ToList();

                return new SuccessDataResult<IEnumerable<MonthlyExpenseByCategoryDto>>(resultList);
            }
        }
    }
}

