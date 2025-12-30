
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
using DataAccess.Concrete.EntityFramework;
using Core.Entities.Concrete;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.IncomeCategories.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class CreateIncomeCategoryCommand : IRequest<IResult>
    {

        public string Name { get; set; }


        public class CreateIncomeCategoryCommandHandler : IRequestHandler<CreateIncomeCategoryCommand, IResult>
        {
            private readonly IIncomeCategoryRepository _incomeCategoryRepository;
            private readonly IMediator _mediator;
            public CreateIncomeCategoryCommandHandler(IIncomeCategoryRepository incomeCategoryRepository, IMediator mediator)
            {
                _incomeCategoryRepository = incomeCategoryRepository;
                _mediator = mediator;
            }

            [ValidationAspect(typeof(CreateIncomeCategoryValidator), Priority = 1)]
            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(CreateIncomeCategoryCommand request, CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();
                var result = await _incomeCategoryRepository.GetListAsync(u => u.UserId == userId);
                if (result.Count() == 10)
                {
                    return new ErrorResult(Messages.RecordLimitExceeded);
                }
                var isThereIncomeCategoryRecord = _incomeCategoryRepository.Query().Any(u => u.UserId == userId && u.Name == request.Name);

                if (isThereIncomeCategoryRecord == true)
                    return new ErrorResult(Messages.NameAlreadyExist);

                var addedIncomeCategory = new IncomeCategory
                {
                    UserId = userId,
                    Name = request.Name,
                    CreatedBy = UserInfoExtensions.GetUserId(),
                    CreatedDate = DateTime.Now

                };

                _incomeCategoryRepository.Add(addedIncomeCategory);
                await _incomeCategoryRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Added);
            }
        }
    }
}