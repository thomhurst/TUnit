using TUnit.Assertions.Core;

namespace TUnit.Assertions.Core;

/// <summary>
/// Represents an Or continuation point in an assertion chain.
/// Returned by Assertion&lt;T&gt;.Or property to enable fluent chaining.
/// Implements IAssertionSource so all extension methods work automatically!
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public class OrContinuation<TValue> : IAssertionSource<TValue>
{
    /// <summary>
    /// The assertion context shared by all assertions in the chain.
    /// </summary>
    public AssertionContext<TValue> Context { get; }

    internal OrContinuation(AssertionContext<TValue> context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Context.ExpressionBuilder.Append(".Or");
    }
}
