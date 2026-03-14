
using Business.BusinessAspects;
using Business.Constants;
using Business.Handlers.ExpenseCategories.ValidationRules;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Extensions;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Core.Entities.Concrete;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Business.Handlers.ExpenseCategories.Commands
{


    public class UpdateExpenseCategoryCommand : IRequest<IResult>
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public class UpdateExpenseCategoryCommandHandler : IRequestHandler<UpdateExpenseCategoryCommand, IResult>
        {
            private readonly IExpenseCategoryRepository _expenseCategoryRepository;
            private readonly ITransactionRepository _transactionRepository;
            private readonly IMediator _mediator;

            public UpdateExpenseCategoryCommandHandler(IExpenseCategoryRepository expenseCategoryRepository, ITransactionRepository transactionRepository, IMediator mediator)
            {
                _expenseCategoryRepository = expenseCategoryRepository;
                _transactionRepository = transactionRepository;
                _mediator = mediator;
            }

            [ValidationAspect(typeof(UpdateExpenseCategoryValidator), Priority = 1)]
            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(UpdateExpenseCategoryCommand request, CancellationToken cancellationToken)
            {
                var isThereExpenseCategoryRecord = await _expenseCategoryRepository.GetAsync(u => u.Id == request.Id);

                if (isThereExpenseCategoryRecord == null)
                {
                    return new ErrorResult("Gider kategorisi bulunamadı.");
                }

                var userId = UserInfoExtensions.GetUserId();

                if (isThereExpenseCategoryRecord.UserId != userId)
                {
                    return new ErrorResult("Sistem kategorilerini güncelleyemezsiniz.");
                }

                // Bağlı Transaction kayıtları var mı kontrol et
                var hasRelatedTransactions = await _transactionRepository.GetAsync(t => t.ExpenseCategoryId == request.Id);
                if (hasRelatedTransactions != null)
                {
                    return new ErrorResult("Bu gider kategorisi kullanılmakta olduğu için güncellenemez. Önce bu kategoriye ait işlemleri silmeniz veya değiştirmeniz gerekmektedir.");
                }

                isThereExpenseCategoryRecord.UserId = userId;
                isThereExpenseCategoryRecord.Name = request.Name;
                isThereExpenseCategoryRecord.IsActive = true;
                isThereExpenseCategoryRecord.UpdatedBy = UserInfoExtensions.GetUserId();
                isThereExpenseCategoryRecord.UpdatedDate = DateTime.Now;


                _expenseCategoryRepository.Update(isThereExpenseCategoryRecord);
                await _expenseCategoryRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Updated);
            }
        }
    }
}

