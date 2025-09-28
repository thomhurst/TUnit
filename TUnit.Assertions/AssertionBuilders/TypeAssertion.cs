using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
///  type assertion with lazy evaluation
/// </summary>
public class TypeAssertion<TActual> : AssertionBase<TActual>
{
    private readonly Type _expectedType;
    private readonly bool _exact;

    public TypeAssertion(Func<Task<TActual>> actualValueProvider, Type expectedType, bool exact)
        : base(actualValueProvider)
    {
        _expectedType = expectedType;
        _exact = exact;
    }

    public TypeAssertion(Func<TActual> actualValueProvider, Type expectedType, bool exact)
        : base(actualValueProvider)
    {
        _expectedType = expectedType;
        _exact = exact;
    }

    public TypeAssertion(TActual actualValue, Type expectedType, bool exact)
        : base(actualValue)
    {
        _expectedType = expectedType;
        _exact = exact;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual == null)
        {
            return AssertionResult.Fail($"Expected type {_expectedType.Name} but was null");
        }

        var actualType = actual.GetType();
        bool passed;

        if (_exact)
        {
            passed = actualType == _expectedType;
        }
        else
        {
            passed = _expectedType.IsAssignableFrom(actualType);
        }

        if (passed)
        {
            return AssertionResult.Passed;
        }

        var relationship = _exact ? "exactly" : "assignable to";
        return AssertionResult.Fail($"Expected type {relationship} {_expectedType.Name} but was {actualType.Name}");
    }
}