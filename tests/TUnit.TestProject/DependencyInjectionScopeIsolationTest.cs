using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Service with state to verify scope isolation between test executions
/// </summary>
public sealed class ScopedTestService
{
    private static int _counter = 0;
    public int InstanceNumber { get; } = Interlocked.Increment(ref _counter);
}

/// <summary>
/// DI data source attribute that provides scoped services
/// </summary>
public class ScopedServiceDataSourceAttribute : DependencyInjectionDataSourceAttribute<IServiceScope>
{
    private static readonly IServiceProvider ServiceProvider = CreateServiceProvider();

    public override IServiceScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return ServiceProvider.CreateAsyncScope();
    }

    public override object? Create(IServiceScope scope, Type type)
    {
        return ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, type);
    }

    private static IServiceProvider CreateServiceProvider()
    {
        return new ServiceCollection()
            .AddScoped<ScopedTestService>()
            .BuildServiceProvider();
    }
}

/// <summary>
/// Test class that verifies dependency injection scope isolation between test executions.
/// This test ensures that each test execution with multiple arguments gets its own DI scope
/// and scoped services are not shared between test invocations.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[ScopedServiceDataSource]
[UnconditionalSuppressMessage("Usage", "TUnit0042:Global hooks should not be mixed with test classes to avoid confusion. Place them in their own class.")]
public class DependencyInjectionScopeIsolationTest(ScopedTestService scopedService)
{
    private static readonly ConcurrentBag<int> _instanceNumbers = new();

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public async Task ScopedServices_ShouldBeIsolated_PerTestExecution(int testNumber)
    {
        // Record the instance number for this test execution
        _instanceNumbers.Add(scopedService.InstanceNumber);
        
        // Basic assertions that service is properly injected
        await Assert.That(scopedService).IsNotNull();
        await Assert.That(scopedService.InstanceNumber).IsGreaterThan(0);
    }

    [After(HookType.Class)]
    public static async Task VerifyAllInstancesWereUnique()
    {
        // Verify that we got 3 different instances (one per test execution)
        // This ensures that each test execution received its own DI scope
        var uniqueInstances = _instanceNumbers.Distinct().ToList();
        await Assert.That(uniqueInstances.Count).IsEqualTo(3);
        
        // Also verify we recorded exactly 3 instances total
        await Assert.That(_instanceNumbers.Count).IsEqualTo(3);
    }
}