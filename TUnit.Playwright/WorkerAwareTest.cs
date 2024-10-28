using System.Collections.Concurrent;
using TUnit.Core;
using TUnit.Core.Enums;

namespace TUnit.Playwright;

public class WorkerAwareTest
{
    internal class Worker
    {
        private static int _lastWorkedIndex = 0;
        public int WorkerIndex = Interlocked.Increment(ref _lastWorkedIndex);
        public Dictionary<string, IWorkerService> Services = new();
    }

    private static readonly ConcurrentStack<Worker> AllWorkers = new();
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
    public async Task WorkerTeardown()
    {
        if (TestOk())
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

    public bool TestOk()
    {
        return TestContext.Current?.Result?.Status is Status.Passed or Status.Skipped;
    }
}
