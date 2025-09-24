using System.Collections.Generic;
using System.Linq;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Base class for fluent assertion builders that eliminates wrapper class explosion
/// </summary>
/// <typeparam name="TActual">The type being asserted</typeparam>
/// <typeparam name="TSelf">The derived type for fluent return</typeparam>
public abstract class FluentAssertionBase<TActual, TSelf> : AssertionBuilder<TActual> 
    where TSelf : FluentAssertionBase<TActual, TSelf>
{
    protected FluentAssertionBase(AssertionBuilder<TActual> assertionBuilder) 
        : base(assertionBuilder.Actual, assertionBuilder.ActualExpression)
    {
        // Copy the assertion chain from the original builder
        foreach (var assertion in assertionBuilder.GetAssertions())
        {
            WithAssertion(assertion);
        }
    }
    
    // Helper to return the correct type for fluent chaining
    protected TSelf Self => (TSelf)this;
    
    // Helper to get the last assertion as a specific type
    protected TAssertion? GetLastAssertionAs<TAssertion>() where TAssertion : class
    {
        var assertions = GetAssertions();
        return assertions.LastOrDefault() as TAssertion;
    }
}