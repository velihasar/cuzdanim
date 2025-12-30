
using Business.Handlers.AssetTypes.Commands;
using FluentValidation;

namespace Business.Handlers.AssetTypes.ValidationRules
{

    public class CreateAssetTypeValidator : AbstractValidator<CreateAssetTypeCommand>
    {
        public CreateAssetTypeValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.TlValue).GreaterThanOrEqualTo(0);

        }
    }
    public class UpdateAssetTypeValidator : AbstractValidator<UpdateAssetTypeCommand>
    {
        public UpdateAssetTypeValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.TlValue).GreaterThanOrEqualTo(0);

        }
    }
}