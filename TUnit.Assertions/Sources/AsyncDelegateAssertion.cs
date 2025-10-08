using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for asynchronous delegates (Func&lt;Task&gt;).
/// This is the entry point for: Assert.That(async () => await SomeMethodAsync())
/// Used primarily for exception checking.
/// Implements IDelegateAssertionSource to enable Throws() extension methods.
/// </summary>
public class AsyncDelegateAssertion : Assertion<object?>, IDelegateAssertionSource<object?>
{
    internal Func<Task> AsyncAction { get; }

    public AsyncDelegateAssertion(Func<Task> action, string? expression)
        : base(new EvaluationContext<object?>(async () =>
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
        }))
    {
        AsyncAction = action ?? throw new ArgumentNullException(nameof(action));
        ExpressionBuilder.Append($"Assert.That({expression ?? "?"})");
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<object?> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        // Source assertions don't perform checks
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to execute";
}
