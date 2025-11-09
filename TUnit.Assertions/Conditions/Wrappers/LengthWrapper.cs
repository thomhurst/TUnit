using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Conditions;
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
    /// Not supported on LengthWrapper - use IsTypeOf on the assertion source before calling HasLength().
    /// </summary>
    TypeOfAssertion<string, TExpected> IAssertionSource<string>.IsTypeOf<TExpected>()
    {
        throw new NotSupportedException(
            "IsTypeOf is not supported after HasLength(). " +
            "Use: Assert.That(value).IsTypeOf<string>().HasLength().EqualTo(5)");
    }

    /// <summary>
    /// Not supported on LengthWrapper - use IsAssignableTo on the assertion source before calling HasLength().
    /// </summary>
    IsAssignableToAssertion<TTarget, string> IAssertionSource<string>.IsAssignableTo<TTarget>()
    {
        throw new NotSupportedException(
            "IsAssignableTo is not supported after HasLength(). " +
            "Use: Assert.That(value).IsAssignableTo<string>().HasLength().EqualTo(5)");
    }

    /// <summary>
    /// Not supported on LengthWrapper - use IsNotAssignableTo on the assertion source before calling HasLength().
    /// </summary>
    IsNotAssignableToAssertion<TTarget, string> IAssertionSource<string>.IsNotAssignableTo<TTarget>()
    {
        throw new NotSupportedException(
            "IsNotAssignableTo is not supported after HasLength(). " +
            "Use: Assert.That(value).IsNotAssignableTo<string>().HasLength().EqualTo(5)");
    }

    /// <summary>
    /// Not supported on LengthWrapper - use IsNotTypeOf on the assertion source before calling HasLength().
    /// </summary>
    IsNotTypeOfAssertion<string, TExpected> IAssertionSource<string>.IsNotTypeOf<TExpected>()
    {
        throw new NotSupportedException(
            "IsNotTypeOf is not supported after HasLength(). " +
            "Use: Assert.That(value).IsNotTypeOf<string>().HasLength().EqualTo(5)");
    }

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
