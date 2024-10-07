using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

// ReSharper disable once CheckNamespace
// This can't go in the main namespace as its generic signature clashes
// with more precise versions such as INumber<TActual> :(
namespace TUnit.Assertions.Extensions.Generic;

public static class GenericIsExtensions
{
    /// <summary>
    /// This is in its own namespace to avoid ambiguous calls due to the same generic signature
    /// Alternatively, you can use `Assert.That(...).IsEquatableOrEqualTo(...)`
    /// </summary>
    public static InvokableValueAssertionBuilder<TActual> IsEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
    {
        return valueSource
            .RegisterAssertion(new EqualsExpectedValueAssertCondition<TActual>(expected), [doNotPopulateThisValue1]);
    }
}