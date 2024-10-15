using TUnit.Core.Exceptions;

namespace TUnit.Assertions.Tests;

public class SkipTests
{
    [Test]
    public async Task Skip_Test_Throws_SkipTestException()
    {
        string reason = "foo";
        var action = () => Skip.Test(reason);

        var exception = await Assert.That(action).ThrowsExactly<SkipTestException>();
        await Assert.That(exception!.Reason).IsEqualTo(reason);
    }

    [Test]
    public async Task Skip_Unless_Throws_SkipTestException_When_Condition_Is_False()
    {
        bool condition = false;
        string reason = "foo";
        var action = () => Skip.Unless(condition, reason);

        var exception = await Assert.That(action).ThrowsExactly<SkipTestException>();
        await Assert.That(exception!.Reason).IsEqualTo(reason);
    }

    [Test]
    public async Task Skip_Unless_Does_Not_Throw_When_Condition_Is_True()
    {
        bool condition = true;
        string reason = "foo";
        var action = () => Skip.Unless(condition, reason);

        var exception = await Assert.That(action).ThrowsNothing();
    }

    [Test]
    public async Task Skip_When_Throws_SkipTestException_When_Condition_Is_True()
    {
        bool condition = true;
        string reason = "foo";
        var action = () => Skip.When(condition, reason);

        var exception = await Assert.That(action).ThrowsExactly<SkipTestException>();
        await Assert.That(exception!.Reason).IsEqualTo(reason);
    }

    [Test]
    public async Task Skip_When_Does_Not_Throw_When_Condition_Is_False()
    {
        bool condition = false;
        string reason = "foo";
        var action = () => Skip.When(condition, reason);

        var exception = await Assert.That(action).ThrowsNothing();
    }
}
