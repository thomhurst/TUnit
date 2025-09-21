using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Assertions.Strings.Conditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class ParseAssertionBuilderWrapper<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] TTarget>
    : InvokableValueAssertionBuilder<string?>
{
    private readonly IValueSource<string?> _valueSource;
    private readonly bool _shouldBeParsable;
    private readonly string?[] _argumentExpressions;

    internal ParseAssertionBuilderWrapper(
        IValueSource<string?> valueSource,
        InvokableValueAssertionBuilder<string?> invokableAssertionBuilder,
        bool shouldBeParsable,
        string?[] argumentExpressions)
        : base(invokableAssertionBuilder)
    {
        _valueSource = valueSource;
        _shouldBeParsable = shouldBeParsable;
        _argumentExpressions = argumentExpressions;
    }

    public InvokableValueAssertionBuilder<string?> WithFormatProvider(IFormatProvider? formatProvider)
    {
        // Create a new assertion with the format provider
        BaseAssertCondition<string?> newCondition = _shouldBeParsable
            ? new StringIsParsableCondition<TTarget>(formatProvider)
            : new StringIsNotParsableCondition<TTarget>(formatProvider);

        // Re-register with the new condition
        return _valueSource.RegisterAssertion(newCondition, _argumentExpressions);
    }
}