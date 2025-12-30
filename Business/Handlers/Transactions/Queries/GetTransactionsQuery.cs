
using Business.BusinessAspects;
using Core.Aspects.Autofac.Performance;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Core.Entities.Concrete;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Aspects.Autofac.Caching;

namespace Business.Handlers.Transactions.Queries
{

    public class GetTransactionsQuery : IRequest<IDataResult<IEnumerable<Transaction>>>
    {
        public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, IDataResult<IEnumerable<Transaction>>>
        {
            private readonly ITransactionRepository _transactionRepository;
            private readonly IMediator _mediator;

            public GetTransactionsQueryHandler(ITransactionRepository transactionRepository, IMediator mediator)
            {
                _transactionRepository = transactionRepository;
                _mediator = mediator;
            }

            [PerformanceAspect(5)]
            //[CacheAspect(10)]
            //[LogAspect(typeof(FileLogger))]
            //[SecuredOperation(Priority = 1)]
            public async Task<IDataResult<IEnumerable<Transaction>>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
            {
                return new SuccessDataResult<IEnumerable<Transaction>>(await _transactionRepository.GetListAsync());
            }
        }
    }
}