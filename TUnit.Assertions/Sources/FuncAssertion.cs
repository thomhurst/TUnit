using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for synchronous functions.
/// This is the entry point for: Assert.That(() => GetValue())
/// </summary>
public class FuncAssertion<TValue> : Assertion<TValue>
{
    public FuncAssertion(Func<TValue> func, string? expression)
        : base(new EvaluationContext<TValue>(async () =>
        {
            try
            {
                // Run on thread pool to avoid blocking
                var result = await Task.Run(func);
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
