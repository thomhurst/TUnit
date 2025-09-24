using TUnit.Assertions.AssertConditions.Comparable;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class NotBetweenAssertionBuilderWrapper<TActual> : AssertionBuilderWrapperBase<TActual> where TActual : IComparable<TActual>
{
    internal NotBetweenAssertionBuilderWrapper(AssertionBuilder<TActual> invokableAssertionBuilder)
        : base(invokableAssertionBuilder)
    {
    }

    public NotBetweenAssertionBuilderWrapper<TActual> WithInclusiveBounds()
    {
        var assertion = GetLastAssertionAs<NotBetweenAssertCondition<TActual>>();

        assertion.Inclusive();

        AppendCallerMethod([]);

        return this;
    }

    public NotBetweenAssertionBuilderWrapper<TActual> WithExclusiveBounds()
    {
        var assertion = GetLastAssertionAs<NotBetweenAssertCondition<TActual>>();

        assertion.Exclusive();

        AppendCallerMethod([]);

        return this;
    }
}
