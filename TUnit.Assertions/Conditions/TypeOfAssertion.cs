using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that a value is of a specific type and transforms the assertion chain to that type.
/// This demonstrates type transformation using EvaluationContext.Map().
/// </summary>
public class TypeOfAssertion<TFrom, TTo> : Assertion<TTo>
{
    private readonly Type _expectedType;

    public TypeOfAssertion(
        AssertionContext<TFrom> parentContext)
        : base(parentContext.Map<TTo>(value =>
            {
                if (value is TTo casted)
                {
                    return casted;
                }

                throw new InvalidCastException(
                    $"Value is of type {value?.GetType().Name ?? "null"}, not {typeof(TTo).Name}");
            }))
    {
        // Note: Pending link transfer is handled by AssertionContext<T>.Map()
        // No need to duplicate that logic here
        _expectedType = typeof(TTo);
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TTo> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        // The type check already happened in the Map function
        // If we got here without exception, the type is correct
        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed(exception.Message));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => $"to be of type {_expectedType.Name}";
}

/// <summary>
/// Asserts that a value is NOT exactly of the specified type.
/// </summary>
public class IsNotTypeOfAssertion<TValue, TExpected> : Assertion<TValue>
{
    private readonly Type _expectedType;

    public IsNotTypeOfAssertion(
        AssertionContext<TValue> context)
        : base(context)
    {
        _expectedType = typeof(TExpected);
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var actualType = value.GetType();

        if (actualType != _expectedType)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"type was {actualType.Name}"));
    }

    protected override string GetExpectation() => $"to not be of type {_expectedType.Name}";
}

/// <summary>
/// Asserts that a value's type is assignable to a specific type (is the type or a subtype).
/// Works with both direct value assertions and exception assertions (via .And after Throws).
/// </summary>
public class IsAssignableToAssertion<TTarget, TValue> : Assertion<TValue>
{
    private readonly Type _targetType;

    public IsAssignableToAssertion(
        AssertionContext<TValue> context)
        : base(context)
    {
        _targetType = typeof(TTarget);
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        object? objectToCheck = null;

        // If we have an exception (from Throws/ThrowsExactly), check that
        if (exception != null)
        {
            objectToCheck = exception;
        }
        // Otherwise check the value
        else if (value != null)
        {
            objectToCheck = value;
        }
        else
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var actualType = objectToCheck.GetType();

        if (_targetType.IsAssignableFrom(actualType))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"type {actualType.Name} is not assignable to {_targetType.Name}"));
    }

    protected override string GetExpectation() => $"to be assignable to {_targetType.Name}";
}

/// <summary>
/// Asserts that a value's type is NOT assignable to a specific type.
/// Works with both direct value assertions and exception assertions (via .And after Throws).
/// </summary>
public class IsNotAssignableToAssertion<TTarget, TValue> : Assertion<TValue>
{
    private readonly Type _targetType;

    public IsNotAssignableToAssertion(
        AssertionContext<TValue> context)
        : base(context)
    {
        _targetType = typeof(TTarget);
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        object? objectToCheck = null;

        // If we have an exception (from Throws/ThrowsExactly), check that
        if (exception != null)
        {
            objectToCheck = exception;
        }
        // Otherwise check the value
        else if (value != null)
        {
            objectToCheck = value;
        }
        else
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var actualType = objectToCheck.GetType();

        if (!_targetType.IsAssignableFrom(actualType))
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"type {actualType.Name} is assignable to {_targetType.Name}"));
    }

    protected override string GetExpectation() => $"to not be assignable to {_targetType.Name}";
}

/// <summary>
/// Asserts that a value is exactly of the specified type (using runtime Type parameter).
/// </summary>
public class IsTypeOfRuntimeAssertion<TValue> : Assertion<TValue>
{
    private readonly Type _expectedType;

    public IsTypeOfRuntimeAssertion(
        AssertionContext<TValue> context,
        Type expectedType)
        : base(context)
    {
        _expectedType = expectedType ?? throw new ArgumentNullException(nameof(expectedType));
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        if (value == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        var actualType = value.GetType();

        if (actualType == _expectedType)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"type was {actualType.Name}"));
    }

    protected override string GetExpectation() => $"to be of type {_expectedType.Name}";
}
