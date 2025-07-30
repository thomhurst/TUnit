namespace TUnit.TestProject;

[RetryOperationCancelledException(3), NotInParallel(nameof(CustomRetryTests), Order = 1)]
public class CustomRetryTests
{
    public static int ExecutionCount1 { get; private set; }
    public static int ExecutionCount2 { get; private set; }
    public static int ExecutionCount3 { get; private set; }
    public static int ExecutionCount4 { get; private set; }

    [Test]
    [RetryOperationCancelledException(1)]
    public void One()
    {
        ExecutionCount1++;
        throw new OperationCanceledException();
    }

    [Test]
    [RetryNullReferenceException(2)]
    public void Two()
    {
        ExecutionCount2++;
        throw new OperationCanceledException();
    }

    [Test]
    public void Three()
    {
        ExecutionCount3++;
        throw new OperationCanceledException();
    }

    [Test]
    public void Four()
    {
        ExecutionCount4++;
        throw new NullReferenceException();
    }

    [Test, NotInParallel(nameof(CustomRetryTests), Order = 2)]
    public async Task AssertCounts()
    {
        await Assert.That(ExecutionCount1).IsEqualTo(2);
        await Assert.That(ExecutionCount2).IsEqualTo(1);
        await Assert.That(ExecutionCount3).IsEqualTo(4);
        await Assert.That(ExecutionCount4).IsEqualTo(1);
    }

    public class RetryOperationCancelledExceptionAttribute(int times) : RetryAttribute(times)
    {
        public override Task<bool> ShouldRetry(TestContext context, Exception exception, int currentRetryCount)
        {
            return Task.FromResult(exception is OperationCanceledException);
        }
    }

    public class RetryNullReferenceExceptionAttribute(int times) : RetryAttribute(times)
    {
        public override Task<bool> ShouldRetry(TestContext context, Exception exception, int currentRetryCount)
        {
            return Task.FromResult(exception is NullReferenceException);
        }
    }
}
