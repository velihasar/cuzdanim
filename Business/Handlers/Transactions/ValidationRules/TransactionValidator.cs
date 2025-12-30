
using Business.Handlers.Transactions.Commands;
using Business.Constants;
using FluentValidation;
using System;

namespace Business.Handlers.Transactions.ValidationRules
{

    public class CreateTransactionValidator : AbstractValidator<CreateTransactionCommand>
    {
        public CreateTransactionValidator()
        {
            RuleFor(x => x.Amount).NotEmpty();
            RuleFor(x => x.Date).NotEmpty();
            RuleFor(x => x.Type).NotEmpty();
            RuleFor(x => x.IsMonthlyRecurring).NotNull();
            
            RuleFor(x => x.Date)
                .Must(date => date >= DateTime.Now.AddYears(-1).Date)
                .WithMessage(Messages.TransactionDateTooOld);

        }
    }
    public class UpdateTransactionValidator : AbstractValidator<UpdateTransactionCommand>
    {
        public UpdateTransactionValidator()
        {
            RuleFor(x => x.Amount).NotEmpty();
            RuleFor(x => x.Date).NotEmpty();
            RuleFor(x => x.Type).NotEmpty();
            RuleFor(x => x.IsMonthlyRecurring).NotNull();
            
            RuleFor(x => x.Date)
                .Must(date => date >= DateTime.Now.AddYears(-1).Date)
                .WithMessage(Messages.TransactionDateTooOld);

        }
    }
}