using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for asynchronous delegates (Func&lt;Task&gt;).
/// This is the entry point for: Assert.That(async () => await SomeMethodAsync())
/// Used primarily for exception checking.
/// </summary>
public class AsyncDelegateAssertion : Assertion<object?>
{
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
        ExpressionBuilder.Append($"Assert.That({expression ?? "?"})");
    }

    protected override Task<AssertionResult> CheckAsync(object? value, Exception? exception)
    {
        // Source assertions don't perform checks
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to execute";
}
