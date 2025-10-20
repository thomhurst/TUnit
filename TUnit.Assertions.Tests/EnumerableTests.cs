using System.Collections;
namespace TUnit.Assertions.Tests;

public class EnumerableTests
{
    [Test]
    public async Task Enumerable_Contains_Item_Good()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array).Contains(1);
    }

    [Test]
    public async Task Enumerable_Contains_Item_Bad()
    {
        int[] array = [1, 2, 3];

        await Assert.That(
                async () => await Assert.That(array).Contains(4)
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task Enumerable_Contains_Matcher_Good()
    {
        int[] array = [1, 2, 3];

        var item = await Assert.That(array).Contains(x => x == 1);
        await Assert.That(item).IsEqualTo(1);
    }

    [Test]
    public async Task Enumerable_Contains_Matcher_Bad()
    {
        int[] array = [1, 2, 3];

        await Assert.That(
            async () => await Assert.That(array).Contains(x => x == 4)
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task Enumerable_ContainsOnly_Matcher_Good()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array).ContainsOnly(x => x < 10);
    }

    [Test]
    public async Task Enumerable_ContainsOnly_Matcher_Bad()
    {
        int[] array = [1, 2, 3];

        await Assert.That(
            async () => await Assert.That(array).ContainsOnly(x => x < 3)
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task Enumerable_DoesNotContain_Item_Good()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array).DoesNotContain(5);
    }

    [Test]
    public async Task Enumerable_DoesNotContain_Item_Bad()
    {
        int[] array = [1, 2, 3];

        await Assert.That(
            async () => await Assert.That(array).DoesNotContain(3)
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task Enumerable_DoesNotContain_Matcher_Good()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array).DoesNotContain(x => x > 10);
    }

    [Test]
    public async Task Enumerable_DoesNotContain_Matcher_Bad()
    {
        int[] array = [1, 2, 3];

        await Assert.That(
            async () => await Assert.That(array).DoesNotContain(x => x < 3)
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task Enumerable_Ordered_Good()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array).IsInOrder();
    }

    [Test]
    public async Task Enumerable_Ordered_Bad()
    {
        int[] array = [1, 3, 2];

        await Assert.That(
            async () => await Assert.That(array).IsInOrder()
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task Enumerable_Ordered_Descending_Good()
    {
        int[] array = [3, 2, 1];

        await Assert.That(array).IsInDescendingOrder();
    }

    [Test]
    public async Task Enumerable_Ordered_Descending_Bad()
    {
        int[] array = [3, 1, 2];

        await Assert.That(
            async () => await Assert.That(array).IsInDescendingOrder()
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task Untyped_Enumerable()
    {
        int[] array = [1, 2, 3];

        IEnumerable<int> enumerable = array;

        await Assert.That(enumerable).IsInOrder();
    }

    [Test]
    public async Task Untyped_Enumerable_EqualTo()
    {
        int[] array = [1, 2, 3];

        // Use generic IEnumerable<int> to preserve reference identity
        IEnumerable<int> enumerable = array;

        await Assert.That(enumerable).IsSameReferenceAs(enumerable);
    }

    [Test]
    public async Task Untyped_Enumerable_ReferenceEqualTo()
    {
        int[] array = [1, 2, 3];

        // Use generic IEnumerable<int> to preserve reference identity
        IEnumerable<int> enumerable = array;

        await Assert.That(enumerable).IsSameReferenceAs(enumerable);
    }

    // ============ IsOrderedBy/IsOrderedByDescending TESTS (Issue #3391) ============

    public class TestItem
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    [Test]
    public async Task IsOrderedBy_WithKeySelector_Ascending_Good()
    {
        var items = new[]
        {
            new TestItem { Name = "Alice", Value = 1 },
            new TestItem { Name = "Bob", Value = 2 },
            new TestItem { Name = "Charlie", Value = 3 }
        };

        await Assert.That(items).IsOrderedBy(i => i.Name);
    }

    [Test]
    public async Task IsOrderedBy_WithKeySelector_Ascending_Bad()
    {
        var items = new[]
        {
            new TestItem { Name = "Charlie", Value = 3 },
            new TestItem { Name = "Alice", Value = 1 },
            new TestItem { Name = "Bob", Value = 2 }
        };

        await Assert.That(
            async () => await Assert.That(items).IsOrderedBy(i => i.Name)
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task IsOrderedBy_WithNumericKeySelector_Good()
    {
        var items = new[]
        {
            new TestItem { Name = "Item1", Value = 10 },
            new TestItem { Name = "Item2", Value = 20 },
            new TestItem { Name = "Item3", Value = 30 }
        };

        await Assert.That(items).IsOrderedBy(i => i.Value);
    }

    [Test]
    public async Task IsOrderedByDescending_WithKeySelector_Good()
    {
        var items = new[]
        {
            new TestItem { Name = "Charlie", Value = 30 },
            new TestItem { Name = "Bob", Value = 20 },
            new TestItem { Name = "Alice", Value = 10 }
        };

        await Assert.That(items).IsOrderedByDescending(i => i.Name);
    }

    [Test]
    public async Task IsOrderedByDescending_WithKeySelector_Bad()
    {
        var items = new[]
        {
            new TestItem { Name = "Alice", Value = 10 },
            new TestItem { Name = "Bob", Value = 20 },
            new TestItem { Name = "Charlie", Value = 30 }
        };

        await Assert.That(
            async () => await Assert.That(items).IsOrderedByDescending(i => i.Name)
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task IsOrderedBy_WithCustomComparer_Good()
    {
        var items = new[] { "apple", "BANANA", "cherry" };

        await Assert.That(items).IsOrderedBy(x => x, StringComparer.OrdinalIgnoreCase);
    }

    [Test]
    public async Task IsOrderedByDescending_WithNumericKeySelector_Good()
    {
        var items = new[]
        {
            new TestItem { Name = "Item3", Value = 30 },
            new TestItem { Name = "Item2", Value = 20 },
            new TestItem { Name = "Item1", Value = 10 }
        };

        await Assert.That(items).IsOrderedByDescending(i => i.Value);
    }

    [Test]
    public async Task IsOrderedBy_IEnumerable_InferenceTest()
    {
        IEnumerable<TestItem> items = new[]
        {
            new TestItem { Name = "A", Value = 1 },
            new TestItem { Name = "B", Value = 2 },
            new TestItem { Name = "C", Value = 3 }
        };

        // Test that type inference works with IEnumerable<T>
        await Assert.That(items).IsOrderedBy(i => i.Name);
    }

    [Test]
    public async Task IQueryable()
    {
        IQueryable<TestItem> items = new EnumerableQuery<TestItem>([]);

        // Test that type inference works with IEnumerable<T>
        await Assert.That(items).IsOrderedBy(i => i.Name);
    }
}
