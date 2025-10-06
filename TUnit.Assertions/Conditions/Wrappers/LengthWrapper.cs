using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions.Wrappers;

/// <summary>
/// Wrapper for string length assertions that provides .EqualTo() method.
/// Example: await Assert.That(str).HasLength().EqualTo(5);
/// </summary>
public class LengthWrapper : IAssertionSource<string>
{
    private readonly EvaluationContext<string> _context;
    private readonly StringBuilder _expressionBuilder;

    public LengthWrapper(EvaluationContext<string> context, StringBuilder expressionBuilder)
    {
        _context = context;
        _expressionBuilder = expressionBuilder;
    }

    EvaluationContext<string> IAssertionSource<string>.Context => _context;
    StringBuilder IAssertionSource<string>.ExpressionBuilder => _expressionBuilder;

    /// <summary>
    /// Asserts that the string length is equal to the expected length.
    /// </summary>
    public StringLengthAssertion EqualTo(
        int expectedLength,
        [CallerArgumentExpression(nameof(expectedLength))] string? expression = null)
    {
        _expressionBuilder.Append($".EqualTo({expression})");
        return new StringLengthAssertion(_context, expectedLength, _expressionBuilder);
    }
}
