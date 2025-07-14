using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

[DynamicCodeOnly]
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

    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => new Random().Next();
    }

    public ValueTask OnTestStart(TestContext testContext)
    {
        if (!IsReregisteredTest(testContext))
        {
            testContext.AddLinkedCancellationToken(_cancellationTokenSource.Token);
        }

        return default(ValueTask);
    }

    [Experimental("WIP")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Dynamic Code Only attribute on test")]
    public async ValueTask OnTestEnd(TestContext testContext)
    {
        if (testContext.Result?.State == TestState.Failed)
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
