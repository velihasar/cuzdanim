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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Transactions.Queries
{
    public class GetTransactionWithIncomeCategoryQuery : IRequest<IDataResult<PaginatedResult<IEnumerable<TransactionsWithIncomeCategoryDto>>>>
    {
        public int IncomecategoryId { get; set; }
        public int Page { get; set; } = 1;
        public int Take { get; set; } = 5;

        public class GetTransactionWithIncomeCategoryQueryHandler: IRequestHandler<GetTransactionWithIncomeCategoryQuery, IDataResult<PaginatedResult<IEnumerable<TransactionsWithIncomeCategoryDto>>>>
        {
            private readonly ITransactionRepository _transactionRepository;

            public GetTransactionWithIncomeCategoryQueryHandler(ITransactionRepository transactionRepository)
            {
                _transactionRepository = transactionRepository;
            }

            [PerformanceAspect(5)]
            public async Task<IDataResult<PaginatedResult<IEnumerable<TransactionsWithIncomeCategoryDto>>>> Handle(GetTransactionWithIncomeCategoryQuery request, CancellationToken cancellationToken)
            {
                // IncomeCategoryId'ye göre transactionları filtrele ve pagination uygula
                var paginatedResult = await _transactionRepository.GetPaginatedListAsync(
                    page: request.Page,
                    take: request.Take,
                    orderBy: "Date",
                    isAscending: false,
                    filter: x => x.IncomeCategoryId == request.IncomecategoryId && x.UserId== UserInfoExtensions.GetUserId(),
                    include: i => i
                        .Include(x => x.IncomeCategory)
                        .Include(x => x.ExpenseCategory),
                    noTracking: true,
                    hideDeleted: true
                );

                // Transaction'ları DTO'ya map et
                var resultList = paginatedResult.Data.Select(t => new TransactionsWithIncomeCategoryDto
                {
                    Id = t.Id,
                    IncomeCategoryId = t.IncomeCategoryId,
                    ExpenseCategoryId = t.ExpenseCategoryId,
                    IncomeCategoryName = t.IncomeCategory?.Name,
                    ExpenseCategoryName = t.ExpenseCategory?.Name,
                    Amount = t.Amount,
                    Date = t.Date,
                    Description = t.Description,
                    Type = t.Type
                }).ToList();

                // PaginatedResult oluştur
                var result = new PaginatedResult<IEnumerable<TransactionsWithIncomeCategoryDto>>(
                    resultList,
                    paginatedResult.PageNumber,
                    paginatedResult.PageSize)
                {
                    TotalRecords = paginatedResult.TotalRecords,
                    TotalPages = paginatedResult.TotalPages,
                    FirstPage = paginatedResult.FirstPage,
                    LastPage = paginatedResult.LastPage,
                    NextPage = paginatedResult.NextPage,
                    PreviousPage = paginatedResult.PreviousPage
                };

                return new SuccessDataResult<PaginatedResult<IEnumerable<TransactionsWithIncomeCategoryDto>>>(result);
            }
        }
    }
}
