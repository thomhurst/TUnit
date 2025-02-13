using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Engine.Extensions;

namespace TUnit.TestProject;

public class DynamicallyRegisteredTests
{
    [Test]
    [DynamicDataGenerator]
    public void MyTest(int value)
    {
        throw new Exception($@"Value {value} !");
    }
}

public class DynamicDataGenerator : DataSourceGeneratorAttribute<int>, ITestStartEventReceiver, ITestEndEventReceiver
{
    private static int _count;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => new Random().Next();
    }

    public ValueTask OnTestStart(BeforeTestContext beforeTestContext)
    {
        if (!IsReregisteredTest(beforeTestContext.TestContext))
        {
            beforeTestContext.AddLinkedCancellationToken(_cancellationTokenSource.Token);
        }

        return default;
    }

    public void OnTestStartSynchronous(BeforeTestContext beforeTestContext)
    {
    }

    [Experimental("WIP")]
    public async ValueTask OnTestEnd(TestContext testContext)
    {
        if (testContext.Result?.Status == Status.Failed)
        {
            await _cancellationTokenSource.CancelAsync();

            // We need a condition to end execution at some point otherwise we could go forever recursively
            if (Interlocked.Increment(ref _count) > 5)
            {
                throw new Exception();
            }

            if (IsReregisteredTest(testContext))
            {
                // Optional to reduce noise
                // testContext.SuppressReportingResult();
            }

            await testContext.ReregisterTestWithArguments(methodArguments: [new Random().Next()],
                objectBag: new()
                {
                    ["DynamicDataGeneratorRetry"] = true
                });
        }
    }

    private static bool IsReregisteredTest(TestContext testContext)
    {
        return testContext.ObjectBag.ContainsKey("DynamicDataGeneratorRetry");
    }

    public int Order => 0;
}