using TUnit.Assertions.AssertConditions.Comparable;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class NotBetweenAssertionBuilderWrapper<TActual> : AssertionBuilder<TActual> where TActual : IComparable<TActual>
{
    internal NotBetweenAssertionBuilderWrapper(AssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public NotBetweenAssertionBuilderWrapper<TActual> WithInclusiveBounds()
    {
        var assertion = (NotBetweenAssertCondition<TActual>) base.Assertions.Peek();

        assertion.Inclusive();

        AppendCallerMethod([]);

        return this;
    }

    public NotBetweenAssertionBuilderWrapper<TActual> WithExclusiveBounds()
    {
        var assertion = (NotBetweenAssertCondition<TActual>) base.Assertions.Peek();

        assertion.Exclusive();

        AppendCallerMethod([]);

        return this;
    }
}
