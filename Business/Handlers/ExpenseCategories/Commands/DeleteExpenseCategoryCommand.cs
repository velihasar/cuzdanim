
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


namespace Business.Handlers.ExpenseCategories.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class DeleteExpenseCategoryCommand : IRequest<IResult>
    {
        public int Id { get; set; }

        public class DeleteExpenseCategoryCommandHandler : IRequestHandler<DeleteExpenseCategoryCommand, IResult>
        {
            private readonly IExpenseCategoryRepository _expenseCategoryRepository;
            private readonly ITransactionRepository _transactionRepository;
            private readonly IMediator _mediator;

            public DeleteExpenseCategoryCommandHandler(IExpenseCategoryRepository expenseCategoryRepository, ITransactionRepository transactionRepository, IMediator mediator)
            {
                _expenseCategoryRepository = expenseCategoryRepository;
                _transactionRepository = transactionRepository;
                _mediator = mediator;
            }

            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(DeleteExpenseCategoryCommand request, CancellationToken cancellationToken)
            {
                var expenseCategoryToDelete = _expenseCategoryRepository.Get(p => p.Id == request.Id);

                if (expenseCategoryToDelete == null)
                {
                    return new ErrorResult("Gider kategorisi bulunamadı.");
                }

                // Bağlı Transaction kayıtları var mı kontrol et
                var hasRelatedTransactions = await _transactionRepository.GetAsync(t => t.ExpenseCategoryId == request.Id);
                if (hasRelatedTransactions != null)
                {
                    return new ErrorResult("Bu gider kategorisi kullanılmakta olduğu için silinemez. Önce bu kategoriye ait işlemleri silmeniz gerekmektedir.");
                }

                _expenseCategoryRepository.Delete(expenseCategoryToDelete);
                await _expenseCategoryRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Deleted);
            }
        }
    }
}

