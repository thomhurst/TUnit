using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Sources;

/// <summary>
/// Source assertion for immediate values.
/// This is the entry point for: Assert.That(value)
/// Does not inherit from Assertion to prevent premature awaiting.
/// </summary>
public class ValueAssertion<TValue> : IAssertionSource<TValue>
{
    public AssertionContext<TValue> Context { get; }

    public ValueAssertion(TValue? value, string? expression)
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder.Append($"Assert.That({expression ?? "?"})");
        Context = new AssertionContext<TValue>(value, expressionBuilder);
    }
}
