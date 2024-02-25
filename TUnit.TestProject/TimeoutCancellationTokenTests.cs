using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

[DataSourceDrivenTest(nameof(DataSource))]
public class TimeoutCancellationTokenTests
{
    public TimeoutCancellationTokenTests(int value)
    {
    }
    
    [Test]
    [Timeout(30_000)]
    public async Task Test(CancellationToken cancellationToken)
    {
        await Assert.That(1).Is.EqualTo(1);
    }

    [DataDrivenTest(1)]
    [Timeout(30_000)]
    public async Task DataTest(int value, CancellationToken cancellationToken)
    {
        await Assert.That(value).Is.EqualTo(1);
    }

    [DataSourceDrivenTest(nameof(DataSource))]
    [Timeout(30_000)]
    public async Task DataSourceTest(int value, CancellationToken cancellationToken)
    {
        await Assert.That(value).Is.EqualTo(1);
    }
    
    [CombinativeTest]
    [Timeout(30_000)]
    public async Task CombinativeTest(
        [CombinativeValues(1, 1, 1)] int value, 
        CancellationToken cancellationToken)
    {
        await Assert.That(value).Is.EqualTo(1);
    }

    public static int DataSource()
    {
        return 1;
    }
}