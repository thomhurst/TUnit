using System.Threading.Channels;
using TUnit.Core;
using TUnit.Engine.Interfaces;

namespace TUnit.Engine.Services;

/// <summary>
/// Thread-safe queue implementation for managing dynamically created tests using System.Threading.Channels.
/// Provides efficient async support for queuing tests created at runtime.
/// Handles discovery notification internally to keep all dynamic test logic in one place.
/// </summary>
internal sealed class DynamicTestQueue : IDynamicTestQueue
{
    private readonly Channel<AbstractExecutableTest> _channel;
    private readonly ITUnitMessageBus _messageBus;
    private int _pendingCount;
    private bool _isCompleted;

    public DynamicTestQueue(ITUnitMessageBus messageBus)
    {
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));

        // Unbounded channel for maximum flexibility
        // Tests can be added at any time during execution
        _channel = Channel.CreateUnbounded<AbstractExecutableTest>(new UnboundedChannelOptions
        {
            SingleReader = false, // Multiple test runners may dequeue
            SingleWriter = false  // Multiple sources may enqueue (AddDynamicTest, CreateTestVariant)
        });
    }

    public void Enqueue(AbstractExecutableTest test)
    {
        Interlocked.Increment(ref _pendingCount);

        if (!_channel.Writer.TryWrite(test))
        {
            Interlocked.Decrement(ref _pendingCount);
            throw new InvalidOperationException("Failed to enqueue test to dynamic test queue.");
        }

        // Skip sending Discovered message - dynamic tests are created during execution,
        // and Discovered messages are only needed for discovery requests
    }

    public bool TryDequeue(out AbstractExecutableTest? test)
    {
        if (_channel.Reader.TryRead(out test))
        {
            Interlocked.Decrement(ref _pendingCount);
            return true;
        }

        test = null;
        return false;
    }

    public int PendingCount => _pendingCount;

    public bool IsCompleted => _isCompleted;

    public void Complete()
    {
        _isCompleted = true;
        _channel.Writer.Complete();
    }

    public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.WaitToReadAsync(cancellationToken);
    }
}
