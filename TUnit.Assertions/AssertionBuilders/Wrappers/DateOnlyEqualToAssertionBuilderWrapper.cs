#if NET

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class DateOnlyEqualToAssertionBuilderWrapper : InvokableValueAssertion<DateOnly>
{
    internal DateOnlyEqualToAssertionBuilderWrapper(InvokableAssertion<DateOnly> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public DateOnlyEqualToAssertionBuilderWrapper WithinDays(int days, [CallerArgumentExpression(nameof(days))] string doNotPopulateThis = "")
    {
        var assertion = (DateOnlyEqualsExpectedValueAssertCondition) Assertions.Peek();

        assertion.SetTolerance(days);
        
        AppendCallerMethod([doNotPopulateThis]);
        
        return this;
    }
}

#endif
