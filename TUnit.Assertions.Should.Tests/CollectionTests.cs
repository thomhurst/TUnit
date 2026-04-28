using TUnit.Assertions.Should;
using TUnit.Assertions.Should.Extensions;

namespace TUnit.Assertions.Should.Tests;

public class CollectionTests
{
    [Test]
    public async Task List_Contain()
    {
        var list = new List<int> { 1, 2, 3 };
        await list.Should().Contain(2);
    }

    [Test]
    public async Task List_NotContain()
    {
        var list = new List<int> { 1, 2, 3 };
        await list.Should().NotContain(99);
    }

    [Test]
    public async Task IReadOnlyList_Contain_infers_element_type()
    {
        IReadOnlyList<string> list = new[] { "a", "b", "c" };
        await list.Should().Contain("b");
    }

    [Test]
    public async Task Array_Contain()
    {
        var arr = new[] { 1, 2, 3 };
        await arr.Should().Contain(2);
    }

    [Test]
    public async Task IEnumerable_Contain()
    {
        IEnumerable<int> seq = Enumerable.Range(1, 5);
        await seq.Should().Contain(3);
    }

    [Test]
    public async Task BeInOrder()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        await list.Should().BeInOrder();
    }

    [Test]
    public async Task BeInDescendingOrder()
    {
        var list = new List<int> { 5, 4, 3, 2, 1 };
        await list.Should().BeInDescendingOrder();
    }

    [Test]
    public async Task All_predicate()
    {
        var list = new List<int> { 2, 4, 6 };
        await list.Should().All(x => x % 2 == 0);
    }

    [Test]
    public async Task Any_predicate()
    {
        var list = new List<int> { 1, 2, 3 };
        await list.Should().Any(x => x > 2);
    }

    [Test]
    public async Task HaveSingleItem()
    {
        var list = new List<int> { 42 };
        await list.Should().HaveSingleItem();
    }

    [Test]
    public async Task HaveSingleItem_with_predicate()
    {
        var list = new List<int> { 1, 2, 3 };
        await list.Should().HaveSingleItem(x => x == 2);
    }

    [Test]
    public async Task HaveDistinctItems()
    {
        var list = new List<int> { 1, 2, 3 };
        await list.Should().HaveDistinctItems();
    }

    [Test]
    public async Task HaveCount()
    {
        var list = new List<int> { 1, 2, 3 };
        await list.Should().HaveCount(3);
    }

    [Test]
    public async Task Contain_predicate()
    {
        var list = new List<int> { 1, 2, 3 };
        await list.Should().Contain(x => x > 2);
    }

    [Test]
    public async Task Empty_list_NotContain()
    {
        var list = new List<int>();
        await list.Should().NotContain(1);
    }

    [Test]
    public async Task Failure_message_contains_collection_method()
    {
        var list = new List<int> { 1, 2, 3 };
        var ex = await Assert.That(async () => await list.Should().Contain(99))
            .Throws<TUnit.Assertions.Exceptions.AssertionException>();
        await Assert.That(ex.Message).Contains("Contain");
    }

    [Test]
    public async Task Failure_message_uses_Should_flavored_expression()
    {
        var list = new List<int> { 1, 2, 3 };
        var ex = await Assert.That(async () => await list.Should().Contain(99))
            .Throws<TUnit.Assertions.Exceptions.AssertionException>();
        // Collection entry must mirror the value entry's expression style — ".Should().Method(...)"
        // rather than the underlying CollectionAssertion's "Assert.That(...)" form.
        await Assert.That(ex.Message).Contains(".Should().Contain(");
    }
}
