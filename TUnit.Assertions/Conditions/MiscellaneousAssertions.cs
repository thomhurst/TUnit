using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

// Exception assertions
[AssertionExtension("HasInnerException")]
public class HasInnerExceptionAssertion : Assertion<Exception>
{
    public HasInnerExceptionAssertion(
        AssertionContext<Exception> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Exception> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("exception was null"));
        }

        if (value.InnerException == null)
        {
            return Task.FromResult(AssertionResult.Failed("exception has no inner exception"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have an inner exception";
}

[AssertionExtension("HasNoInnerException")]
public class HasNoInnerExceptionAssertion : Assertion<Exception>
{
    public HasNoInnerExceptionAssertion(
        AssertionContext<Exception> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Exception> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("exception was null"));
        }

        if (value.InnerException != null)
        {
            return Task.FromResult(AssertionResult.Failed($"exception has inner exception: {value.InnerException.GetType().Name}"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have no inner exception";
}

[AssertionExtension("HasStackTrace")]
public class HasStackTraceAssertion : Assertion<Exception>
{
    public HasStackTraceAssertion(
        AssertionContext<Exception> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Exception> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("exception was null"));
        }

        if (string.IsNullOrWhiteSpace(value.StackTrace))
        {
            return Task.FromResult(AssertionResult.Failed("exception has no stack trace"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have a stack trace";
}

[AssertionExtension("HasNoData")]
public class HasNoDataAssertion : Assertion<Exception>
{
    public HasNoDataAssertion(
        AssertionContext<Exception> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Exception> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("exception was null"));
        }

        if (value.Data.Count > 0)
        {
            return Task.FromResult(AssertionResult.Failed($"exception has {value.Data.Count} data entries"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have no data";
}

// StringBuilder assertions
[AssertionExtension("IsEmpty")]
public class StringBuilderIsEmptyAssertion : Assertion<StringBuilder>
{
    public StringBuilderIsEmptyAssertion(
        AssertionContext<StringBuilder> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<StringBuilder> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("StringBuilder was null"));
        }

        if (value.Length > 0)
        {
            return Task.FromResult(AssertionResult.Failed($"StringBuilder has length {value.Length}"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be empty";
}

[AssertionExtension("IsNotEmpty")]
public class StringBuilderIsNotEmptyAssertion : Assertion<StringBuilder>
{
    public StringBuilderIsNotEmptyAssertion(
        AssertionContext<StringBuilder> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<StringBuilder> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("StringBuilder was null"));
        }

        if (value.Length == 0)
        {
            return Task.FromResult(AssertionResult.Failed("StringBuilder is empty"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be empty";
}

[AssertionExtension("HasExcessCapacity")]
public class StringBuilderHasExcessCapacityAssertion : Assertion<StringBuilder>
{
    public StringBuilderHasExcessCapacityAssertion(
        AssertionContext<StringBuilder> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<StringBuilder> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("StringBuilder was null"));
        }

        if (value.Capacity <= value.Length)
        {
            return Task.FromResult(AssertionResult.Failed($"StringBuilder has no excess capacity (capacity: {value.Capacity}, length: {value.Length})"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to have excess capacity";
}

// DayOfWeek assertions
[AssertionExtension("IsWeekend")]
public class IsWeekendAssertion : Assertion<DayOfWeek>
{
    public IsWeekendAssertion(
        AssertionContext<DayOfWeek> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DayOfWeek> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value != DayOfWeek.Saturday && value != DayOfWeek.Sunday)
        {
            return Task.FromResult(AssertionResult.Failed($"day was {value}"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a weekend day";
}

[AssertionExtension("IsWeekday")]
public class IsWeekdayAssertion : Assertion<DayOfWeek>
{
    public IsWeekdayAssertion(
        AssertionContext<DayOfWeek> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DayOfWeek> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == DayOfWeek.Saturday || value == DayOfWeek.Sunday)
        {
            return Task.FromResult(AssertionResult.Failed($"day was {value}"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a weekday";
}

[AssertionExtension("IsMonday")]
public class IsMondayAssertion : Assertion<DayOfWeek>
{
    public IsMondayAssertion(
        AssertionContext<DayOfWeek> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DayOfWeek> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value != DayOfWeek.Monday)
        {
            return Task.FromResult(AssertionResult.Failed($"day was {value}"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be Monday";
}

[AssertionExtension("IsFriday")]
public class IsFridayAssertion : Assertion<DayOfWeek>
{
    public IsFridayAssertion(
        AssertionContext<DayOfWeek> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<DayOfWeek> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value != DayOfWeek.Friday)
        {
            return Task.FromResult(AssertionResult.Failed($"day was {value}"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be Friday";
}
