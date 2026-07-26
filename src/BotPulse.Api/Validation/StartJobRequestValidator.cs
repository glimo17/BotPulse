using FluentValidation;

namespace BotPulse.Api.Validation;

public sealed record StartJobApiRequest(
    string ProcessExternalId,
    string? RobotExternalId = null,
    Dictionary<string, object>? Parameters = null,
    string Priority = "Normal");

public sealed class StartJobApiRequestValidator : AbstractValidator<StartJobApiRequest>
{
    public StartJobApiRequestValidator()
    {
        RuleFor(x => x.ProcessExternalId).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Priority).Must(p => p is "Normal" or "High" or "Low")
            .WithMessage("Priority must be Normal, High, or Low");
    }
}
