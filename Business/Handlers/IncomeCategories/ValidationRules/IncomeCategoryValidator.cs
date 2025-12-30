
using Business.Handlers.IncomeCategories.Commands;
using FluentValidation;

namespace Business.Handlers.IncomeCategories.ValidationRules
{

    public class CreateIncomeCategoryValidator : AbstractValidator<CreateIncomeCategoryCommand>
    {
        public CreateIncomeCategoryValidator()
        {
            RuleFor(x => x.Name).NotEmpty();

        }
    }
    public class UpdateIncomeCategoryValidator : AbstractValidator<UpdateIncomeCategoryCommand>
    {
        public UpdateIncomeCategoryValidator()
        {
            RuleFor(x => x.Name).NotEmpty();

        }
    }
}