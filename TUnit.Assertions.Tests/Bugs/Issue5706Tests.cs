namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for GitHub issue #5706:
/// ItemAt(...).Satisfies(...) should preserve specialised assertion sources for
/// collection-like item values instead of exposing only IAssertionSource&lt;TItem&gt;.
/// </summary>
public class Issue5706Tests
{
    [Test]
    public async Task List_ItemAt_Satisfies_Preserves_String_Item_Source()
    {
        IList<string> items = new List<string> { "alpha" };

        await Assert.That(items).ItemAt(0).Satisfies(item => item.Contains("pha"));
    }

    [Test]
    public async Task List_ItemAt_Satisfies_Preserves_Collection_Item_Source()
    {
        IList<IEnumerable<int>> items = new List<IEnumerable<int>> { new[] { 1, 2, 3 } };

        await Assert.That(items).ItemAt(0).Satisfies(item => item.Count().IsEqualTo(3));
    }

    [Test]
    public async Task List_ItemAt_Satisfies_Preserves_Dictionary_Item_Source()
    {
        IList<IDictionary<string, int>> items = new List<IDictionary<string, int>>
        {
            new Dictionary<string, int> { ["answer"] = 42 }
        };

        await Assert.That(items).ItemAt(0).Satisfies(item => item.ContainsKey("answer"));
    }

    [Test]
    public async Task List_ItemAt_Satisfies_Preserves_Set_Item_Source()
    {
        IList<ISet<int>> items = new List<ISet<int>>
        {
            new HashSet<int> { 1, 2, 3 }
        };

        await Assert.That(items).ItemAt(0).Satisfies(item => item.IsSupersetOf(new[] { 1, 2 }));
    }

    [Test]
    public async Task ReadOnlyList_ItemAt_Satisfies_Preserves_String_Item_Source()
    {
        IReadOnlyList<string> items = new List<string> { "alpha" };

        await Assert.That(items).ItemAt(0).Satisfies(item => item.Contains("pha"));
    }

    [Test]
    public async Task ReadOnlyList_ItemAt_Satisfies_Preserves_Collection_Item_Source()
    {
        IReadOnlyList<List<int>> items = new List<List<int>>
        {
            new() { 1, 2, 3 }
        };

        await Assert.That(items).ItemAt(0).Satisfies(item => item.Count().IsEqualTo(3));
    }

    [Test]
    public async Task ReadOnlyList_ItemAt_Satisfies_Preserves_Dictionary_Item_Source()
    {
        IReadOnlyList<Dictionary<string, int>> items = new List<Dictionary<string, int>>
        {
            new() { ["answer"] = 42 }
        };

        await Assert.That(items).ItemAt(0).Satisfies(item => item.ContainsKey("answer"));
    }

    [Test]
    public async Task ReadOnlyList_ItemAt_Satisfies_Preserves_Set_Item_Source()
    {
        IReadOnlyList<HashSet<int>> items = new List<HashSet<int>>
        {
            new() { 1, 2, 3 }
        };

        await Assert.That(items).ItemAt(0).Satisfies(item => item.IsSupersetOf(new[] { 1, 2 }));
    }
}
