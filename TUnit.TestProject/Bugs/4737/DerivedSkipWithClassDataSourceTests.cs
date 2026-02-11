using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs._4737;

/// <summary>
/// Derived SkipAttribute that always skips - simulates a CI-environment skip attribute
/// like the user's IntegrationTestAttribute in issue #4737.
/// </summary>
public class AlwaysSkipAttribute() : SkipAttribute("Skip in CI environment")
{
    public override Task<bool> ShouldSkip(TestRegisteredContext context)
    {
        return Task.FromResult(true);
    }
}

/// <summary>
/// A data source that throws during InitializeAsync - simulates a WebApplicationFactory
/// that fails to connect to a database that doesn't exist in CI.
/// </summary>
public class FailingDataSource : IAsyncInitializer
{
    public Task InitializeAsync()
    {
        throw new InvalidOperationException("Simulated infrastructure failure: cannot connect to database");
    }
}

/// <summary>
/// Regression test for https://github.com/thomhurst/TUnit/issues/4737
/// A class-level derived SkipAttribute combined with a ClassDataSource whose
/// IAsyncInitializer would throw. The test must be reported as skipped, not failed.
/// </summary>
[ClassDataSource<FailingDataSource>(Shared = SharedType.PerTestSession)]
[AlwaysSkip]
public class DerivedSkipWithFailingClassDataSourceTests(FailingDataSource dataSource)
{
    [Test]
    public void Test()
    {
        throw new Exception("This test should have been skipped, not executed!");
    }
}

/// <summary>
/// A data source that succeeds initialization.
/// </summary>
public class SucceedingDataSource : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Verifies that a derived SkipAttribute at the class level correctly skips tests
/// even when a ClassDataSource is present and would succeed. The test body would
/// fail if executed, proving the skip is effective.
/// </summary>
[ClassDataSource<SucceedingDataSource>(Shared = SharedType.PerTestSession)]
[AlwaysSkip]
public class DerivedSkipWithSucceedingClassDataSourceTests(SucceedingDataSource dataSource)
{
    [Test]
    public void Test()
    {
        throw new Exception("This test should have been skipped, not executed!");
    }
}
