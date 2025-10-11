using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Char-specific assertion extension methods.
/// </summary>
public static class CharAssertionExtensions
{
    public static IsLetterAssertion IsLetter(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsLetter()");
        return new IsLetterAssertion(source.Context);
    }

    public static IsNotLetterAssertion IsNotLetter(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotLetter()");
        return new IsNotLetterAssertion(source.Context);
    }

    public static IsDigitAssertion IsDigit(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsDigit()");
        return new IsDigitAssertion(source.Context);
    }

    public static IsNotDigitAssertion IsNotDigit(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotDigit()");
        return new IsNotDigitAssertion(source.Context);
    }

    public static IsWhiteSpaceAssertion IsWhiteSpace(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsWhiteSpace()");
        return new IsWhiteSpaceAssertion(source.Context);
    }

    public static IsNotWhiteSpaceAssertion IsNotWhiteSpace(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotWhiteSpace()");
        return new IsNotWhiteSpaceAssertion(source.Context);
    }

    public static IsUpperAssertion IsUpper(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsUpper()");
        return new IsUpperAssertion(source.Context);
    }

    public static IsNotUpperAssertion IsNotUpper(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotUpper()");
        return new IsNotUpperAssertion(source.Context);
    }

    public static IsLowerAssertion IsLower(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsLower()");
        return new IsLowerAssertion(source.Context);
    }

    public static IsNotLowerAssertion IsNotLower(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotLower()");
        return new IsNotLowerAssertion(source.Context);
    }

    public static IsControlAssertion IsControl(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsControl()");
        return new IsControlAssertion(source.Context);
    }

    public static IsNotControlAssertion IsNotControl(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotControl()");
        return new IsNotControlAssertion(source.Context);
    }

    public static IsPunctuationAssertion IsPunctuation(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsPunctuation()");
        return new IsPunctuationAssertion(source.Context);
    }

    public static IsNotPunctuationAssertion IsNotPunctuation(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotPunctuation()");
        return new IsNotPunctuationAssertion(source.Context);
    }

    public static IsSymbolAssertion IsSymbol(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsSymbol()");
        return new IsSymbolAssertion(source.Context);
    }

    public static IsNotSymbolAssertion IsNotSymbol(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotSymbol()");
        return new IsNotSymbolAssertion(source.Context);
    }

    public static IsNumberAssertion IsNumber(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNumber()");
        return new IsNumberAssertion(source.Context);
    }

    public static IsNotNumberAssertion IsNotNumber(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotNumber()");
        return new IsNotNumberAssertion(source.Context);
    }

    public static IsSeparatorAssertion IsSeparator(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsSeparator()");
        return new IsSeparatorAssertion(source.Context);
    }

    public static IsNotSeparatorAssertion IsNotSeparator(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotSeparator()");
        return new IsNotSeparatorAssertion(source.Context);
    }

    public static IsSurrogateAssertion IsSurrogate(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsSurrogate()");
        return new IsSurrogateAssertion(source.Context);
    }

    public static IsNotSurrogateAssertion IsNotSurrogate(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotSurrogate()");
        return new IsNotSurrogateAssertion(source.Context);
    }

    public static IsHighSurrogateAssertion IsHighSurrogate(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsHighSurrogate()");
        return new IsHighSurrogateAssertion(source.Context);
    }

    public static IsNotHighSurrogateAssertion IsNotHighSurrogate(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotHighSurrogate()");
        return new IsNotHighSurrogateAssertion(source.Context);
    }

    public static IsLowSurrogateAssertion IsLowSurrogate(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsLowSurrogate()");
        return new IsLowSurrogateAssertion(source.Context);
    }

    public static IsNotLowSurrogateAssertion IsNotLowSurrogate(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotLowSurrogate()");
        return new IsNotLowSurrogateAssertion(source.Context);
    }

    public static IsLetterOrDigitAssertion IsLetterOrDigit(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsLetterOrDigit()");
        return new IsLetterOrDigitAssertion(source.Context);
    }

    public static IsNotLetterOrDigitAssertion IsNotLetterOrDigit(this IAssertionSource<char> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotLetterOrDigit()");
        return new IsNotLetterOrDigitAssertion(source.Context);
    }
}
