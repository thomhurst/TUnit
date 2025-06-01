#if NET9_0_OR_GREATER

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2075;

public class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    public Task InitializeAsync()
    {
        _ = Server;

        return Task.CompletedTask;
    }
}

public class FromWebApplicationFactoryAttribute : NonTypedDataSourceGeneratorAttribute
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

        if (dataGeneratorMetadata.ClassInstanceArguments.OfType<WebApplicationFactory<Program>>().FirstOrDefault() is not
            { } webApplicationFactory)
        {
            throw new Exception("WebApplicationFactory is not part of the class constructor arguments");
        }
        
        var serviceProvider = webApplicationFactory.Server.Services;
        
        yield return () =>
        {
            return dataGeneratorMetadata.MembersToGenerate.Select(x => ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, x.Type)).ToArray();
        };
    }
}

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<WebApplicationFactory<Program>>(Shared = SharedType.PerTestSession)]
public class MyTests(WebApplicationFactory<Program> factory)
{
    [FromWebApplicationFactory]
    public required DbContext DbContext { get; init; }
    
    [FromWebApplicationFactory]
    public required IFirebaseClient FirebaseClient { get; init; }

    [Test]
    public void Test()
    {
    }
}

public class Program;

public class DbContext;

public interface IFirebaseClient;

public class FirebaseClient : IFirebaseClient;

#endif
