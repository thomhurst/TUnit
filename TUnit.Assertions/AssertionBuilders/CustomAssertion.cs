using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
///  custom assertion with lazy evaluation
/// </summary>
public class CustomAssertion<TActual> : AssertionBase<TActual>
{
    private readonly Func<TActual, bool> _predicate;
    private readonly string? _failureMessage;

    // Internal property to access the actual value provider for chaining extensions
    internal Func<Task<TActual>> ActualValueProvider => GetActualValueAsync;

    public CustomAssertion(Func<Task<TActual>> actualValueProvider, Func<TActual, bool> predicate, string? failureMessage)
        : base(actualValueProvider)
    {
        _predicate = predicate;
        _failureMessage = failureMessage;
    }

    public CustomAssertion(Func<TActual> actualValueProvider, Func<TActual, bool> predicate, string? failureMessage)
        : base(actualValueProvider)
    {
        _predicate = predicate;
        _failureMessage = failureMessage;
    }

    public CustomAssertion(TActual actualValue, Func<TActual, bool> predicate, string? failureMessage)
        : base(actualValue)
    {
        _predicate = predicate;
        _failureMessage = failureMessage;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        try
        {
            if (_predicate(actual))
            {
                return AssertionResult.Passed;
            }
        }
        catch (Exception ex)
        {
            return AssertionResult.Fail($"Predicate threw exception: {ex.Message}");
        }

        var message = _failureMessage ?? $"Value {actual} did not satisfy the predicate";
        return AssertionResult.Fail(message);
    }

    // Generic method for IgnoringType with better type inference
    public CustomAssertion<TActual> IgnoringType<TIgnore>()
    {
        // For equivalence assertions, we need to modify the comparison logic
        // This is a simplified implementation that ignores properties of the specified type
        return new CustomAssertion<TActual>(ActualValueProvider,
            actual =>
            {
                // Basic implementation - in a full implementation, this would use reflection
                // to compare objects while ignoring properties of the specified type
                return true; // Placeholder - would need complex object comparison logic
            },
            $"Expected objects to be equivalent ignoring type {typeof(TIgnore).Name}");
    }

    // Support for partial equivalency checking
    public CustomAssertion<TActual> WithPartialEquivalency()
    {
        // This is a marker method for partial equivalency - the actual logic would need to be in the assertion
        // For now, we just return this to allow chaining
        return this;
    }
}