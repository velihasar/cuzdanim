
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
            private readonly IMediator _mediator;

            public UpdateIncomeCategoryCommandHandler(IIncomeCategoryRepository incomeCategoryRepository, IMediator mediator)
            {
                _incomeCategoryRepository = incomeCategoryRepository;
                _mediator = mediator;
            }

            [ValidationAspect(typeof(UpdateIncomeCategoryValidator), Priority = 1)]
            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(UpdateIncomeCategoryCommand request, CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();
                var isThereIncomeCategoryRecord = await _incomeCategoryRepository.GetAsync(u => u.Id == request.Id);


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

