
using Business.Constants;
using Core.Aspects.Autofac.Caching;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using DataAccess.Abstract;
using MediatR;
using System.Threading;
using System.Threading.Tasks;


namespace Business.Handlers.Assets.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class DeleteAssetCommand : IRequest<IResult>
    {
        public int Id { get; set; }

        public class DeleteAssetCommandHandler : IRequestHandler<DeleteAssetCommand, IResult>
        {
            private readonly IAssetRepository _assetRepository;
            private readonly IMediator _mediator;

            public DeleteAssetCommandHandler(IAssetRepository assetRepository, IMediator mediator)
            {
                _assetRepository = assetRepository;
                _mediator = mediator;
            }

            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(DeleteAssetCommand request, CancellationToken cancellationToken)
            {
                var assetToDelete = _assetRepository.Get(p => p.Id == request.Id);

                _assetRepository.Delete(assetToDelete);
                await _assetRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Deleted);
            }
        }
    }
}

