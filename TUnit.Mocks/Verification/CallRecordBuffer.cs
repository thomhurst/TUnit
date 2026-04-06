using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TUnit.Mocks.Verification;

/// <summary>
/// Thread-safe append-only buffer for call records.
/// Uses a per-buffer lock (lock(this)) with a small critical section for the append,
/// which is significantly cheaper than the previous shared MockEngine Lock because:
///   - No contention with setup operations (separate lock)
///   - No contention between different members (per-buffer lock)
///   - No capacity check overhead (pre-allocated during setup)
/// Readers (verification) can read without locking via volatile count + snapshot.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class CallRecordBuffer
{
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

    /// <summary>
    /// Appends a call record. Thread-safe via a lightweight per-buffer lock.
    /// The critical section is minimal: index claim + array write + count publish.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(CallRecord record)
    {
        lock (this)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CallRecord Get(int index) => Volatile.Read(ref _items)[index]!;

    /// <summary>
    /// Returns a snapshot copy of all recorded calls.
    /// </summary>
    public CallRecord[] ToArray()
    {
        var count = Count;
        if (count == 0) return [];
        var items = Volatile.Read(ref _items);
        var result = new CallRecord[count];
        Array.Copy(items, result, count);
        return result;
    }

    /// <summary>
    /// Iterates all records and applies an action. No allocation.
    /// </summary>
    public void ForEach(Action<CallRecord> action)
    {
        var count = Count;
        var items = Volatile.Read(ref _items);
        for (int i = 0; i < count; i++)
        {
            action(items[i]!);
        }
    }

    /// <summary>
    /// Collects records into a target list, optionally filtered.
    /// </summary>
    public void CollectInto(List<CallRecord> target, Func<CallRecord, bool>? filter = null)
    {
        var count = Count;
        var items = Volatile.Read(ref _items);
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
