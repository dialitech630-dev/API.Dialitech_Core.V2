using FluentValidation;

namespace API.Dialitech.Application.Commands.HealthData.CreateHealthData;

public class CreateHealthDataCommandValidator : AbstractValidator<CreateHealthDataCommand>
{
    public CreateHealthDataCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.HeartRate)
            .InclusiveBetween(30, 250).WithMessage("HeartRate must be between 30 and 250");

        RuleFor(x => x.SpO2)
            .InclusiveBetween(50.0, 100.0).WithMessage("SpO2 must be between 50 and 100");

        RuleFor(x => x.ActivityLevel)
            .InclusiveBetween(0, 100).WithMessage("ActivityLevel must be between 0 and 100");

        RuleFor(x => x.Timestamp)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Timestamp cannot be in the future");
    }
}
