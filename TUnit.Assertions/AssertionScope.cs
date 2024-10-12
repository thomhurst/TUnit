using System.Runtime.ExceptionServices;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions;

internal class AssertionScope : IAsyncDisposable
{
    private static readonly AsyncLocal<AssertionScope?> CurrentScope = new();
    private readonly AssertionScope? _parent;
    private readonly List<AssertionException> _exceptions = [];

    internal AssertionScope()
    {
        _parent = GetCurrentAssertionScope();
        SetCurrentAssertionScope(this);
    }

    private readonly HashSet<IInvokableAssertionBuilder> _assertionBuilders = [];
    
    internal void Add(IInvokableAssertionBuilder assertionBuilder) => _assertionBuilders.Add(assertionBuilder);

    public async ValueTask DisposeAsync()
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