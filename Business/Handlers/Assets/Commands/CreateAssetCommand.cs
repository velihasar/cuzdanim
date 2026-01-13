
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
    /// <summary>
    /// 
    /// </summary>
    public class CreateAssetCommand : IRequest<IResult>
    {

        public int AssetTypeId { get; set; }
        public string Name { get; set; }
        public decimal Piece { get; set; }
        public bool IsDebt { get; set; } = false;


        public class CreateAssetCommandHandler : IRequestHandler<CreateAssetCommand, IResult>
        {
            private readonly IAssetRepository _assetRepository;
            private readonly IMediator _mediator;
            public CreateAssetCommandHandler(IAssetRepository assetRepository, IMediator mediator)
            {
                _assetRepository = assetRepository;
                _mediator = mediator;
            }

            [ValidationAspect(typeof(CreateAssetValidator), Priority = 1)]
            //[CacheRemoveAspect("Get")]
            //[LogAspect(typeof(FileLogger))]
            [SecuredOperation(Priority = 1)]
            public async Task<IResult> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
            {

                var addedAsset = new Asset
                {
                    UserId = UserInfoExtensions.GetUserId(),
                    AssetTypeId = request.AssetTypeId,
                    Name = request.Name,
                    Piece = request.Piece,
                    IsDebt = request.IsDebt,
                    CreatedBy = UserInfoExtensions.GetUserId(),
                    CreatedDate = DateTime.Now
                };

                _assetRepository.Add(addedAsset);
                await _assetRepository.SaveChangesAsync();
                return new SuccessResult(Messages.Added);
            }
        }
    }
}