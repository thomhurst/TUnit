using System.Linq;
using TUnit.Assertions.AssertConditions.Comparable;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class BetweenAssertionBuilderWrapper<TActual> : AssertionBuilder<TActual> where TActual : IComparable<TActual>
{
    internal BetweenAssertionBuilderWrapper(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder.Actual, assertionBuilder.ActualExpression)
    {
        // Copy the assertion chain from the original builder
        foreach (var assertion in assertionBuilder.GetAssertions())
        {
            WithAssertion(assertion);
        }
    }

    public BetweenAssertionBuilderWrapper<TActual> WithInclusiveBounds()
    {
        var assertions = GetAssertions();
        var assertion = assertions.LastOrDefault() as BetweenAssertCondition<TActual>;
        
        if (assertion != null)
        {
            assertion.Inclusive();
            AppendExpression("WithInclusiveBounds()");
        }

        return this;
    }

    public BetweenAssertionBuilderWrapper<TActual> WithExclusiveBounds()
    {
        var assertions = GetAssertions();
        var assertion = assertions.LastOrDefault() as BetweenAssertCondition<TActual>;
        
        if (assertion != null)
        {
            assertion.Exclusive();
            AppendExpression("WithExclusiveBounds()");
        }

        return this;
    }
}
