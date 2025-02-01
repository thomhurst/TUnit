using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

[InheritsTests]
public class DependsOnWithBaseTests : DependsOnBase
{
    private static DateTime _subTypeTestStart;

    [Test, DependsOn(nameof(BaseTest))]
    public async Task SubTypeTest()
    {
        _subTypeTestStart = TestContext.Current!.TestStart!.Value.DateTime;
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AssertStartTimes()
    {
        await Assert.That(BaseTestStart).IsNotDefault();
        await Assert.That(_subTypeTestStart).IsAfterOrEqualTo(BaseTestStart.AddSeconds(4.9));
    }
}

public abstract class DependsOnBase
{
    protected static DateTime BaseTestStart { get; private set; }

    [Test]
    public async Task BaseTest()
    {
        BaseTestStart = TestContext.Current!.TestStart!.Value.DateTime;
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}