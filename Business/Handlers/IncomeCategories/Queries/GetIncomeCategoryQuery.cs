
using Business.BusinessAspects;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Core.Entities.Concrete;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;


namespace Business.Handlers.IncomeCategories.Queries
{
    public class GetIncomeCategoryQuery : IRequest<IDataResult<IncomeCategory>>
    {
        public int Id { get; set; }

        public class GetIncomeCategoryQueryHandler : IRequestHandler<GetIncomeCategoryQuery, IDataResult<IncomeCategory>>
        {
            private readonly IIncomeCategoryRepository _incomeCategoryRepository;
            private readonly IMediator _mediator;

            public GetIncomeCategoryQueryHandler(IIncomeCategoryRepository incomeCategoryRepository, IMediator mediator)
            {
                _incomeCategoryRepository = incomeCategoryRepository;
                _mediator = mediator;
            }
            [LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<IncomeCategory>> Handle(GetIncomeCategoryQuery request, CancellationToken cancellationToken)
            {
                var incomeCategory = await _incomeCategoryRepository.GetAsync(p => p.Id == request.Id);
                return new SuccessDataResult<IncomeCategory>(incomeCategory);
            }
        }
    }
}
