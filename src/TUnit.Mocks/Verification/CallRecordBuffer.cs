using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TUnit.Mocks.Verification;

/// <summary>
/// Thread-safe append-only buffer for call records.
/// Stores the first call inline. Further appends use a per-buffer lock,
/// independent of setup and other members. Readers use lock-free snapshots.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class CallRecordBuffer
{
    private readonly CallRecord _first;
    private Lock? _syncRoot;
    private CallRecord[]? _items;
    private int _count = 1;

    public CallRecordBuffer(CallRecord first)
    {
        _first = first;
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Volatile.Read(ref _count);
    }

    public void Add(CallRecord record)
    {
        lock (Volatile.Read(ref _syncRoot) ?? InitializeLock())
        {
            var count = _count;
            var items = _items;
            if (items is null || (uint)count >= (uint)items.Length)
            {
                items = Grow(items);
            }
            items[count] = record;
            Volatile.Write(ref _count, count + 1);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Lock InitializeLock()
    {
        var newLock = new Lock();
        return Interlocked.CompareExchange(ref _syncRoot, newLock, null) ?? newLock;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private CallRecord[] Grow(CallRecord[]? items)
    {
        var newItems = new CallRecord[items is null ? 4 : items.Length * 2];
        if (items is null)
        {
            newItems[0] = _first;
        }
        else
        {
            Array.Copy(items, newItems, items.Length);
        }
        Volatile.Write(ref _items, newItems);
        return newItems;
    }

    /// <summary>
    /// Returns a consistent snapshot for lock-free iteration.
    /// </summary>
    /// <remarks>
    /// Read count first, then items. A writer always updates items (including Grow)
    /// before incrementing count (inside the lock). If count is one, either
    /// storage location can supply the first record.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Snapshot GetSnapshot()
    {
        var count = Volatile.Read(ref _count);
        var items = Volatile.Read(ref _items);
        return new Snapshot(_first, items, count);
    }

    public CallRecord[] ToArray() => GetSnapshot().ToArray();

    public void ForEach(Action<CallRecord> action)
    {
        var snapshot = GetSnapshot();
        for (var i = 0; i < snapshot.Count; i++)
        {
            action(snapshot[i]);
        }
    }

    public void CollectInto(List<CallRecord> target, Func<CallRecord, bool>? filter = null)
    {
        var snapshot = GetSnapshot();
        for (var i = 0; i < snapshot.Count; i++)
        {
            var record = snapshot[i];
            if (filter is null || filter(record))
            {
                target.Add(record);
            }
        }
    }

    internal readonly struct Snapshot(CallRecord first, CallRecord[]? items, int count)
    {
        public int Count { get; } = count;

        public CallRecord this[int index] => items is null ? first : items[index];

        public CallRecord[] ToArray() => items is null ? [first] : items.AsSpan(0, Count).ToArray();
    }
}
