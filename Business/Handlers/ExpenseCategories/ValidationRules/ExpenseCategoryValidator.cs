
using Business.Handlers.ExpenseCategories.Commands;
using FluentValidation;

namespace Business.Handlers.ExpenseCategories.ValidationRules
{

    public class CreateExpenseCategoryValidator : AbstractValidator<CreateExpenseCategoryCommand>
    {
        public CreateExpenseCategoryValidator()
        {
            RuleFor(x => x.Name).NotEmpty();

        }
    }
    public class UpdateExpenseCategoryValidator : AbstractValidator<UpdateExpenseCategoryCommand>
    {
        public UpdateExpenseCategoryValidator()
        {
            RuleFor(x => x.Name).NotEmpty();

        }
    }
}