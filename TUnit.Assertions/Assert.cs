using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions;

public static class Assert
{
    public static ValueAssertionBuilder<TActual> That<TActual>(TActual value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new ValueAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }
    
    public static DelegateAssertionBuilder That(Action value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertionBuilder(value, doNotPopulateThisValue);
    }
    
    public static ValueDelegateAssertionBuilder<TActual> That<TActual>(Func<TActual> value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new ValueDelegateAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }
    
    public static AsyncDelegateAssertionBuilder That(Func<Task> value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new AsyncDelegateAssertionBuilder(value, doNotPopulateThisValue);
    }
    
    public static AsyncValueDelegateAssertionBuilder<TActual> That<TActual>(Func<Task<TActual>> value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new AsyncValueDelegateAssertionBuilder<TActual>(value, doNotPopulateThisValue);
    }
    
    public static AsyncDelegateAssertionBuilder That(Task value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new AsyncDelegateAssertionBuilder(async () => await value, doNotPopulateThisValue);
    }
    
    public static AsyncValueDelegateAssertionBuilder<TActual> That<TActual>(Task<TActual> value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new AsyncValueDelegateAssertionBuilder<TActual>(async () => await value, doNotPopulateThisValue);
    }
    
    public static AsyncDelegateAssertionBuilder That(ValueTask value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new AsyncDelegateAssertionBuilder(async () => await value, doNotPopulateThisValue);
    }
    
    public static AsyncValueDelegateAssertionBuilder<TActual> That<TActual>(ValueTask<TActual> value, [CallerArgumentExpression("value")] string doNotPopulateThisValue = "")
    {
        return new AsyncValueDelegateAssertionBuilder<TActual>(async () => await value, doNotPopulateThisValue);
    }

    public static IDisposable Multiple()
    {
        return new AssertionScope();
    }

    [DoesNotReturn]
    public static void Fail(string reason)
    {
        throw new AssertionException(reason);
    }
}