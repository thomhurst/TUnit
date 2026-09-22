using System.Buffers;
using System.IO;

namespace TUnit.Engine.Reporters.Aggregation;

/// <summary>
/// Append-only <see cref="IBufferWriter{T}"/> backed by a list of pooled chunks. Report JSON
/// for large suites runs to tens of megabytes; <see cref="ArrayBufferWriter{T}"/> /
/// <see cref="MemoryStream"/> grow by doubling and copying (several times the payload in
/// transient large-object-heap churn) and then copy once more to hand out a
/// <c>byte[]</c>. Chunks are never copied or resized here, and consumers stream them straight
/// to their destination (a file, a compression stream) via <see cref="WriteTo"/>.
/// Source-linked into TUnit.Reporting.Tool alongside the serializers that use it.
/// </summary>
internal sealed class SegmentedBufferWriter : IBufferWriter<byte>, IDisposable
{
    // Above Utf8JsonWriter's 4KB growth request so tail waste per chunk stays small, and
    // within ArrayPool<byte>.Shared's pooled size range so chunks are recycled across uses.
    private const int ChunkSize = 256 * 1024;

    private readonly List<byte[]> _completed = [];
    private readonly List<int> _completedCounts = [];
    private byte[] _current = [];
    private int _index;
    private long _completedLength;

    public long Length => _completedLength + _index;

    public void Advance(int count)
    {
        if (count < 0 || _index > _current.Length - count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        _index += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _current.AsMemory(_index);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _current.AsSpan(_index);
    }

    private void EnsureCapacity(int sizeHint)
    {
        if (sizeHint < 1)
        {
            sizeHint = 1;
        }

        if (_current.Length - _index >= sizeHint)
        {
            return;
        }

        if (_index > 0)
        {
            _completed.Add(_current);
            _completedCounts.Add(_index);
            _completedLength += _index;
        }
        else if (_current.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(_current);
        }

        _current = ArrayPool<byte>.Shared.Rent(Math.Max(sizeHint, ChunkSize));
        _index = 0;
    }

    /// <summary>Copies the written bytes to <paramref name="destination"/> in order.</summary>
    public void WriteTo(Stream destination)
    {
        for (var i = 0; i < _completed.Count; i++)
        {
            destination.Write(_completed[i], 0, _completedCounts[i]);
        }

        if (_index > 0)
        {
            destination.Write(_current, 0, _index);
        }
    }

    public byte[] ToArray()
    {
        var result = new byte[checked((int)Length)];
        var offset = 0;
        for (var i = 0; i < _completed.Count; i++)
        {
            Buffer.BlockCopy(_completed[i], 0, result, offset, _completedCounts[i]);
            offset += _completedCounts[i];
        }

        Buffer.BlockCopy(_current, 0, result, offset, _index);
        return result;
    }

    public void Dispose()
    {
        foreach (var chunk in _completed)
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }

        _completed.Clear();
        _completedCounts.Clear();
        _completedLength = 0;

        if (_current.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(_current);
        }

        _current = [];
        _index = 0;
    }
}
