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
    public EvaluationContext<TValue> Context { get; }
    public StringBuilder ExpressionBuilder { get; }

    public ValueAssertion(TValue? value, string? expression)
    {
        Context = new EvaluationContext<TValue>(value);
        ExpressionBuilder = new StringBuilder();
        ExpressionBuilder.Append($"Assert.That({expression ?? "?"})");
    }
}
