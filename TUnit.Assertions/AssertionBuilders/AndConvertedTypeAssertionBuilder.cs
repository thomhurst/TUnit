using System.Text;

namespace TUnit.Assertions.AssertionBuilders;

public class AndConvertedTypeAssertionBuilder<TFromType, TToType>(
    InvokableAssertionBuilder<TFromType> otherTypeAssertionBuilder,
    ValueTask<AssertionData<TToType>> actual,
    string actualExpression,
    StringBuilder expressionBuilder)
    : ConvertedTypeAssertionBuilder<TFromType, TToType>(otherTypeAssertionBuilder, actual, actualExpression,
        expressionBuilder), IAndAssertionBuilder;