using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertionBuilders;

public class InvokableDelegateAssertionBuilder<TActual> : InvokableAssertionBuilder<TActual>, IDelegateSource<TActual>
{
    internal InvokableDelegateAssertionBuilder(Func<Task<AssertionData<TActual>>> assertionDataDelegate, AssertionBuilder<TActual> assertionBuilder) : base(assertionDataDelegate, assertionBuilder)
    {
    }

    public AssertionBuilder<TActual> AssertionBuilder => this;
    
    public DelegateAnd<TActual> And => new(AssertionBuilder);
    public DelegateOr<TActual> Or => new(AssertionBuilder);
}