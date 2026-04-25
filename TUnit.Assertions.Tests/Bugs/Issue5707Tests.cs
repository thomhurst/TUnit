using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for GitHub issue #5707:
/// `.Count(itemAssertion)` per-item overload only exposed a generic
/// `IAssertionSource&lt;TItem&gt;` inside the lambda, so specialised
/// assertions defined on collection / dictionary / set / list bases
/// (e.g. <c>HasCount</c>, <c>ContainsKey</c>, <c>IsSubsetOf</c>,
/// <c>HasItemAt</c>) were unreachable when the items themselves were
/// collections, dictionaries or sets.
///
/// Specialised <c>Count</c> overloads now hand the lambda a typed
/// source (CollectionAssertion, ListAssertion, DictionaryAssertion,
/// SetAssertion, etc.) so the failure message also keeps the
/// specialised assertion's expectation rather than a generic wrapper.
/// </summary>
public class Issue5707Tests
{
    [Test]
    public async Task Count_String_Items_Use_String_Assertion_Source()
    {
        var items = new List<string> { "apple", "banana", "apricot", "cherry" };

        await Assert.That(items).Count(s => s.IsEqualTo("apple")).IsEqualTo(1);
    }

    [Test]
    public async Task Count_Enumerable_Items_Reach_HasCount_On_Inner()
    {
        IEnumerable<IEnumerable<int>> listOfLists = new List<List<int>>
        {
            new() { 1, 2, 3 },
            new() { 1 },
            new() { 1, 2, 3 },
        };

        await Assert.That(listOfLists).Count(l => l.Count().IsEqualTo(3)).IsEqualTo(2);
    }

    [Test]
    public async Task Count_List_Items_Reach_HasItemAt_On_Inner()
    {
        var listOfLists = new List<IList<int>>
        {
            new List<int> { 10, 20 },
            new List<int> { 99 },
            new List<int> { 10, 30 },
        };

        await Assert.That(listOfLists).Count(l => l.HasItemAt(0, 10)).IsEqualTo(2);
    }

    [Test]
    public async Task Count_ReadOnlyList_Items_Reach_HasItemAt_On_Inner()
    {
        var listOfLists = new List<IReadOnlyList<int>>
        {
            new List<int> { 10, 20 },
            new List<int> { 99 },
            new List<int> { 10, 30 },
        };

        await Assert.That(listOfLists).Count(l => l.HasItemAt(0, 10)).IsEqualTo(2);
    }

    [Test]
    public async Task Count_ReadOnlyDictionary_Items_Reach_ContainsKey_On_Inner()
    {
        IReadOnlyDictionary<string, int> a = new Dictionary<string, int> { ["k"] = 1 };
        IReadOnlyDictionary<string, int> b = new Dictionary<string, int> { ["other"] = 2 };
        IReadOnlyDictionary<string, int> c = new Dictionary<string, int> { ["k"] = 5 };
        var dicts = new List<IReadOnlyDictionary<string, int>> { a, b, c };

        await Assert.That(dicts).Count(d => d.ContainsKey("k")).IsEqualTo(2);
    }

    [Test]
    public async Task Count_Dictionary_Items_Reach_ContainsKey_On_Inner()
    {
        var dicts = new List<IDictionary<string, int>>
        {
            new Dictionary<string, int> { ["k"] = 1 },
            new Dictionary<string, int> { ["other"] = 2 },
            new Dictionary<string, int> { ["k"] = 5 },
        };

        await Assert.That(dicts).Count(d => d.ContainsKey("k")).IsEqualTo(2);
    }

    [Test]
    public async Task Count_Set_Items_Reach_IsSubsetOf_On_Inner()
    {
        var universe = new HashSet<int> { 1, 2, 3, 4, 5 };
        var sets = new List<ISet<int>>
        {
            new HashSet<int> { 1, 2 },
            new HashSet<int> { 6 },
            new HashSet<int> { 3, 4 },
        };

        await Assert.That(sets).Count(s => s.IsSubsetOf(universe)).IsEqualTo(2);
    }

    [Test]
    public async Task Count_ReadOnlySet_Items_Reach_IsSubsetOf_On_Inner()
    {
        var universe = new HashSet<int> { 1, 2, 3, 4, 5 };
        var sets = new List<IReadOnlySet<int>>
        {
            new HashSet<int> { 1, 2 },
            new HashSet<int> { 6 },
            new HashSet<int> { 3, 4 },
        };

        await Assert.That(sets).Count(s => s.IsSubsetOf(universe)).IsEqualTo(2);
    }

    [Test]
    public async Task Count_Array_Items_Reach_IsSingleElement_On_Inner()
    {
        var listOfArrays = new List<int[]>
        {
            new[] { 10, 20 },
            new[] { 99 },
            new[] { 10 },
        };

        await Assert.That(listOfArrays).Count(a => a.IsSingleElement()).IsEqualTo(2);
    }

    [Test]
    public async Task Count_Specialised_Source_Failure_Message_Mentions_Inner_Expectation()
    {
        IEnumerable<IEnumerable<int>> listOfLists = new List<List<int>>
        {
            new() { 1 },
            new() { 1 },
        };

        // Expect 5 items with inner-count==3 → there are 0; ensure failure message
        // surfaces the specialised inner expectation rather than just "received 0".
        var ex = await Assert.That(async () =>
                await Assert.That(listOfLists).Count(l => l.Count().IsEqualTo(3)).IsEqualTo(5))
            .Throws<AssertionException>();

        // The chained expression should include `.Count(...)` per-item filter.
        await Assert.That(ex.Message).Contains(".Count(l => l.Count().IsEqualTo(3))");
    }

    [Test]
    public async Task Count_List_Failure_Message_Mentions_Specialised_Inner_Expectation()
    {
        var listOfLists = new List<IList<int>>
        {
            new List<int> { 99 },
            new List<int> { 99 },
        };

        var ex = await Assert.That(async () =>
                await Assert.That(listOfLists).Count(l => l.HasItemAt(0, 10)).IsEqualTo(5))
            .Throws<AssertionException>();

        await Assert.That(ex.Message).Contains(".Count(l => l.HasItemAt(0, 10))");
    }

    [Test]
    public async Task Count_Dictionary_Failure_Message_Mentions_Specialised_Inner_Expectation()
    {
        var dicts = new List<IDictionary<string, int>>
        {
            new Dictionary<string, int> { ["other"] = 1 },
            new Dictionary<string, int> { ["other"] = 2 },
        };

        var ex = await Assert.That(async () =>
                await Assert.That(dicts).Count(d => d.ContainsKey("k")).IsEqualTo(5))
            .Throws<AssertionException>();

        await Assert.That(ex.Message).Contains(".Count(d => d.ContainsKey(\"k\"))");
    }

    [Test]
    public async Task Count_Set_Failure_Message_Mentions_Specialised_Inner_Expectation()
    {
        var universe = new HashSet<int> { 1, 2, 3 };
        var sets = new List<ISet<int>>
        {
            new HashSet<int> { 6 },
            new HashSet<int> { 7 },
        };

        var ex = await Assert.That(async () =>
                await Assert.That(sets).Count(s => s.IsSubsetOf(universe)).IsEqualTo(5))
            .Throws<AssertionException>();

        await Assert.That(ex.Message).Contains(".Count(s => s.IsSubsetOf(universe))");
    }

    [Test]
    public async Task Count_ReadOnlyList_Failure_Message_Mentions_Specialised_Inner_Expectation()
    {
        var listOfLists = new List<IReadOnlyList<int>>
        {
            new List<int> { 99 },
            new List<int> { 99 },
        };

        var ex = await Assert.That(async () =>
                await Assert.That(listOfLists).Count(l => l.HasItemAt(0, 10)).IsEqualTo(5))
            .Throws<AssertionException>();

        await Assert.That(ex.Message).Contains(".Count(l => l.HasItemAt(0, 10))");
    }

    [Test]
    public async Task Count_ReadOnlySet_Failure_Message_Mentions_Specialised_Inner_Expectation()
    {
        var universe = new HashSet<int> { 1, 2, 3 };
        var sets = new List<IReadOnlySet<int>>
        {
            new HashSet<int> { 6 },
            new HashSet<int> { 7 },
        };

        var ex = await Assert.That(async () =>
                await Assert.That(sets).Count(s => s.IsSubsetOf(universe)).IsEqualTo(5))
            .Throws<AssertionException>();

        await Assert.That(ex.Message).Contains(".Count(s => s.IsSubsetOf(universe))");
    }

    [Test]
    public async Task Count_Array_Failure_Message_Mentions_Specialised_Inner_Expectation()
    {
        var listOfArrays = new List<int[]>
        {
            new[] { 1, 2 },
            new[] { 1, 2 },
        };

        var ex = await Assert.That(async () =>
                await Assert.That(listOfArrays).Count(a => a.IsSingleElement()).IsEqualTo(5))
            .Throws<AssertionException>();

        await Assert.That(ex.Message).Contains(".Count(a => a.IsSingleElement())");
    }
}
