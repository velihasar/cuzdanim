
using Business.BusinessAspects;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Core.Extensions;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos.ExpenseCategory;
using MailKit.Search;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.ExpenseCategories.Queries
{

    public class GetExpenseCategoriesQuery : IRequest<IDataResult<IEnumerable<ExpenseCategoryGetAllDto>>>
    {
        public class GetExpenseCategoriesQueryHandler : IRequestHandler<GetExpenseCategoriesQuery, IDataResult<IEnumerable<ExpenseCategoryGetAllDto>>>
        {
            private readonly IExpenseCategoryRepository _expenseCategoryRepository;
            private readonly IMediator _mediator;

            public GetExpenseCategoriesQueryHandler(IExpenseCategoryRepository expenseCategoryRepository, IMediator mediator)
            {
                _expenseCategoryRepository = expenseCategoryRepository;
                _mediator = mediator;
            }

            //[PerformanceAspect(5)]
            //[CacheAspect(10)]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<IEnumerable<ExpenseCategoryGetAllDto>>> Handle(GetExpenseCategoriesQuery request, CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();
                var result = await _expenseCategoryRepository.FindAllAsync(u => u.UserId == userId, orderBy: "Name");
                var expensecategoryDto = result.Select(x => new ExpenseCategoryGetAllDto
                {
                    Id = x.Id,
                    Name = x.Name,
                }).ToList();
                return new SuccessDataResult<IEnumerable<ExpenseCategoryGetAllDto>>(expensecategoryDto);
            }
        }
    }
}