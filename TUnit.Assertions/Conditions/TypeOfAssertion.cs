using System.Text;
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
        EvaluationContext<TFrom> parentContext,
        StringBuilder expressionBuilder)
        : base(
            parentContext.Map<TTo>(value =>
            {
                if (value is TTo casted)
                    return casted;

                throw new InvalidCastException(
                    $"Value is of type {value?.GetType().Name ?? "null"}, not {typeof(TTo).Name}");
            }),
            expressionBuilder)
    {
        _expectedType = typeof(TTo);
    }

    protected override Task<AssertionResult> CheckAsync(TTo? value, Exception? exception)
    {
        // The type check already happened in the Map function
        // If we got here without exception, the type is correct
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed(exception.Message));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => $"to be of type {_expectedType.Name}";
}
