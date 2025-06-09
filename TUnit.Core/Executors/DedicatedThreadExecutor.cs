namespace TUnit.Core;

public class DedicatedThreadExecutor : GenericAbstractExecutor
{
    private SpinWait _spinWait = new();

    protected sealed override async ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        var tcs = new TaskCompletionSource<object?>();

        var thread = new Thread(() =>
        {
            Exception? capturedException = null;

            try
            {
                Initialize();

                var previousContext = SynchronizationContext.Current;
                var dedicatedContext = new DedicatedThreadSynchronizationContext();

                SynchronizationContext.SetSynchronizationContext(dedicatedContext);
                TestContext.Current!.SynchronizationContext = dedicatedContext;

                try
                {
                    // Execute the action using our STA-safe async runner
                    ExecuteAsyncActionWithMessagePump(action, dedicatedContext, tcs);
                }
                catch (Exception e)
                {
                    capturedException = e;
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(previousContext);
                }
            }
            catch (Exception e)
            {
                capturedException = e;
            }
            finally
            {
                CleanUp();

                // Set result if not already set
                if (capturedException != null && !tcs.Task.IsCompleted)
                {
                    tcs.SetException(capturedException);
                }
            }
        });

        ConfigureThread(thread);
        thread.Start();

        await tcs.Task;
    }

    private void ExecuteAsyncActionWithMessagePump(Func<ValueTask> action, DedicatedThreadSynchronizationContext context, TaskCompletionSource<object?> tcs)
    {
        try
        {
            var taskScheduler = new DedicatedThreadTaskScheduler();

            // Execute the action with our custom task scheduler
            var task = Task.Factory.StartNew(async () =>
            {
                try
                {
                    await action().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    return;
                }

                tcs.SetResult(null);
            }, CancellationToken.None, TaskCreationOptions.None, taskScheduler).Unwrap();

            // Run a message pump to handle async continuations on this thread
            while (!task.IsCompleted)
            {
                // Process any pending work on this synchronization context
                context.ProcessPendingWork();

                // Process any pending tasks in our task scheduler
                taskScheduler.ProcessPendingTasks();

                _spinWait.SpinOnce();
            }
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }
    }protected virtual void ConfigureThread(Thread thread)
    {
    }

    protected virtual void Initialize()
    {
    }

    protected virtual void CleanUp()
    {
    }

    internal sealed class DedicatedThreadSynchronizationContext : SynchronizationContext
    {
        private readonly Thread _dedicatedThread;
        private readonly Queue<(SendOrPostCallback callback, object? state)> _workQueue = new();
        private readonly object _queueLock = new object();

        public DedicatedThreadSynchronizationContext()
        {
            _dedicatedThread = Thread.CurrentThread;
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (_queueLock)
            {
                _workQueue.Enqueue((d, state));
            }
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (Thread.CurrentThread == _dedicatedThread)
            {
                // We're on the correct thread, execute immediately
                d(state);
            }
            else
            {
                // We're on a different thread, queue the work and wait
                var waitHandle = new ManualResetEventSlim(false);
                Exception? exception = null;

                Post((s) =>
                {
                    try
                    {
                        d(state);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                    finally
                    {
                        waitHandle.Set();
                    }
                }, null);

                waitHandle.Wait();

                if (exception != null)
                {
                    throw exception;
                }
            }
        }

        public void ProcessPendingWork()
        {
            // Only the dedicated thread should call this
            if (Thread.CurrentThread != _dedicatedThread)
            {
                return;
            }

            while (true)
            {
                (SendOrPostCallback callback, object? state) work;

                lock (_queueLock)
                {
                    if (_workQueue.Count == 0)
                    {
                        break;
                    }

                    work = _workQueue.Dequeue();
                }

                try
                {
                    work.callback(work.state);
                }
                catch
                {
                    // Ignore exceptions in async continuations during processing
                    // The actual exception will be handled by the task
                }
            }
        }        public override SynchronizationContext CreateCopy()
        {
            return this; // Return the same instance to ensure continuity
        }
    }

    internal sealed class DedicatedThreadTaskScheduler : TaskScheduler
    {
        private readonly Thread _dedicatedThread;
        private readonly Queue<Task> _taskQueue = new();
        private readonly Lock _queueLock = new();

        public DedicatedThreadTaskScheduler()
        {
            _dedicatedThread = Thread.CurrentThread;
        }

        protected override void QueueTask(Task task)
        {
            lock (_queueLock)
            {
                _taskQueue.Enqueue(task);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // Only execute inline if we're on the dedicated thread
            if (Thread.CurrentThread != _dedicatedThread)
            {
                return false;
            }

            // If the task was previously queued, remove it from the queue
            if (taskWasPreviouslyQueued)
            {
                lock (_queueLock)
                {
                    // Try to remove from queue (may not be there anymore)
                    var tempQueue = new Queue<Task>();
                    while (_taskQueue.Count > 0)
                    {
                        var queuedTask = _taskQueue.Dequeue();
                        if (queuedTask != task)
                        {
                            tempQueue.Enqueue(queuedTask);
                        }
                    }
                    while (tempQueue.Count > 0)
                    {
                        _taskQueue.Enqueue(tempQueue.Dequeue());
                    }
                }
            }

            return TryExecuteTask(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            lock (_queueLock)
            {
                return _taskQueue.ToArray();
            }
        }

        public void ProcessPendingTasks()
        {
            // Only the dedicated thread should call this
            if (Thread.CurrentThread != _dedicatedThread)
            {
                return;
            }

            while (true)
            {
                Task? task;

                lock (_queueLock)
                {
                    if (_taskQueue.Count == 0)
                    {
                        break;
                    }

                    task = _taskQueue.Dequeue();
                }

                TryExecuteTask(task);
            }
        }

        public override int MaximumConcurrencyLevel => 1;
    }
}
