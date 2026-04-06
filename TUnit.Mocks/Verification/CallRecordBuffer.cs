using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TUnit.Mocks.Verification;

/// <summary>
/// Thread-safe append-only buffer for call records.
/// Uses a per-buffer lock with a small critical section for append, which is cheaper
/// than the previous shared MockEngine Lock (no contention with setup, no contention
/// between different members, pre-allocated during setup).
/// Readers can read without locking via volatile count + snapshot.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class CallRecordBuffer
{
    private readonly object _syncRoot = new();
    private CallRecord?[] _items;
    private int _count;

    public CallRecordBuffer(int initialCapacity = 4)
    {
        _items = new CallRecord?[initialCapacity];
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref _count);
    }

    public void Add(CallRecord record)
    {
        lock (_syncRoot)
        {
            var count = _count;
            var items = _items;
            if ((uint)count >= (uint)items.Length)
            {
                items = Grow();
            }
            items[count] = record;
            _count = count + 1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private CallRecord?[] Grow()
    {
        var items = _items;
        var newItems = new CallRecord?[items.Length * 2];
        Array.Copy(items, newItems, items.Length);
        _items = newItems;
        return newItems;
    }

    /// <summary>
    /// Returns the current items array and count for lock-free iteration.
    /// The returned array and count form a consistent snapshot: all items at
    /// indices 0..count-1 are guaranteed to be non-null.
    /// </summary>
    /// <summary>
    /// Read items first, then count. A writer always updates items before incrementing
    /// count (inside the lock), so this order guarantees count &lt;= items.Length.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal (CallRecord?[] Items, int Count) GetSnapshot()
    {
        var items = Volatile.Read(ref _items);
        var count = Volatile.Read(ref _count);
        return (items, count);
    }

    public CallRecord[] ToArray()
    {
        var (items, count) = GetSnapshot();
        if (count == 0) return [];
        var result = new CallRecord[count];
        Array.Copy(items, result, count);
        return result;
    }

    public void ForEach(Action<CallRecord> action)
    {
        var (items, count) = GetSnapshot();
        for (int i = 0; i < count; i++)
        {
            action(items[i]!);
        }
    }

    public void CollectInto(List<CallRecord> target, Func<CallRecord, bool>? filter = null)
    {
        var (items, count) = GetSnapshot();
        for (int i = 0; i < count; i++)
        {
            var record = items[i]!;
            if (filter is null || filter(record))
            {
                target.Add(record);
            }
        }
    }
}
