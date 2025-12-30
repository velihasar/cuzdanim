
using Business.BusinessAspects;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Performance;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Entities.Concrete;
using Core.Extensions;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Entities.Dtos.ExpenseCategory;
using Entities.Dtos.IncomeCategory;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.IncomeCategories.Queries
{

    public class GetIncomeCategoriesQuery : IRequest<IDataResult<IEnumerable<IncomeCategoryGetAllDto>>>
    {
        public class GetIncomeCategoriesQueryHandler : IRequestHandler<GetIncomeCategoriesQuery, IDataResult<IEnumerable<IncomeCategoryGetAllDto>>>
        {
            private readonly IIncomeCategoryRepository _incomeCategoryRepository;
            private readonly IMediator _mediator;

            public GetIncomeCategoriesQueryHandler(IIncomeCategoryRepository incomeCategoryRepository, IMediator mediator)
            {
                _incomeCategoryRepository = incomeCategoryRepository;
                _mediator = mediator;
            }

            [PerformanceAspect(5)]
            //[CacheAspect(10)]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<IEnumerable<IncomeCategoryGetAllDto>>> Handle(GetIncomeCategoriesQuery request, CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();
                var result = await _incomeCategoryRepository.FindAllAsync(u => u.UserId == userId, orderBy:"Name");
                var incomeCategoryDto = result.Select(x => new IncomeCategoryGetAllDto
                {
                    Id = x.Id,
                    Name = x.Name,
                }).ToList();
                return new SuccessDataResult<IEnumerable<IncomeCategoryGetAllDto>>(incomeCategoryDto);
            }
        }
    }
}