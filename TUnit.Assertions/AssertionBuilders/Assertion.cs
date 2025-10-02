using System.Text;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Generic assertion base class - provides type-specific assertion functionality
/// </summary>
public abstract class Assertion<TActual> : AssertionCore
{
    protected Assertion(TActual value, string? expressionBuilder)
        : base(value.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }

    protected Assertion(ISource source) : base(source)
    {
    }

    protected Assertion(ValueTask<AssertionData> assertionDataTask, string actualExpression, StringBuilder expressionBuilder, Stack<BaseAssertCondition> assertions)
        : base(assertionDataTask, actualExpression, expressionBuilder, assertions)
    {
    }
}
