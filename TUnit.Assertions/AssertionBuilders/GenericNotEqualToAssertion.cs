using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Assertions.Base;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.AssertionBuilders.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Clean generic not-equal assertion - no inheritance, just configuration
/// </summary>
public class GenericNotEqualToAssertion<TActual> : Assertion<TActual>
{
    private readonly TActual _expected;
    private readonly string?[] _expressions;

    // Configuration
#if NET
    private Func<TActual?, TActual?, AssertionDecision>? _customComparer;
#else
    private readonly Func<TActual?, TActual?, AssertionDecision>? _customComparer = null;
#endif

    internal GenericNotEqualToAssertion(IValueSource<TActual> source, TActual expected, string?[] expressions, IAssertionChain chain = null!)
        : base(source, chain)
    {
        _expected = expected;
        _expressions = expressions;
    }

#if NET
    public GenericNotEqualToAssertion<TActual> Within<T>(T tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
        where T : IComparable<T>
    {
        if (typeof(TActual) == typeof(T))
        {
            _customComparer = (actual, expected) =>
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
            };
        }

        return this;
    }
#endif

    protected override BaseAssertCondition? CreateCondition()
    {
        // Create condition with all configuration
        var condition = new NotEqualsExpectedValueAssertCondition<TActual>(_expected);

        // Apply configuration
        if (_customComparer != null)
            condition.WithComparer(_customComparer);

        return condition;
    }
}
