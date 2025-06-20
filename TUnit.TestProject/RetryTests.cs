using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Failure)]
[Retry(3)]
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

    [Test]
    [DependsOn(nameof(One), ProceedOnFailure = true)]
    [DependsOn(nameof(Two), ProceedOnFailure = true)]
    [DependsOn(nameof(Three), ProceedOnFailure = true)]
    public async Task AssertCounts()
    {
        await Assert.That(RetryCount1).IsEqualTo(2);
        await Assert.That(RetryCount2).IsEqualTo(3);
        await Assert.That(RetryCount3).IsEqualTo(4);
    }
}