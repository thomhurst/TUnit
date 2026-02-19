using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class DedicatedThreadExecutor : GenericAbstractExecutor, ITestRegisteredEventReceiver
{
    protected sealed override ValueTask ExecuteAsync(Func<ValueTask> action)
    {
        // On browser platforms, threading is not supported, so fall back to direct execution
#if NET5_0_OR_GREATER
        if (OperatingSystem.IsBrowser())
        {
            return action();
        }
#endif

        var tcs = new TaskCompletionSource<object?>();

        var thread = new Thread(static state =>
        {
            var (threadExecutor, action, tcs) = (ValueTuple<DedicatedThreadExecutor, Func<ValueTask>, TaskCompletionSource<object?>>)state!;
            Exception? capturedException = null;

            try
            {
                threadExecutor.Initialize();

                try
                {
                    threadExecutor.ExecuteAsyncActionWithMessagePump(action, tcs);
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
                threadExecutor.CleanUp();

                if (capturedException != null && !tcs.Task.IsCompleted)
                {
                    tcs.SetException(capturedException);
                }
            }
        });

        var state = (this, action, tcs);

        ConfigureThread(thread);
        thread.Start(state);

        return new ValueTask(tcs.Task);
    }

    private void ExecuteAsyncActionWithMessagePump(Func<ValueTask> action, TaskCompletionSource<object?> tcs)
    {
        try
        {
            var previousContext = SynchronizationContext.Current;
            ManualResetEventSlim? workAvailableEvent = null;
#if NET5_0_OR_GREATER
            if (!OperatingSystem.IsBrowser())
            {
                workAvailableEvent = new ManualResetEventSlim(false);
            }
#else
            workAvailableEvent = new ManualResetEventSlim(false);
#endif
            var taskScheduler = new DedicatedThreadTaskScheduler(Thread.CurrentThread, workAvailableEvent);
            var dedicatedContext = new DedicatedThreadSynchronizationContext(workAvailableEvent);

            SynchronizationContext.SetSynchronizationContext(dedicatedContext);

            try
            {
                var task = Task.Factory.StartNew(
                    static action => ((Func<ValueTask>)action!)().AsTask(),
                    action, CancellationToken.None, TaskCreationOptions.None, taskScheduler)
                    .Unwrap();

                // Try fast path first - many tests complete quickly
                // Use IsCompleted to avoid synchronous wait
                if (task.IsCompleted)
                {
                    HandleTaskCompletion(task, tcs);
                    return;
                }

                // Pump messages until the task completes with event-driven signaling
                var deadline = DateTime.UtcNow.AddMinutes(5);
                var spinWait = new SpinWait();
                const int MaxSpinCount = 50;
                const int WaitTimeoutMs = 100;

                while (!task.IsCompleted)
                {
                    var hadWork = dedicatedContext.ProcessPendingWork();
                    hadWork |= taskScheduler.ProcessPendingTasks();

                    if (!hadWork)
                    {
                        // Fast path: spin briefly for immediate continuations
                        if (spinWait.Count < MaxSpinCount)
                        {
                            spinWait.SpinOnce();
                        }
                        else
                        {
#if NET5_0_OR_GREATER
                            if (workAvailableEvent != null && !OperatingSystem.IsBrowser())
                            {
                                // No work after spinning - use event-driven wait (eliminates Thread.Sleep)
                                // Thread blocks efficiently in kernel, wakes instantly when work queued
                                workAvailableEvent.Wait(WaitTimeoutMs);
                                workAvailableEvent.Reset();
                                spinWait.Reset();
                            }
                            else
                            {
                                // Fallback for browser or null event
                                Thread.Yield();
                                spinWait.Reset();
                            }
#else
                            if (workAvailableEvent != null)
                            {
                                workAvailableEvent.Wait(WaitTimeoutMs);
                                workAvailableEvent.Reset();
                                spinWait.Reset();
                            }
                            else
                            {
                                Thread.Yield();
                                spinWait.Reset();
                            }
#endif
                            // Check timeout after waiting
                            if (DateTime.UtcNow >= deadline)
                            {
                                tcs.SetException(new TimeoutException("Async operation timed out after 5 minutes"));
                                return;
                            }
                        }
                    }
                    else
                    {
                        // Had work, reset spin counter
                        spinWait.Reset();
                    }
                }

                HandleTaskCompletion(task, tcs);
            }
            finally
            {
                workAvailableEvent?.Dispose();
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
        private readonly ManualResetEventSlim? _workAvailableEvent;
        private readonly Lock _queueLock = new();
        private readonly List<Task> _taskQueue =
        [
        ];

        public DedicatedThreadTaskScheduler(Thread dedicatedThread, ManualResetEventSlim? workAvailableEvent)
        {
            _dedicatedThread = dedicatedThread;
            _workAvailableEvent = workAvailableEvent;
        }

        protected override void QueueTask(Task task)
        {
            lock (_queueLock)
            {
                _taskQueue.Add(task);
            }
            // Signal that work is available (wake message pump immediately)
            _workAvailableEvent?.Set();
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
        private Queue<(SendOrPostCallback callback, object? state)>? _workQueue = null;
        private readonly Thread _dedicatedThread;
        private readonly ManualResetEventSlim? _workAvailableEvent;
        private readonly Lock _queueLock = new();

        public DedicatedThreadSynchronizationContext(ManualResetEventSlim? workAvailableEvent)
        {
            _dedicatedThread = Thread.CurrentThread;
            _workAvailableEvent = workAvailableEvent;
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            // Always queue the work to ensure it runs on the dedicated thread
            lock (_queueLock)
            {
                _workQueue ??= new();
                _workQueue.Enqueue((d, state));
            }
            // Signal that work is available (wake message pump immediately)
            _workAvailableEvent?.Set();
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
                    var timeoutTask = Task.Delay(Defaults.TestTimeout);
                    var completedTask = await Task.WhenAny(tcs.Task, timeoutTask).ConfigureAwait(false);

                    if (completedTask == timeoutTask)
                    {
                        throw new TimeoutException($"Synchronous operation on dedicated thread timed out after {Defaults.TestTimeout.TotalMinutes} minutes");
                    }

                    // Await the actual task to get its result or exception
                    await tcs.Task.ConfigureAwait(false);
                });

                // This blocking wait is intentional and safe from deadlocks because:
                // 1. We verified above that the current thread is NOT the dedicated thread
                // 2. The work is posted to the dedicated thread's queue via Post()
                // 3. waitTask runs via Task.Run without a SynchronizationContext, so no context capture
                // 4. SynchronizationContext.Send is synchronous by API contract — blocking is required
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
                    if (_workQueue == null || _workQueue.Count == 0)
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

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetParallelLimiter(new ProcessorCountParallelLimit());
        return default(ValueTask);
    }
}
