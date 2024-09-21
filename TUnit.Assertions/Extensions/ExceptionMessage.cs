using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public class ExceptionMessage<TActual>
    where TActual : Exception
{
    private readonly IDelegateSource<TActual> _delegateSource;
    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }

    public ExceptionMessage(IDelegateSource<TActual> delegateSource)
    {
        _delegateSource = delegateSource;
        AssertionBuilder = delegateSource.AssertionBuilder.AppendExpression("Message");
    }

    public InvokableDelegateAssertionBuilder<TActual> EqualTo(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return EqualTo(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public InvokableDelegateAssertionBuilder<TActual> EqualTo(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return _delegateSource.RegisterAssertion(new DelegateAssertCondition<TActual, string>(expected, (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return string.Equals(actual.Message, expected, stringComparison);
            },
            (_, actual, _) =>
                $"Exception had a message of '{actual?.Message}' instead of '{expected}'")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
    }
}