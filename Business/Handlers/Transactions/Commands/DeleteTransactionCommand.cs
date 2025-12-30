
using Business.Constants;
using Core.Aspects.Autofac.Caching;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using System.Threading;
using System.Threading.Tasks;


namespace Business.Handlers.Transactions.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class DeleteTransactionCommand : IRequest<IResult>
    {
        public int Id { get; set; }

        public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand, IResult>
        {
            private readonly ITransactionRepository _transactionRepository;
            private readonly IMediator _mediator;

            public DeleteTransactionCommandHandler(ITransactionRepository transactionRepository, IMediator mediator)
            {
                _transactionRepository = transactionRepository;
                _mediator = mediator;
            }

            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
            {
                var transactionToDelete = _transactionRepository.Get(p => p.Id == request.Id);

                _transactionRepository.Delete(transactionToDelete);
                await _transactionRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Deleted);
            }
        }
    }
}

