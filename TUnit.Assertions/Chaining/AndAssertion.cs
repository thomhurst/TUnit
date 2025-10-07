using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Chaining;

/// <summary>
/// Combines two assertions with And logic - both must pass.
/// Used internally by And chaining. Most users won't interact with this directly.
/// </summary>
public class AndAssertion<TValue> : Assertion<TValue>
{
    private readonly Assertion<TValue> _first;
    private readonly Assertion<TValue> _second;

    public AndAssertion(
        Assertion<TValue> first,
        Assertion<TValue> second)
        : base(((IAssertionSource<TValue>)first).Context, ((IAssertionSource<TValue>)first).ExpressionBuilder)
    {
        _first = first ?? throw new ArgumentNullException(nameof(first));
        _second = second ?? throw new ArgumentNullException(nameof(second));
    }

    public override async Task<TValue?> AssertAsync()
    {
        // Both must pass - short circuit on first failure
        await _first.AssertAsync();
        return await _second.AssertAsync();
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "both conditions";
}
