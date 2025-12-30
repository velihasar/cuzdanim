
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
using Entities.Dtos.AssetType;
using Entities.Dtos.IncomeCategory;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.AssetTypes.Queries
{

    public class GetAssetTypesQuery : IRequest<IDataResult<IEnumerable<AssetTypeGetAllDto>>>
    {
        public class GetAssetTypesQueryHandler : IRequestHandler<GetAssetTypesQuery, IDataResult<IEnumerable<AssetTypeGetAllDto>>>
        {
            private readonly IAssetTypeRepository _assetTypeRepository;
            private readonly IMediator _mediator;

            public GetAssetTypesQueryHandler(IAssetTypeRepository assetTypeRepository, IMediator mediator)
            {
                _assetTypeRepository = assetTypeRepository;
                _mediator = mediator;
            }

            [PerformanceAspect(5)]
            //[CacheAspect(10)]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<IEnumerable<AssetTypeGetAllDto>>> Handle(GetAssetTypesQuery request, CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();
                var result = await _assetTypeRepository.FindAllAsync(u => u.UserId == userId || u.UserId == 1);
                var assetTypeDto = result.Select(x => new AssetTypeGetAllDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    ConvertedAmountType = x.ConvertedAmountType,
                    TlValue = x.TlValue,
                    ApiUrlKey = x.ApiUrlKey,
                    UserId = x.UserId,
                    UpdatedDate = x.UpdatedDate,
                })
                .OrderBy(x => (int)x.ConvertedAmountType) // AssetConvertType enum değerine göre sırala
                .ToList();
                return new SuccessDataResult<IEnumerable<AssetTypeGetAllDto>>(assetTypeDto);
            }
        }
    }
}