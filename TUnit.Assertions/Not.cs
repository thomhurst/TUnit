using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;

namespace TUnit.Assertions;

public class Not
{
    public AssertCondition<object, object> Null => new NotNullAssertCondition();
    public AssertCondition<object, TExpected> TypeOf<TExpected>() => new NotTypeOfAssertCondition<object, TExpected>();
}