using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

// Exception assertions
public class HasInnerExceptionAssertion : Assertion<Exception>
{
    public HasInnerExceptionAssertion(
        EvaluationContext<Exception> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(Exception? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("exception was null"));

        if (value.InnerException == null)
            return Task.FromResult(AssertionResult.Failed("exception has no inner exception"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have an inner exception";
}

public class HasNoInnerExceptionAssertion : Assertion<Exception>
{
    public HasNoInnerExceptionAssertion(
        EvaluationContext<Exception> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(Exception? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("exception was null"));

        if (value.InnerException != null)
            return Task.FromResult(AssertionResult.Failed($"exception has inner exception: {value.InnerException.GetType().Name}"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have no inner exception";
}

public class HasStackTraceAssertion : Assertion<Exception>
{
    public HasStackTraceAssertion(
        EvaluationContext<Exception> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(Exception? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("exception was null"));

        if (string.IsNullOrWhiteSpace(value.StackTrace))
            return Task.FromResult(AssertionResult.Failed("exception has no stack trace"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have a stack trace";
}

public class HasNoDataAssertion : Assertion<Exception>
{
    public HasNoDataAssertion(
        EvaluationContext<Exception> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(Exception? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("exception was null"));

        if (value.Data.Count > 0)
            return Task.FromResult(AssertionResult.Failed($"exception has {value.Data.Count} data entries"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have no data";
}

// StringBuilder assertions
public class StringBuilderIsEmptyAssertion : Assertion<StringBuilder>
{
    public StringBuilderIsEmptyAssertion(
        EvaluationContext<StringBuilder> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(StringBuilder? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("StringBuilder was null"));

        if (value.Length > 0)
            return Task.FromResult(AssertionResult.Failed($"StringBuilder has length {value.Length}"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be empty";
}

public class StringBuilderIsNotEmptyAssertion : Assertion<StringBuilder>
{
    public StringBuilderIsNotEmptyAssertion(
        EvaluationContext<StringBuilder> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(StringBuilder? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("StringBuilder was null"));

        if (value.Length == 0)
            return Task.FromResult(AssertionResult.Failed("StringBuilder is empty"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be empty";
}

public class StringBuilderHasExcessCapacityAssertion : Assertion<StringBuilder>
{
    public StringBuilderHasExcessCapacityAssertion(
        EvaluationContext<StringBuilder> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(StringBuilder? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("StringBuilder was null"));

        if (value.Capacity <= value.Length)
            return Task.FromResult(AssertionResult.Failed($"StringBuilder has no excess capacity (capacity: {value.Capacity}, length: {value.Length})"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have excess capacity";
}

// DayOfWeek assertions
public class IsWeekendAssertion : Assertion<DayOfWeek>
{
    public IsWeekendAssertion(
        EvaluationContext<DayOfWeek> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(DayOfWeek value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value != DayOfWeek.Saturday && value != DayOfWeek.Sunday)
            return Task.FromResult(AssertionResult.Failed($"day was {value}"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a weekend day";
}

public class IsWeekdayAssertion : Assertion<DayOfWeek>
{
    public IsWeekdayAssertion(
        EvaluationContext<DayOfWeek> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(DayOfWeek value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == DayOfWeek.Saturday || value == DayOfWeek.Sunday)
            return Task.FromResult(AssertionResult.Failed($"day was {value}"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a weekday";
}

public class IsMondayAssertion : Assertion<DayOfWeek>
{
    public IsMondayAssertion(
        EvaluationContext<DayOfWeek> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(DayOfWeek value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value != DayOfWeek.Monday)
            return Task.FromResult(AssertionResult.Failed($"day was {value}"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be Monday";
}

public class IsFridayAssertion : Assertion<DayOfWeek>
{
    public IsFridayAssertion(
        EvaluationContext<DayOfWeek> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(DayOfWeek value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value != DayOfWeek.Friday)
            return Task.FromResult(AssertionResult.Failed($"day was {value}"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be Friday";
}

// WeakReference assertions
public class IsAliveAssertion : Assertion<WeakReference>
{
    public IsAliveAssertion(
        EvaluationContext<WeakReference> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(WeakReference? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("WeakReference was null"));

        if (!value.IsAlive)
            return Task.FromResult(AssertionResult.Failed("WeakReference target is not alive"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be alive";
}

public class IsDeadAssertion : Assertion<WeakReference>
{
    public IsDeadAssertion(
        EvaluationContext<WeakReference> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(WeakReference? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        if (value == null)
            return Task.FromResult(AssertionResult.Failed("WeakReference was null"));

        if (value.IsAlive)
            return Task.FromResult(AssertionResult.Failed("WeakReference target is still alive"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be dead";
}
