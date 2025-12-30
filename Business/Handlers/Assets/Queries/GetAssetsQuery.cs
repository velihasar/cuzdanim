
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
using Entities.Dtos.Asset;
using Entities.Dtos.AssetType;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Assets.Queries
{

    public class GetAssetsQuery : IRequest<IDataResult<IEnumerable<AssetGetAllDto>>>
    {
        public class GetAssetsQueryHandler : IRequestHandler<GetAssetsQuery, IDataResult<IEnumerable<AssetGetAllDto>>>
        {
            private readonly IAssetRepository _assetRepository;
            private readonly IMediator _mediator;

            public GetAssetsQueryHandler(IAssetRepository assetRepository, IMediator mediator)
            {
                _assetRepository = assetRepository;
                _mediator = mediator;
            }

            [PerformanceAspect(5)]
            //[CacheAspect(10)]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<IEnumerable<AssetGetAllDto>>> Handle(GetAssetsQuery request, CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();
                var result = await _assetRepository.FindAllAsync(u =>u.IsDebt==false&& u.UserId == userId, include: i => i.Include(m => m.AssetType));
                var assetDto = result.Select(x => new AssetGetAllDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Piece = x.Piece,
                    AssetType = x.AssetType.Name,
                    IsDebt = x.IsDebt
                }).ToList();
                return new SuccessDataResult<IEnumerable<AssetGetAllDto>>(assetDto);
            }
        }
    }
}