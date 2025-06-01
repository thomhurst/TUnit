using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[MethodDataSource(nameof(DataSource))]
[Category("Timeout Cancellation Token Tests")]
public class TimeoutCancellationTokenTests
{
    public TimeoutCancellationTokenTests(int value)
    {
        // Dummy method
    }

    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public async Task DefaultTest(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }

    [Test]
    [Timeout(5_000)]
    [Category("Blah")]
    [EngineTest(ExpectedResult.Failure)]
    public async Task BasicTest(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
    }

    [Test]
    [FiveSecondTimeout]
    [EngineTest(ExpectedResult.Failure)]
    public async Task InheritedTimeoutAttribute(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
    }

    [Test]
    [Arguments(1)]
    [Timeout(5_000)]
    [EngineTest(ExpectedResult.Failure)]
    public async Task DataTest(int value, CancellationToken cancellationToken)
    {
        await Assert.That(value).IsEqualTo(1);
        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
    }

    [MethodDataSource(nameof(DataSource))]
    [Timeout(5_000)]
    [EngineTest(ExpectedResult.Failure)]
    [Test]
    public async Task DataSourceTest(int value, CancellationToken cancellationToken)
    {
        await Assert.That(value).IsEqualTo(1);
        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
    }

    [Test]
    [MatrixDataSource]
    [Timeout(5_000)]
    [EngineTest(ExpectedResult.Failure)]
    [Category("Blah")]
    public async Task MatrixTest(
        [Matrix(1, 2, 3)] int value,
        CancellationToken cancellationToken)
    {
        await Assert.That(value).IsEqualTo(1).Or.IsEqualTo(2).Or.IsEqualTo(3);
        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
    }

    public static int DataSource()
    {
        return 1;
    }

    public class FiveSecondTimeout() : TimeoutAttribute(5_000);
}