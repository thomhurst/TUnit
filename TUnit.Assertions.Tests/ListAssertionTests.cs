namespace TUnit.Assertions.Tests;

public class ListAssertionTests
{
    // Basic list tests - inherits collection capabilities
    [Test]
    public async Task Test_List_IsEmpty()
    {
        IList<int> list = new List<int>();
        await Assert.That(list).IsEmpty();
    }

    [Test]
    public async Task Test_List_IsNotEmpty()
    {
        IList<int> list = new List<int> { 1, 2, 3 };
        await Assert.That(list).IsNotEmpty();
    }

    [Test]
    public async Task Test_List_Contains()
    {
        IList<int> list = new List<int> { 1, 2, 3, 4, 5 };
        await Assert.That(list).Contains(3);
    }

    [Test]
    public async Task Test_List_DoesNotContain()
    {
        IList<int> list = new List<int> { 1, 2, 3, 4, 5 };
        await Assert.That(list).DoesNotContain(10);
    }

    [Test]
    public async Task Test_List_Count_IsEqualTo()
    {
        IList<int> list = new List<int> { 1, 2, 3 };
        await Assert.That(list).Count().IsEqualTo(3);
    }

    // Index-based methods - unique to IList<T>
    [Test]
    public async Task Test_List_HasItemAt_Passes()
    {
        IList<string> list = new List<string> { "first", "second", "third" };
        await Assert.That(list).HasItemAt(0, "first");
    }

    [Test]
    public async Task Test_List_HasItemAt_MiddleIndex_Passes()
    {
        IList<string> list = new List<string> { "first", "second", "third" };
        await Assert.That(list).HasItemAt(1, "second");
    }

    [Test]
    public async Task Test_List_HasItemAt_LastIndex_Passes()
    {
        IList<string> list = new List<string> { "first", "second", "third" };
        await Assert.That(list).HasItemAt(2, "third");
    }

    [Test]
    public async Task Test_List_HasItemAt_Fails_WrongValue()
    {
        IList<string> list = new List<string> { "first", "second", "third" };
        var action = async () => await Assert.That(list).HasItemAt(0, "wrong");

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("item at index 0 was first");
    }

    [Test]
    public async Task Test_List_HasItemAt_Fails_IndexOutOfRange()
    {
        IList<string> list = new List<string> { "first", "second", "third" };
        var action = async () => await Assert.That(list).HasItemAt(10, "value");

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("index 10 is out of range");
    }

    // ItemAt() method - for chained assertions
    [Test]
    public async Task Test_List_ItemAt_IsEqualTo_Passes()
    {
        IList<int> list = new List<int> { 10, 20, 30 };
        await Assert.That(list).ItemAt(1).IsEqualTo(20);
    }

    [Test]
    public async Task Test_List_ItemAt_IsNotEqualTo_Passes()
    {
        IList<int> list = new List<int> { 10, 20, 30 };
        await Assert.That(list).ItemAt(1).IsNotEqualTo(99);
    }

    [Test]
    public async Task Test_List_ItemAt_IsNull_Passes()
    {
        IList<string?> list = new List<string?> { "first", null, "third" };
        await Assert.That(list).ItemAt(1).IsNull();
    }

    [Test]
    public async Task Test_List_ItemAt_IsNotNull_Passes()
    {
        IList<string?> list = new List<string?> { "first", null, "third" };
        await Assert.That(list).ItemAt(0).IsNotNull();
    }

    // FirstItem() method
    [Test]
    public async Task Test_List_FirstItem_IsEqualTo_Passes()
    {
        IList<int> list = new List<int> { 100, 200, 300 };
        await Assert.That(list).FirstItem().IsEqualTo(100);
    }

    [Test]
    public async Task Test_List_FirstItem_IsNotEqualTo_Passes()
    {
        IList<int> list = new List<int> { 100, 200, 300 };
        await Assert.That(list).FirstItem().IsNotEqualTo(999);
    }

    // LastItem() method
    [Test]
    public async Task Test_List_LastItem_IsEqualTo_Passes()
    {
        IList<int> list = new List<int> { 100, 200, 300 };
        await Assert.That(list).LastItem().IsEqualTo(300);
    }

    [Test]
    public async Task Test_List_LastItem_IsNotEqualTo_Passes()
    {
        IList<int> list = new List<int> { 100, 200, 300 };
        await Assert.That(list).LastItem().IsNotEqualTo(999);
    }

    [Test]
    public async Task Test_List_LastItem_Fails_EmptyList()
    {
        IList<int> list = new List<int>();
        var action = async () => await Assert.That(list).LastItem().IsEqualTo(1);

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("list was empty");
    }

    // And/Or chaining preserves list type
    [Test]
    public async Task Test_List_And_Chaining_Works()
    {
        IList<int> list = new List<int> { 1, 2, 3 };
        await Assert.That(list).IsNotEmpty().And.Contains(2);
    }

    [Test]
    public async Task Test_List_And_Chaining_With_HasItemAt()
    {
        IList<string> list = new List<string> { "a", "b", "c" };
        await Assert.That(list)
            .HasItemAt(0, "a")
            .And.HasItemAt(2, "c");
    }

    [Test]
    public async Task Test_List_Or_Chaining_Works()
    {
        IList<int> list = new List<int> { 1, 2, 3 };
        await Assert.That(list)
            .Contains(1)
            .Or.Contains(99); // First one passes, so overall passes
    }

    // Array as IList<T>
    [Test]
    public async Task Test_Array_As_List_Works()
    {
        IList<int> list = new[] { 1, 2, 3 };
        await Assert.That(list).HasItemAt(0, 1);
        await Assert.That(list).FirstItem().IsEqualTo(1);
        await Assert.That(list).LastItem().IsEqualTo(3);
    }

    // Inherited collection methods still work
    [Test]
    public async Task Test_List_InheritsCollectionMethods()
    {
        IList<int> list = new List<int> { 1, 2, 3, 4, 5 };

        await Assert.That(list).HasCount(5);
        await Assert.That(list).All(x => x > 0);
        await Assert.That(list).Any(x => x == 3);
    }

    [Test]
    public async Task Test_List_HasSingleItem()
    {
        IList<int> list = new List<int> { 42 };
        await Assert.That(list).HasSingleItem();
    }

    [Test]
    public async Task Test_List_IsInOrder()
    {
        IList<int> list = new List<int> { 1, 2, 3, 4, 5 };
        await Assert.That(list).IsInOrder();
    }

    [Test]
    public async Task Test_List_IsInDescendingOrder()
    {
        IList<int> list = new List<int> { 5, 4, 3, 2, 1 };
        await Assert.That(list).IsInDescendingOrder();
    }

    [Test]
    public async Task Test_List_HasDistinctItems()
    {
        IList<int> list = new List<int> { 1, 2, 3, 4, 5 };
        await Assert.That(list).HasDistinctItems();
    }

    // ItemAt with Satisfies
    [Test]
    public async Task Test_List_ItemAt_Satisfies_Passes()
    {
        IList<int> list = new List<int> { 10, 20, 30 };
        await Assert.That(list).ItemAt(1).Satisfies(item => item.IsPositive());
    }

    // LastItem with Satisfies
    [Test]
    public async Task Test_List_LastItem_Satisfies_Passes()
    {
        IList<int> list = new List<int> { 10, 20, 30 };
        await Assert.That(list).LastItem().Satisfies(item => item.IsEqualTo(30));
    }
}
