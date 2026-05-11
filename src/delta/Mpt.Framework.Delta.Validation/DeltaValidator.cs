using FluentValidation;
using FluentValidation.Results;
using System.Linq.Expressions;

namespace Mpt.Framework.Delta.Validation;

public interface IDeltaValidator<T> : IValidator<Delta<T>>
{
}

public abstract class DeltaValidator<T> : AbstractValidator<Delta<T>>, IDeltaValidator<T>
{
    public IRuleBuilderInitial<Delta<T>, Delta<TProperty>> RuleForDelta<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        return RuleFor(t => t.GetDelta(expression));
    }

    public override ValidationResult Validate(ValidationContext<Delta<T>> context)
    {
        return AdjustErrorPropertyName(base.Validate(context));
    }

    public override async Task<ValidationResult> ValidateAsync(ValidationContext<Delta<T>> context, CancellationToken cancellation = default)
    {
        return AdjustErrorPropertyName(await base.ValidateAsync(context, cancellation));
    }

    private static ValidationResult AdjustErrorPropertyName(ValidationResult validationResult)
    {
        // Delta<T>.Path which is stored in PropertyName placeholder value, always contains correct full path of the property
        foreach (var item in validationResult.Errors)
        {
            if (item.CustomState != null)
            {
                continue;
            }

            item.PropertyName = item.FormattedMessagePlaceholderValues != null && item.FormattedMessagePlaceholderValues.TryGetValue(nameof(ValidationFailure.PropertyName), out var formatterMessage)
                ? (string)formatterMessage
                : item.PropertyName;
            item.CustomState = 1;
        }

        return validationResult;
    }
}
