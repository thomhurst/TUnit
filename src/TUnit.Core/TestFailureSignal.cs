using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

internal sealed class TestFailureSignal(CancellationToken cancellationToken) : ITestFailureSignal, IDisposable
{
    private readonly object _lock = new();
    private readonly CancellationTokenSource _cancellationTokenSource =
        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    private readonly TaskCompletionSource<Exception> _failureSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Exception? _exception;
    private bool _closed;

    internal CancellationToken CancellationToken => _cancellationTokenSource.Token;
    internal Task<Exception> FailureTask => _failureSource.Task;

    public void Report(Exception exception)
    {
        TryReport(exception);
    }

    public void Report(string reason)
    {
        TryReport(reason);
    }

    public bool TryReport(Exception exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        lock (_lock)
        {
            if (_closed || _exception is not null)
            {
                return false;
            }

            _exception = exception;
            _failureSource.TrySetResult(exception);
        }

        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch (Exception ex) when (ex is AggregateException or ObjectDisposedException)
        {
            // Cancellation callback exceptions must not escape on the reporting thread.
        }

        return true;
    }

    public bool TryReport(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Failure reason cannot be empty or whitespace.", nameof(reason));
        }

        return TryReport(new FailTestException(reason));
    }

    internal Exception? Complete()
    {
        lock (_lock)
        {
            _closed = true;
            return _exception;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _closed = true;
        }

        _cancellationTokenSource.Dispose();
    }
}
