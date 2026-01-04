namespace TUnit.Assertions.Tests;

public class ReadOnlyListAssertionTests
{
    // HasItemAt tests
    [Test]
    public async Task Test_HasItemAt_Passes_When_Item_Matches()
    {
        IReadOnlyList<string> list = new List<string> { "a", "b", "c" };
        await Assert.That(list).HasItemAt(1, "b");
    }

    [Test]
    public async Task Test_HasItemAt_Fails_When_Item_Does_Not_Match()
    {
        IReadOnlyList<string> list = new List<string> { "a", "b", "c" };
        var action = async () => await Assert.That(list).HasItemAt(1, "x");

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("item at index 1 was b");
    }

    [Test]
    public async Task Test_HasItemAt_Fails_When_Index_Out_Of_Range()
    {
        IReadOnlyList<string> list = new List<string> { "a", "b", "c" };
        var action = async () => await Assert.That(list).HasItemAt(10, "x");

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("index 10 is out of range");
    }

    [Test]
    public async Task Test_HasItemAt_With_Custom_Comparer()
    {
        IReadOnlyList<string> list = new List<string> { "ABC", "DEF", "GHI" };
        await Assert.That(list).HasItemAt(0, "abc", StringComparer.OrdinalIgnoreCase);
    }

    // ItemAt tests
    [Test]
    public async Task Test_ItemAt_Passes_With_IsEqualTo()
    {
        IReadOnlyList<int> list = new List<int> { 10, 20, 30 };
        await Assert.That(list).ItemAt(1).IsEqualTo(20);
    }

    [Test]
    public async Task Test_ItemAt_Fails_With_Wrong_Value()
    {
        IReadOnlyList<int> list = new List<int> { 10, 20, 30 };
        var action = async () => await Assert.That(list).ItemAt(0).IsEqualTo(99);

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("10");
    }

    [Test]
    public async Task Test_ItemAt_Out_Of_Range_Fails()
    {
        IReadOnlyList<int> list = new List<int> { 10, 20, 30 };
        var action = async () => await Assert.That(list).ItemAt(5).IsEqualTo(99);

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("out of range");
    }

    [Test]
    public async Task Test_ItemAt_IsNotEqualTo_Passes()
    {
        IReadOnlyList<int> list = new List<int> { 10, 20, 30 };
        await Assert.That(list).ItemAt(0).IsNotEqualTo(99);
    }

    [Test]
    public async Task Test_ItemAt_IsNull_Passes()
    {
        IReadOnlyList<string?> list = new List<string?> { "a", null, "c" };
        await Assert.That(list).ItemAt(1).IsNull();
    }

    [Test]
    public async Task Test_ItemAt_IsNotNull_Passes()
    {
        IReadOnlyList<string?> list = new List<string?> { "a", null, "c" };
        await Assert.That(list).ItemAt(0).IsNotNull();
    }

    // FirstItem tests
    [Test]
    public async Task Test_FirstItem_Passes()
    {
        IReadOnlyList<string> list = new List<string> { "first", "second", "third" };
        await Assert.That(list).FirstItem().IsEqualTo("first");
    }

    [Test]
    public async Task Test_FirstItem_Empty_List_Fails()
    {
        IReadOnlyList<string> list = new List<string>();
        var action = async () => await Assert.That(list).FirstItem().IsEqualTo("x");

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("out of range");
    }

    // LastItem tests
    [Test]
    public async Task Test_LastItem_Passes()
    {
        IReadOnlyList<string> list = new List<string> { "first", "second", "third" };
        await Assert.That(list).LastItem().IsEqualTo("third");
    }

    [Test]
    public async Task Test_LastItem_Empty_List_Fails()
    {
        IReadOnlyList<string> list = new List<string>();
        var action = async () => await Assert.That(list).LastItem().IsEqualTo("x");

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("empty");
    }

    [Test]
    public async Task Test_LastItem_Single_Item()
    {
        IReadOnlyList<string> list = new List<string> { "only" };
        await Assert.That(list).LastItem().IsEqualTo("only");
    }

    [Test]
    public async Task Test_LastItem_IsNotEqualTo_Passes()
    {
        IReadOnlyList<string> list = new List<string> { "first", "second", "third" };
        await Assert.That(list).LastItem().IsNotEqualTo("first");
    }

    [Test]
    public async Task Test_LastItem_IsNull_Passes()
    {
        IReadOnlyList<string?> list = new List<string?> { "a", "b", null };
        await Assert.That(list).LastItem().IsNull();
    }

    [Test]
    public async Task Test_LastItem_IsNotNull_Passes()
    {
        IReadOnlyList<string?> list = new List<string?> { "a", "b", "c" };
        await Assert.That(list).LastItem().IsNotNull();
    }

    // Inherited collection methods
    [Test]
    public async Task Test_IReadOnlyList_IsEmpty_Works()
    {
        IReadOnlyList<int> list = new List<int>();
        await Assert.That(list).IsEmpty();
    }

    [Test]
    public async Task Test_IReadOnlyList_IsNotEmpty_Works()
    {
        IReadOnlyList<int> list = new List<int> { 1, 2, 3 };
        await Assert.That(list).IsNotEmpty();
    }

    [Test]
    public async Task Test_IReadOnlyList_Contains_Works()
    {
        IReadOnlyList<int> list = new List<int> { 1, 2, 3 };
        await Assert.That(list).Contains(2);
    }

    [Test]
    public async Task Test_IReadOnlyList_HasCount_Works()
    {
        IReadOnlyList<int> list = new List<int> { 1, 2, 3, 4, 5 };
        await Assert.That(list).HasCount().EqualTo(5);
    }

    // Chaining tests
    [Test]
    public async Task Test_IReadOnlyList_And_Chain_Works()
    {
        IReadOnlyList<int> list = new List<int> { 1, 2, 3 };
        await Assert.That(list)
            .HasItemAt(0, 1)
            .And.HasItemAt(1, 2)
            .And.HasItemAt(2, 3);
    }

    [Test]
    public async Task Test_IReadOnlyList_Or_Chain_Works()
    {
        IReadOnlyList<int> list = new List<int> { 1, 2, 3 };
        await Assert.That(list)
            .HasItemAt(0, 1)
            .Or.HasItemAt(0, 99); // First passes, so overall passes
    }

    // Null handling
    [Test]
    public async Task Test_IReadOnlyList_Null_Fails()
    {
        IReadOnlyList<int>? list = null;
        var action = async () => await Assert.That(list!).IsEmpty();

        var exception = await Assert.That(action).Throws<AssertionException>();
        await Assert.That(exception.Message).Contains("null");
    }

    // Satisfies tests
    [Test]
    public async Task Test_ItemAt_Satisfies_Passes()
    {
        IReadOnlyList<int> list = new List<int> { 10, 20, 30 };
        await Assert.That(list).ItemAt(1).Satisfies(item => item.IsGreaterThan(15));
    }

    [Test]
    public async Task Test_LastItem_Satisfies_Passes()
    {
        IReadOnlyList<string> list = new List<string> { "a", "bb", "ccc" };
        await Assert.That(list).LastItem().Satisfies(item => item.HasLength(3));
    }
}
