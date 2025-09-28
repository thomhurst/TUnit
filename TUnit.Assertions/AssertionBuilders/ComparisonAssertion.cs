using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public enum ComparisonType
{
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual
}

/// <summary>
///  comparison assertion with lazy evaluation
/// </summary>
public class ComparisonAssertion<TActual> : AssertionBase<TActual>
{
    private readonly TActual _comparisonValue;
    private readonly ComparisonType _comparisonType;
    private readonly Func<Task<TActual>> _originalProvider;

    public ComparisonAssertion(Func<Task<TActual>> actualValueProvider, TActual comparisonValue, ComparisonType comparisonType)
        : base(actualValueProvider)
    {
        _originalProvider = actualValueProvider;
        _comparisonValue = comparisonValue;
        _comparisonType = comparisonType;
    }

    public ComparisonAssertion(Func<TActual> actualValueProvider, TActual comparisonValue, ComparisonType comparisonType)
        : base(actualValueProvider)
    {
        _originalProvider = () => Task.FromResult(actualValueProvider());
        _comparisonValue = comparisonValue;
        _comparisonType = comparisonType;
    }

    public ComparisonAssertion(TActual actualValue, TActual comparisonValue, ComparisonType comparisonType)
        : base(actualValue)
    {
        _originalProvider = () => Task.FromResult(actualValue);
        _comparisonValue = comparisonValue;
        _comparisonType = comparisonType;
    }


    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual == null)
        {
            return AssertionResult.Fail($"Cannot compare null value to {_comparisonValue}");
        }

        // Check if type implements IComparable<TActual> at runtime
        if (actual is not IComparable<TActual> comparableActual)
        {
            return AssertionResult.Fail($"Type {typeof(TActual).Name} does not implement IComparable<{typeof(TActual).Name}>");
        }

        var comparisonResult = comparableActual.CompareTo(_comparisonValue);
        bool passed = _comparisonType switch
        {
            ComparisonType.GreaterThan => comparisonResult > 0,
            ComparisonType.GreaterThanOrEqual => comparisonResult >= 0,
            ComparisonType.LessThan => comparisonResult < 0,
            ComparisonType.LessThanOrEqual => comparisonResult <= 0,
            _ => false
        };

        if (passed)
        {
            return AssertionResult.Passed;
        }

        var operatorText = _comparisonType switch
        {
            ComparisonType.GreaterThan => "greater than",
            ComparisonType.GreaterThanOrEqual => "greater than or equal to",
            ComparisonType.LessThan => "less than",
            ComparisonType.LessThanOrEqual => "less than or equal to",
            _ => "compared to"
        };

        return AssertionResult.Fail($"Expected {actual} to be {operatorText} {_comparisonValue}");
    }
}

/// <summary>
/// Helper class to enable chaining comparison assertions
/// </summary>
public class ComparisonAndChain<TActual>
{
    private readonly ComparisonAssertion<TActual> _previousAssertion;
    private readonly Func<Task<TActual>> _valueProvider;

    public ComparisonAndChain(ComparisonAssertion<TActual> previousAssertion, Func<Task<TActual>> valueProvider)
    {
        _previousAssertion = previousAssertion;
        _valueProvider = valueProvider;
    }

    public async Task IsLessThanOrEqualTo(TActual value)
    {
        // First execute the previous assertion
        await _previousAssertion;

        // Then execute the new assertion
        var newAssertion = new ComparisonAssertion<TActual>(_valueProvider, value, ComparisonType.LessThanOrEqual);
        await newAssertion;
    }

    public async Task IsLessThan(TActual value)
    {
        await _previousAssertion;
        var newAssertion = new ComparisonAssertion<TActual>(_valueProvider, value, ComparisonType.LessThan);
        await newAssertion;
    }

    public async Task IsGreaterThan(TActual value)
    {
        await _previousAssertion;
        var newAssertion = new ComparisonAssertion<TActual>(_valueProvider, value, ComparisonType.GreaterThan);
        await newAssertion;
    }

    public async Task IsGreaterThanOrEqualTo(TActual value)
    {
        await _previousAssertion;
        var newAssertion = new ComparisonAssertion<TActual>(_valueProvider, value, ComparisonType.GreaterThanOrEqual);
        await newAssertion;
    }
}