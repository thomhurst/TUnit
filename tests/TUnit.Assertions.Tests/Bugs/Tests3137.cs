namespace TUnit.Assertions.Tests.Bugs;

public class Tests3137
{
    [Test]
    public async Task AssertOnAsyncLambdaIsNotEvaluatedTooEarly()
    {
        int count = 0;
        int countBefore = 0;

        // The lambda should not be evaluated until we actually invoke/await the assertion
        // Suppress analyzer warning because we intentionally don't await the assertion immediately
        #pragma warning disable TUnitAssertions0002
        var delegateAssertion = Assert.That(() =>
        {
            count++;
            return Task.CompletedTask;
        });
        #pragma warning restore TUnitAssertions0002

        // Lambda should NOT have been executed yet
        countBefore = count;
        await Assert.That(count).IsEqualTo(0);

        // Now execute an assertion on the delegate - this should execute the lambda
        try
        {
            // This will fail because the lambda doesn't throw, but that's expected
            await delegateAssertion.Throws<InvalidOperationException>();
        }
        catch
        {
            // Expected to fail
        }

        // Verify the lambda was executed exactly once
        await Assert.That(count - countBefore).IsEqualTo(1);
    }
}
