using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for not-between comparisons
/// </summary>
public class NotBetweenAssertion<TActual> : FluentAssertionBase<TActual, NotBetweenAssertion<TActual>>
{
    internal NotBetweenAssertion(AssertionBuilder<TActual> assertionBuilder)
        : base(assertionBuilder)
    {
    }

    public NotBetweenAssertion<TActual> Inclusive()
    {
        var assertion = GetLastAssertionAs<IBetweenCondition>();
        assertion?.Inclusive();
        
        AppendCallerMethod([]);
        return this;
    }
    
    public NotBetweenAssertion<TActual> Exclusive()
    {
        var assertion = GetLastAssertionAs<IBetweenCondition>();
        assertion?.Exclusive();
        
        AppendCallerMethod([]);
        return this;
    }
}