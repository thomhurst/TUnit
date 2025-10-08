using System.Globalization;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

public static class CultureInfoAssertionExtensions
{
    public static IsInvariantCultureAssertion IsInvariant(
        this IAssertionSource<CultureInfo> source)
    {
        source.ExpressionBuilder.Append(".IsInvariant()");
        return new IsInvariantCultureAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsNotInvariantCultureAssertion IsNotInvariant(
        this IAssertionSource<CultureInfo> source)
    {
        source.ExpressionBuilder.Append(".IsNotInvariant()");
        return new IsNotInvariantCultureAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsNeutralCultureAssertion IsNeutralCulture(
        this IAssertionSource<CultureInfo> source)
    {
        source.ExpressionBuilder.Append(".IsNeutralCulture()");
        return new IsNeutralCultureAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsNotNeutralCultureAssertion IsNotNeutralCulture(
        this IAssertionSource<CultureInfo> source)
    {
        source.ExpressionBuilder.Append(".IsNotNeutralCulture()");
        return new IsNotNeutralCultureAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsEnglishCultureAssertion IsEnglish(
        this IAssertionSource<CultureInfo> source)
    {
        source.ExpressionBuilder.Append(".IsEnglish()");
        return new IsEnglishCultureAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsNotEnglishCultureAssertion IsNotEnglish(
        this IAssertionSource<CultureInfo> source)
    {
        source.ExpressionBuilder.Append(".IsNotEnglish()");
        return new IsNotEnglishCultureAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsRightToLeftCultureAssertion IsRightToLeft(
        this IAssertionSource<CultureInfo> source)
    {
        source.ExpressionBuilder.Append(".IsRightToLeft()");
        return new IsRightToLeftCultureAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsLeftToRightCultureAssertion IsLeftToRight(
        this IAssertionSource<CultureInfo> source)
    {
        source.ExpressionBuilder.Append(".IsLeftToRight()");
        return new IsLeftToRightCultureAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsReadOnlyCultureAssertion IsReadOnly(
        this IAssertionSource<CultureInfo> source)
    {
        source.ExpressionBuilder.Append(".IsReadOnly()");
        return new IsReadOnlyCultureAssertion(source.Context, source.ExpressionBuilder);
    }
}

