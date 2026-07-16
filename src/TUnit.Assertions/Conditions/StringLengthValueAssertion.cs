using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Assertion that evaluates the length of a string and provides numeric assertions on that length.
/// Implements IAssertionSource&lt;int&gt; to enable all numeric assertion methods.
/// Example: await Assert.That(str).Length().IsGreaterThan(5);
/// </summary>
public class StringLengthValueAssertion : Sources.ValueAssertion<int>
{
    public StringLengthValueAssertion(AssertionContext<string> stringContext)
        : base(stringContext.Map<int>(s => s?.Length ?? 0))
    {
    }
}
