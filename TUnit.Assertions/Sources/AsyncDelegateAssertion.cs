using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for asynchronous delegates (Func&lt;Task&gt;).
/// This is the entry point for: Assert.That(async () => await SomeMethodAsync())
/// Used primarily for exception checking.
/// Implements IDelegateAssertionSource to enable Throws() extension methods.
/// Does not inherit from Assertion to prevent premature awaiting.
/// </summary>
public class AsyncDelegateAssertion : IAssertionSource<object?>, IDelegateAssertionSource<object?>
{
    public EvaluationContext<object?> Context { get; }
    public StringBuilder ExpressionBuilder { get; }
    internal Func<Task> AsyncAction { get; }

    public AsyncDelegateAssertion(Func<Task> action, string? expression)
    {
        AsyncAction = action ?? throw new ArgumentNullException(nameof(action));
        Context = new EvaluationContext<object?>(async () =>
        {
            try
            {
                await action();
                return (null, null);
            }
            catch (Exception ex)
            {
                return (null, ex);
            }
        });
        ExpressionBuilder = new StringBuilder();
        ExpressionBuilder.Append($"Assert.That({expression ?? "?"})");
    }
}
