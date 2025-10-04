using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class DedicatedThreadExecutor : GenericAbstractExecutor, ITestRegisteredEventReceiver
{
    protected sealed override async ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        // On browser platforms, threading is not supported, so fall back to direct execution
#if NET5_0_OR_GREATER
        if (OperatingSystem.IsBrowser())
        {
            await action();
            return;
        }
#endif

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
            var taskScheduler = new DedicatedThreadTaskScheduler(Thread.CurrentThread);
            var dedicatedContext = new DedicatedThreadSynchronizationContext(taskScheduler);

            SynchronizationContext.SetSynchronizationContext(dedicatedContext);

            try
            {
                var task = Task.Factory.StartNew(async () =>
                {
                    // Inside this task, TaskScheduler.Current will be our scheduler
                    await action();
                }, CancellationToken.None, TaskCreationOptions.None, taskScheduler).Unwrap();

                // Try fast path first - many tests complete quickly
                // Use IsCompleted to avoid synchronous wait
                if (task.IsCompleted)
                {
                    HandleTaskCompletion(task, tcs);
                    return;
                }

                // Pump messages until the task completes with optimized waiting
                var deadline = DateTime.UtcNow.AddMinutes(5);
                var spinWait = new SpinWait();
                var lastTimeCheck = DateTime.UtcNow;
                const int TimeCheckIntervalMs = 100;
                
                while (!task.IsCompleted)
                {
                    var hadWork = dedicatedContext.ProcessPendingWork();
                    hadWork |= taskScheduler.ProcessPendingTasks();

                    if (!hadWork)
                    {
                        // No work available, use efficient waiting
                        if (spinWait.Count < 10)
                        {
                            spinWait.SpinOnce();
                        }
                        else
                        {
                            // After initial spins, yield to other threads
                            Thread.Yield();
                            if (spinWait.Count > 100)
                            {
                                // After many iterations, do a brief sleep
                                Thread.Sleep(0);
                                spinWait.Reset();
                            }
                        }
                    }
                    else
                    {
                        // Had work, reset spin counter
                        spinWait.Reset();
                    }

                    // Check timeout periodically instead of every iteration
                    var now = DateTime.UtcNow;
                    if ((now - lastTimeCheck).TotalMilliseconds >= TimeCheckIntervalMs)
                    {
                        if (now >= deadline)
                        {
                            tcs.SetException(new TimeoutException("Async operation timed out after 5 minutes"));
                            return;
                        }
                        lastTimeCheck = now;
                    }
                }

                HandleTaskCompletion(task, tcs);
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

    private static void HandleTaskCompletion(Task task, TaskCompletionSource<object?> tcs)
    {
        if (task.IsFaulted)
        {
            tcs.SetException(task.Exception!.InnerExceptions.Count == 1 
                ? task.Exception.InnerException! 
                : task.Exception);
        }
        else if (task.IsCanceled)
        {
            tcs.SetCanceled();
        }
        else
        {
            tcs.SetResult(null);
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
        private readonly Thread _dedicatedThread;
        private readonly List<Task> _taskQueue =
        [
        ];
        private readonly Lock _queueLock = new();

        public DedicatedThreadTaskScheduler(Thread dedicatedThread)
        {
            _dedicatedThread = dedicatedThread;
        }

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

        public bool ProcessPendingTasks()
        {
            if (Thread.CurrentThread != _dedicatedThread)
            {
                throw new InvalidOperationException("ProcessPendingTasks can only be called from the dedicated thread.");
            }

            var hadWork = false;
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
                    hadWork = true;
                }

                TryExecuteTask(task);
            }
            return hadWork;
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
                // Use Task.Run to avoid potential deadlocks by ensuring we don't capture any synchronization context
                var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

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

                // Use a more robust synchronous wait pattern to avoid deadlocks
                // We use Task.Run to ensure we don't capture the current SynchronizationContext
                // which is a common cause of deadlocks
                var waitTask = Task.Run(async () =>
                {
                    // For .NET Standard 2.0 compatibility, use Task.Delay for timeout
                    var timeoutTask = Task.Delay(TimeSpan.FromMinutes(30));
                    var completedTask = await Task.WhenAny(tcs.Task, timeoutTask).ConfigureAwait(false);
                    
                    if (completedTask == timeoutTask)
                    {
                        throw new TimeoutException("Synchronous operation on dedicated thread timed out after 30 minutes");
                    }
                    
                    // Await the actual task to get its result or exception
                    await tcs.Task.ConfigureAwait(false);
                });
                
                // This wait is safe because it's on a Task.Run thread without SynchronizationContext
                waitTask.GetAwaiter().GetResult();
            }
        }

        public bool ProcessPendingWork()
        {
            // Only the dedicated thread should call this
            if (Thread.CurrentThread != _dedicatedThread)
            {
                return false;
            }

            var hadWork = false;
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
                    hadWork = true;
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
            return hadWork;
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }
    }

#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Type comes from runtime objects that cannot be annotated")]
#endif
    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetParallelLimiter(new ProcessorCountParallelLimit());
        return default(ValueTask);
    }
}
