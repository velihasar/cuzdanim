
using Business.BusinessAspects;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Core.Entities.Concrete;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;


namespace Business.Handlers.Transactions.Queries
{
    public class GetTransactionQuery : IRequest<IDataResult<Transaction>>
    {
        public int Id { get; set; }

        public class GetTransactionQueryHandler : IRequestHandler<GetTransactionQuery, IDataResult<Transaction>>
        {
            private readonly ITransactionRepository _transactionRepository;
            private readonly IMediator _mediator;

            public GetTransactionQueryHandler(ITransactionRepository transactionRepository, IMediator mediator)
            {
                _transactionRepository = transactionRepository;
                _mediator = mediator;
            }
            [LogAspect(typeof(FileLogger))]
            //[SecuredOperation(Priority = 1)]
            public async Task<IDataResult<Transaction>> Handle(GetTransactionQuery request, CancellationToken cancellationToken)
            {
                var transaction = await _transactionRepository.GetAsync(p => p.Id == request.Id);
                return new SuccessDataResult<Transaction>(transaction);
            }
        }
    }
}
