using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions;


public class ValueAssertionBuilder<T> : AssertionBuilder<T>
{
    private readonly T? _value;
    
    public Does<T> Does => new(this, ConnectorType.None, null);
    public Is<T> Is => new(this, ConnectorType.None, null);
    public Has<T> Has => new(this, ConnectorType.None, null);

    internal ValueAssertionBuilder(T? value)
    {
        _value = value;
    }

    protected internal override Task<AssertionData<T>> GetAssertionData()
    {
        return Task.FromResult(new AssertionData<T>(_value, null));
    }
}