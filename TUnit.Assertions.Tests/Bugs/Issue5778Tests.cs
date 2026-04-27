namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests mirroring <see cref="Issue5706Tests"/> for the LastItem path.
/// LastItem(...).Satisfies(...) should preserve specialised assertion sources for
/// collection-like item values instead of exposing only IAssertionSource&lt;TItem&gt;.
/// </summary>
public class Issue5778Tests
{
    [Test]
    public async Task List_LastItem_Satisfies_Preserves_String_Item_Source()
    {
        IList<string> items = new List<string> { "alpha" };

        await Assert.That(items).LastItem().Satisfies(item => item.Contains("pha"));
    }

    [Test]
    public async Task List_LastItem_Satisfies_Preserves_Collection_Item_Source()
    {
        IList<IEnumerable<int>> items = new List<IEnumerable<int>> { new[] { 1, 2, 3 } };

        await Assert.That(items).LastItem().Satisfies(item => item.Count().IsEqualTo(3));
    }

    [Test]
    public async Task List_LastItem_Satisfies_Preserves_IList_Item_Source()
    {
        IList<IList<int>> items = new List<IList<int>> { new List<int> { 1, 2, 3 } };

        await Assert.That(items).LastItem().Satisfies(item => item.Count().IsEqualTo(3));
    }

    [Test]
    public async Task List_LastItem_Satisfies_Preserves_IReadOnlyList_Item_Source()
    {
        IList<IReadOnlyList<int>> items = new List<IReadOnlyList<int>> { new List<int> { 1, 2, 3 } };

        await Assert.That(items).LastItem().Satisfies(item => item.Count().IsEqualTo(3));
    }

    [Test]
    public async Task List_LastItem_Satisfies_Preserves_Array_Item_Source()
    {
        IList<int[]> items = new List<int[]> { new[] { 1, 2, 3 } };

        await Assert.That(items).LastItem().Satisfies(item => item.Count().IsEqualTo(3));
    }

    [Test]
    public async Task List_LastItem_Satisfies_Preserves_ConcreteList_Item_Source()
    {
        IList<List<int>> items = new List<List<int>> { new() { 1, 2, 3 } };

        await Assert.That(items).LastItem().Satisfies(item => item.Count().IsEqualTo(3));
    }

    [Test]
    public async Task List_LastItem_Satisfies_Preserves_Dictionary_Item_Source()
    {
        IList<IDictionary<string, int>> items = new List<IDictionary<string, int>>
        {
            new Dictionary<string, int> { ["answer"] = 42 }
        };

        await Assert.That(items).LastItem().Satisfies(item => item.ContainsKey("answer"));
    }

    [Test]
    public async Task List_LastItem_Satisfies_Preserves_Concrete_Dictionary_Item_Source()
    {
        IList<Dictionary<string, int>> items = new List<Dictionary<string, int>>
        {
            new() { ["answer"] = 42 }
        };

        await Assert.That(items).LastItem().Satisfies(item => item.ContainsKey("answer"));
    }

    [Test]
    public async Task List_LastItem_Satisfies_Preserves_Set_Item_Source()
    {
        IList<ISet<int>> items = new List<ISet<int>>
        {
            new HashSet<int> { 1, 2, 3 }
        };

        await Assert.That(items).LastItem().Satisfies(item => item.IsSupersetOf(new[] { 1, 2 }));
    }

    [Test]
    public async Task List_LastItem_Satisfies_Preserves_HashSet_Item_Source()
    {
        IList<HashSet<int>> items = new List<HashSet<int>>
        {
            new() { 1, 2, 3 }
        };

        await Assert.That(items).LastItem().Satisfies(item => item.IsSupersetOf(new[] { 1, 2 }));
    }

    [Test]
    public async Task ReadOnlyList_LastItem_Satisfies_Preserves_String_Item_Source()
    {
        IReadOnlyList<string> items = new List<string> { "alpha" };

        await Assert.That(items).LastItem().Satisfies(item => item.Contains("pha"));
    }

    [Test]
    public async Task ReadOnlyList_LastItem_Satisfies_Preserves_Collection_Item_Source()
    {
        IReadOnlyList<List<int>> items = new List<List<int>>
        {
            new() { 1, 2, 3 }
        };

        await Assert.That(items).LastItem().Satisfies(item => item.Count().IsEqualTo(3));
    }

    [Test]
    public async Task ReadOnlyList_LastItem_Satisfies_Preserves_IList_Item_Source()
    {
        IReadOnlyList<IList<int>> items = new List<IList<int>> { new List<int> { 1, 2, 3 } };

        await Assert.That(items).LastItem().Satisfies(item => item.Count().IsEqualTo(3));
    }

    [Test]
    public async Task ReadOnlyList_LastItem_Satisfies_Preserves_IReadOnlyList_Item_Source()
    {
        IReadOnlyList<IReadOnlyList<int>> items = new List<IReadOnlyList<int>> { new List<int> { 1, 2, 3 } };

        await Assert.That(items).LastItem().Satisfies(item => item.Count().IsEqualTo(3));
    }

    [Test]
    public async Task ReadOnlyList_LastItem_Satisfies_Preserves_Array_Item_Source()
    {
        IReadOnlyList<int[]> items = new List<int[]> { new[] { 1, 2, 3 } };

        await Assert.That(items).LastItem().Satisfies(item => item.Count().IsEqualTo(3));
    }

    [Test]
    public async Task ReadOnlyList_LastItem_Satisfies_Preserves_Dictionary_Item_Source()
    {
        IReadOnlyList<Dictionary<string, int>> items = new List<Dictionary<string, int>>
        {
            new() { ["answer"] = 42 }
        };

        await Assert.That(items).LastItem().Satisfies(item => item.ContainsKey("answer"));
    }

    [Test]
    public async Task ReadOnlyList_LastItem_Satisfies_Preserves_ReadOnly_Dictionary_Item_Source()
    {
        IReadOnlyList<IReadOnlyDictionary<string, int>> items = new List<IReadOnlyDictionary<string, int>>
        {
            new Dictionary<string, int> { ["answer"] = 42 }
        };

        await Assert.That(items).LastItem().Satisfies(item => item.ContainsKey("answer"));
    }

    [Test]
    public async Task ReadOnlyList_LastItem_Satisfies_Preserves_Set_Item_Source()
    {
        IReadOnlyList<HashSet<int>> items = new List<HashSet<int>>
        {
            new() { 1, 2, 3 }
        };

        await Assert.That(items).LastItem().Satisfies(item => item.IsSupersetOf(new[] { 1, 2 }));
    }

    [Test]
    public async Task ReadOnlyList_LastItem_Satisfies_Preserves_ReadOnly_Set_Item_Source()
    {
        IReadOnlyList<IReadOnlySet<int>> items = new List<IReadOnlySet<int>>
        {
            new HashSet<int> { 1, 2, 3 }
        };

        await Assert.That(items).LastItem().Satisfies(item => item.IsSupersetOf(new[] { 1, 2 }));
    }
}
