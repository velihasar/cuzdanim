
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
using DataAccess.Concrete.EntityFramework;
using Core.Entities.Concrete;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.ExpenseCategories.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class CreateExpenseCategoryCommand : IRequest<IResult>
    {
        public string Name { get; set; }


        public class CreateExpenseCategoryCommandHandler : IRequestHandler<CreateExpenseCategoryCommand, IResult>
        {
            private readonly IExpenseCategoryRepository _expenseCategoryRepository;
            private readonly IMediator _mediator;
            public CreateExpenseCategoryCommandHandler(IExpenseCategoryRepository expenseCategoryRepository, IMediator mediator)
            {
                _expenseCategoryRepository = expenseCategoryRepository;
                _mediator = mediator;
            }

            [ValidationAspect(typeof(CreateExpenseCategoryValidator), Priority = 1)]
            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(CreateExpenseCategoryCommand request, CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();
                var result = await _expenseCategoryRepository.GetListAsync(u => u.UserId == userId);
                if (result.Count() == 10)
                {
                    return new ErrorResult(Messages.RecordLimitExceeded);
                }

                var isThereExpenseCategoryRecord = _expenseCategoryRepository.Query().Any(u => u.Name == request.Name && u.UserId == userId);

                if (isThereExpenseCategoryRecord == true)
                    return new ErrorResult(Messages.NameAlreadyExist);

                var addedExpenseCategory = new ExpenseCategory
                {
                    UserId = userId,
                    Name = request.Name,
                    CreatedBy = UserInfoExtensions.GetUserId(),
                    CreatedDate = DateTime.Now
                };

                _expenseCategoryRepository.Add(addedExpenseCategory);
                await _expenseCategoryRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Added);
            }
        }
    }
}