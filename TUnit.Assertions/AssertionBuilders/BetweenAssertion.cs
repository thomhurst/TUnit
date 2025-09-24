using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for between comparisons
/// </summary>
public class BetweenAssertion<TActual> : FluentAssertionBase<TActual, BetweenAssertion<TActual>>
{
    internal BetweenAssertion(AssertionBuilder<TActual> assertionBuilder)
        : base(assertionBuilder)
    {
    }

    public BetweenAssertion<TActual> Inclusive()
    {
        var assertion = GetLastAssertionAs<IBetweenCondition>();
        assertion?.Inclusive();
        
        AppendCallerMethod([]);
        return this;
    }
    
    public BetweenAssertion<TActual> Exclusive()
    {
        var assertion = GetLastAssertionAs<IBetweenCondition>();
        assertion?.Exclusive();
        
        AppendCallerMethod([]);
        return this;
    }
}