using Business.BusinessAspects;
using Core.Aspects.Autofac.Performance;
using Core.Extensions;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Dtos.Asset;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Handlers.Assets.Queries
{
    public class GetDebtAssetsQuery : IRequest<IDataResult<IEnumerable<AssetDebtGetAllDto>>>
    {
        public class GetDebtAssetsQueryHandler : IRequestHandler<GetDebtAssetsQuery, IDataResult<IEnumerable<AssetDebtGetAllDto>>>
        {
            private readonly IAssetRepository _assetRepository;
            private readonly IMediator _mediator;

            public GetDebtAssetsQueryHandler(IAssetRepository assetRepository, IMediator mediator)
            {
                _assetRepository = assetRepository;
                _mediator = mediator;
            }

            [PerformanceAspect(5)]
            //[CacheAspect(10)]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<IEnumerable<AssetDebtGetAllDto>>> Handle(GetDebtAssetsQuery request, CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();
                var result = await _assetRepository.FindAllAsync(u =>u.IsDebt==true&& u.UserId == userId, include: i => i.Include(m => m.AssetType));
                var assetDto = result.Select(x => new AssetDebtGetAllDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Piece = x.Piece,
                    AssetType = x.AssetType.Name,
                    IsDebt = x.IsDebt
                }).ToList();
                return new SuccessDataResult<IEnumerable<AssetDebtGetAllDto>>(assetDto);
            }
        }
    }
}