using System.Text;

namespace TUnit.Core.Logging;

/// <summary>
/// Thread-safe line buffer for console interceptor partial writes.
/// Uses <see cref="Lock"/> internally for efficient synchronization.
/// Each context owns its own buffer, preventing output mixing between parallel tests.
/// </summary>
internal sealed class ConsoleLineBuffer
{
    private readonly Lazy<StringBuilder> _buffer = new(() => new StringBuilder());
    private readonly Lock _lock = new();

    /// <summary>
    /// Appends a string to the buffer.
    /// </summary>
    internal void Append(string value)
    {
        lock (_lock)
        {
            _buffer.Value.Append(value);
        }
    }

    /// <summary>
    /// Appends a single character to the buffer.
    /// </summary>
    internal void Append(char value)
    {
        lock (_lock)
        {
            _buffer.Value.Append(value);
        }
    }

    /// <summary>
    /// Appends a range of characters to the buffer.
    /// </summary>
    internal void Append(char[] buffer, int index, int count)
    {
        lock (_lock)
        {
            _buffer.Value.Append(buffer, index, count);
        }
    }

    /// <summary>
    /// Drains all buffered content and clears the buffer.
    /// </summary>
    internal string Drain()
    {
        lock (_lock)
        {
            var buf = _buffer.Value;
            var result = buf.ToString();
            buf.Clear();
            return result;
        }
    }

    /// <summary>
    /// If the buffer has content, appends <paramref name="value"/> to it, drains, and returns the combined result.
    /// If the buffer is empty, returns <paramref name="value"/> unchanged.
    /// </summary>
    internal string? AppendAndDrain(string? value)
    {
        lock (_lock)
        {
            var buf = _buffer.Value;
            if (buf.Length > 0)
            {
                buf.Append(value);
                value = buf.ToString();
                buf.Clear();
            }
        }

        return value;
    }

    /// <summary>
    /// If the buffer has content, drains and returns it. Otherwise returns <c>null</c>.
    /// </summary>
    internal string? FlushIfNonEmpty()
    {
        lock (_lock)
        {
            var buf = _buffer.Value;
            if (buf.Length > 0)
            {
                var result = buf.ToString();
                buf.Clear();
                return result;
            }

            return null;
        }
    }
}
