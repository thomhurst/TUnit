using TUnit.Assertions.AssertConditions.Comparable;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class BetweenAssertionBuilderWrapper<TActual> : InvokableValueAssertionBuilder<TActual> where TActual : IComparable<TActual>
{
    internal BetweenAssertionBuilderWrapper(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public BetweenAssertionBuilderWrapper<TActual> WithInclusiveBounds()
    {
        var assertion = (BetweenAssertCondition<TActual>) Assertions.Peek();

        assertion.Inclusive();
        
        AppendCallerMethod([]);
        
        return this;
    }
    
    public BetweenAssertionBuilderWrapper<TActual> WithExclusiveBounds()
    {
        var assertion = (BetweenAssertCondition<TActual>) Assertions.Peek();

        assertion.Exclusive();
        
        AppendCallerMethod([]);
        
        return this;
    }
}