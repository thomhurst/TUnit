using System.Text;

namespace TUnit.Assertions.AssertionBuilders;

public class AndConvertedTypeAssertionBuilder<TToType>(
    InvokableAssertionBuilder<object?> otherTypeAssertionBuilder,
    ValueTask<AssertionData> actual,
    string actualExpression,
    StringBuilder expressionBuilder)
    : ConvertedTypeAssertionBuilder<object?, TToType>(otherTypeAssertionBuilder, actual, actualExpression,
        expressionBuilder), IAndAssertionBuilder;