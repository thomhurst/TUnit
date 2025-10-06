using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Tests.Bugs;

public class Tests3137
{
    [Test]
    public async Task AssertOnAsyncLambdaIsNotEvaluatedToEarly()
    {
        int count = 0;
        await Assert.That(() =>
        {
            count++;
            return Task.CompletedTask;
        }).RegisterAssertion(new TestAssertCondition(() => count, expectedIncrement: 1), []);
    }

    private class TestAssertCondition(Func<int> getCount, int expectedIncrement) : DelegateAssertCondition
    {
        private readonly int _before = getCount();

        protected override ValueTask<AssertionResult> GetResult(
            object? actualValue,
            Exception? exception,
            AssertionMetadata assertionMetadata)
        {
            var after = getCount();
            var actualIncrement = after - _before;
            return AssertionResult.FailIf(
                actualIncrement != expectedIncrement,
                $"Actual increment was {actualIncrement} but expected {expectedIncrement}.");

            // silence Codacy
            _ = actualValue;
            _ = exception;
            _ = assertionMetadata;
        }
    }
}
