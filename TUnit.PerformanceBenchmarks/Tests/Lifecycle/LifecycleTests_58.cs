using TUnit.Core.Interfaces;

namespace TUnit.PerformanceBenchmarks.Tests.Lifecycle;

public class LifecycleTests_58 : IAsyncInitializer, IAsyncDisposable
{
    private int _initialized;

    public Task InitializeAsync()
    {
        _initialized = 1;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _initialized = 0;
        return ValueTask.CompletedTask;
    }

    [Before(Test)]
    public void BeforeEach() { _ = _initialized; }

    [After(Test)]
    public void AfterEach() { _ = _initialized; }

    [Test]
    [Arguments(1), Arguments(2), Arguments(3), Arguments(4), Arguments(5)]
    public void LifecycleTest_01(int v) { _ = v + _initialized; }

    [Test]
    [Arguments(1), Arguments(2), Arguments(3), Arguments(4), Arguments(5)]
    public void LifecycleTest_02(int v) { _ = v + _initialized; }
}
