
using Business.BusinessAspects;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Core.Entities.Concrete;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;


namespace Business.Handlers.AssetTypes.Queries
{
    public class GetAssetTypeQuery : IRequest<IDataResult<AssetType>>
    {
        public int Id { get; set; }

        public class GetAssetTypeQueryHandler : IRequestHandler<GetAssetTypeQuery, IDataResult<AssetType>>
        {
            private readonly IAssetTypeRepository _assetTypeRepository;
            private readonly IMediator _mediator;

            public GetAssetTypeQueryHandler(IAssetTypeRepository assetTypeRepository, IMediator mediator)
            {
                _assetTypeRepository = assetTypeRepository;
                _mediator = mediator;
            }
            [LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<AssetType>> Handle(GetAssetTypeQuery request, CancellationToken cancellationToken)
            {
                var assetType = await _assetTypeRepository.GetAsync(p => p.Id == request.Id);
                return new SuccessDataResult<AssetType>(assetType);
            }
        }
    }
}
