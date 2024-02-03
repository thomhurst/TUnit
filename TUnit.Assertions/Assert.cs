namespace TUnit.Assertions;

public static class Assert
{
    public static AssertionBuilder<TActual> That<TActual>(TActual value)
    {
        return new ValueAssertionBuilder<TActual>(value);
    }
    
    public static DelegateAssertionBuilder That(Action value)
    {
        return new DelegateAssertionBuilder(value);
    }
    
    public static DelegateAssertionBuilder<T> That<T>(Func<T> value)
    {
        return new DelegateAssertionBuilder<T>(value);
    }
    
    public static AsyncDelegateAssertionBuilder That(Func<Task> value)
    {
        return new AsyncDelegateAssertionBuilder(value);
    }
    
    public static AsyncDelegateAssertionBuilder<T> That<T>(Func<Task<T>> value)
    {
        return new AsyncDelegateAssertionBuilder<T>(value!);
    }
}