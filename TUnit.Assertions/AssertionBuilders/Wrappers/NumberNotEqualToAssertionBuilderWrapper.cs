using System.Numerics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Numbers;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class NumberNotEqualToAssertionBuilderWrapper<TActual> : InvokableValueAssertionBuilder<TActual> where TActual : INumber<TActual>
{
    internal NumberNotEqualToAssertionBuilderWrapper(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public NumberNotEqualToAssertionBuilderWrapper<TActual> Within(TActual tolerance, [CallerArgumentExpression("tolerance")] string doNotPopulateThis = "")
    {
        var assertion = (NumericNotEqualExpectedValueAssertCondition<TActual>) Assertions.Peek();

        assertion.SetTolerance(tolerance);

        AppendCallerMethod([doNotPopulateThis]);
        
        return this;
    }
}