
using Business.BusinessAspects;
using Business.Constants;
using Business.Handlers.AssetTypes.ValidationRules;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Extensions;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Core.Entities.Concrete;
using Core.Enums;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic;


namespace Business.Handlers.AssetTypes.Commands
{


    public class UpdateAssetTypeCommand : IRequest<IResult>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public AssetConvertType ConvertedAmountType { get; set; }
        public decimal TlValue { get; set; }
        public string ApiUrlKey { get; set; } // SystemSetting'teki Key (hangi API'den veri alınacağını belirtir)

        public class UpdateAssetTypeCommandHandler : IRequestHandler<UpdateAssetTypeCommand, IResult>
        {
            private readonly IAssetTypeRepository _assetTypeRepository;
            private readonly IMediator _mediator;

            public UpdateAssetTypeCommandHandler(IAssetTypeRepository assetTypeRepository, IMediator mediator)
            {
                _assetTypeRepository = assetTypeRepository;
                _mediator = mediator;
            }

            [ValidationAspect(typeof(UpdateAssetTypeValidator), Priority = 1)]
            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(UpdateAssetTypeCommand request, CancellationToken cancellationToken)
            {
                var isThereAssetTypeRecord = await _assetTypeRepository.GetAsync(u => u.Id == request.Id);
                var userId = UserInfoExtensions.GetUserId();

                // Sadece Diger (99) olan varlık türleri güncellenebilir
                if (isThereAssetTypeRecord.ConvertedAmountType != AssetConvertType.Diger)
                {
                    return new ErrorResult("Bu varlık türü güncellenemez. Sadece 'Diğer' kategorisindeki varlık türleri güncellenebilir.");
                }

                // Sadece kullanıcının kendi tiplerini güncelleyebilir (UserId == 1)
                if (isThereAssetTypeRecord.UserId != userId)
                {
                    return new ErrorResult("Bu varlık türü güncellenemez. Sadece kendi eklediğiniz varlık türlerini güncelleyebilirsiniz.");
                }

                // UserId zaten 1, değiştirmeye gerek yok
                isThereAssetTypeRecord.Name = request.Name;
                isThereAssetTypeRecord.ConvertedAmountType = request.ConvertedAmountType;
                isThereAssetTypeRecord.TlValue = request.TlValue;
                isThereAssetTypeRecord.ApiUrlKey = request.ApiUrlKey;
                isThereAssetTypeRecord.IsActive = true;
                isThereAssetTypeRecord.UpdatedBy = UserInfoExtensions.GetUserId();
                isThereAssetTypeRecord.UpdatedDate = DateTime.Now;


                _assetTypeRepository.Update(isThereAssetTypeRecord);
                await _assetTypeRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Updated);
            }
        }
    }
}

