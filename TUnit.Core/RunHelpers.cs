using TUnit.Core.Exceptions;
using TimeoutException = TUnit.Core.Exceptions.TimeoutException;

namespace TUnit.Core;

internal static class RunHelpers
{
    internal static async Task RunWithTimeoutAsync(Func<CancellationToken, Task> taskDelegate, TimeSpan? timeout)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(EngineCancellationToken.Token);

        var cancellationToken = cancellationTokenSource.Token;

        var taskCompletionSource = new TaskCompletionSource();

        var task = taskDelegate(cancellationToken);

        _ = task.ContinueWith(async t =>
        {
            try
            {
                await t;
                taskCompletionSource.TrySetResult();
            }
            catch (Exception e)
            {
                taskCompletionSource.TrySetException(e);
            }
        }, CancellationToken.None);

        CancellationTokenRegistration? cancellationTokenRegistration = null;
        if (cancellationToken.CanBeCanceled)
        {
            cancellationTokenRegistration = cancellationToken.Register(() =>
            {
                if (EngineCancellationToken.Token.IsCancellationRequested)
                {
                    taskCompletionSource.TrySetException(new TestRunCanceledException());
                    return;
                }

                if (timeout.HasValue)
                {
                    taskCompletionSource.TrySetException(new TimeoutException(timeout.Value));
                }
                else
                {
                    taskCompletionSource.TrySetCanceled(cancellationToken);
                }
            });
        }

        if (timeout != null)
        {
            cancellationTokenSource.CancelAfter(timeout.Value);
        }

        await using (cancellationTokenRegistration)
        {
            await taskCompletionSource.Task;
        }
    }

    public static async Task RunSafelyAsync(Func<Task> action, List<Exception> exceptions)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            exceptions.Add(exception);
        }
    }

    public static async Task RunValueTaskSafelyAsync(Func<ValueTask> action, List<Exception> exceptions)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            exceptions.Add(exception);
        }
    }
}