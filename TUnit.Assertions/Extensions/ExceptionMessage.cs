using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public class ExceptionMessage<TActual>
    where TActual : Exception
{
    private readonly IValueSource<TActual> _valueSource;
    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }

    public ExceptionMessage(IValueSource<TActual> valueSource)
    {
        _valueSource = valueSource;
        AssertionBuilder = valueSource.AssertionBuilder.AppendExpression("HasMessage");
    }

    public InvokableValueAssertionBuilder<TActual> EqualTo(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return EqualTo(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public InvokableValueAssertionBuilder<TActual> EqualTo(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return _valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, string>(expected, (actual, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return string.Equals(actual.Message, expected, stringComparison);
            },
            (_, actual, _) =>
                $"Exception had a message of '{actual?.Message}' instead of '{expected}'")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
}