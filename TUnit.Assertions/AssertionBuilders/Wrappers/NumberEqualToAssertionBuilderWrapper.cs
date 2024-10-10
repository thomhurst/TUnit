using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Numbers;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class NumberEqualToAssertionBuilderWrapper<TActual> : InvokableValueAssertionBuilder<TActual> where TActual : INumber<TActual>
{
    internal NumberEqualToAssertionBuilderWrapper(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public NumberEqualToAssertionBuilderWrapper<TActual> Within(TActual tolerance, [CallerArgumentExpression("tolerance")] string doNotPopulateThis = "")
    {
        var assertion = (NumericEqualsExpectedValueAssertCondition<TActual>) Assertions.Peek();

        assertion.SetTolerance(tolerance);
        
        AppendCallerMethod([doNotPopulateThis]);
        
        return this;
    }
}