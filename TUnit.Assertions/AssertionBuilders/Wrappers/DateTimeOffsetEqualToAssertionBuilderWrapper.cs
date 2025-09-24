using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Chronology;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class DateTimeOffsetEqualToAssertionBuilderWrapper : AssertionBuilderWrapperBase<DateTimeOffset>
{
    internal DateTimeOffsetEqualToAssertionBuilderWrapper(AssertionBuilder<DateTimeOffset> invokableAssertionBuilder)
        : base(invokableAssertionBuilder)
    {
    }

    public DateTimeOffsetEqualToAssertionBuilderWrapper Within(TimeSpan tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<DateTimeOffsetEqualsExpectedValueAssertCondition>();

        assertion.SetTolerance(tolerance);

        AppendCallerMethod([doNotPopulateThis]);

        return this;
    }
}
