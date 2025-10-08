using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for synchronous delegates (Action).
/// This is the entry point for: Assert.That(() => SomeMethod())
/// Used primarily for exception checking.
/// Implements IDelegateAssertionSource to enable Throws() extension methods.
/// Does not inherit from Assertion to prevent premature awaiting.
/// </summary>
public class DelegateAssertion : IAssertionSource<object?>, IDelegateAssertionSource<object?>
{
    public EvaluationContext<object?> Context { get; }
    public StringBuilder ExpressionBuilder { get; }
    internal Action Action { get; }

    public DelegateAssertion(Action action, string? expression)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Context = new EvaluationContext<object?>(async () =>
        {
            try
            {
                await Task.Run(action);
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
