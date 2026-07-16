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
        await Assert.That(container.Service).IsSameReferenceAs(container.ServiceCapturedDuringInitialization);
        await Assert.That(container.Service.IsInitialized).IsTrue();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class Issue5753ValueTypePropertyInjectionTests
{
    [Issue5753IntDataSource]
    public int Value { get; set; }

    [Test]
    public async Task ValueTypeProperty_IsInjected_WhenDefaultValueIsBoxed()
    {
        await Assert.That(Value).IsEqualTo(42);
    }
}

public class Issue5753Container : IAsyncInitializer
{
    [ClassDataSource<Issue5753InjectedService>]
    public Issue5753InjectedService Service { get; set; } = null!;

    public Issue5753InjectedService? ServiceCapturedDuringInitialization { get; private set; }

    public Task InitializeAsync()
    {
        ServiceCapturedDuringInitialization = Service;
        return Task.CompletedTask;
    }
}

public class Issue5753InjectedService : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
}

public class Issue5753IntDataSourceAttribute : DataSourceGeneratorAttribute<int>
{
    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => 42;
    }
}
