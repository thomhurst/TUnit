using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

public class MicrosoftDependencyInjectionDataSourceAttribute : DependencyInjectionDataSourceAttribute<IServiceScope>
{
    private static readonly IServiceProvider ServiceProvider = CreateServiceProvider();

    public override IServiceScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return ServiceProvider.CreateAsyncScope();
    }

    public override object? Create(IServiceScope scope, Type type)
    {
        return scope.ServiceProvider.GetService(type);
    }
    
    private static IServiceProvider CreateServiceProvider()
    {
        return new ServiceCollection()
            .AddTransient<MEDIClass>()
            .BuildServiceProvider();
    }
}

[MicrosoftDependencyInjectionDataSource]
public class MEDITest(MEDIClass mediClass)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(mediClass.IsInitialized).IsTrue();
        await Assert.That(mediClass.IsDisposed).IsFalse();
    }

    [After(TestSession)]
    public static async Task CheckDisposed(TestSessionContext testSessionContext)
    {
        var mediClass = testSessionContext.TestClasses
                .FirstOrDefault(x => x.ClassType == typeof(MEDITest))
            ?.Tests
            .FirstOrDefault()
            ?.TestDetails
            .TestClassArguments
            .OfType<MEDIClass>()
            .First();


        if (mediClass == null)
        {
            return;
        }
        await Assert.That(mediClass.IsDisposed).IsTrue();
    }
}

public class MEDIClass : IAsyncInitializer, IAsyncDisposable
{
    public bool IsInitialized { get; private set; }
    public bool IsDisposed { get; private set; }
   
    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return default;
    }
}