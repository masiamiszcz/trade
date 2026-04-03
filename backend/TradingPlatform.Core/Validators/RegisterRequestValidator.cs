using FluentValidation;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private const int UsernameMinLength = 8;
    private const int UsernameMaxLength = 50;
    private const int PasswordMinLength = 8;
    private const int PasswordMaxLength = 128;

    // Regex: At least 1 uppercase, 1 lowercase, 1 digit, 1 special character
    private const string PasswordPattern = @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";

    public RegisterRequestValidator()
    {
        // UserName validation
        RuleFor(x => x.UserName)
            .NotEmpty()
            .WithMessage("Nazwa użytkownika jest wymagana.")
            .Length(UsernameMinLength, UsernameMaxLength)
            .WithMessage($"Nazwa użytkownika musi mieć od {UsernameMinLength} do {UsernameMaxLength} znaków.")
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("Nazwa użytkownika może zawierać tylko litery, cyfry, myślnik i podkreślenie.");

        // Email validation
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email jest wymagany.")
            .EmailAddress()
            .WithMessage("Podaj prawidłowy adres email.");

        // Password validation
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Hasło jest wymagane.")
            .Length(PasswordMinLength, PasswordMaxLength)
            .WithMessage($"Hasło musi mieć od {PasswordMinLength} do {PasswordMaxLength} znaków.")
            .Matches(PasswordPattern)
            .WithMessage(
                "Hasło musi zawierać co najmniej jedną wielką literę, jedną małą literę, " +
                "jedną cyfrę i jeden znak specjalny (@$!%*?&).");

        // FirstName validation
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("Imię jest wymagane.")
            .MaximumLength(50)
            .WithMessage("Imię nie może być dłuższe niż 50 znaków.");

        // LastName validation
        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Nazwisko jest wymagane.")
            .MaximumLength(50)
            .WithMessage("Nazwisko nie może być dłuższe niż 50 znaków.");
    }
}
