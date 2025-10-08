using System.Runtime.ExceptionServices;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions;

/// <summary>
/// Internal implementation of Assert.Multiple() functionality.
/// Accumulates assertion failures instead of throwing immediately,
/// then throws all failures together when disposed.
/// </summary>
internal class AssertionScope : IDisposable
{
    private static readonly AsyncLocal<AssertionScope?> CurrentScope = new();
    private readonly AssertionScope? _parent;
    private readonly List<Exception> _exceptions = [];

    internal AssertionScope()
    {
        _parent = GetCurrentAssertionScope();
        SetCurrentAssertionScope(this);
    }

    public void Dispose()
    {
        SetCurrentAssertionScope(_parent);

        // If we have a parent scope, bubble up all exceptions to it
        if (_parent != null)
        {
            foreach (var exception in _exceptions)
            {
                _parent._exceptions.Add(exception);
            }

            return;
        }

        // No exceptions accumulated - all assertions passed
        if (_exceptions.Count == 0)
        {
            return;
        }

        // Single exception - throw it directly to preserve stack trace
        if (_exceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(_exceptions[0]).Throw();
        }

        // Multiple exceptions - throw aggregate with combined messages
        var message = string.Join(Environment.NewLine + Environment.NewLine, _exceptions.Select(e => e.Message));
        throw new AssertionException(message, new AggregateException(_exceptions));
    }

    internal static AssertionScope? GetCurrentAssertionScope()
    {
        return CurrentScope.Value;
    }

    private static void SetCurrentAssertionScope(AssertionScope? scope)
    {
        CurrentScope.Value = scope;
    }

    internal void AddException(AssertionException exception)
    {
        _exceptions.Add(exception);
    }
}
