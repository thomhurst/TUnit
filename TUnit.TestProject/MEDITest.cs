using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

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

[EngineTest(ExpectedResult.Pass)]
[MicrosoftDependencyInjectionDataSource]
[UnconditionalSuppressMessage("Usage", "TUnit0042:Global hooks should not be mixed with test classes to avoid confusion. Place them in their own class.")]
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
