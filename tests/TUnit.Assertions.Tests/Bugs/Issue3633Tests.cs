namespace TUnit.Assertions.Tests.Bugs;

public class Issue3633Tests
{
    [Test]
    public async Task IsNotNull_Should_Throw_Immediately_Outside_Scope()
    {
        string? nullValue = null;
        var exceptionThrown = false;

        try
        {
            await Assert.That(nullValue).IsNotNull();
        }
        catch (TUnit.Assertions.Exceptions.AssertionException)
        {
            exceptionThrown = true;
        }

        await Assert.That(exceptionThrown).IsTrue();
    }

    [Test]
    public async Task IsNotNull_Should_Throw_Before_NullReferenceException()
    {
        string? nullValue = null;

        await Assert.That(async () =>
        {
            await Assert.That(nullValue).IsNotNull();

            var length = nullValue.Length;
        }).ThrowsExactly<TUnit.Assertions.Exceptions.AssertionException>();
    }

    [Test]
    public async Task IsNotNull_Should_Return_NonNull_Value_When_Assertion_Passes()
    {
        string? nonNullValue = "test";

        var result = await Assert.That(nonNullValue).IsNotNull();

        if (result is null)
        {
            throw new Exception("IsNotNull returned null even though assertion passed!");
        }

        await Assert.That(result.Length).IsEqualTo(4);
    }

    [Test]
    public async Task IsNotNull_Should_Not_Stop_Execution_Inside_AssertMultiple()
    {
        var secondAssertionReached = false;

        await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                string? nullValue = null;
                await Assert.That(nullValue).IsNotNull();

                secondAssertionReached = true;

                await Assert.That(1).IsEqualTo(2);
            }
        }).ThrowsException();

        await Assert.That(secondAssertionReached).IsTrue();
    }
}
