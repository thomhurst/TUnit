using System.Text;

namespace TUnit.Assertions.Core;

/// <summary>
/// Common interface for all assertion sources (assertions and continuations).
/// Extension methods target this interface, eliminating duplication.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public interface IAssertionSource<TValue>
{
    /// <summary>
    /// The evaluation context shared by all assertions in this chain.
    /// </summary>
    EvaluationContext<TValue> Context { get; }

    /// <summary>
    /// The expression builder for constructing error messages.
    /// </summary>
    StringBuilder ExpressionBuilder { get; }
}
