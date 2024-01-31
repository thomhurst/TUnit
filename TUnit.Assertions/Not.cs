using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;

public class Not
{
    public AssertCondition<object, object> Null => new NotNullAssertCondition();
}