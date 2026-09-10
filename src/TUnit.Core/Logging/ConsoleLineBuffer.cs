using System.Text;

namespace TUnit.Core.Logging;

/// <summary>
/// Thread-safe line buffer for console interceptor partial writes.
/// Uses <see cref="Lock"/> internally for efficient synchronization.
/// Each context owns its own buffer, preventing output mixing between parallel tests.
/// </summary>
internal sealed class ConsoleLineBuffer
{
    private readonly StringBuilder _buffer = new();
    private readonly Lock _lock = new();

    internal void Append(string value)
    {
        lock (_lock)
        {
            _buffer.Append(value);
        }
    }

    internal void Append(char value)
    {
        lock (_lock)
        {
            _buffer.Append(value);
        }
    }

    internal void Append(char[] buffer, int index, int count)
    {
        lock (_lock)
        {
            _buffer.Append(buffer, index, count);
        }
    }

    internal string Drain()
    {
        lock (_lock)
        {
            var result = _buffer.ToString();
            _buffer.Clear();
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
            if (_buffer.Length > 0)
            {
                _buffer.Append(value);
                value = _buffer.ToString();
                _buffer.Clear();
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
            if (_buffer.Length > 0)
            {
                var result = _buffer.ToString();
                _buffer.Clear();
                return result;
            }

            return null;
        }
    }
}
