using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

[Retry(3), NotInParallel(nameof(RetryTests), Order = 1)]
public class RetryTests
{
    public static int RetryCount1 { get; private set; }
    public static int RetryCount2 { get; private set; }
    public static int RetryCount3 { get; private set; }
    
    [Test]
    [Retry(1)]
    public void One()
    {
        RetryCount1++;
        throw new Exception();
    }
    
    [Test]
    [Retry(2)]
    public void Two()
    {
        RetryCount2++;
        throw new Exception();
    }
    
    [Test]
    public void Three()
    {
        RetryCount3++;
        throw new Exception();
    }

    [Test, NotInParallel(nameof(RetryTests), Order = 2)]
    public async Task AssertCounts()
    {
        await Assert.That(RetryCount1).IsEqualTo(2);
        await Assert.That(RetryCount2).IsEqualTo(3);
        await Assert.That(RetryCount3).IsEqualTo(4);
    }
}