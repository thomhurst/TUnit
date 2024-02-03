using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;

public abstract class AssertionBuilder<T>
{
    protected internal abstract Task<AssertionData<T>> GetAssertionData();

    public Does<T> Does => new(this, ConnectorType.None, null);
    public Is<T> Is => new(this, ConnectorType.None, null);
    public Has<T> Has => new(this, ConnectorType.None, null);
}