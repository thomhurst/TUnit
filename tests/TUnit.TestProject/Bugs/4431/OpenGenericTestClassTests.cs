using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4431;

/// <summary>
/// Reproduces issue #4431: Open generic test classes with ClassDataSource cause runtime errors.
///
/// The issue: When a test class is an open generic type (e.g., ChildTest&lt;T&gt;),
/// TUnit in reflection mode tries to discover tests from it and fails with:
/// "Could not resolve type for generic parameter(s) of type 'ChildTest`1' from constructor arguments."
///
/// Expected: Generic test classes should work when [GenerateGenericTest] attribute specifies type arguments.
/// </summary>

// Simple data source that mimics the user's InMemoryPostgres pattern
public class SimpleDataSource4431 : IAsyncInitializer
{
    public string ConnectionString { get; private set; } = null!;

    public Task InitializeAsync()
    {
        ConnectionString = "Server=localhost;Database=test";
        return Task.CompletedTask;
    }
}

// Matches user's ParentTest
public class ParentTest4431
{
    [ClassDataSource<SimpleDataSource4431>(Shared = SharedType.PerTestSession)]
    public SimpleDataSource4431 DataSource { get; init; } = null!;

    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public virtual async Task Should_Succeed()
    {
        await Assert.That(DataSource.ConnectionString).IsNotNull();
    }
}

// Matches user's ChildTest<T> - WITH [GenerateGenericTest] to specify type argument
// This should work - TUnit should create a concrete instantiation with T = ParentTest4431
[GenerateGenericTest(typeof(ParentTest4431))]
public class ChildTest4431<T> where T : ParentTest4431, new()
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public async Task Should_Succeed()
    {
        T testInstance = new T();
        // Note: This test demonstrates the issue - the manually created instance
        // won't have its DataSource property initialized by TUnit's DI
        await Assert.That(testInstance).IsNotNull();
    }
}

// Matches user's SecondChildTest
public class SecondChildTest4431 : ParentTest4431
{
    [Test]
    [EngineTest(ExpectedResult.Pass)]
    public override async Task Should_Succeed()
    {
        await Assert.That(DataSource.ConnectionString).IsNotNull();
    }
}
