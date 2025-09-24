using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

// Non-generic base class for compatibility with existing code
// Non-generic base class for compatibility with existing code
public abstract class AssertionBuilder : IInvokableAssertionBuilder
{
    public virtual Stack<BaseAssertCondition> Assertions => new Stack<BaseAssertCondition>();
    public abstract TaskAwaiter GetAwaiter();
    
    public abstract ValueTask<AssertionData> GetAssertionData();
    public abstract ValueTask ProcessAssertionsAsync(AssertionData data);
    
    // Methods needed by wrapper classes
    public virtual IEnumerable<BaseAssertCondition> GetAssertions()
    {
        return Enumerable.Empty<BaseAssertCondition>();
    }
    
    public virtual void WithAssertion(BaseAssertCondition assertion)
    {
        // Default implementation - overridden in derived classes
    }
    
    public virtual string? ActualExpression { get; protected set; }
    
    public virtual void AppendExpression(string expression)
    {
        // Default implementation - overridden in derived classes
    }
    
    
    public virtual void SetBecause(string reason, string? expression)
    {
        // Default implementation - overridden in derived classes
    }


    [Obsolete("This is a base `object` method that should not be called.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerHidden]
    public new void Equals(object? obj)
    {
        throw new InvalidOperationException("This is a base `object` method that should not be called.");
    }

    [Obsolete("This is a base `object` method that should not be called.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DebuggerHidden]
    public new void ReferenceEquals(object a, object b)
    {
        throw new InvalidOperationException("This is a base `object` method that should not be called.");
    }
}