using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for synchronous delegates (Action).
/// This is the entry point for: Assert.That(() => SomeMethod())
/// Used primarily for exception checking.
/// Implements IDelegateAssertionSource to enable Throws() extension methods.
/// </summary>
public class DelegateAssertion : Assertion<object?>, IDelegateAssertionSource<object?>
{
    internal Action Action { get; }

    public DelegateAssertion(Action action, string? expression)
        : base(new EvaluationContext<object?>(async () =>
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
        }))
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        ExpressionBuilder.Append($"Assert.That({expression ?? "?"})");
    }

    protected override Task<AssertionResult> CheckAsync(object? value, Exception? exception)
    {
        // Source assertions don't perform checks
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to execute";
}
