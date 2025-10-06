using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Core;

/// <summary>
/// Represents an And continuation point in an assertion chain.
/// Returned by Assertion&lt;T&gt;.And property to enable fluent chaining.
/// Implements IAssertionSource so all extension methods work automatically!
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public class AndContinuation<TValue> : IAssertionSource<TValue>
{
    /// <summary>
    /// The evaluation context shared by all assertions in the chain.
    /// </summary>
    public EvaluationContext<TValue> Context { get; }

    /// <summary>
    /// The expression builder being mutated as the chain grows.
    /// </summary>
    public StringBuilder ExpressionBuilder { get; }

    internal AndContinuation(EvaluationContext<TValue> context, StringBuilder expressionBuilder)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        ExpressionBuilder = expressionBuilder ?? throw new ArgumentNullException(nameof(expressionBuilder));
        ExpressionBuilder.Append(".And");
    }
}
