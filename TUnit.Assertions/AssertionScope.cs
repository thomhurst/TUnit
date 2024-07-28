using System.Diagnostics;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions;

internal class AssertionScope : IAsyncDisposable
{
    private static readonly AsyncLocal<AssertionScope> CurrentScope = new();
    private readonly AssertionScope? _parent;
    private readonly List<AssertionException> _exceptions = [];

    internal AssertionScope()
    {
        _parent = GetCurrentAssertionScope();
        SetCurrentAssertionScope(this);
    }

    private readonly List<BaseAssertCondition> _assertConditions = [];

    public ValueTaskAwaiter GetAwaiter() => DisposeAsync().GetAwaiter();

    internal void Add(BaseAssertCondition assertCondition) => _assertConditions.Add(assertCondition);

    public async ValueTask DisposeAsync()
    {
        SetCurrentAssertionScope(_parent);
        
        var failed = new List<BaseAssertCondition>();
        
        foreach (var baseAssertCondition in _assertConditions)
        {
            if (!await baseAssertCondition.AssertAsync())
            {
                if (Debugger.IsAttached)
                {
                    throw new AssertionException(baseAssertCondition.Message?.Trim());
                }
                
                failed.Add(baseAssertCondition);
            }
        }
        
        foreach (var exception in _exceptions)
        {
            _parent?.AddException(exception);
        }
        
        if (failed.Any())
        {
            var assertionException = new AssertionException(string.Join($"{Environment.NewLine}{Environment.NewLine}", failed.Select(x => x.Message?.Trim())));
            
            if (_parent != null)
            {
                _parent.AddException(assertionException);
            }
            else
            {
                AddException(assertionException);
            }
        }

        if (_parent != null)
        {
            return;
        }
        
        if (_exceptions.Count == 1)
        {
            throw _exceptions[0];
        }

        if (_exceptions.Count > 1)
        {
            throw new AggregateException(_exceptions);
        }
    }

    private void AddException(AssertionException exception)
    {
        _exceptions.Insert(0, exception);
    }
    
    internal static AssertionScope? GetCurrentAssertionScope()
    {
        return CurrentScope.Value;
    }

    private static void SetCurrentAssertionScope(AssertionScope? scope)
    {
        CurrentScope.Value = scope!;
    }
}