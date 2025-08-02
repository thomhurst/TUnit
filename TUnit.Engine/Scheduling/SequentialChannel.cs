using System.Threading.Channels;
using TUnit.Core.Enums;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// A channel that maintains strict FIFO ordering for sequential test execution
/// </summary>
internal class SequentialChannel<T> where T : class
{
    private readonly Channel<T> _channel;

    public SequentialChannel(int capacity)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true, // Ensure only one consumer reads at a time
            SingleWriter = false,
            AllowSynchronousContinuations = true
        };
        _channel = Channel.CreateBounded<T>(options);
    }

    public ChannelReader<T> Reader => _channel.Reader;

    public async ValueTask TryWriteAsync(T item, Priority priority, CancellationToken cancellationToken = default)
    {
        // Ignore priority for sequential channels - maintain FIFO order
        await _channel.Writer.WriteAsync(item, cancellationToken);
    }

    public bool TryComplete()
    {
        return _channel.Writer.TryComplete();
    }
}