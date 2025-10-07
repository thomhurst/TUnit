using System.Text;
using TUnit.Assertions.Core;
using TUnit.Assertions.Exceptions;

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

    /// <summary>
    /// Throws when attempting to mix Or with And operators.
    /// </summary>
    public new OrContinuation<TValue> Or => throw new MixedAndOrAssertionsException();

    public override async Task<TValue?> AssertAsync()
    {
        // Both must pass - short circuit on first failure
        await _first.AssertAsync();
        return await _second.AssertAsync();
    }

    /// <summary>
    /// Not used - AndAssertion overrides AssertAsync directly for custom composition logic.
    /// </summary>
    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        throw new NotImplementedException("AndAssertion uses custom AssertAsync logic and does not call CheckAsync");
    }

    protected override string GetExpectation() => "both conditions";
}
