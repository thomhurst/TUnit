using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Assertions.Base;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.AssertionBuilders.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Clean generic equality assertion - no inheritance, just configuration
/// </summary>
public class GenericEqualToAssertion<TActual> : Assertion<TActual>
{
    private readonly TActual _expected;
    private readonly IEqualityComparer<TActual> _comparer;
    
    // Configuration
#if NET
    private Func<TActual?, TActual?, AssertionDecision>? _customComparer;
#else
    private readonly Func<TActual?, TActual?, AssertionDecision>? _customComparer = null;
#endif

    internal GenericEqualToAssertion(IValueSource<TActual> source, TActual expected, IEqualityComparer<TActual> comparer, IAssertionChain chain = null!)
        : base(source, chain)
    {
        _expected = expected;
        _comparer = comparer;
    }

#if NET
    public GenericEqualToAssertion<TActual> Within<T>(T tolerance, [CallerArgumentExpression(nameof(tolerance))] string doNotPopulateThis = "")
        where T : IComparable<T>
    {
        if (typeof(TActual) == typeof(T))
        {
            _customComparer = (actual, expected) =>
            {
                dynamic actualDynamic = actual!;
                dynamic expectedDynamic = expected!;
                dynamic toleranceDynamic = tolerance;
                
                if (actualDynamic >= expectedDynamic - toleranceDynamic && actualDynamic <= expectedDynamic + toleranceDynamic)
                {
                    return AssertionDecision.Pass;
                }
                
                return AssertionDecision.Fail($"Expected {actual} to be equal to {expected} ±{tolerance}.");
            };
        }
        
        return this;
    }
#endif

    protected override BaseAssertCondition? CreateCondition()
    {
        var condition = new EqualsExpectedValueAssertCondition<TActual>(_expected, _comparer);
        
        if (_customComparer != null)
            condition.WithComparer(_customComparer);
        
        return condition;
    }
}
