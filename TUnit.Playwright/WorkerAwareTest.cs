using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Enums;
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
        if (!_currentWorker.Services.ContainsKey(name))
        {
            _currentWorker.Services[name] = await factory().ConfigureAwait(false);
        }

        return (_currentWorker.Services[name] as T)!;
    }

    [Before(HookType.Test)]
    public void WorkerSetup()
    {
        if (!AllWorkers.TryPop(out _currentWorker!))
        {
            _currentWorker = new();
        }
        
        WorkerIndex = _currentWorker.WorkerIndex;
    }

    [After(HookType.Test)]
    public async Task WorkerTeardown(TestContext testContext)
    {
        if (TestOk(testContext))
        {
            foreach (var kv in _currentWorker.Services)
            {
                await kv.Value.ResetAsync().ConfigureAwait(false);
            }
            
            AllWorkers.Push(_currentWorker);
        }
        else
        {
            foreach (var kv in _currentWorker.Services)
            {
                await kv.Value.DisposeAsync().ConfigureAwait(false);
            }
            
            _currentWorker.Services.Clear();
        }
    }

    protected bool TestOk(TestContext testContext)
    {
        return testContext.Result?.Status is Status.Passed or Status.Skipped;
    }

    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        if (UseDefaultParallelLimiter)
        {
            context.SetParallelLimiter(new DefaultPlaywrightParallelLimiter());
        }
        
        return ValueTask.CompletedTask;
    }
}
