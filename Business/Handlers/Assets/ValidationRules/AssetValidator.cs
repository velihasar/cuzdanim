
using Business.Handlers.Assets.Commands;
using FluentValidation;

namespace Business.Handlers.Assets.ValidationRules
{

    public class CreateAssetValidator : AbstractValidator<CreateAssetCommand>
    {
        public CreateAssetValidator()
        {
            RuleFor(x => x.AssetTypeId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Piece).GreaterThanOrEqualTo(0);

        }
    }
    public class UpdateAssetValidator : AbstractValidator<UpdateAssetCommand>
    {
        public UpdateAssetValidator()
        {
            RuleFor(x => x.AssetTypeId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Piece).GreaterThanOrEqualTo(0);

        }
    }
}