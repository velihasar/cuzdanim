using Core.Aspects.Autofac.Performance;
using Core.Enums;
using Core.Extensions;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos.Transaction;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Transactions.Queries
{
    public class GetUserTotalBalanceWithCategoryQuery
        : IRequest<IDataResult<IEnumerable<UserTotalBalanceWithCategoryDto>>>
    {

        public class GetUserTotalBalanceWithCategoryQueryHandler
            : IRequestHandler<GetUserTotalBalanceWithCategoryQuery, IDataResult<IEnumerable<UserTotalBalanceWithCategoryDto>>>
        {
            private readonly ITransactionRepository _transactionRepository;

            public GetUserTotalBalanceWithCategoryQueryHandler(ITransactionRepository transactionRepository)
            {
                _transactionRepository = transactionRepository;
            }

            [PerformanceAspect(5)]
            public async Task<IDataResult<IEnumerable<UserTotalBalanceWithCategoryDto>>> Handle(
                GetUserTotalBalanceWithCategoryQuery request,
                CancellationToken cancellationToken)
            {
                // Tüm transactionları kullanıcıya göre getir (Include ile kategorileri yükle)
                var transactions = await _transactionRepository.FindAllAsync(
                    filter: x => x.UserId == UserInfoExtensions.GetUserId(),
                    include: i => i
                        .Include(x => x.IncomeCategory)
                        .Include(x => x.ExpenseCategory)
                );

                // Tüm gelirleri gruplayarak başlıyoruz
                var incomeGroups = transactions
                    .Where(t => t.Type == TransactionType.Income)
                    .GroupBy(t => new {
                        CategoryId = t.IncomeCategoryId,
                        CategoryName = t.IncomeCategory != null ? t.IncomeCategory.Name : "Bilinmeyen"
                    })
                    .ToList();

                var resultList = new List<UserTotalBalanceWithCategoryDto>();

                foreach (var incomeGroup in incomeGroups)
                {
                    // Kategoriye ait toplam gelir
                    decimal totalIncome = incomeGroup.Sum(x => x.Amount);

                    // Aynı gelir kategorisine bağlı giderleri bul
                    var relatedExpenses = transactions
                        .Where(e => e.Type == TransactionType.Expense &&
                                    e.IncomeCategoryId == incomeGroup.Key.CategoryId)
                        .ToList();

                    decimal totalExpenses = relatedExpenses.Sum(e => e.Amount);

                    // Net bakiye = gelir - gider
                    decimal netBalance = totalIncome - totalExpenses;

                    resultList.Add(new UserTotalBalanceWithCategoryDto
                    {
                        IncomeCategoryId = incomeGroup.Key.CategoryId,
                        Category = incomeGroup.Key.CategoryName,
                        TotalBalance = netBalance
                    });
                }

                return new SuccessDataResult<IEnumerable<UserTotalBalanceWithCategoryDto>>(resultList);
            }
        }
    }
}
