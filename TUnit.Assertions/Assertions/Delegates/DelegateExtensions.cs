using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Assertions.Delegates;

public static class DelegateExtensions
{
    public static DelegateAssertionBuilder CompletesWithin(this IDelegateSource delegateSource, TimeSpan timeSpan, [CallerArgumentExpression("timeSpan")] string? doNotPopulateThisValue = null)
    {
        return delegateSource.RegisterAssertion(new CompleteWithinAssertCondition<object?>(timeSpan), [doNotPopulateThisValue]);
    }

    public static AssertionBuilder<TActual> CompletesWithin<TActual>(this IValueDelegateSource<TActual> delegateSource, TimeSpan timeSpan, [CallerArgumentExpression("timeSpan")] string? doNotPopulateThisValue = null)
    {
        IValueSource<TActual> valueSource = delegateSource;

        return valueSource.RegisterAssertion(new CompleteWithinAssertCondition<TActual>(timeSpan), [doNotPopulateThisValue]);
    }
}
