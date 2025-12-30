
using Business.BusinessAspects;
using Business.Constants;
using Business.Handlers.AssetTypes.ValidationRules;
using Core.Aspects.Autofac.Validation;
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

namespace Business.Handlers.AssetTypes.Commands
{
    /// <summary>
    /// 
    /// </summary>
    public class CreateAssetTypeCommand : IRequest<Core.Utilities.Results.IResult>
    {

        public string Name { get; set; }
        public AssetConvertType ConvertedAmountType { get; set; }
        public decimal TlValue { get; set; }
        public string ApiUrlKey { get; set; } // SystemSetting'teki Key (hangi API'den veri alınacağını belirtir)


        public class CreateAssetTypeCommandHandler : IRequestHandler<CreateAssetTypeCommand, Core.Utilities.Results.IResult>
        {
            private readonly IAssetTypeRepository _assetTypeRepository;
            private readonly IMediator _mediator;

            public CreateAssetTypeCommandHandler(IAssetTypeRepository assetTypeRepository, IMediator mediator)
            {
                _assetTypeRepository = assetTypeRepository;
                _mediator = mediator;
            }

            [ValidationAspect(typeof(CreateAssetTypeValidator), Priority = 1)]
            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<Core.Utilities.Results.IResult> Handle(CreateAssetTypeCommand request, CancellationToken cancellationToken)
            {
                var userId = UserInfoExtensions.GetUserId();
                
                // Admin kullanıcısı (UserId == 1) için limit kontrolü yapma
                if (userId != 1)
                {
                    var result = await _assetTypeRepository.GetListAsync(u => u.UserId == userId);
                    if (result.Count() >= 5)
                    {
                        return new ErrorResult(Messages.RecordLimitExceeded);
                    }
                }

                var addedAssetType = new AssetType
                {
                    UserId = userId,
                    Name = request.Name,
                    ConvertedAmountType = request.ConvertedAmountType,
                    TlValue = request.TlValue,
                    ApiUrlKey = request.ApiUrlKey,
                    CreatedBy = userId,
                    CreatedDate = DateTime.Now
                };

                _assetTypeRepository.Add(addedAssetType);
                await _assetTypeRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Added);
            }
        }
    }
}