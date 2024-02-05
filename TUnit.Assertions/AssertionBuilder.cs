namespace TUnit.Assertions;

public abstract class AssertionBuilder<TActual>
{
    protected internal abstract Task<AssertionData<TActual?>> GetAssertionData();
}