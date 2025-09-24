#if NET

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class DateOnlyEqualToAssertionBuilderWrapper : AssertionBuilder<DateOnly>
{
    internal DateOnlyEqualToAssertionBuilderWrapper(AssertionBuilder<DateOnly> assertionBuilder) : base(assertionBuilder.Actual, assertionBuilder.ActualExpression)
    {
        // Copy the assertion chain from the original builder
        foreach (var assertion in assertionBuilder.GetAssertions())
        {
            WithAssertion(assertion);
        }
    }

    public DateOnlyEqualToAssertionBuilderWrapper WithinDays(int days, [CallerArgumentExpression(nameof(days))] string doNotPopulateThis = "")
    {
        var assertion = (DateOnlyEqualsExpectedValueAssertCondition) base.Assertions.Peek();

        assertion.SetTolerance(days);
        
        AppendCallerMethod([doNotPopulateThis]);
        
        return this;
    }
}

#endif
