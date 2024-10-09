using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertionBuilders;
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

    private readonly List<IInvokableAssertionBuilder> _assertionBuilders = [];

    public ValueTaskAwaiter GetAwaiter() => DisposeAsync().GetAwaiter();

    internal void Add(IInvokableAssertionBuilder assertionBuilder) => _assertionBuilders.Add(assertionBuilder);

    public async ValueTask DisposeAsync()
    {
        SetCurrentAssertionScope(_parent);
        
        var failed = new List<(IInvokableAssertionBuilder, List<BaseAssertCondition>)>();
        
        foreach (var assertionBuilder in _assertionBuilders)
        {
            var list = new List<BaseAssertCondition>();
            
            await foreach (var failedAssertion in assertionBuilder.GetFailures())
            {
                list.Add(failedAssertion);
            }

            if (list.Count != 0)
            {
                failed.Add((assertionBuilder, list));
            }
        }
        
        foreach (var exception in _exceptions)
        {
            _parent?.AddException(exception);
        }
        
        if (failed.Any())
        {
            var assertionException = new AssertionException(string.Join($"{Environment.NewLine}{Environment.NewLine}", failed.Select(x =>
            {
                return $"""
                       {x.Item1.GetExpression()}
                       {string.Join(Environment.NewLine, x.Item2.Select(e =>  e.OverriddenMessage ?? e.GetExpectationWithReason()?.Trim()))}
                       """;
            })));
            
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
            ExceptionDispatchInfo.Throw(_exceptions[0]);
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