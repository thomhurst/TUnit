#if NET9_0_OR_GREATER
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests to verify that all collection types resolve to the correct Assert.That overload
/// without any ambiguity compilation errors. If this file fails to compile, it indicates
/// a regression in the overload resolution priority configuration.
/// </summary>
public class CollectionOverloadResolutionTests
{
    // ============ Arrays (Priority 5) ============

    [Test]
    public async Task Array_ResolvesToCollectionAssertion()
    {
        int[] array = [1, 2, 3];
        await Assert.That(array).Contains(1);
    }

    [Test]
    public async Task StringArray_ResolvesToCollectionAssertion()
    {
        string[] array = ["a", "b", "c"];
        await Assert.That(array).Contains("a");
    }

    // ============ IList<T> (Priority 4) ============

    [Test]
    public async Task IList_ResolvesToListAssertion()
    {
        IList<int> list = new List<int> { 1, 2, 3 };
        await Assert.That(list).Contains(1);
    }

    [Test]
    public async Task List_ResolvesToListAssertion()
    {
        List<int> list = [1, 2, 3];
        await Assert.That(list).Contains(1);
    }

    // ============ ICollection<T> (Priority 3) ============

    [Test]
    public async Task ICollection_ResolvesToCollectionAssertion()
    {
        ICollection<int> collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).Contains(1);
    }

    [Test]
    public async Task ICollection_SupportsIsEmpty()
    {
        ICollection<int> collection = new List<int>();
        await Assert.That(collection).IsEmpty();
    }

    // ============ ISet<T> (Priority 3) ============

    [Test]
    public async Task ISet_ResolvesToSetAssertion()
    {
        ISet<int> set = new HashSet<int> { 1, 2, 3 };
        await Assert.That(set).Contains(1);
    }

    [Test]
    public async Task SortedSet_ResolvesToSetAssertion()
    {
        SortedSet<int> set = [1, 2, 3];
        await Assert.That(set).Contains(1);
    }

    // ============ HashSet<T> (Priority 3) ============

    [Test]
    public async Task HashSet_ResolvesToHashSetAssertion()
    {
        HashSet<int> hashSet = [1, 2, 3];
        await Assert.That(hashSet).Contains(1);
    }

    // ============ IDictionary<TKey, TValue> (Priority 3) ============

    [Test]
    public async Task IDictionary_ResolvesToMutableDictionaryAssertion()
    {
        IDictionary<string, int> dict = new Dictionary<string, int> { ["a"] = 1 };
        await Assert.That(dict).ContainsKey("a");
    }

    [Test]
    public async Task Dictionary_ResolvesToMutableDictionaryAssertion()
    {
        Dictionary<string, int> dict = new() { ["a"] = 1 };
        await Assert.That(dict).ContainsKey("a");
    }

    [Test]
    public async Task SortedDictionary_ResolvesToMutableDictionaryAssertion()
    {
        SortedDictionary<string, int> dict = new() { ["a"] = 1 };
        await Assert.That(dict).ContainsKey("a");
    }

    [Test]
    public async Task ConcurrentDictionary_ResolvesToMutableDictionaryAssertion()
    {
        ConcurrentDictionary<string, int> dict = new();
        dict["a"] = 1;
        await Assert.That(dict).ContainsKey("a");
    }

    // ============ IReadOnlyList<T> (Priority 3) ============

    [Test]
    public async Task IReadOnlyList_ResolvesToReadOnlyListAssertion()
    {
        IReadOnlyList<int> list = new List<int> { 1, 2, 3 };
        await Assert.That(list).Contains(1);
    }

    [Test]
    public async Task ImmutableList_ResolvesToReadOnlyListAssertion()
    {
        ImmutableList<int> list = [1, 2, 3];
        await Assert.That(list).Contains(1);
    }

    [Test]
    public async Task ImmutableArray_ResolvesToCollectionAssertion()
    {
        ImmutableArray<int> array = [1, 2, 3];
        await Assert.That(array).Contains(1);
    }

    // ============ IReadOnlyCollection<T> (Priority 2) ============

    [Test]
    public async Task IReadOnlyCollection_ResolvesToCollectionAssertion()
    {
        IReadOnlyCollection<int> collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).Contains(1);
    }

    [Test]
    public async Task IReadOnlyCollection_SupportsIsEmpty()
    {
        IReadOnlyCollection<int> collection = Array.Empty<int>();
        await Assert.That(collection).IsEmpty();
    }

    // ============ IReadOnlyDictionary<TKey, TValue> (Priority 2) ============

    [Test]
    public async Task IReadOnlyDictionary_ResolvesToDictionaryAssertion()
    {
        IReadOnlyDictionary<string, int> dict = new Dictionary<string, int> { ["a"] = 1 };
        await Assert.That(dict).ContainsKey("a");
    }

    [Test]
    public async Task ReadOnlyDictionary_ResolvesToDictionaryAssertion()
    {
        ReadOnlyDictionary<string, int> dict = new(new Dictionary<string, int> { ["a"] = 1 });
        await Assert.That(dict).ContainsKey("a");
    }

    [Test]
    public async Task ImmutableDictionary_ResolvesToDictionaryAssertion()
    {
        ImmutableDictionary<string, int> dict = ImmutableDictionary<string, int>.Empty.Add("a", 1);
        await Assert.That(dict).ContainsKey("a");
    }

    // ============ IReadOnlySet<T> (Priority 2) ============

    [Test]
    public async Task IReadOnlySet_ResolvesToReadOnlySetAssertion()
    {
        IReadOnlySet<int> set = new HashSet<int> { 1, 2, 3 };
        await Assert.That(set).Contains(1);
    }

    [Test]
    public async Task ImmutableHashSet_ResolvesToReadOnlySetAssertion()
    {
        ImmutableHashSet<int> set = [1, 2, 3];
        await Assert.That(set).Contains(1);
    }

    [Test]
    public async Task FrozenSet_ResolvesToReadOnlySetAssertion()
    {
        FrozenSet<int> set = new HashSet<int> { 1, 2, 3 }.ToFrozenSet();
        await Assert.That(set).Contains(1);
    }

    // ============ IEnumerable<T> (Priority 1) ============

    [Test]
    public async Task IEnumerable_ResolvesToCollectionAssertion()
    {
        IEnumerable<int> enumerable = new List<int> { 1, 2, 3 };
        await Assert.That(enumerable).Contains(1);
    }

    [Test]
    public async Task LinqEnumerable_ResolvesToCollectionAssertion()
    {
        IEnumerable<int> enumerable = Enumerable.Range(1, 3);
        await Assert.That(enumerable).Contains(1);
    }

    [Test]
    public async Task IQueryable_ResolvesToCollectionAssertion()
    {
        IQueryable<int> queryable = new List<int> { 1, 2, 3 }.AsQueryable();
        await Assert.That(queryable).Contains(1);
    }

    // ============ Non-generic IEnumerable ============

    [Test]
    public async Task NonGenericIEnumerable_ResolvesToCollectionAssertion()
    {
        IEnumerable enumerable = new ArrayList { 1, 2, 3 };
        await Assert.That(enumerable).IsNotEmpty();
    }

    // ============ String (Priority 2 - treated as value, not char collection) ============

    [Test]
    public async Task String_ResolvesToValueAssertion()
    {
        string value = "hello";
        await Assert.That(value).IsEqualTo("hello");
    }

    // ============ Memory types (Priority 1) ============

    [Test]
    public async Task Memory_ResolvesToMemoryAssertion()
    {
        Memory<int> memory = new int[] { 1, 2, 3 };
        await Assert.That(memory).IsNotEmpty();
    }

    [Test]
    public async Task ReadOnlyMemory_ResolvesToReadOnlyMemoryAssertion()
    {
        ReadOnlyMemory<int> memory = new int[] { 1, 2, 3 };
        await Assert.That(memory).IsNotEmpty();
    }

    // ============ IAsyncEnumerable<T> (Priority 1) ============

    [Test]
    public async Task IAsyncEnumerable_ResolvesToAsyncEnumerableAssertion()
    {
        async IAsyncEnumerable<int> GetItems()
        {
            yield return 1;
            yield return 2;
            yield return 3;
            await Task.CompletedTask;
        }

        await Assert.That(GetItems()).Contains(1);
    }

    // ============ Collection<T> (should resolve via IList<T> or ICollection<T>) ============

    [Test]
    public async Task Collection_ResolvesToListAssertion()
    {
        Collection<int> collection = [1, 2, 3];
        await Assert.That(collection).Contains(1);
    }

    [Test]
    public async Task ObservableCollection_ResolvesToListAssertion()
    {
        ObservableCollection<int> collection = [1, 2, 3];
        await Assert.That(collection).Contains(1);
    }

    // ============ ReadOnlyCollection<T> (should resolve via IReadOnlyList<T>) ============

    [Test]
    public async Task ReadOnlyCollection_ResolvesToReadOnlyListAssertion()
    {
        ReadOnlyCollection<int> collection = new(new List<int> { 1, 2, 3 });
        await Assert.That(collection).Contains(1);
    }

    // ============ Stack and Queue (should resolve via IEnumerable<T> or ICollection<T>) ============

    [Test]
    public async Task Stack_ResolvesToCollectionAssertion()
    {
        Stack<int> stack = new([1, 2, 3]);
        await Assert.That(stack).Contains(1);
    }

    [Test]
    public async Task Queue_ResolvesToCollectionAssertion()
    {
        Queue<int> queue = new([1, 2, 3]);
        await Assert.That(queue).Contains(1);
    }

    [Test]
    public async Task ConcurrentQueue_ResolvesToCollectionAssertion()
    {
        ConcurrentQueue<int> queue = new([1, 2, 3]);
        await Assert.That(queue).Contains(1);
    }

    [Test]
    public async Task ConcurrentStack_ResolvesToCollectionAssertion()
    {
        ConcurrentStack<int> stack = new([1, 2, 3]);
        await Assert.That(stack).Contains(1);
    }

    [Test]
    public async Task ConcurrentBag_ResolvesToCollectionAssertion()
    {
        ConcurrentBag<int> bag = [1, 2, 3];
        await Assert.That(bag).Contains(1);
    }

    // ============ LinkedList<T> (should resolve via ICollection<T>) ============

    [Test]
    public async Task LinkedList_ResolvesToCollectionAssertion()
    {
        LinkedList<int> list = new([1, 2, 3]);
        await Assert.That(list).Contains(1);
    }

    // ============ Immutable collections ============

    [Test]
    public async Task ImmutableQueue_ResolvesToCollectionAssertion()
    {
        ImmutableQueue<int> queue = ImmutableQueue.Create(1, 2, 3);
        await Assert.That(queue).Contains(1);
    }

    [Test]
    public async Task ImmutableStack_ResolvesToCollectionAssertion()
    {
        ImmutableStack<int> stack = ImmutableStack.Create(1, 2, 3);
        await Assert.That(stack).Contains(1);
    }

    // ============ Nullable collection types ============

    [Test]
    public async Task NullableArray_ResolvesToCollectionAssertion()
    {
        int[]? array = [1, 2, 3];
        await Assert.That(array).Contains(1);
    }

    [Test]
    public async Task NullableList_ResolvesToListAssertion()
    {
        List<int>? list = [1, 2, 3];
        await Assert.That(list).Contains(1);
    }

    [Test]
    public async Task NullableICollection_ResolvesToCollectionAssertion()
    {
        ICollection<int>? collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).Contains(1);
    }

    [Test]
    public async Task NullableIReadOnlyCollection_ResolvesToCollectionAssertion()
    {
        IReadOnlyCollection<int>? collection = new List<int> { 1, 2, 3 };
        await Assert.That(collection).Contains(1);
    }
}
#endif
