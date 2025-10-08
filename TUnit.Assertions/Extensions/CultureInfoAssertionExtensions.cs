using System.Globalization;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

public static class CultureInfoAssertionExtensions
{
    public static IsInvariantCultureAssertion IsInvariant(
        this IAssertionSource<CultureInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsInvariant()");
        return new IsInvariantCultureAssertion(source.Context);
    }

    public static IsNotInvariantCultureAssertion IsNotInvariant(
        this IAssertionSource<CultureInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotInvariant()");
        return new IsNotInvariantCultureAssertion(source.Context);
    }

    public static IsNeutralCultureAssertion IsNeutralCulture(
        this IAssertionSource<CultureInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNeutralCulture()");
        return new IsNeutralCultureAssertion(source.Context);
    }

    public static IsNotNeutralCultureAssertion IsNotNeutralCulture(
        this IAssertionSource<CultureInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotNeutralCulture()");
        return new IsNotNeutralCultureAssertion(source.Context);
    }

    public static IsEnglishCultureAssertion IsEnglish(
        this IAssertionSource<CultureInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsEnglish()");
        return new IsEnglishCultureAssertion(source.Context);
    }

    public static IsNotEnglishCultureAssertion IsNotEnglish(
        this IAssertionSource<CultureInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotEnglish()");
        return new IsNotEnglishCultureAssertion(source.Context);
    }

    public static IsRightToLeftCultureAssertion IsRightToLeft(
        this IAssertionSource<CultureInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsRightToLeft()");
        return new IsRightToLeftCultureAssertion(source.Context);
    }

    public static IsLeftToRightCultureAssertion IsLeftToRight(
        this IAssertionSource<CultureInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsLeftToRight()");
        return new IsLeftToRightCultureAssertion(source.Context);
    }

    public static IsReadOnlyCultureAssertion IsReadOnly(
        this IAssertionSource<CultureInfo> source)
    {
        source.Context.ExpressionBuilder.Append(".IsReadOnly()");
        return new IsReadOnlyCultureAssertion(source.Context);
    }
}

