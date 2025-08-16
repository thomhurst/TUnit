using Microsoft.Extensions.DependencyInjection;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public sealed class SimpleDependency
{
    private static int _value = 0;
    public int Value { get; } = Interlocked.Increment(ref _value);
}

public sealed class SimpleDependencyInjectionDataSource : DependencyInjectionDataSourceAttribute<IServiceScope>
{
    private readonly IServiceProvider _serviceProvider = BuildServiceProvider();

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<SimpleDependency>();
        return services.BuildServiceProvider();
    }

    public override object? Create(IServiceScope scope, Type type) =>
        ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, type);

    public override IServiceScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata) =>
        _serviceProvider.CreateAsyncScope();
}

[EngineTest(ExpectedResult.Pass)]
[SimpleDependencyInjectionDataSource]
public sealed class SimpleDIScopeTestClass(SimpleDependency dependency)
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public void TestSeparateScopes(int testNumber)
    {
        // This should output different values if scopes are properly separated
        Console.WriteLine($"Test {testNumber}: SimpleDependency Value = {dependency.Value}");
        
        // Each test should get a different dependency instance due to separate scopes
        Assert.That(dependency.Value).IsGreaterThan(0).And.IsLessThanOrEqualTo(2);
    }
}