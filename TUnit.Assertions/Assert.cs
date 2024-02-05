using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions;

public static class Assert
{
    public static ValueAssertionBuilder<TActual> That<TActual>(TActual value)
    {
        return new ValueAssertionBuilder<TActual>(value);
    }
    
    public static DelegateAssertionBuilder That(Action value)
    {
        return new DelegateAssertionBuilder(value);
    }
    
    public static DelegateAssertionBuilder<TActual> That<TActual>(Func<TActual> value)
    {
        return new DelegateAssertionBuilder<TActual>(value);
    }
    
    public static AsyncDelegateAssertionBuilder That(Func<Task> value)
    {
        return new AsyncDelegateAssertionBuilder(value);
    }
    
    public static AsyncDelegateAssertionBuilder<TActual> That<TActual>(Func<Task<TActual>> value)
    {
        return new AsyncDelegateAssertionBuilder<TActual>(value!);
    }

    public static AssertMultipleHandler Multiple(Action action)
    {
        return new AssertMultipleHandler(action);
    }

    public static void Fail(string reason)
    {
        throw new AssertionException(reason);
    }
}