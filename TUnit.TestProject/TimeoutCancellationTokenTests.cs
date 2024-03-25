using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

[MethodData(nameof(DataSource))]
[TestCategory("Timeout Cancellation Token Tests")]
public class TimeoutCancellationTokenTests
{
    public TimeoutCancellationTokenTests(int value)
    {
    }
    
    [Test]
    [Timeout(30_000)]
    [TestCategory("Blah")]
    public async Task Test(CancellationToken cancellationToken)
    {
        await Assert.That(1).Is.EqualTo(1);
    }

    [DataDrivenTest]
    [Arguments(1)]
    [Timeout(30_000)]
    public async Task DataTest(int value, CancellationToken cancellationToken)
    {
        await Assert.That(value).Is.EqualTo(1);
    }

    [MethodData(nameof(DataSource))]
    [Timeout(30_000)]
    public async Task DataSourceTest(int value, CancellationToken cancellationToken)
    {
        await Assert.That(value).Is.EqualTo(1);
    }
    
    [CombinativeTest]
    [Timeout(30_000)]
    [TestCategory("Blah")]
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