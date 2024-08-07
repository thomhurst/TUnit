using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

[RetryOperationCancelledException(3), NotInParallel(nameof(CustomRetryTests), Order = 1)]
public class CustomRetryTests
{
    public static int RetryCount1 { get; private set; }
    public static int RetryCount2 { get; private set; }
    public static int RetryCount3 { get; private set; }
    public static int RetryCount4 { get; private set; }
    
    [Test]
    [RetryOperationCancelledException(1)]
    public void One()
    {
        RetryCount1++;
        throw new OperationCanceledException();
    }
    
    [Test]
    [RetryNullReferenceException(2)]
    public void Two()
    {
        RetryCount2++;
        throw new OperationCanceledException();
    }
    
    [Test]
    public void Three()
    {
        RetryCount3++;
        throw new OperationCanceledException();
    }
    
    [Test]
    public void Four()
    {
        RetryCount4++;
        throw new NullReferenceException();
    }

    [Test, NotInParallel(nameof(CustomRetryTests), Order = 2)]
    public async Task AssertCounts()
    {
        await Assert.That(RetryCount1).Is.EqualTo(2);
        await Assert.That(RetryCount2).Is.EqualTo(1);
        await Assert.That(RetryCount3).Is.EqualTo(4);
        await Assert.That(RetryCount4).Is.EqualTo(1);
    }

    private class RetryOperationCancelledExceptionAttribute : RetryAttribute
    {
        public RetryOperationCancelledExceptionAttribute(int times) : base(times)
        {
        }

        public override Task<bool> ShouldRetry(TestContext context, Exception exception, int currentRetryCount)
        {
            return Task.FromResult(exception is OperationCanceledException);
        }
    }
    
    private class RetryNullReferenceExceptionAttribute : RetryAttribute
    {
        public RetryNullReferenceExceptionAttribute(int times) : base(times)
        {
        }

        public override Task<bool> ShouldRetry(TestContext context, Exception exception, int currentRetryCount)
        {
            return Task.FromResult(exception is NullReferenceException);
        }
    }
}