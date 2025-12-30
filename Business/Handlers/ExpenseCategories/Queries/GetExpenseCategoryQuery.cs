
using Business.BusinessAspects;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Core.Entities.Concrete;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;


namespace Business.Handlers.ExpenseCategories.Queries
{
    public class GetExpenseCategoryQuery : IRequest<IDataResult<ExpenseCategory>>
    {
        public int Id { get; set; }

        public class GetExpenseCategoryQueryHandler : IRequestHandler<GetExpenseCategoryQuery, IDataResult<ExpenseCategory>>
        {
            private readonly IExpenseCategoryRepository _expenseCategoryRepository;
            private readonly IMediator _mediator;

            public GetExpenseCategoryQueryHandler(IExpenseCategoryRepository expenseCategoryRepository, IMediator mediator)
            {
                _expenseCategoryRepository = expenseCategoryRepository;
                _mediator = mediator;
            }
            [LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<ExpenseCategory>> Handle(GetExpenseCategoryQuery request, CancellationToken cancellationToken)
            {
                var expenseCategory = await _expenseCategoryRepository.GetAsync(p => p.Id == request.Id);
                return new SuccessDataResult<ExpenseCategory>(expenseCategory);
            }
        }
    }
}
