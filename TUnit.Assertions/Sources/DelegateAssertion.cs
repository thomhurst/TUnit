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
    public AssertionContext<object?> Context { get; }
    internal Action Action { get; }

    public DelegateAssertion(Action action, string? expression)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        var evaluationContext = new EvaluationContext<object?>(async () =>
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
        Context = new AssertionContext<object?>(evaluationContext, expressionBuilder);
    }
}
