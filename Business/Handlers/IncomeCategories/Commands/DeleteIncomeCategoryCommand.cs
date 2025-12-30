
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


namespace Business.Handlers.IncomeCategories.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class DeleteIncomeCategoryCommand : IRequest<IResult>
    {
        public int Id { get; set; }

        public class DeleteIncomeCategoryCommandHandler : IRequestHandler<DeleteIncomeCategoryCommand, IResult>
        {
            private readonly IIncomeCategoryRepository _incomeCategoryRepository;
            private readonly ITransactionRepository _transactionRepository;
            private readonly IMediator _mediator;

            public DeleteIncomeCategoryCommandHandler(IIncomeCategoryRepository incomeCategoryRepository, ITransactionRepository transactionRepository, IMediator mediator)
            {
                _incomeCategoryRepository = incomeCategoryRepository;
                _transactionRepository = transactionRepository;
                _mediator = mediator;
            }

            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(DeleteIncomeCategoryCommand request, CancellationToken cancellationToken)
            {
                var incomeCategoryToDelete = _incomeCategoryRepository.Get(p => p.Id == request.Id);

                if (incomeCategoryToDelete == null)
                {
                    return new ErrorResult("Gelir kategorisi bulunamadı.");
                }

                // Bağlı Transaction kayıtları var mı kontrol et
                var hasRelatedTransactions = await _transactionRepository.GetAsync(t => t.IncomeCategoryId == request.Id);
                if (hasRelatedTransactions != null)
                {
                    return new ErrorResult("Bu gelir kategorisi kullanılmakta olduğu için silinemez. Önce bu kategoriye ait işlemleri silmeniz gerekmektedir.");
                }

                _incomeCategoryRepository.Delete(incomeCategoryToDelete);
                await _incomeCategoryRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Deleted);
            }
        }
    }
}

