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

public class DynamicDataGenerator : DataSourceGeneratorAttribute<int>, ITestStartEvent, ITestEndEvent
{
    private static int _count;

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public override IEnumerable<int> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return Random.Shared.Next();
    }

    public ValueTask OnTestStart(BeforeTestContext beforeTestContext)
    {
        if (!beforeTestContext.TestContext.ObjectBag.ContainsKey("DynamicDataGeneratorRetry"))
        {
            beforeTestContext.AddLinkedCancellationToken(_cancellationTokenSource.Token);
        }
        
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnTestEnd(TestContext testContext)
    {
        if (testContext.Result?.Status == Status.Failed)
        {
            await _cancellationTokenSource.CancelAsync();
            
            // We need a condition to end execution at some point otherwise we could go forever recursively
            if (_count++ > 5)
            {
                throw new Exception();
                return;
            }

            await testContext.ReregisterTestWithArguments<DynamicallyRegisteredTests>(methodArguments: [Random.Shared.Next()],
                objectBag: new()
                {
                    ["DynamicDataGeneratorRetry"] = true
                },
                dataAttributes: [this]
            );
        }
    }
}