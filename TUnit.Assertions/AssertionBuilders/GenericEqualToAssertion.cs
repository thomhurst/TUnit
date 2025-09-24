using System;
using System.Linq;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for generic equality comparisons
/// </summary>
public class GenericEqualToAssertion<TActual> : FluentAssertionBase<TActual, GenericEqualToAssertion<TActual>>
{
    internal GenericEqualToAssertion(AssertionBuilder<TActual> assertionBuilder) 
        : base(assertionBuilder)
    {
    }

#if NET
    public GenericEqualToAssertion<TActual> Within<T>(T tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
        where T : IComparable<T>
    {
        var assertion = GetLastAssertion() as EqualsExpectedValueAssertCondition<TActual>;
        if (assertion is null)
        {
            throw new InvalidOperationException($"Expected last assertion to be EqualsExpectedValueAssertCondition<{typeof(TActual).Name}>");
        }

        if (typeof(TActual) == typeof(T))
        {
            assertion.WithComparer((actual, expected) =>
            {
                dynamic actualDynamic = actual!;
                dynamic expectedDynamic = expected!;
                dynamic toleranceDynamic = tolerance;
                
                if (actualDynamic >= expectedDynamic - toleranceDynamic && actualDynamic <= expectedDynamic + toleranceDynamic)
                {
                    return AssertionDecision.Pass;
                }
                
                return AssertionDecision.Fail($"Expected {actual} to be equal to {expected} ±{tolerance}.");
            });
        }
        
        AppendCallerMethod([doNotPopulateThis]);
        
        return Self;
    }
#endif
}