using FluentValidation;
using LangLearn.Backend.Models;

namespace LangLearn.Backend.Validators;

public abstract class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    protected RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}
