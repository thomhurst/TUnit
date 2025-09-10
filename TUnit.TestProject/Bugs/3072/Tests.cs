using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3072;

public record DataClass
{
    public string TestProperty { get; init; } = "TestValue";
}

public abstract class BaseClass
{
    [ClassDataSource<DataClass>(Shared = SharedType.PerTestSession)]
    public required DataClass TestData { get; init; }
}

public class TestFactory : BaseClass, IAsyncInitializer
{
    public Task InitializeAsync()
    {
        var test = TestData.TestProperty; // TestData is null here in 0.57.24
        return Task.CompletedTask;
    }
}

[EngineTest(ExpectedResult.Pass)]
public class Tests : IAsyncInitializer
{
    [ClassDataSource<TestFactory>(Shared = SharedType.PerTestSession)]
    public required TestFactory TestDataFactory { get; init; }

    public Task InitializeAsync() => Task.CompletedTask;

    [Test]
    public async Task Test()
    {
        await Assert.That(TestDataFactory?.TestData?.TestProperty).IsEqualTo("TestValue");
    }
}
