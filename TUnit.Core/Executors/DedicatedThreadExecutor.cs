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
            var previousContext = SynchronizationContext.Current;
            var taskScheduler = new DedicatedThreadTaskScheduler();
            var dedicatedContext = new DedicatedThreadSynchronizationContext(taskScheduler);

            SynchronizationContext.SetSynchronizationContext(dedicatedContext);

            try
            {
                var task = Task.Factory.StartNew(async () =>
                {
                    // Inside this task, TaskScheduler.Current will be our scheduler
                    await action();
                }, CancellationToken.None, TaskCreationOptions.None, taskScheduler).Unwrap();

                // Pump messages until the task completes
                var deadline = DateTime.UtcNow.AddMinutes(5);

                while (!task.IsCompleted && DateTime.UtcNow < deadline)
                {
                    dedicatedContext.ProcessPendingWork();

                    taskScheduler.ProcessPendingTasks();

                    Thread.Sleep(1);
                }

                if (!task.IsCompleted)
                {
                    tcs.SetException(new TimeoutException("Async operation timed out after 5 minutes"));
                    return;
                }

                try
                {
                    task.GetAwaiter().GetResult();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
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
            // ALWAYS execute inline if we're on the dedicated thread, regardless of context
            // This is crucial for capturing continuations that would otherwise escape
            if (Thread.CurrentThread == _dedicatedThread)
            {
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

            // If we're not on the dedicated thread, queue it to be executed later
            if (!taskWasPreviouslyQueued)
            {
                QueueTask(task);
            }
            return false;
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
        private readonly DedicatedThreadTaskScheduler _taskScheduler;
        private readonly Queue<(SendOrPostCallback callback, object? state)> _workQueue = new();
        private readonly Lock _queueLock = new();

        public DedicatedThreadSynchronizationContext(DedicatedThreadTaskScheduler taskScheduler)
        {
            _dedicatedThread = Thread.CurrentThread;
            _taskScheduler = taskScheduler;
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            // Always queue the work to ensure it runs on the dedicated thread
            lock (_queueLock)
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
