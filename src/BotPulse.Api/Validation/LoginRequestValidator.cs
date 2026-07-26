using BotPulse.Api.Controllers.V1;
using FluentValidation;

namespace BotPulse.Api.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(1024);
    }
}
