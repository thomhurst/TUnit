using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Playwright;

public class WorkerAwareTest : ITestRegisteredEventReceiver
{
    private class Worker
    {
        private static int _lastWorkedIndex = 0;
        public readonly int WorkerIndex = Interlocked.Increment(ref _lastWorkedIndex);
        public readonly Dictionary<string, IWorkerService> Services = [];
    }

    public virtual bool UseDefaultParallelLimiter => true;

    private static readonly ConcurrentStack<Worker> AllWorkers = [];
    private Worker _currentWorker = null!;

    public int WorkerIndex { get; internal set; }

    public async Task<T> RegisterService<T>(string name, Func<Task<T>> factory) where T : class, IWorkerService
    {
        if (!_currentWorker.Services.TryGetValue(name, out var existing))
        {
            var service = await factory().ConfigureAwait(false);
            _currentWorker.Services[name] = service;
            return service;
        }

        return (existing as T)!;
    }

    [Before(HookType.Test, "", 0)]
    public void WorkerSetup()
    {
        if (!AllWorkers.TryPop(out _currentWorker!))
        {
            _currentWorker = new();
        }

        WorkerIndex = _currentWorker.WorkerIndex;
    }

    [After(HookType.Test, "", 0)]
    public async Task WorkerTeardown(TestContext testContext)
    {
        var worker = _currentWorker;

        if (worker == null)
        {
            return;
        }

        if (TestOk(testContext))
        {
            try
            {
                foreach (var kv in worker.Services)
                {
                    await kv.Value.ResetAsync().ConfigureAwait(false);
                }

                AllWorkers.Push(worker);
            }
            catch
            {
                await DisposeAllServicesAsync(worker).ConfigureAwait(false);
                throw;
            }
        }
        else
        {
            await DisposeAllServicesAsync(worker).ConfigureAwait(false);
        }
    }

    private static async Task DisposeAllServicesAsync(Worker worker)
    {
        List<Exception>? exceptions = null;

        foreach (var kv in worker.Services)
        {
            try
            {
                await kv.Value.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        worker.Services.Clear();

        if (exceptions is { Count: > 0 })
        {
            throw new AggregateException("One or more worker services failed to dispose.", exceptions);
        }
    }

    protected bool TestOk(TestContext testContext)
    {
        return testContext.Execution.Result?.State is TestState.Passed or TestState.Skipped;
    }

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        if (UseDefaultParallelLimiter)
        {
            context.SetParallelLimiter(new DefaultPlaywrightParallelLimiter());
        }

        return default(ValueTask);
    }

    int IEventReceiver.Order => 0;
}
