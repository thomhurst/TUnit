using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is null.
/// </summary>
[AssertionExtension("IsNull")]
public class NullAssertion<TValue> : Assertion<TValue>
{
    public NullAssertion(
        AssertionContext<TValue> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"found {value}"));
    }

    protected override string GetExpectation() => "to be null";
}

/// <summary>
/// Asserts that a value is not null.
/// </summary>
public class NotNullAssertion<TValue> : Assertion<TValue>
{
    public NotNullAssertion(
        AssertionContext<TValue> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;

        if (value != null)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed("value is null"));
    }

    protected override string GetExpectation() => "to not be null";

    public new TaskAwaiter<TValue> GetAwaiter()
    {
        return GetNonNullValueAsync().GetAwaiter();
    }

    private async Task<TValue> GetNonNullValueAsync()
    {
        var (value, _) = await Context.GetAsync();

        return value!;
    }
}

/// <summary>
/// Asserts that a value is equal to the default value for its type.
/// For reference types, this is null. For value types, this is the zero-initialized value.
/// </summary>
[AssertionExtension("IsDefault")]
public class IsDefaultAssertion<TValue> : Assertion<TValue> where TValue : struct
{
    public IsDefaultAssertion(
        AssertionContext<TValue> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (EqualityComparer<TValue>.Default.Equals(value!, default!))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value is {value}"));
    }

    protected override string GetExpectation() => $"to be default({typeof(TValue).Name})";
}

/// <summary>
/// Asserts that a value is not the default value for its type.
/// </summary>
[AssertionExtension("IsNotDefault")]
public class IsNotDefaultAssertion<TValue> : Assertion<TValue> where TValue : struct
{
    public IsNotDefaultAssertion(
        AssertionContext<TValue> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (!EqualityComparer<TValue>.Default.Equals(value!, default!))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value is default({typeof(TValue).Name})"));
    }

    protected override string GetExpectation() => $"to not be default({typeof(TValue).Name})";
}

/// <summary>
/// Asserts that a nullable value type is equal to the default value (null).
/// For nullable value types like bool?, int?, this checks if the value is null.
/// </summary>
[AssertionExtension("IsDefault")]
public class IsDefaultNullableAssertion<TValue> : Assertion<TValue?> where TValue : struct
{
    public IsDefaultNullableAssertion(
        AssertionContext<TValue?> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue?> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (!value.HasValue)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value is {value}"));
    }

    protected override string GetExpectation() => $"to be default({typeof(TValue).Name}?)";
}

/// <summary>
/// Asserts that a nullable value type is not the default value (not null).
/// For nullable value types like bool?, int?, this checks if the value has a value.
/// </summary>
[AssertionExtension("IsNotDefault")]
public class IsNotDefaultNullableAssertion<TValue> : Assertion<TValue?> where TValue : struct
{
    public IsNotDefaultNullableAssertion(
        AssertionContext<TValue?> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue?> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value.HasValue)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value is default({typeof(TValue).Name}?)"));
    }

    protected override string GetExpectation() => $"to not be default({typeof(TValue).Name}?)";
}

/// <summary>
/// Asserts that a reference type value is equal to the default value (null).
/// For reference types, this is equivalent to IsNull().
/// </summary>
[AssertionExtension("IsDefault")]
public class IsDefaultReferenceAssertion<TValue> : Assertion<TValue> where TValue : class
{
    public IsDefaultReferenceAssertion(
        AssertionContext<TValue> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (EqualityComparer<TValue>.Default.Equals(value!, default!))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value is {value}"));
    }

    protected override string GetExpectation() => $"to be default({typeof(TValue).Name})";
}

/// <summary>
/// Asserts that a reference type value is not the default value (not null).
/// For reference types, this is equivalent to IsNotNull().
/// </summary>
[AssertionExtension("IsNotDefault")]
public class IsNotDefaultReferenceAssertion<TValue> : Assertion<TValue> where TValue : class
{
    public IsNotDefaultReferenceAssertion(
        AssertionContext<TValue> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (!EqualityComparer<TValue>.Default.Equals(value!, default!))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"value is default({typeof(TValue).Name})"));
    }

    protected override string GetExpectation() => $"to not be default({typeof(TValue).Name})";
}
