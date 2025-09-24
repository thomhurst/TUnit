#if NET

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class DateOnlyEqualToAssertionBuilderWrapper : AssertionBuilderWrapperBase<DateOnly>
{
    internal DateOnlyEqualToAssertionBuilderWrapper(AssertionBuilder<DateOnly> assertionBuilder)
        : base(assertionBuilder)
    {
    }

    public DateOnlyEqualToAssertionBuilderWrapper WithinDays(int days, [CallerArgumentExpression(nameof(days))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<DateOnlyEqualsExpectedValueAssertCondition>();

        assertion.SetTolerance(days);
        
        AppendCallerMethod([doNotPopulateThis]);
        
        return this;
    }
}

#endif
