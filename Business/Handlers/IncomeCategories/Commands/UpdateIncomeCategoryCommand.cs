
using Business.BusinessAspects;
using Business.Constants;
using Business.Handlers.IncomeCategories.ValidationRules;
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


namespace Business.Handlers.IncomeCategories.Commands
{


    public class UpdateIncomeCategoryCommand : IRequest<IResult>
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public class UpdateIncomeCategoryCommandHandler : IRequestHandler<UpdateIncomeCategoryCommand, IResult>
        {
            private readonly IIncomeCategoryRepository _incomeCategoryRepository;
            private readonly ITransactionRepository _transactionRepository;
            private readonly IMediator _mediator;

            public UpdateIncomeCategoryCommandHandler(IIncomeCategoryRepository incomeCategoryRepository, ITransactionRepository transactionRepository, IMediator mediator)
            {
                _incomeCategoryRepository = incomeCategoryRepository;
                _transactionRepository = transactionRepository;
                _mediator = mediator;
            }

            [ValidationAspect(typeof(UpdateIncomeCategoryValidator), Priority = 1)]
            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(UpdateIncomeCategoryCommand request, CancellationToken cancellationToken)
            {
                var isThereIncomeCategoryRecord = await _incomeCategoryRepository.GetAsync(u => u.Id == request.Id);

                if (isThereIncomeCategoryRecord == null)
                {
                    return new ErrorResult("Gelir kategorisi bulunamadı.");
                }

                var userId = UserInfoExtensions.GetUserId();

                if (isThereIncomeCategoryRecord.UserId != userId)
                {
                    return new ErrorResult("Sistem kategorilerini güncelleyemezsiniz.");
                }

                // Bağlı Transaction kayıtları var mı kontrol et
                var hasRelatedTransactions = await _transactionRepository.GetAsync(t => t.IncomeCategoryId == request.Id);
                if (hasRelatedTransactions != null)
                {
                    return new ErrorResult("Bu gelir kategorisi kullanılmakta olduğu için güncellenemez. Önce bu kategoriye ait işlemleri silmeniz veya değiştirmeniz gerekmektedir.");
                }

                isThereIncomeCategoryRecord.UserId = userId;
                isThereIncomeCategoryRecord.Name = request.Name;
                isThereIncomeCategoryRecord.IsActive = true;
                isThereIncomeCategoryRecord.UpdatedBy = UserInfoExtensions.GetUserId();
                isThereIncomeCategoryRecord.UpdatedDate = DateTime.Now;


                _incomeCategoryRepository.Update(isThereIncomeCategoryRecord);
                await _incomeCategoryRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Updated);
            }
        }
    }
}

