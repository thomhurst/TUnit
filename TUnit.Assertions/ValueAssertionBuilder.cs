namespace TUnit.Assertions;


public class ValueAssertionBuilder<T> : AssertionBuilder<T>
{
    private readonly T? _value;

    internal ValueAssertionBuilder(T? value)
    {
        _value = value;
    }

    protected internal override Task<AssertionData<T>> GetAssertionData()
    {
        return Task.FromResult(new AssertionData<T>(_value, null));
    }
}