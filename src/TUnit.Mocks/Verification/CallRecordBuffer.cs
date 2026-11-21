using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TUnit.Mocks.Verification;

/// <summary>
/// Thread-safe append-only buffer for call records.
/// Stores the first two calls inline. Further appends use a per-buffer lock,
/// independent of setup and other members. Readers use lock-free snapshots.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class CallRecordBuffer
{
    // Grow the first page with usage. Later pages keep large histories from
    // copying every existing record or allocating a single large backing array.
    private const int PageShift = 8;
    private const int PageSize = 1 << PageShift;
    private const int PageMask = PageSize - 1;

    private readonly CallRecord _first;
    private CallRecord? _second;
    private Lock? _syncRoot;
    private CallRecord[]? _items;
    private CallRecord[][]? _pages;
    // Once a second call is present this tracks the published count, including
    // both inline calls. Until then _second is the only publication needed.
    private int _count = 2;

    public CallRecordBuffer(CallRecord first)
    {
        _first = first;
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var count = Volatile.Read(ref _count);
            return count == 2 && Volatile.Read(ref _second) is null ? 1 : count;
        }
    }

    public void Add(CallRecord record)
    {
        var syncRoot = Volatile.Read(ref _syncRoot);
        if (syncRoot is null)
        {
            if (Volatile.Read(ref _second) is null && Interlocked.CompareExchange(ref _second, record, null) is null)
            {
                return;
            }
            syncRoot = InitializeLock();
        }

        lock (syncRoot)
        {
            var count = _count;
            var index = count & PageMask;
            CallRecord[] items;
            if (count < PageSize)
            {
                var firstPage = _items;
                items = firstPage is null || (uint)index >= (uint)firstPage.Length
                    ? GrowFirstPage(firstPage)
                    : firstPage;
            }
            else
            {
                var page = count >> PageShift;
                items = index == 0 ? AddPage(page) : _pages![page];
            }
            items[index] = record;
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
    private CallRecord[] GrowFirstPage(CallRecord[]? items)
    {
        var newItems = new CallRecord[items is null ? 4 : items.Length * 2];
        if (items is null)
        {
            newItems[0] = _first;
            newItems[1] = _second!;
        }
        else
        {
            Array.Copy(items, newItems, items.Length);
        }
        Volatile.Write(ref _items, newItems);
        return newItems;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private CallRecord[] AddPage(int page)
    {
        var pages = _pages;
        if (pages is null)
        {
            pages = new CallRecord[4][];
            pages[0] = _items!;
        }
        else if (page == pages.Length)
        {
            Array.Resize(ref pages, pages.Length * 2);
        }

        var next = new CallRecord[PageSize];
        pages[page] = next;
        Volatile.Write(ref _pages, pages);
        return next;
    }

    /// <summary>
    /// Returns a consistent snapshot for lock-free iteration.
    /// </summary>
    /// <remarks>
    /// Read count first, then storage. A writer publishes any new page or array
    /// before publishing the new count. The first two calls use inline records
    /// published with the buffer and with the second-record compare exchange.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Snapshot GetSnapshot()
    {
        var count = Volatile.Read(ref _count);
        if (count == 2)
        {
            var second = Volatile.Read(ref _second);
            return new Snapshot(_first, second, null, null, second is null ? 1 : 2);
        }
        if (count <= PageSize)
        {
            return new Snapshot(_first, null, Volatile.Read(ref _items), null, count);
        }
        return new Snapshot(_first, null, null, Volatile.Read(ref _pages), count);
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

    internal readonly struct Snapshot(
        CallRecord first, CallRecord? second, CallRecord[]? items, CallRecord[][]? pages, int count)
    {
        public int Count { get; } = count;

        public CallRecord this[int index]
        {
            get
            {
                if (pages is not null) return pages[index >> PageShift][index & PageMask];
                if (items is not null) return items[index];
                return index == 0 ? first : second!;
            }
        }

        public CallRecord[] ToArray()
        {
            if (pages is not null)
            {
                var result = new CallRecord[Count];
                var offset = 0;
                while (offset < Count)
                {
                    var length = Math.Min(PageSize, Count - offset);
                    Array.Copy(pages[offset >> PageShift], 0, result, offset, length);
                    offset += length;
                }
                return result;
            }
            if (items is not null)
            {
                return items.AsSpan(0, Count).ToArray();
            }
            return second is null ? [first] : [first, second];
        }
    }
}
