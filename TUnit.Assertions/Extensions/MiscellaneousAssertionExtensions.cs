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
        source.Context.ExpressionBuilder.Append(".HasInnerException()");
        return new HasInnerExceptionAssertion(source.Context);
    }

    public static HasNoInnerExceptionAssertion HasNoInnerException(
        this IAssertionSource<Exception> source)
    {
        source.Context.ExpressionBuilder.Append(".HasNoInnerException()");
        return new HasNoInnerExceptionAssertion(source.Context);
    }

    public static HasStackTraceAssertion HasStackTrace(
        this IAssertionSource<Exception> source)
    {
        source.Context.ExpressionBuilder.Append(".HasStackTrace()");
        return new HasStackTraceAssertion(source.Context);
    }

    public static HasNoDataAssertion HasNoData(
        this IAssertionSource<Exception> source)
    {
        source.Context.ExpressionBuilder.Append(".HasNoData()");
        return new HasNoDataAssertion(source.Context);
    }

    // StringBuilder extensions
    public static StringBuilderIsEmptyAssertion IsEmpty(
        this IAssertionSource<StringBuilder> source)
    {
        source.Context.ExpressionBuilder.Append(".IsEmpty()");
        return new StringBuilderIsEmptyAssertion(source.Context);
    }

    public static StringBuilderIsNotEmptyAssertion IsNotEmpty(
        this IAssertionSource<StringBuilder> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotEmpty()");
        return new StringBuilderIsNotEmptyAssertion(source.Context);
    }

    public static StringBuilderHasExcessCapacityAssertion HasExcessCapacity(
        this IAssertionSource<StringBuilder> source)
    {
        source.Context.ExpressionBuilder.Append(".HasExcessCapacity()");
        return new StringBuilderHasExcessCapacityAssertion(source.Context);
    }

    // DayOfWeek extensions
    public static IsWeekendAssertion IsWeekend(
        this IAssertionSource<DayOfWeek> source)
    {
        source.Context.ExpressionBuilder.Append(".IsWeekend()");
        return new IsWeekendAssertion(source.Context);
    }

    public static IsWeekdayAssertion IsWeekday(
        this IAssertionSource<DayOfWeek> source)
    {
        source.Context.ExpressionBuilder.Append(".IsWeekday()");
        return new IsWeekdayAssertion(source.Context);
    }

    public static IsMondayAssertion IsMonday(
        this IAssertionSource<DayOfWeek> source)
    {
        source.Context.ExpressionBuilder.Append(".IsMonday()");
        return new IsMondayAssertion(source.Context);
    }

    public static IsFridayAssertion IsFriday(
        this IAssertionSource<DayOfWeek> source)
    {
        source.Context.ExpressionBuilder.Append(".IsFriday()");
        return new IsFridayAssertion(source.Context);
    }

    // WeakReference extensions
    public static IsAliveAssertion IsAlive(
        this IAssertionSource<WeakReference> source)
    {
        source.Context.ExpressionBuilder.Append(".IsAlive()");
        return new IsAliveAssertion(source.Context);
    }

    public static IsDeadAssertion IsDead(
        this IAssertionSource<WeakReference> source)
    {
        source.Context.ExpressionBuilder.Append(".IsDead()");
        return new IsDeadAssertion(source.Context);
    }
}
