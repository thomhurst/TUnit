namespace TUnit.TestProject.Bugs._3633;

public class Tests
{
    [Test]
    public async Task Assertion_Should_Stop_Execution_When_Failing_Outside_AssertMultiple()
    {
        string? nullValue = null;

        // This should throw immediately and stop execution
        await Assert.That(nullValue).IsNotNull();

        // This line should NEVER be reached because the assertion above should have thrown
        var length = nullValue!.Length; // This would cause NullReferenceException if reached

        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task Assertion_Should_Not_Stop_Execution_When_Failing_Inside_AssertMultiple()
    {
        var valueReached = false;

        await Assert.That(() =>
        {
            using (Assert.Multiple())
            {
                string? nullValue = null;

                // This should NOT throw immediately - should accumulate
                Assert.That(nullValue).IsNotNull().GetAwaiter().GetResult();

                // This line SHOULD be reached even though assertion above failed
                valueReached = true;

                Assert.That(1).IsEqualTo(2).GetAwaiter().GetResult();
            }
        }).ThrowsException();

        // Verify that code after first assertion was executed
        await Assert.That(valueReached).IsTrue();
    }

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
}
