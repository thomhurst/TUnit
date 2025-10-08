using System.Text;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

public static class MiscellaneousAssertionExtensions
{
    // Exception extensions
    public static HasInnerExceptionAssertion HasInnerException(
        this IAssertionSource<Exception> source)
    {
        source.ExpressionBuilder.Append(".HasInnerException()");
        return new HasInnerExceptionAssertion(source.Context, source.ExpressionBuilder);
    }

    public static HasNoInnerExceptionAssertion HasNoInnerException(
        this IAssertionSource<Exception> source)
    {
        source.ExpressionBuilder.Append(".HasNoInnerException()");
        return new HasNoInnerExceptionAssertion(source.Context, source.ExpressionBuilder);
    }

    public static HasStackTraceAssertion HasStackTrace(
        this IAssertionSource<Exception> source)
    {
        source.ExpressionBuilder.Append(".HasStackTrace()");
        return new HasStackTraceAssertion(source.Context, source.ExpressionBuilder);
    }

    public static HasNoDataAssertion HasNoData(
        this IAssertionSource<Exception> source)
    {
        source.ExpressionBuilder.Append(".HasNoData()");
        return new HasNoDataAssertion(source.Context, source.ExpressionBuilder);
    }

    // StringBuilder extensions
    public static StringBuilderIsEmptyAssertion IsEmpty(
        this IAssertionSource<StringBuilder> source)
    {
        source.ExpressionBuilder.Append(".IsEmpty()");
        return new StringBuilderIsEmptyAssertion(source.Context, source.ExpressionBuilder);
    }

    public static StringBuilderIsNotEmptyAssertion IsNotEmpty(
        this IAssertionSource<StringBuilder> source)
    {
        source.ExpressionBuilder.Append(".IsNotEmpty()");
        return new StringBuilderIsNotEmptyAssertion(source.Context, source.ExpressionBuilder);
    }

    public static StringBuilderHasExcessCapacityAssertion HasExcessCapacity(
        this IAssertionSource<StringBuilder> source)
    {
        source.ExpressionBuilder.Append(".HasExcessCapacity()");
        return new StringBuilderHasExcessCapacityAssertion(source.Context, source.ExpressionBuilder);
    }

    // DayOfWeek extensions
    public static IsWeekendAssertion IsWeekend(
        this IAssertionSource<DayOfWeek> source)
    {
        source.ExpressionBuilder.Append(".IsWeekend()");
        return new IsWeekendAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsWeekdayAssertion IsWeekday(
        this IAssertionSource<DayOfWeek> source)
    {
        source.ExpressionBuilder.Append(".IsWeekday()");
        return new IsWeekdayAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsMondayAssertion IsMonday(
        this IAssertionSource<DayOfWeek> source)
    {
        source.ExpressionBuilder.Append(".IsMonday()");
        return new IsMondayAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsFridayAssertion IsFriday(
        this IAssertionSource<DayOfWeek> source)
    {
        source.ExpressionBuilder.Append(".IsFriday()");
        return new IsFridayAssertion(source.Context, source.ExpressionBuilder);
    }

    // WeakReference extensions
    public static IsAliveAssertion IsAlive(
        this IAssertionSource<WeakReference> source)
    {
        source.ExpressionBuilder.Append(".IsAlive()");
        return new IsAliveAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsDeadAssertion IsDead(
        this IAssertionSource<WeakReference> source)
    {
        source.ExpressionBuilder.Append(".IsDead()");
        return new IsDeadAssertion(source.Context, source.ExpressionBuilder);
    }
}
