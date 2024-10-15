namespace TUnit.Assertions.Tests;

public class FailTests
{
    [Test]
    public async Task Fail_Test_Throws_AssertionException()
    {
        string reason = "foo";
        var action = () => Fail.Test(reason);

        var exception = await Assert.That(action).ThrowsExactly<AssertionException>();
        await Assert.That(exception!.Message).IsEqualTo(reason);
    }

    [Test]
    public async Task Fail_Unless_Throws_AssertionException_When_Condition_Is_False()
    {
        bool condition = false;
        string reason = "foo";
        var action = () => Fail.Unless(condition, reason);

        var exception = await Assert.That(action).ThrowsExactly<AssertionException>();
        await Assert.That(exception!.Message).IsEqualTo(reason);
    }

    [Test]
    public async Task Fail_Unless_Does_Not_Throw_When_Condition_Is_True()
    {
        bool condition = true;
        string reason = "foo";
        var action = () => Fail.Unless(condition, reason);

        var exception = await Assert.That(action).ThrowsNothing();
    }

    [Test]
    public async Task Fail_When_Throws_AssertionException_When_Condition_Is_True()
    {
        bool condition = true;
        string reason = "foo";
        var action = () => Fail.When(condition, reason);

        var exception = await Assert.That(action).ThrowsExactly<AssertionException>();
        await Assert.That(exception!.Message).IsEqualTo(reason);
    }

    [Test]
    public async Task Fail_When_Does_Not_Throw_When_Condition_Is_False()
    {
        bool condition = false;
        string reason = "foo";
        var action = () => Fail.When(condition, reason);

        var exception = await Assert.That(action).ThrowsNothing();
    }
}
