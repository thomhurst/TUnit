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
        
        var failed = new List<(IInvokableAssertionBuilder, List<(BaseAssertCondition Assertion, AssertionResult Result)>)>();
        
        foreach (var assertionBuilder in _assertionBuilders)
        {
            var list = new List<(BaseAssertCondition Assertion, AssertionResult Result)>();
            
            await foreach (var failedAssertionWithResult in assertionBuilder.GetFailures())
            {
                list.Add(failedAssertionWithResult);
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
                       {string.Join(Environment.NewLine, x.Item2.Select(e =>  $"Expected {e.Assertion.Subject} {e.Assertion.GetExpectationWithReason()}, but {e.Result.Message}."))}
                       At {x.Item1.GetExpression()}
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