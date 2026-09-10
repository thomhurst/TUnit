using System.Runtime.ExceptionServices;
using System.Text;
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
    private readonly Lock _exceptionsLock = new();

    internal AssertionScope()
    {
        _parent = GetCurrentAssertionScope();
        SetCurrentAssertionScope(this);
    }

    // Chains and pre-work must inspect only their own failures across awaits.
    // The child merges its final failures into the shared parent when disposed.
    internal static AssertionScope? CreateIsolatedScope()
    {
        return GetCurrentAssertionScope() is null ? null : new AssertionScope();
    }

    public void Dispose()
    {
        SetCurrentAssertionScope(_parent);

        Exception[] exceptions;
        lock (_exceptionsLock)
        {
            exceptions = _exceptions.ToArray();
        }

        if (exceptions.Length == 0)
        {
            return;
        }

        if (_parent != null)
        {
            lock (_parent._exceptionsLock)
            {
                _parent._exceptions.AddRange(exceptions);
            }

            return;
        }

        if (exceptions.Length == 1)
        {
            ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
        }

        // Use StringBuilder for message concatenation instead of LINQ
        var sb = new StringBuilder();
        for (int i = 0; i < exceptions.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(Environment.NewLine).Append(Environment.NewLine);
            }
            sb.Append(exceptions[i].Message);
        }
        var message = sb.ToString();
        throw new AssertionException(message, new AggregateException(exceptions));
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
        lock (_exceptionsLock)
        {
            _exceptions.Add(exception);
        }
    }

    internal bool HasExceptions => ExceptionCount > 0;

    internal int ExceptionCount
    {
        get
        {
            lock (_exceptionsLock)
            {
                return _exceptions.Count;
            }
        }
    }

    internal Exception GetFirstException()
    {
        lock (_exceptionsLock)
        {
            return _exceptions.Count > 0 ? _exceptions[0] : throw new InvalidOperationException("No exceptions in scope");
        }
    }

    internal Exception GetLastException()
    {
        lock (_exceptionsLock)
        {
            return _exceptions.Count > 0 ? _exceptions[^1] : throw new InvalidOperationException("No exceptions in scope");
        }
    }

    internal void RemoveLastExceptions(int count)
    {
        lock (_exceptionsLock)
        {
            if (count > _exceptions.Count)
            {
                throw new InvalidOperationException($"Cannot remove {count} exceptions when only {_exceptions.Count} exist");
            }

            _exceptions.RemoveRange(_exceptions.Count - count, count);
        }
    }
}
