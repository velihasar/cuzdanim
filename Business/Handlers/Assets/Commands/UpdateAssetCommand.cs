
using Business.BusinessAspects;
using Business.Constants;
using Business.Handlers.Assets.ValidationRules;
using Core.Aspects.Autofac.Caching;
using Core.Aspects.Autofac.Logging;
using Core.Aspects.Autofac.Validation;
using Core.CrossCuttingConcerns.Logging.Serilog.Loggers;
using Core.Extensions;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Core.Entities.Concrete;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Business.Handlers.Assets.Commands
{


    public class UpdateAssetCommand : IRequest<IResult>
    {
        public int Id { get; set; }
        public int AssetTypeId { get; set; }
        public string Name { get; set; }
        public int Piece { get; set; }
        public bool IsDebt { get; set; } = false;

        public class UpdateAssetCommandHandler : IRequestHandler<UpdateAssetCommand, IResult>
        {
            private readonly IAssetRepository _assetRepository;
            private readonly IMediator _mediator;

            public UpdateAssetCommandHandler(IAssetRepository assetRepository, IMediator mediator)
            {
                _assetRepository = assetRepository;
                _mediator = mediator;
            }

            [ValidationAspect(typeof(UpdateAssetValidator), Priority = 1)]
            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(UpdateAssetCommand request, CancellationToken cancellationToken)
            {
                var isThereAssetRecord = await _assetRepository.GetAsync(u => u.Id == request.Id);


                isThereAssetRecord.UserId = UserInfoExtensions.GetUserId();
                isThereAssetRecord.AssetTypeId = request.AssetTypeId;
                isThereAssetRecord.Name = request.Name;
                isThereAssetRecord.Piece = request.Piece;
                isThereAssetRecord.IsDebt = request.IsDebt;
                isThereAssetRecord.IsActive = true;
                isThereAssetRecord.UpdatedBy = UserInfoExtensions.GetUserId();
                isThereAssetRecord.UpdatedDate = DateTime.Now;



                _assetRepository.Update(isThereAssetRecord);
                await _assetRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Updated);
            }
        }
    }
}

