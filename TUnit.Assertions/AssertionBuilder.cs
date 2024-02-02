namespace TUnit.Assertions;


public abstract class AssertionBuilder<T>
{
    protected internal abstract Task<AssertionData<T>> GetAssertionData();

    public Does<T> Does => new(this);
    public Is<T> Is => new(this);
    public Has<T> Has => new(this);
}