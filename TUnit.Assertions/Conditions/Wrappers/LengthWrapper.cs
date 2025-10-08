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
    private readonly AssertionContext<string> _context;

    public LengthWrapper(AssertionContext<string> context)
    {
        _context = context;
    }

    AssertionContext<string> IAssertionSource<string>.Context => _context;

    /// <summary>
    /// Asserts that the string length is equal to the expected length.
    /// </summary>
    public StringLengthAssertion EqualTo(
        int expectedLength,
        [CallerArgumentExpression(nameof(expectedLength))] string? expression = null)
    {
        _context.ExpressionBuilder.Append($".EqualTo({expression})");
        return new StringLengthAssertion(_context, expectedLength);
    }
}
