namespace TUnit.Assertions.Tests.AssertConditions;

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

        var exception = await Assert.ThrowsAsync<AssertionException>(action);
        await Assert.That(exception.Message).Contains(because);
    }

    [Test]
    [Arguments("we prefix the reason", "because we prefix the reason")]
    [Arguments("  we ignore whitespace", "because we ignore whitespace")]
    [Arguments("because we honor a leading 'because'", "because we honor a leading 'because'")]
    public async Task Prefix_Because_Message(string because, string expectedWithPrefix)
    {
        var variable = true;

        var action = async () =>
        {
            await Assert.That(variable).IsFalse().Because(because);
        };

        var exception = await Assert.ThrowsAsync<AssertionException>(action);
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

        var exception = await Assert.ThrowsAsync<AssertionException>(action);
        await Assert.That(exception.Message).Contains(because)
            .And.DoesNotContain("because because");
    }

    [Test]
    public async Task Without_Because_Use_Empty_String()
    {
        var expectedMessage = """
                              Expected variable to be equal to False
                              
                              but found True
                              
                              at Assert.That(variable).IsFalse()
                              """;

        var variable = true;

        var action = async () =>
        {
            await Assert.That(variable).IsFalse();
        };

        var exception = await Assert.ThrowsAsync<AssertionException>(action);
        await Assert.That(exception.Message).IsEqualTo(expectedMessage);
    }

    [Test]
    public async Task Apply_Because_Reasons_Only_On_Previous_Assertions()
    {
        var expectedMessage = """
                              Expected variable to be equal to True, because we only apply it to previous assertions
                               and to be equal to False
                              
                              but found True
                              
                              at Assert.That(variable).IsTrue().And.IsFalse()
                              """;
        var because = "we only apply it to previous assertions";
        var variable = true;

        var action = async () =>
        {
            await Assert.That(variable).IsTrue().Because(because)
                .And.IsFalse();
        };

        var exception = await Assert.ThrowsAsync<AssertionException>(action);
        await Assert.That(exception.Message).IsEqualTo(expectedMessage);
    }

    [Test]
    public async Task Do_Not_Overwrite_Previous_Because_Reasons()
    {
        var because1 = "this is the first reason";
        var because2 = "this is the second reason";
        var variable = false;

        var action = async () =>
        {
            await Assert.That(variable).IsTrue().Because(because1)
                .And.IsFalse().Because(because2);
        };

        var exception = await Assert.ThrowsAsync<AssertionException>(action);
        await Assert.That(exception.Message).Contains(because1);
    }

    [Test]
    public async Task Apply_Because_Reason_When_Combining_With_And()
    {
        var because1 = "this is the first reason";
        var because2 = "this is the second reason";
        var variable = true;

        var action = async () =>
        {
            await Assert.That(variable).IsTrue().Because(because1)
                .And.IsFalse().Because(because2);
        };

        var exception = await Assert.ThrowsAsync<AssertionException>(action);
        await Assert.That(exception.Message).Contains(because2);
    }

    [Test]
    public async Task Apply_Because_Reason_When_Combining_With_Or()
    {
        var because1 = "this is the first reason";
        var because2 = "this is the second reason";
        var variable = true;

        var action = async () =>
        {
            await Assert.That(variable).IsFalse().Because(because1)
                .Or.IsFalse().Because(because2);
        };

        var exception = await Assert.ThrowsAsync<AssertionException>(action);
        await Assert.That(exception.Message).Contains(because1).And.Contains(because2);
    }
}