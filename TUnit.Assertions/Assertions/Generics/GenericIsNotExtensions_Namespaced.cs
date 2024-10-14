#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;

// ReSharper disable once CheckNamespace
namespace TUnit.Assertions.Extensions.Generic;

public static class GenericIsNotExtensions
{
    /// <summary>
    /// This is in its own namespace to avoid ambiguous calls due to the same generic signature
    /// Alternatively, you can use `Assert.That(...).IsNotEquatableOrEqualTo(...)`
    /// </summary>
    public static InvokableValueAssertionBuilder<TActual> IsNotEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TActual>(expected)
            , [doNotPopulateThisValue]);
    }
}