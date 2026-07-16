using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4715;

/// <summary>
/// Closest reproduction to issue #4715: A DI data source attribute with
/// [ClassDataSource] property pointing to a factory that has a nested
/// IAsyncDiscoveryInitializer that throws during InitializeAsync.
///
/// Pattern from the issue:
///   [AspireDataSource] on test class →
///   AspireDataSourceAttribute has [ClassDataSource&lt;TestWebApplicationFactory&gt;] property →
///   TestWebApplicationFactory has [ClassDataSource&lt;TestAppHost&gt;] property →
///   TestAppHost : IAsyncDiscoveryInitializer → InitializeAsync() throws TimeoutException
/// </summary>

public class FailingHost4715Attr : IAsyncDiscoveryInitializer
{
    public Task InitializeAsync()
    {
        throw new TimeoutException("Simulated: AppHost startup timed out.");
    }
}

public class Factory4715Attr : IAsyncDiscoveryInitializer
{
    [ClassDataSource<FailingHost4715Attr>(Shared = SharedType.PerTestSession)]
    public required FailingHost4715Attr AppHost { get; init; }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}

public class TestDiDataSourceAttribute : DependencyInjectionDataSourceAttribute<IDisposable>
{
    [ClassDataSource<Factory4715Attr>(Shared = SharedType.PerTestSession)]
    public Factory4715Attr Factory { get; init; } = default!;

    public override IDisposable CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // This should never be reached because Factory initialization throws
        return new MemoryStream();
    }

    public override object? Create(IDisposable scope, Type type)
    {
        return new object();
    }
}

[EngineTest(ExpectedResult.Failure)]
[TestDiDataSource]
public class AttributePropertyInjectionFailureTests(object _)
{
    [Test]
    public void Test_Should_Fail_Not_Stall()
    {
        throw new InvalidOperationException("This test should not have executed");
    }
}
