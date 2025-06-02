#if NET9_0_OR_GREATER

using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2075;

public class ServiceProviderFactory : IAsyncInitializer
{
    public ServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();
    
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}

public class FromServiceProviderFactoryAttribute : NonTypedDataSourceGeneratorAttribute
{
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        if (dataGeneratorMetadata.ClassInstanceArguments is null)
        {
            throw new Exception("ClassInstanceArguments is null");
        }

        if (dataGeneratorMetadata.ClassInstanceArguments.OfType<ServiceProviderFactory>().FirstOrDefault() is not
            { } webApplicationFactory)
        {
            throw new Exception("WebApplicationFactory is not part of the class constructor arguments");
        }
        
        var serviceProvider = webApplicationFactory.ServiceProvider;
        
        yield return () =>
        {
            return dataGeneratorMetadata.MembersToGenerate.Select(x => ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, x.Type)).ToArray();
        };
    }
}

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<ServiceProviderFactory>(Shared = SharedType.PerTestSession)]
public class MyTests(ServiceProviderFactory factory)
{
    [FromServiceProviderFactory]
    public required DbContext DbContext { get; init; }
    
    [FromServiceProviderFactory]
    public required IFirebaseClient FirebaseClient { get; init; }

    [Test]
    public void Test()
    {
    }
}

public class DbContext;

public interface IFirebaseClient;

#endif
