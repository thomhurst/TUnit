using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests.AssertConditions;

public class BecauseTests
{
    [Test]
    public async Task Include_Because_Reason_In_Message()
    {
        var because = "I want to test 'because'";
        var variable = true;

        var action = async () =>
        {
            await Assert.That(variable).IsFalse().Because(because);
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(action);
        await Assert.That(exception.Message).Contains(because);
    }

    [Test]
    [TestCase("we prefix the reason", "because we prefix the reason")]
    [TestCase("  we ignore whitespace", "because we ignore whitespace")]
    [TestCase("because we honor a leading 'because'", "because we honor a leading 'because'")]
    public async Task Prefix_Because_Message(string because, string expectedWithPrefix)
    {
        var variable = true;

        var action = async () =>
        {
            await Assert.That(variable).IsFalse().Because(because);
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(action);
        await Assert.That(exception.Message).Contains(expectedWithPrefix);
    }

    [Test]
    public async Task Honor_Already_Present_Because_Prefix()
    {
        var because = "because we honor a leading 'because'";
        var variable = true;

        var action = async () =>
        {
            await Assert.That(variable).IsFalse().Because(because);
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(action);
        await Assert.That(exception.Message).Contains(because)
            .And.DoesNotContain($"because {because}");
    }

    [Test]
    public async Task Without_Because_Use_Empty_String()
    {
        var variable = true;

        var action = async () =>
        {
            await Assert.That(variable).IsFalse();
        };

        var exception = await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(action);
        await Assert.That(exception.Message).Contains("Expected variable to be False, but found True");
    }
}