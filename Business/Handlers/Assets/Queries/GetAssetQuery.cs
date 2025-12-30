
using Business.BusinessAspects;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Core.Entities.Concrete;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;


namespace Business.Handlers.Assets.Queries
{
    public class GetAssetQuery : IRequest<IDataResult<Asset>>
    {
        public int Id { get; set; }

        public class GetAssetQueryHandler : IRequestHandler<GetAssetQuery, IDataResult<Asset>>
        {
            private readonly IAssetRepository _assetRepository;
            private readonly IMediator _mediator;

            public GetAssetQueryHandler(IAssetRepository assetRepository, IMediator mediator)
            {
                _assetRepository = assetRepository;
                _mediator = mediator;
            }
            [LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IDataResult<Asset>> Handle(GetAssetQuery request, CancellationToken cancellationToken)
            {
                var asset = await _assetRepository.GetAsync(p => p.Id == request.Id);
                return new SuccessDataResult<Asset>(asset);
            }
        }
    }
}
