using System;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for generic not-equal comparisons
/// </summary>
public class GenericNotEqualToAssertion<TActual> : FluentAssertionBase<TActual, GenericNotEqualToAssertion<TActual>>
{
    internal GenericNotEqualToAssertion(AssertionBuilder<TActual> assertionBuilder)
        : base(assertionBuilder)
    {
    }

#if NET
    public GenericNotEqualToAssertion<TActual> Within<T>(T tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
        where T : IComparable<T>
    {
        var assertion = GetLastAssertionAs<NotEqualsExpectedValueAssertCondition<TActual>>();
        if (assertion is null)
        {
            throw new InvalidOperationException($"Expected last assertion to be NotEqualsExpectedValueAssertCondition<{typeof(TActual).Name}>");
        }

        if (typeof(TActual) == typeof(T))
        {
            assertion.WithComparer((actual, expected) =>
            {
                dynamic actualDynamic = actual!;
                dynamic expectedDynamic = expected!;
                dynamic toleranceDynamic = tolerance;

                // Not equal means outside the tolerance range
                if (actualDynamic < expectedDynamic - toleranceDynamic || actualDynamic > expectedDynamic + toleranceDynamic)
                {
                    return AssertionDecision.Pass;
                }

                return AssertionDecision.Fail($"Expected {actual} to not be equal to {expected} ±{tolerance}.");
            });
        }

        AppendCallerMethod([doNotPopulateThis]);

        return Self;
    }
#endif
}