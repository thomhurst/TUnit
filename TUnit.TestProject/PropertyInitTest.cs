using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class PropertyInitTest
{
    [SimpleDataGenerator]
    public required InitializableService Service { get; set; }
    
    [Test]
    public async Task Service_Should_Be_Initialized()
    {
        await Assert.That(Service).IsNotNull();
        await Assert.That(Service.IsInitialized).IsTrue();
    }
}

public class InitializableService : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }
    
    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
}

public class SimpleDataGeneratorAttribute : DataSourceGeneratorAttribute<InitializableService>
{
    protected override IEnumerable<Func<InitializableService>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => new InitializableService();
    }
}