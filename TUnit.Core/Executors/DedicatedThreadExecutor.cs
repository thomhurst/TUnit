using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public class DedicatedThreadExecutor : GenericAbstractExecutor, ITestRegisteredEventReceiver
{
    private static readonly DedicatedThreadTaskScheduler _dedicatedThreadTaskScheduler = new();

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
                    ExecuteAsyncActionWithMessagePump(action, _dedicatedThreadTaskScheduler, tcs);
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

    private void ExecuteAsyncActionWithMessagePump(Func<ValueTask> action, DedicatedThreadTaskScheduler taskScheduler, TaskCompletionSource<object?> tcs)
    {
        try
        {
            var valueTask = action();

            if (valueTask.IsCompletedSuccessfully)
            {
                tcs.SetResult(null);
                taskScheduler.ProcessPendingTasks();
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

            // Message pump: process pending work until both the main task and all work items are complete
            while (!tcs.Task.IsCompleted)
            {
                taskScheduler.ProcessPendingTasks();
                Thread.Sleep(1);
            }

            taskScheduler.ProcessPendingTasks();
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
            // Only execute inline if we're on the dedicated thread
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

                    task = _taskQueue[0];
                    _taskQueue.RemoveAt(0);
                }

                TryExecuteTask(task);
            }
        }

        public override int MaximumConcurrencyLevel => 1;
    }

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetTaskScheduler(_dedicatedThreadTaskScheduler);
        context.SetParallelLimiter(new ProcessorCountParallelLimit());

        return default;
    }
}
