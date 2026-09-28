using FluentValidation;

namespace TaskFlow.Application.Tasks.Commands;

public class ChangeTaskStatusCommandValidator : AbstractValidator<ChangeTaskStatusCommand>
{
    public ChangeTaskStatusCommandValidator()
    {
        RuleFor(x => x.NewStatus).IsInEnum();
    }
}
