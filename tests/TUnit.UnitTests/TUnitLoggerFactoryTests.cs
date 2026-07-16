using TUnit.Core.Logging;

namespace TUnit.UnitTests;

[NotInParallel]
public class TUnitLoggerFactoryTests
{
    [Before(Test)]
    public void SetUp()
    {
        // Ensure clean state before each test
        TUnitLoggerFactory.Clear();
    }

    [After(Test)]
    public async Task TearDown()
    {
        // Ensure clean state after each test
        await TUnitLoggerFactory.DisposeAllAsync();
    }

    [Test]
    public async Task AddSink_RegistersSink()
    {
        var sink = new MockLogSink();

        TUnitLoggerFactory.AddSink(sink);

        var sinks = TUnitLoggerFactory.GetSinks();
        await Assert.That(sinks).Count().IsEqualTo(1);
        await Assert.That(sinks[0]).IsSameReferenceAs(sink);
    }

    [Test]
    public async Task AddSink_Generic_InstantiatesAndRegisters()
    {
        TUnitLoggerFactory.AddSink<MockLogSink>();

        var sinks = TUnitLoggerFactory.GetSinks();
        await Assert.That(sinks).Count().IsEqualTo(1);
        await Assert.That(sinks[0]).IsTypeOf<MockLogSink>();
    }

    [Test]
    public async Task GetSinks_ReturnsEmptyList_WhenNoSinksRegistered()
    {
        var sinks = TUnitLoggerFactory.GetSinks();

        await Assert.That(sinks).IsEmpty();
    }

    [Test]
    public async Task Clear_RemovesAllSinks()
    {
        var sink1 = new MockLogSink();
        var sink2 = new MockLogSink();
        TUnitLoggerFactory.AddSink(sink1);
        TUnitLoggerFactory.AddSink(sink2);

        TUnitLoggerFactory.Clear();

        var sinks = TUnitLoggerFactory.GetSinks();
        await Assert.That(sinks).IsEmpty();
    }

    [Test]
    public async Task Clear_DoesNotDisposeSinks()
    {
        var disposableSink = new DisposableMockSink();
        TUnitLoggerFactory.AddSink(disposableSink);

        TUnitLoggerFactory.Clear();

        await Assert.That(disposableSink.Disposed).IsFalse();
    }

    [Test]
    public async Task DisposeAllAsync_DisposesAsyncDisposable()
    {
        var asyncDisposableSink = new AsyncDisposableMockSink();
        TUnitLoggerFactory.AddSink(asyncDisposableSink);

        await TUnitLoggerFactory.DisposeAllAsync();

        await Assert.That(asyncDisposableSink.Disposed).IsTrue();
    }

    [Test]
    public async Task DisposeAllAsync_DisposesDisposable()
    {
        var disposableSink = new DisposableMockSink();
        TUnitLoggerFactory.AddSink(disposableSink);

        await TUnitLoggerFactory.DisposeAllAsync();

        await Assert.That(disposableSink.Disposed).IsTrue();
    }

    [Test]
    public async Task DisposeAllAsync_ClearsSinksList()
    {
        var sink = new MockLogSink();
        TUnitLoggerFactory.AddSink(sink);

        await TUnitLoggerFactory.DisposeAllAsync();

        var sinks = TUnitLoggerFactory.GetSinks();
        await Assert.That(sinks).IsEmpty();
    }

    [Test]
    public async Task DisposeAllAsync_ContinuesOnError()
    {
        var faultySink = new FaultyDisposableSink();
        var goodSink = new AsyncDisposableMockSink();
        TUnitLoggerFactory.AddSink(faultySink);
        TUnitLoggerFactory.AddSink(goodSink);

        // Should not throw even though faultySink throws
        await TUnitLoggerFactory.DisposeAllAsync();

        // Verify the second sink was still disposed despite first one failing
        await Assert.That(goodSink.Disposed).IsTrue();
    }

    [Test]
    public async Task DisposeAllAsync_PrefersAsyncDisposableOverDisposable()
    {
        var dualDisposableSink = new DualDisposableMockSink();
        TUnitLoggerFactory.AddSink(dualDisposableSink);

        await TUnitLoggerFactory.DisposeAllAsync();

        // Should use async dispose, not sync dispose
        await Assert.That(dualDisposableSink.AsyncDisposed).IsTrue();
        await Assert.That(dualDisposableSink.SyncDisposed).IsFalse();
    }

    [Test]
    public async Task AddSink_MultipleSinks_AllAreRegistered()
    {
        var sink1 = new MockLogSink();
        var sink2 = new MockLogSink();
        var sink3 = new MockLogSink();

        TUnitLoggerFactory.AddSink(sink1);
        TUnitLoggerFactory.AddSink(sink2);
        TUnitLoggerFactory.AddSink(sink3);

        var sinks = TUnitLoggerFactory.GetSinks();
        await Assert.That(sinks).Count().IsEqualTo(3);
    }

    [Test]
    public async Task GetSinks_ReturnsSnapshot_NotLiveList()
    {
        var sink1 = new MockLogSink();
        TUnitLoggerFactory.AddSink(sink1);

        var sinks = TUnitLoggerFactory.GetSinks();

        // Add another sink after getting the snapshot
        var sink2 = new MockLogSink();
        TUnitLoggerFactory.AddSink(sink2);

        // Original snapshot should not be affected
        await Assert.That(sinks).Count().IsEqualTo(1);

        // New call should show both
        var newSinks = TUnitLoggerFactory.GetSinks();
        await Assert.That(newSinks).Count().IsEqualTo(2);
    }

    #region Mock Sinks

    private class MockLogSink : ILogSink
    {
        public bool IsEnabled(LogLevel level) => true;

        public void Log(LogLevel level, string message, Exception? exception, Context? context)
        {
        }

        public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
        {
            return ValueTask.CompletedTask;
        }
    }

    private class DisposableMockSink : ILogSink, IDisposable
    {
        public bool Disposed { get; private set; }

        public bool IsEnabled(LogLevel level) => true;

        public void Log(LogLevel level, string message, Exception? exception, Context? context)
        {
        }

        public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
        {
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private class AsyncDisposableMockSink : ILogSink, IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public bool IsEnabled(LogLevel level) => true;

        public void Log(LogLevel level, string message, Exception? exception, Context? context)
        {
        }

        public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private class DualDisposableMockSink : ILogSink, IAsyncDisposable, IDisposable
    {
        public bool AsyncDisposed { get; private set; }
        public bool SyncDisposed { get; private set; }

        public bool IsEnabled(LogLevel level) => true;

        public void Log(LogLevel level, string message, Exception? exception, Context? context)
        {
        }

        public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            AsyncDisposed = true;
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            SyncDisposed = true;
        }
    }

    private class FaultyDisposableSink : ILogSink, IAsyncDisposable
    {
        public bool IsEnabled(LogLevel level) => true;

        public void Log(LogLevel level, string message, Exception? exception, Context? context)
        {
        }

        public ValueTask LogAsync(LogLevel level, string message, Exception? exception, Context? context)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            throw new InvalidOperationException("Simulated disposal failure");
        }
    }

    #endregion
}
