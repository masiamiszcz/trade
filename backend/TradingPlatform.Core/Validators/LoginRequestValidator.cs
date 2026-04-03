using FluentValidation;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserNameOrEmail)
            .NotEmpty()
            .WithMessage("Nazwa użytkownika lub email jest wymagany.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Hasło jest wymagane.");
    }
}
