using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions;


public class ValueAssertionBuilder<TActual> : AssertionBuilder<TActual>
{
    private readonly TActual _value;
    
    public Does<TActual, ValueAnd<TActual>, ValueOr<TActual>> Does => new(this, ConnectorType.None, null);
    public Is<TActual, ValueAnd<TActual>, ValueOr<TActual>> Is => new(this, ConnectorType.None, null);
    public Has<TActual, ValueAnd<TActual>, ValueOr<TActual>> Has => new(this, ConnectorType.None, null);

    internal ValueAssertionBuilder(TActual value)
    {
        _value = value;
    }

    protected internal override Task<AssertionData<TActual>> GetAssertionData()
    {
        return Task.FromResult(new AssertionData<TActual>(_value, null));
    }
}