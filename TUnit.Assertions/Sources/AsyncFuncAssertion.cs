using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for asynchronous functions.
/// This is the entry point for: Assert.That(async () => await GetValueAsync())
/// </summary>
public class AsyncFuncAssertion<TValue> : Assertion<TValue>
{
    public AsyncFuncAssertion(Func<Task<TValue>> func, string? expression)
        : base(new EvaluationContext<TValue>(async () =>
        {
            try
            {
                var result = await func();
                return (result, null);
            }
            catch (Exception ex)
            {
                return (default(TValue), ex);
            }
        }))
    {
        ExpressionBuilder.Append($"Assert.That({expression ?? "?"})");
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        // Source assertions don't perform checks
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to evaluate successfully";
}
