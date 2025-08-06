using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class InitializableTestClassTests : IAsyncInitializer
{
    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public bool IsInitialized
    {
        get;
        set;
    }

    [Test]
    public async Task Basic()
    {
        await Assert.That(IsInitialized).IsTrue();
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Args(int i)
    {
        await Assert.That(IsInitialized).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(Data))]
    public async Task Method(int i)
    {
        await Assert.That(IsInitialized).IsTrue();
    }

    [Test]
    [DataGenerator]
    public async Task DataGenerator(int i)
    {
        await Assert.That(IsInitialized).IsTrue();
    }

    public static int Data()
    {
        return 3;
    }

    public class DataGeneratorAttribute : DataSourceGeneratorAttribute<int>
    {
        protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
        {
            yield return () => 4;
            yield return () => 5;
        }
    }
}
