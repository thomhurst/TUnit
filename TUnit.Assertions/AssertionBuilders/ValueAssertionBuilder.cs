using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertionBuilders;


public class ValueAssertionBuilder<TActual> : AssertionBuilder, IValueSource<TActual>
{
    internal ValueAssertionBuilder(TActual value, string? expressionBuilder) : base(value.AsAssertionData(expressionBuilder), expressionBuilder)
    {
    }

    public ValueAssertionBuilder(ISource source) : base(source)
    {
    }
}
