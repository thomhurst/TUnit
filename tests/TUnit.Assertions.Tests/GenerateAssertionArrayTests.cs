using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests that [GenerateAssertion] works correctly when targeting concrete array types.
/// The source generator creates an extension method on IAssertionSource&lt;string[]&gt;
/// and ArrayAssertion&lt;string&gt; (returned by Assert.That(string[])) implements that interface.
/// </summary>
public class GenerateAssertionArrayTests
{
    [Test]
    public async Task ContainsMessage_ExactMatch_Passes()
    {
        string[] items = ["food", "bard"];
        await Assert.That(items).ContainsMessage("food", true);
    }

    [Test]
    public async Task ContainsMessage_PartialMatch_Passes()
    {
        string[] items = ["food", "bard"];
        await Assert.That(items).ContainsMessage("foo", false);
    }

    [Test]
    public async Task ContainsMessage_NoMatch_Fails()
    {
        string[] items = ["food", "bard"];
        var action = async () => await Assert.That(items).ContainsMessage("xyz", true);

        await Assert.That(action).Throws<AssertionException>();
    }
}

public static partial class GenerateAssertionArrayTestExtensions
{
    [GenerateAssertion(ExpectationMessage = "to contain message '{needle}'")]
    public static bool ContainsMessage(this string[] strings, string needle, bool exact = true)
    {
        return strings.Any(x => exact ? x == needle : x.Contains(needle));
    }
}
