using System.Runtime.CompilerServices;
using TUnit.Assertions.Assertions.Delegates;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class DelegateCompletionExtensions
{
    public static InvokableDelegateAssertionBuilder CompletesWithin(this IDelegateSource delegateSource, TimeSpan timeSpan, [CallerArgumentExpression("timeSpan")] string? doNotPopulateThisValue = null)
    {
        return delegateSource.RegisterAssertion(new CompleteWithinAssertCondition<object?>(timeSpan), [doNotPopulateThisValue]);
    }

    public static InvokableValueAssertionBuilder<TActual> CompletesWithin<TActual>(this IValueDelegateSource<TActual> delegateSource, TimeSpan timeSpan, [CallerArgumentExpression("timeSpan")] string? doNotPopulateThisValue = null)
    {
        IValueSource<TActual> valueSource = delegateSource;

        return valueSource.RegisterAssertion(new CompleteWithinAssertCondition<TActual>(timeSpan), [doNotPopulateThisValue]);
    }
}
