using FluentValidation;
using FluentValidation.Validators;
using System.Linq.Expressions;

namespace Mpt.Framework.Delta.Validation;

public static class DeltaValidatorExtensions
{

    public static IRuleBuilderOptions<Delta<T>, Delta<TElement>> MustBeDefined<T, TElement>(this IRuleBuilderInitial<Delta<T>, Delta<TElement>> ruleBuilder)
       => ruleBuilder.MustBeDefined(null);

    public static IRuleBuilderOptions<Delta<T>, Delta<TElement>> MustBeDefined<T, TElement>(this IRuleBuilderInitial<Delta<T>, Delta<TElement>> ruleBuilder, Action<IRuleBuilder<Delta<TElement>, TElement>>? valueRule)
    {
        var options = ruleBuilder.ChildRules(c =>
        {
            c.RuleFor(t => t).Cascade(CascadeMode.Stop).Must(delta => delta.IsDefined).WithName(t => t.Path).WithMessage("Property must be provided");
        });

        if (valueRule != null)
        {
            options.WhenDefined(valueRule);
        }

        return options;
    }

    public static void MustBeOmitted<T, TElement>(this IRuleBuilderInitial<Delta<T>, Delta<TElement>> ruleBuilder)
    {
        ruleBuilder.ChildRules(c =>
        {
            c.RuleFor(t => t).Must(delta => !delta.IsDefined).WithName(t => t.Path).WithMessage("Property must be omitted");
        });
    }

    public static IRuleBuilderOptions<Delta<T>, Delta<TElement>> WhenDefined<T, TElement>(this IRuleBuilderInitial<Delta<T>, Delta<TElement>> ruleBuilder)
    {
        return ruleBuilder.WhenDefined(null!);
    }

    public static IRuleBuilderOptions<Delta<T>, Delta<TElement>> WhenDefined<T, TElement>(this IRuleBuilder<Delta<T>, Delta<TElement>> ruleBuilder)
    {
        return ruleBuilder.WhenDefined(null!);
    }

    public static IRuleBuilderOptions<Delta<T>, Delta<TElement>> WhenDefined<T, TElement>(this IRuleBuilder<Delta<T>, Delta<TElement>> ruleBuilder, Action<IRuleBuilder<Delta<TElement>, TElement>> valueRule)
    {
        return ruleBuilder.ChildRules(a =>
        {
            var rb = a.RuleFor<TElement>(t => t);
            valueRule?.Invoke(rb);
            rb.SetValidator(new EmptyValidator<TElement>()!).When(t => t.IsDefined).WithName(t => t.Path);
        });
    }

    public static IRuleBuilder<Delta<T>, Delta<TElement>> Path<T, TElement, TElementSub>(this IRuleBuilder<Delta<T>, Delta<TElement>> options, Expression<Func<TElement, TElementSub>> expression, Action<IRuleBuilderInitial<Delta<TElement>, Delta<TElementSub>>> valueRule)
        where TElement : class
    {
        options.ChildRules(a =>
        {
            var rb = a.RuleFor(t => t.GetDelta(expression));
            valueRule?.Invoke(rb);
        });

        return options;
    }

    public static void ForEachDelta<T, TElement>(this IRuleBuilderOptions<Delta<T>, IDelta<IEnumerable<TElement>>> options, IDeltaValidator<TElement> validator)
        where TElement : class
    {
        options.ChildRules(a => a.RuleForEach(t => t.Split()).SetValidator(validator).WithName(t => t.Path));
    }

    public static void ForEachDelta<T, TElement>(this IRuleBuilderOptions<Delta<T>, IDelta<IEnumerable<TElement>>> options, Action<InlineDeltaValidator<TElement>> configure)
        where TElement : class
    {
        var validator = new InlineDeltaValidator<TElement>();
        configure(validator);
        options.ChildRules(a => a.RuleForEach(t => t.Split()).SetValidator(validator).WithName(t => t.Path));
    }

    private sealed class EmptyValidator<TProp> : IPropertyValidator<Delta<TProp>, TProp>
    {
        public string Name => "Empty";

        public string GetDefaultMessageTemplate(string errorCode) => string.Empty;

        public bool IsValid(ValidationContext<Delta<TProp>> context, TProp value) => true;
    }

    public sealed class InlineDeltaValidator<TElement> : DeltaValidator<TElement>
    {
    }
}
