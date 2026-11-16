using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.AssertConditions;

/// <summary>
/// <c>Because</c> must not change what the rest of the chain can do: the awaited value, <c>Item</c>,
/// <c>And.Value</c> and the typed <c>And</c> stay as they are without a reason.
/// </summary>
public class BecauseChainingTests
{
    [Test]
    public async Task HasSingleItem_Because_Awaits_The_Item()
    {
        int item = await Assert.That(new[] { 7 }).HasSingleItem().Because("there is one order");

        await Assert.That(item).IsEqualTo(7);
    }

    [Test]
    public async Task HasSingleItem_Predicate_Because_Awaits_The_Item()
    {
        List<int> values = [1, 2, 3];

        int item = await Assert.That(values).HasSingleItem(x => x > 2).Because("one value is above two");

        await Assert.That(item).IsEqualTo(3);
    }

    [Test]
    public async Task Contains_Predicate_Because_Awaits_The_Item()
    {
        List<int> values = [1, 2, 3];

        int item = await Assert.That(values).Contains(x => x > 1).Because("a value is above one");

        await Assert.That(item).IsEqualTo(2);
    }

    [Test]
    public async Task IsNotNull_Because_Awaits_A_Non_Null_Value()
    {
        string? text = "abc";

        string value = await Assert.That(text).IsNotNull().Because("the text is set");

        await Assert.That(value.Length).IsEqualTo(3);
    }

    [Test]
    public async Task Generated_Assertion_Result_Because_Awaits_The_Result()
    {
        IEnumerable<string> words = ["foo", "bar"];

        string match = await Assert.That(words).FindsWordContaining("ar").Because("bar is in the list");

        await Assert.That(match).IsEqualTo("bar");
    }

    [Test]
    public async Task Generated_Async_Assertion_Result_Because_Awaits_The_Result()
    {
        IEnumerable<string> words = ["foo", "bar"];

        string match = await Assert.That(words).FindsWordContainingAsync("ar").Because("bar is in the list");

        await Assert.That(match).IsEqualTo("bar");
    }

    [Test]
    public async Task HasSingleItem_Because_Keeps_Item()
    {
        await Assert.That(new[] { 7 }).HasSingleItem().Because("there is one order").Item.IsEqualTo(7);
    }

    [Test]
    public async Task HasSingleItem_Because_Keeps_The_Collection_And()
    {
        List<int> values = [1, 2, 3];

        await Assert.That(values).HasSingleItem(x => x > 2).Because("one value is above two").And.Count().IsEqualTo(3);
    }

    [Test]
    public async Task ReadOnlyDictionary_ContainsKey_Because_Keeps_Value()
    {
        IReadOnlyDictionary<string, int> values = new Dictionary<string, int> { ["a"] = 1 };

        await Assert.That(values).ContainsKey("a").Because("a is configured").And.Value.IsEqualTo(1);
    }

    [Test]
    public async Task MutableDictionary_ContainsKey_Because_Keeps_Value()
    {
        var values = new Dictionary<string, int> { ["a"] = 1 };

        await Assert.That(values).ContainsKey("a").Because("a is configured").And.Value.IsEqualTo(1);
    }

    [Test]
    public async Task ReadOnlyDictionary_ContainsKey_Because_Then_Using_Keeps_The_Reason()
    {
        IReadOnlyDictionary<string, int> values = new Dictionary<string, int> { ["a"] = 1 };

        await Assert.That(values).ContainsKey("A").Because("a is configured").Using(StringComparer.OrdinalIgnoreCase);

        var action = async () =>
        {
            await Assert.That(values).ContainsKey("b").Because("b is configured").Using(StringComparer.OrdinalIgnoreCase);
        };

        await Assert.That(action).Throws<AssertionException>().WithMessageContaining("because b is configured");
    }

    [Test]
    public async Task ReadOnlyDictionary_ContainsKey_Because_Then_Using_A_Predicate_Keeps_The_Reason()
    {
        IReadOnlyDictionary<string, int> values = new Dictionary<string, int> { ["a"] = 1 };

        var action = async () =>
        {
            await Assert.That(values).ContainsKey("b").Because("b is configured")
                .Using((x, y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase));
        };

        await Assert.That(action).Throws<AssertionException>().WithMessageContaining("because b is configured");
    }

    [Test]
    public async Task MutableDictionary_ContainsKey_Because_Then_Using_Keeps_The_Reason()
    {
        var values = new Dictionary<string, int> { ["a"] = 1 };

        await Assert.That(values).ContainsKey("A").Because("a is configured").Using((x, y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase));

        var action = async () =>
        {
            await Assert.That(values).ContainsKey("b").Because("b is configured").Using(StringComparer.OrdinalIgnoreCase);
        };

        await Assert.That(action).Throws<AssertionException>().WithMessageContaining("because b is configured");
    }

    [Test]
    public async Task MutableDictionary_ContainsKey_Because_Then_Using_A_Predicate_Keeps_The_Reason()
    {
        var values = new Dictionary<string, int> { ["a"] = 1 };

        var action = async () =>
        {
            await Assert.That(values).ContainsKey("b").Because("b is configured")
                .Using((x, y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase));
        };

        await Assert.That(action).Throws<AssertionException>().WithMessageContaining("because b is configured");
    }

    [Test]
    public async Task ReadOnlyDictionary_ContainsKey_Using_Keeps_The_Assertion_Before_And()
    {
        IReadOnlyDictionary<string, int> values = new Dictionary<string, int> { ["a"] = 1 };

        var action = async () =>
        {
            await Assert.That(values).IsEmpty().And.ContainsKey("A").Using(StringComparer.OrdinalIgnoreCase);
        };

        // ContainsKey("A") passes with the comparer, so the failure can only come from IsEmpty().
        await Assert.That(action).Throws<AssertionException>().WithMessageContaining("Expected to be empty");
    }

    [Test]
    public async Task MutableDictionary_ContainsKey_Using_Keeps_The_Assertion_Before_And()
    {
        var values = new Dictionary<string, int> { ["a"] = 1 };

        var action = async () =>
        {
            await Assert.That(values).IsEmpty().And.ContainsKey("A").Using(StringComparer.OrdinalIgnoreCase);
        };

        // ContainsKey("A") passes with the comparer, so the failure can only come from IsEmpty().
        await Assert.That(action).Throws<AssertionException>().WithMessageContaining("Expected to be empty");
    }

    [Test]
    public async Task Memory_HasSingleItem_Because_Keeps_Item()
    {
        Memory<int> values = new[] { 7 };

        await Assert.That(values).HasSingleItem().Because("there is one order").Item.IsEqualTo(7);
    }

    [Test]
    public async Task HasSingleItem_Because_Reason_Is_In_The_Failure()
    {
        var action = async () =>
        {
            _ = await Assert.That(new[] { 1, 2 }).HasSingleItem().Because("there is one order");
        };

        await Assert.That(action).Throws<AssertionException>().WithMessageContaining("because there is one order");
    }
}

/// <summary>
/// A generated assertion whose result is the matching word, for the <c>Because</c> tests.
/// </summary>
public static partial class BecauseChainingAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to contain a word containing '{part}'")]
    public static AssertionResult<string> FindsWordContaining(this IEnumerable<string> words, string part)
    {
        var match = words.FirstOrDefault(word => word.Contains(part));
        if (match is not null)
        {
            return AssertionResult<string>.Passed(match);
        }

        return AssertionResult.Failed($"no word contains {part}");
    }

    [GenerateAssertion(ExpectationMessage = "to contain a word containing '{part}'")]
    public static async Task<AssertionResult<string>> FindsWordContainingAsync(this IEnumerable<string> words, string part)
    {
        await Task.Yield();
        return FindsWordContaining(words, part);
    }
}
