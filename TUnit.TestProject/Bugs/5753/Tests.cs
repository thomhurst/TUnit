using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._5753;

[EngineTest(ExpectedResult.Pass)]
public class Issue5753ReflectionPropertyInjectionTests
{
    [Test]
    [ClassDataSource<Issue5753Container>]
    public async Task InjectedProperty_IsNotResolvedAgain_WhenAlreadySet(Issue5753Container container)
    {
        await Assert.That(container.Service).IsNotNull();
        await Assert.That(container.Service.InstanceNumber).IsEqualTo(1);
        await Assert.That(Issue5753InjectedService.InstancesCreated).IsEqualTo(1);
        await Assert.That(container.Service.IsInitialized).IsTrue();
    }
}

public class Issue5753Container
{
    [ClassDataSource<Issue5753InjectedService>]
    public Issue5753InjectedService Service { get; set; } = null!;
}

public class Issue5753InjectedService : IAsyncInitializer
{
    private static int _instancesCreated;

    public static int InstancesCreated => Volatile.Read(ref _instancesCreated);

    public int InstanceNumber { get; } = Interlocked.Increment(ref _instancesCreated);

    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
}
