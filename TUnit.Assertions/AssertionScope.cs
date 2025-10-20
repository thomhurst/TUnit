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

        if (_parent != null)
        {
            foreach (var exception in _exceptions)
            {
                _parent._exceptions.Add(exception);
            }

            return;
        }

        if (_exceptions.Count == 0)
        {
            return;
        }

        if (_exceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(_exceptions[0]).Throw();
        }

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

    internal bool HasExceptions => _exceptions.Count > 0;

    internal int ExceptionCount => _exceptions.Count;

    internal Exception GetFirstException()
    {
        return _exceptions.Count > 0 ? _exceptions[0] : throw new InvalidOperationException("No exceptions in scope");
    }

    internal Exception GetLastException()
    {
        return _exceptions.Count > 0 ? _exceptions[^1] : throw new InvalidOperationException("No exceptions in scope");
    }

    internal void RemoveLastExceptions(int count)
    {
        if (count > _exceptions.Count)
        {
            throw new InvalidOperationException($"Cannot remove {count} exceptions when only {_exceptions.Count} exist");
        }

        _exceptions.RemoveRange(_exceptions.Count - count, count);
    }
}
