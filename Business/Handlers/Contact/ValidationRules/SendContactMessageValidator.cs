using Business.Handlers.Contact.Commands;
using FluentValidation;

namespace Business.Handlers.Contact.ValidationRules
{
    public class SendContactMessageValidator : AbstractValidator<SendContactMessageCommand>
    {
        public SendContactMessageValidator()
        {
            RuleFor(x => x.Subject).NotEmpty().WithMessage("Konu gereklidir.");
            RuleFor(x => x.Message).NotEmpty().WithMessage("Mesaj gereklidir.");
        }
    }
}

