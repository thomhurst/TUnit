using Microsoft.Extensions.DependencyInjection;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public sealed class Dependency
{
    private static int _value = 0;
    public int Value { get; } = Interlocked.Increment(ref _value);
}

public sealed class DependencyInjectionScopeTestDataSource : DependencyInjectionDataSourceAttribute<IServiceScope>
{
    private readonly IServiceProvider _serviceProvider = BuildServiceProvider();

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<Dependency>();
        return services.BuildServiceProvider();
    }

    public override object? Create(IServiceScope scope, Type type) =>
        ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, type);

    public override IServiceScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata) =>
        _serviceProvider.CreateAsyncScope();
}

[EngineTest(ExpectedResult.Pass)]
[DependencyInjectionScopeTestDataSource]
public sealed class ActualTestClass(Dependency dependency)
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public void Test1(int testNumber)
    {
        Console.WriteLine($"Test {testNumber}: Dependency Value = {dependency.Value}");
    }
}