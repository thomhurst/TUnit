using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class DedicatedThreadExecutor : GenericAbstractExecutor, ITestRegisteredEventReceiver
{
    protected sealed override async ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        var tcs = new TaskCompletionSource<object?>();

        var thread = new Thread(() =>
        {
            Exception? capturedException = null;

            try
            {
                Initialize();

                try
                {
                    ExecuteAsyncActionWithMessagePump(action, tcs);
                }
                catch (Exception e)
                {
                    capturedException = e;
                }
            }
            catch (Exception e)
            {
                capturedException = e;
            }
            finally
            {
                CleanUp();

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

    private void ExecuteAsyncActionWithMessagePump(Func<ValueTask> action, TaskCompletionSource<object?> tcs)
    {
        try
        {
            var taskScheduler = new DedicatedThreadTaskScheduler();

            var previousContext = SynchronizationContext.Current;
            var dedicatedContext = new DedicatedThreadSynchronizationContext();

            SynchronizationContext.SetSynchronizationContext(dedicatedContext);

            try
            {
                Task.Factory.StartNew(
                    () =>
                    {
                        SynchronizationContext.SetSynchronizationContext(dedicatedContext);

                        var valueTask = action();

                        if (valueTask.IsCompletedSuccessfully)
                        {
                            tcs.SetResult(null);
                            return;
                        }

                        var task = valueTask.AsTask();

                        task.ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                tcs.SetException(t.Exception!);
                            }
                            else if (t.IsCanceled)
                            {
                                tcs.SetCanceled();
                            }
                            else
                            {
                                tcs.SetResult(null);
                            }
                        }, CancellationToken.None, TaskContinuationOptions.None, taskScheduler);
                    },
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    taskScheduler);

                // Message pump: process pending work until both the main task and all work items are complete
                while (!tcs.Task.IsCompleted)
                {
                    taskScheduler.ProcessPendingTasks();
                    dedicatedContext.ProcessPendingWork();
                    Thread.Sleep(1);
                }

                taskScheduler.ProcessPendingTasks();
                dedicatedContext.ProcessPendingWork();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previousContext);
            }
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }
    }

    protected virtual void ConfigureThread(Thread thread)
    {
    }

    protected virtual void Initialize()
    {
    }

    protected virtual void CleanUp()
    {
    }

    internal sealed class DedicatedThreadTaskScheduler : TaskScheduler
    {
        private readonly Thread _dedicatedThread = Thread.CurrentThread;
        private readonly List<Task> _taskQueue = new();
        private readonly Lock _queueLock = new();

        protected override void QueueTask(Task task)
        {
            lock (_queueLock)
            {
                _taskQueue.Add(task);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (Thread.CurrentThread != _dedicatedThread)
            {
                return false;
            }

            if (taskWasPreviouslyQueued)
            {
                lock (_queueLock)
                {
                    if (_taskQueue.Contains(task))
                    {
                        _taskQueue.Remove(task);
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
            if (Thread.CurrentThread != _dedicatedThread)
            {
                throw new InvalidOperationException("ProcessPendingTasks can only be called from the dedicated thread.");
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

                    task = _taskQueue[0];
                    _taskQueue.RemoveAt(0);
                }

                TryExecuteTask(task);
            }
        }

        public override int MaximumConcurrencyLevel => 1;
    }

    internal sealed class DedicatedThreadSynchronizationContext : SynchronizationContext
    {
        private readonly Thread _dedicatedThread;
        private readonly Queue<(SendOrPostCallback callback, object? state)> _workQueue = new();
        private readonly Lock _queueLock = new();

        public DedicatedThreadSynchronizationContext()
        {
            _dedicatedThread = Thread.CurrentThread;
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            if (Thread.CurrentThread == _dedicatedThread)
            {
                // We're already on the dedicated thread, execute immediately
                d(state);
            }
            else
            {
                // Queue the work to be executed on the dedicated thread
                lock (_queueLock)
                {
                    _workQueue.Enqueue((d, state));
                }
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
                // For Send, we need to block until completion
                // This is less ideal but necessary for the Send semantics
                var tcs = new TaskCompletionSource<object?>();

                Post(_ =>
                {
                    try
                    {
                        d(state);
                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }, null);

                // Wait for completion (this will block)
                tcs.Task.GetAwaiter().GetResult();
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
                (SendOrPostCallback callback, object? state) workItem;

                lock (_queueLock)
                {
                    if (_workQueue.Count == 0)
                    {
                        break;
                    }

                    workItem = _workQueue.Dequeue();
                }

                try
                {
                    workItem.callback(workItem.state);
                }
                catch
                {
                    // Swallow exceptions in work items to avoid crashing the message pump
                    // The exception will be handled by the async machinery
                }
            }
        }

        public override SynchronizationContext CreateCopy()
        {
            return this; // Return the same instance to maintain thread affinity
        }
    }

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetParallelLimiter(new ProcessorCountParallelLimit());

        return default;
    }
}
