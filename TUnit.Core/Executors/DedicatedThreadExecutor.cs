namespace TUnit.Core;

public class DedicatedThreadExecutor : GenericAbstractExecutor
{
    protected sealed override async ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        var tcs = new TaskCompletionSource<object?>();
        var thread = new Thread(() =>
        {
            try
            {
                Initialize();

                // Set a custom SynchronizationContext that keeps all continuations on this thread
                var previousContext = SynchronizationContext.Current;
                var dedicatedContext = new DedicatedThreadSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(dedicatedContext);

                try
                {
                    var valueTask = action();

                    if (!valueTask.IsCompletedSuccessfully)
                    {
                        valueTask.GetAwaiter().GetResult();
                    }

                    // Process any remaining work posted to our context
                    dedicatedContext.RunToCompletion();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }

                tcs.SetResult(null);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
            finally
            {
                CleanUp();
            }
        });

        ConfigureThread(thread);

        thread.Start();

        await tcs.Task;
    }

    protected virtual void ConfigureThread(Thread thread)
    {
    }    protected virtual void Initialize()
    {
    }

    protected virtual void CleanUp()
    {
    }

    internal sealed class DedicatedThreadSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback callback, object? state)> _workQueue = new();
        private readonly Lock _lock = new();
        private readonly Thread _dedicatedThread = Thread.CurrentThread;

        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (_lock)
            {
                _workQueue.Enqueue((d, state));
            }
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (Thread.CurrentThread == _dedicatedThread)
            {
                // We're already on the dedicated thread, execute immediately
                d(state);
            }
            else
            {
                // We're on a different thread, this shouldn't happen in our scenario
                // but handle it by posting to the queue
                Post(d, state);
            }
        }

        public void RunToCompletion()
        {
            while (true)
            {
                (SendOrPostCallback callback, object? state) work;

                lock (_lock)
                {
                    if (_workQueue.Count == 0)
                    {
                        break;
                    }

                    work = _workQueue.Dequeue();
                }

                work.callback(work.state);
            }
        }
    }
}
