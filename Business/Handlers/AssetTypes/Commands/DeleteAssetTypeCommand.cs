
using Business.Constants;
using Core.Aspects.Autofac.Caching;
using Business.BusinessAspects;
using Core.Aspects.Autofac.Logging;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Utilities.Results;
using Core.Enums;
using DataAccess.Abstract;
using MediatR;
using System.Threading;
using System.Threading.Tasks;


namespace Business.Handlers.AssetTypes.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class DeleteAssetTypeCommand : IRequest<IResult>
    {
        public int Id { get; set; }

        public class DeleteAssetTypeCommandHandler : IRequestHandler<DeleteAssetTypeCommand, IResult>
        {
            private readonly IAssetTypeRepository _assetTypeRepository;
            private readonly IAssetRepository _assetRepository;
            private readonly IMediator _mediator;

            public DeleteAssetTypeCommandHandler(IAssetTypeRepository assetTypeRepository, IAssetRepository assetRepository, IMediator mediator)
            {
                _assetTypeRepository = assetTypeRepository;
                _assetRepository = assetRepository;
                _mediator = mediator;
            }

            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(DeleteAssetTypeCommand request, CancellationToken cancellationToken)
            {
                var assetTypeToDelete = _assetTypeRepository.Get(p => p.Id == request.Id);

                if (assetTypeToDelete == null)
                {
                    return new ErrorResult("Varlık türü bulunamadı.");
                }

                // Sadece Diger (99) olan varlık türleri silinebilir
                if (assetTypeToDelete.ConvertedAmountType != AssetConvertType.Diger)
                {
                    return new ErrorResult("Bu varlık türü silinemez. Sadece 'Diğer' kategorisindeki varlık türleri silinebilir.");
                }

                // Bağlı Asset kayıtları var mı kontrol et
                var hasRelatedAssets = await _assetRepository.GetAsync(a => a.AssetTypeId == request.Id);
                if (hasRelatedAssets != null)
                {
                    return new ErrorResult("Bu varlık türü kullanılmakta olduğu için silinemez. Önce bu türe ait varlıkları silmeniz gerekmektedir.");
                }

                _assetTypeRepository.Delete(assetTypeToDelete);
                await _assetTypeRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Deleted);
            }
        }
    }
}

