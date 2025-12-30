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
    public class GetTopExpensesQuery
        : IRequest<IDataResult<IEnumerable<TopExpenseDto>>>
    {
        public class GetTopExpensesQueryHandler
            : IRequestHandler<GetTopExpensesQuery, IDataResult<IEnumerable<TopExpenseDto>>>
        {
            private readonly ITransactionRepository _transactionRepository;

            public GetTopExpensesQueryHandler(ITransactionRepository transactionRepository)
            {
                _transactionRepository = transactionRepository;
            }

            [PerformanceAspect(5)]
            public async Task<IDataResult<IEnumerable<TopExpenseDto>>> Handle(
                GetTopExpensesQuery request,
                CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();
                var now = DateTime.Now;

                // Bu ayın başlangıç ve bitiş tarihlerini hesapla
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

                // Amount'a göre sırala ve en büyük 3'ünü al, DTO'ya çevir
                var topExpenses = transactions
                    .OrderByDescending(t => t.Amount)
                    .Take(3)
                    .Select(t => new TopExpenseDto
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        Date = t.Date.ToString("yyyy-MM-dd"),
                        Description = t.Description,
                        ExpenseCategoryName = t.ExpenseCategory != null ? t.ExpenseCategory.Name : null
                    })
                    .ToList();

                return new SuccessDataResult<IEnumerable<TopExpenseDto>>(topExpenses);
            }
        }
    }
}

