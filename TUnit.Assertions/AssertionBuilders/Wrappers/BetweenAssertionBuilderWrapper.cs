using System.Linq;
using TUnit.Assertions.AssertConditions.Comparable;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class BetweenAssertionBuilderWrapper<TActual> : AssertionBuilderWrapperBase<TActual> where TActual : IComparable<TActual>
{
    internal BetweenAssertionBuilderWrapper(AssertionBuilder<TActual> assertionBuilder) : base(assertionBuilder)
    {

    public BetweenAssertionBuilderWrapper<TActual> WithInclusiveBounds()
    {
        var assertion = GetLastAssertionAs<BetweenAssertCondition<TActual>>();
        assertion.Inclusive();
        AppendCallerMethod([]);
        return this;
    }

    public BetweenAssertionBuilderWrapper<TActual> WithExclusiveBounds()
    {
        var assertion = GetLastAssertionAs<BetweenAssertCondition<TActual>>();
        assertion.Exclusive();
        AppendCallerMethod([]);
        return this;
    }
}
