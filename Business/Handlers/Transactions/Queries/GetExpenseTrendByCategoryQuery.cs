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
    public class GetExpenseTrendByCategoryQuery
        : IRequest<IDataResult<IEnumerable<ExpenseTrendByCategoryDto>>>
    {
        public class GetExpenseTrendByCategoryQueryHandler
            : IRequestHandler<GetExpenseTrendByCategoryQuery, IDataResult<IEnumerable<ExpenseTrendByCategoryDto>>>
        {
            private readonly ITransactionRepository _transactionRepository;

            public GetExpenseTrendByCategoryQueryHandler(ITransactionRepository transactionRepository)
            {
                _transactionRepository = transactionRepository;
            }

            [PerformanceAspect(5)]
            public async Task<IDataResult<IEnumerable<ExpenseTrendByCategoryDto>>> Handle(
                GetExpenseTrendByCategoryQuery request,
                CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();
                var now = DateTime.Now;

                // Bu ayın başlangıç ve bitiş tarihleri
                var currentMonthStart = new DateTime(now.Year, now.Month, 1);
                var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

                // Geçen ayın başlangıç ve bitiş tarihleri
                var previousMonthStart = currentMonthStart.AddMonths(-1);
                var previousMonthEnd = currentMonthStart.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

                // Bu ayki expense transaction'ları getir
                var currentMonthTransactions = await _transactionRepository.FindAllAsync(
                    filter: x => x.UserId == userId &&
                                 x.Type == TransactionType.Expense &&
                                 x.Date >= currentMonthStart &&
                                 x.Date <= currentMonthEnd,
                    include: i => i.Include(x => x.ExpenseCategory)
                );

                // Geçen ayki expense transaction'ları getir
                var previousMonthTransactions = await _transactionRepository.FindAllAsync(
                    filter: x => x.UserId == userId &&
                                 x.Type == TransactionType.Expense &&
                                 x.Date >= previousMonthStart &&
                                 x.Date <= previousMonthEnd,
                    include: i => i.Include(x => x.ExpenseCategory)
                );

                // Bu ayki kategorilere göre grupla
                var currentMonthGroups = currentMonthTransactions
                    .Where(t => t.ExpenseCategoryId.HasValue)
                    .GroupBy(t => new
                    {
                        CategoryId = t.ExpenseCategoryId.Value,
                        CategoryName = t.ExpenseCategory != null ? t.ExpenseCategory.Name : "Bilinmeyen"
                    })
                    .Select(g => new
                    {
                        g.Key.CategoryId,
                        g.Key.CategoryName,
                        TotalAmount = g.Sum(t => t.Amount)
                    })
                    .ToList();

                // Geçen ayki kategorilere göre grupla
                var previousMonthGroups = previousMonthTransactions
                    .Where(t => t.ExpenseCategoryId.HasValue)
                    .GroupBy(t => new
                    {
                        CategoryId = t.ExpenseCategoryId.Value,
                        CategoryName = t.ExpenseCategory != null ? t.ExpenseCategory.Name : "Bilinmeyen"
                    })
                    .Select(g => new
                    {
                        g.Key.CategoryId,
                        g.Key.CategoryName,
                        TotalAmount = g.Sum(t => t.Amount)
                    })
                    .ToList();

                // Tüm kategorileri birleştir (bu ay veya geçen ay olanlar)
                var allCategories = currentMonthGroups
                    .Select(c => c.CategoryId)
                    .Union(previousMonthGroups.Select(p => p.CategoryId))
                    .Distinct()
                    .ToList();

                var resultList = new List<ExpenseTrendByCategoryDto>();

                foreach (var categoryId in allCategories)
                {
                    var currentMonthData = currentMonthGroups.FirstOrDefault(c => c.CategoryId == categoryId);
                    var previousMonthData = previousMonthGroups.FirstOrDefault(p => p.CategoryId == categoryId);

                    var currentAmount = currentMonthData?.TotalAmount ?? 0;
                    var previousAmount = previousMonthData?.TotalAmount ?? 0;
                    var categoryName = currentMonthData?.CategoryName ?? previousMonthData?.CategoryName ?? "Bilinmeyen";

                    // Değişim tutarı
                    var changeAmount = currentAmount - previousAmount;

                    // Değişim yüzdesi
                    decimal changePercentage = 0;
                    if (previousAmount > 0)
                    {
                        changePercentage = ((currentAmount - previousAmount) / previousAmount) * 100;
                    }
                    else if (currentAmount > 0)
                    {
                        // Geçen ay 0, bu ay var -> %100 artış
                        changePercentage = 100;
                    }
                    // Her ikisi de 0 ise changePercentage 0 kalır

                    resultList.Add(new ExpenseTrendByCategoryDto
                    {
                        CategoryName = categoryName,
                        CurrentMonthAmount = currentAmount,
                        PreviousMonthAmount = previousAmount,
                        ChangePercentage = Math.Round(changePercentage, 2),
                        ChangeAmount = changeAmount
                    });
                }

                // Artış/azalışa göre sırala (en çok artanlar üstte, en çok azalanlar altta)
                var sortedResult = resultList
                    .OrderByDescending(x => x.ChangePercentage)
                    .ToList();

                return new SuccessDataResult<IEnumerable<ExpenseTrendByCategoryDto>>(sortedResult);
            }
        }
    }
}

