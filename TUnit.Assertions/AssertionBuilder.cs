using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;

public abstract class AssertionBuilder<T>
{
    protected internal abstract Task<AssertionData<T>> GetAssertionData();
}