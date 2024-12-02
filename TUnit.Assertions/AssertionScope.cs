using System.Runtime.ExceptionServices;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions;

internal class AssertionScope : IDisposable
{
    private static readonly AsyncLocal<AssertionScope?> CurrentScope = new();
    private readonly AssertionScope? _parent;
    private readonly List<Exception> _exceptions = [];

    static AssertionScope()
    {
        AppDomain.CurrentDomain.FirstChanceException += InterceptException;
    }
    
    internal AssertionScope()
    {
        _parent = GetCurrentAssertionScope();
        SetCurrentAssertionScope(this);
    }

    private static void InterceptException(object? sender, FirstChanceExceptionEventArgs firstChanceExceptionEventArgs)
    {
        if (GetCurrentAssertionScope() is { } validScope)
        {
            validScope._exceptions.Add(firstChanceExceptionEventArgs.Exception);
        }
    }

    public void Dispose()
    {
        SetCurrentAssertionScope(_parent);

        if (_parent != null)
        {
            foreach (var exception in _exceptions)
            {
                _parent._exceptions.Add(exception);
            }
            
            return;
        }
        
        if (_exceptions.Count == 1)
        {
            ExceptionDispatchInfo.Throw(_exceptions[0]);
        }

        if (_exceptions.Count > 1)
        {
            throw new AggregateException(_exceptions);
        }
    }
    
    internal static AssertionScope? GetCurrentAssertionScope()
    {
        return CurrentScope.Value;
    }

    private static void SetCurrentAssertionScope(AssertionScope? scope)
    {
        CurrentScope.Value = scope;
    }

    public void AddException(AssertionException exception)
    {
        _exceptions.Add(exception);
    }
}