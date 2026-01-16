using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4431;

/// <summary>
/// Tests that replicate the user's pattern from issue #4431 comments.
/// The user is trying to use composition (new T()) instead of inheritance,
/// which means TUnit's DI system doesn't process the ClassDataSource attributes.
/// </summary>

#region User's Pattern - Composition (This Pattern Does NOT Work)

/// <summary>
/// Interface for database providers (matches user's IDbProvider).
/// </summary>
public interface IDbProvider4431
{
    string GetConnectionString();
}

/// <summary>
/// Data source that implements IAsyncInitializer.
/// In the user's case, this would be a container like Postgres.
/// </summary>
public class DatabaseContainer4431 : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }
    public string? ConnectionString { get; private set; }

    public Task InitializeAsync()
    {
        Console.WriteLine("DatabaseContainer4431.InitializeAsync starting");
        IsInitialized = true;
        ConnectionString = "Server=container;Database=test";
        Console.WriteLine("DatabaseContainer4431.InitializeAsync completed");
        return Task.CompletedTask;
    }
}

/// <summary>
/// User's ParentTest1 pattern - a sealed class with ClassDataSource.
/// This class is NOT in the test's inheritance chain.
/// </summary>
public sealed class ProviderWithClassDataSource4431 : IDbProvider4431
{
    [ClassDataSource<DatabaseContainer4431>(Shared = SharedType.PerTestSession)]
    public DatabaseContainer4431 Database { get; init; } = null!;

    public string GetConnectionString()
    {
        // This will throw NullReferenceException because Database is not injected
        // when this class is created via new T()
        return Database.ConnectionString!;
    }
}

/// <summary>
/// User's TestBaseDatabase pattern - creates provider via new T().
/// This is the problematic pattern because new T() doesn't go through TUnit's DI.
/// </summary>
public abstract class TestBaseDatabaseWithComposition4431<T> where T : IDbProvider4431, new()
{
    // This creates T via plain constructor - TUnit doesn't process ClassDataSource attributes
    protected readonly T Provider = new T();
}

/// <summary>
/// This test demonstrates the user's exact pattern.
/// It is expected to FAIL because the composition pattern doesn't work with TUnit's DI.
/// </summary>
[EngineTest(ExpectedResult.Failure)]
public class CompositionPatternDoesNotWork_4431 : TestBaseDatabaseWithComposition4431<ProviderWithClassDataSource4431>
{
    [Test]
    public async Task Composition_DoesNotInjectDataSources()
    {
        // This WILL FAIL with NullReferenceException
        // Because Provider was created with new T(), not by TUnit's DI
        // The ClassDataSource attribute on ProviderWithClassDataSource4431.Database is never processed
        var connectionString = Provider.GetConnectionString();
        await Assert.That(connectionString).IsNotNull();
    }
}

#endregion

#region Correct Pattern - Using Inheritance

/// <summary>
/// The CORRECT way to achieve the user's goal: use inheritance instead of composition.
/// The base class has the ClassDataSource, and child tests inherit from it.
/// </summary>
public abstract class TestBaseDatabaseWithInheritance4431
{
    [ClassDataSource<DatabaseContainer4431>(Shared = SharedType.PerTestSession)]
    public DatabaseContainer4431 Database { get; init; } = null!;

    public string GetConnectionString() => Database.ConnectionString!;
}

/// <summary>
/// Test using the correct inheritance pattern.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(InheritancePatternWorks_4431))]
public class InheritancePatternWorks_4431 : TestBaseDatabaseWithInheritance4431
{
    [Test]
    public async Task Inheritance_InjectsDataSources()
    {
        // This WILL PASS because Database is in the inheritance chain
        // TUnit processes ClassDataSource on base classes
        await Assert.That(Database).IsNotNull();
        await Assert.That(Database.IsInitialized).IsTrue();
        await Assert.That(GetConnectionString()).IsEqualTo("Server=container;Database=test");
    }
}

#endregion

#region Alternative Pattern - ClassDataSource on Property Using Interface Type

/// <summary>
/// Alternative pattern: Use ClassDataSource directly on the test class,
/// referencing the provider type that implements IDbProvider.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(DirectClassDataSourcePattern_4431))]
public class DirectClassDataSourcePattern_4431
{
    // Use ClassDataSource to get an instance that TUnit manages
    [ClassDataSource<ProviderWithClassDataSource4431>(Shared = SharedType.PerTestSession)]
    public ProviderWithClassDataSource4431 Provider { get; init; } = null!;

    [Test]
    public async Task DirectClassDataSource_InjectsProviderAndDependencies()
    {
        // TUnit will:
        // 1. Create ProviderWithClassDataSource4431
        // 2. Inject its ClassDataSource<DatabaseContainer4431> property
        // 3. Initialize DatabaseContainer4431 (IAsyncInitializer)
        // 4. Inject the fully initialized provider here
        await Assert.That(Provider).IsNotNull();
        await Assert.That(Provider.Database).IsNotNull();
        await Assert.That(Provider.Database.IsInitialized).IsTrue();
        await Assert.That(Provider.GetConnectionString()).IsEqualTo("Server=container;Database=test");
    }
}

#endregion

#region Option 3: Non-Generic ClassDataSource with Type Inference

/// <summary>
/// Test using the non-generic ClassDataSource on the concrete class.
/// The type is inferred from the property type.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(NonGenericClassDataSourceWithTypeInference_4431))]
public class NonGenericClassDataSourceWithTypeInference_4431
{
    // The non-generic [ClassDataSource] infers the type from the property type
    [ClassDataSource(Shared = [SharedType.PerTestSession])]
    public ProviderWithClassDataSource4431 Provider { get; init; } = default!;

    [Test]
    public async Task NonGenericClassDataSource_InfersTypeFromProperty()
    {
        // TUnit infers the type from the property type (ProviderWithClassDataSource4431)
        // and properly injects it with all nested dependencies
        await Assert.That(Provider).IsNotNull();
        await Assert.That(Provider.Database).IsNotNull();
        await Assert.That(Provider.Database.IsInitialized).IsTrue();
        await Assert.That(Provider.GetConnectionString()).IsEqualTo("Server=container;Database=test");
    }
}

/// <summary>
/// Generic base class that uses [ClassDataSource] (non-generic) to infer type from property.
/// This allows the property type to be a generic type parameter T.
/// </summary>
public abstract class GenericBaseWithInferredClassDataSource<T> where T : class, IDbProvider4431
{
    // The non-generic [ClassDataSource] infers the type from the property type T
    [ClassDataSource(Shared = [SharedType.PerTestSession])]
    public T Provider { get; init; } = default!;
}

/// <summary>
/// Test using the non-generic ClassDataSource with a generic base class.
/// This is the cleanest solution for the user's scenario.
/// Currently fails due to backing field lookup issue with generic types.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(NonGenericClassDataSourceWithGenericBase_First_4431))]
public class NonGenericClassDataSourceWithGenericBase_First_4431
    : GenericBaseWithInferredClassDataSource<ProviderWithClassDataSource4431>
{
    [Test]
    public async Task NonGenericClassDataSource_OnGenericBase_InfersTypeFromProperty()
    {
        // TUnit should infer the type from the property type (ProviderWithClassDataSource4431)
        // and properly inject it with all nested dependencies
        await Assert.That(Provider).IsNotNull();
        await Assert.That(Provider.Database).IsNotNull();
        await Assert.That(Provider.Database.IsInitialized).IsTrue();
        await Assert.That(Provider.GetConnectionString()).IsEqualTo("Server=container;Database=test");
    }
}

/// <summary>
/// Second test using a different provider type with the same generic base.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(NonGenericClassDataSourceWithGenericBase_Second_4431))]
public class NonGenericClassDataSourceWithGenericBase_Second_4431
    : GenericBaseWithInferredClassDataSource<SecondProviderWithClassDataSource4431>
{
    [Test]
    public async Task NonGenericClassDataSource_OnGenericBase_WorksWithDifferentTypes()
    {
        await Assert.That(Provider).IsNotNull();
        await Assert.That(Provider.Database).IsNotNull();
        await Assert.That(Provider.Database.IsInitialized).IsTrue();
        await Assert.That(Provider.GetConnectionString()).IsEqualTo("Server=secondary;Database=other");
    }
}

#endregion

#region Additional Test Scenarios

/// <summary>
/// Second data source for testing with different databases.
/// </summary>
public class SecondDatabaseContainer4431 : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }
    public string? ConnectionString { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        ConnectionString = "Server=secondary;Database=other";
        return Task.CompletedTask;
    }
}

/// <summary>
/// Provider that uses the second database.
/// </summary>
public sealed class SecondProviderWithClassDataSource4431 : IDbProvider4431
{
    [ClassDataSource<SecondDatabaseContainer4431>(Shared = SharedType.PerTestSession)]
    public SecondDatabaseContainer4431 Database { get; init; } = null!;

    public string GetConnectionString() => Database.ConnectionString!;
}

/// <summary>
/// Concrete test class that directly uses ClassDataSource for the first provider.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(GenericBaseWithConcreteProvider_First_4431))]
public class GenericBaseWithConcreteProvider_First_4431
{
    [ClassDataSource<ProviderWithClassDataSource4431>(Shared = SharedType.PerTestSession)]
    public ProviderWithClassDataSource4431 Provider { get; init; } = null!;

    [Test]
    public async Task DirectClassDataSource_WithNestedInjection_Works()
    {
        await Assert.That(Provider).IsNotNull();
        await Assert.That(Provider.Database).IsNotNull();
        await Assert.That(Provider.Database.IsInitialized).IsTrue();
    }
}

/// <summary>
/// Second concrete test class using a different provider.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(GenericBaseWithConcreteProvider_Second_4431))]
public class GenericBaseWithConcreteProvider_Second_4431
{
    [ClassDataSource<SecondProviderWithClassDataSource4431>(Shared = SharedType.PerTestSession)]
    public SecondProviderWithClassDataSource4431 Provider { get; init; } = null!;

    [Test]
    public async Task DirectClassDataSource_WithDifferentProvider_Works()
    {
        await Assert.That(Provider).IsNotNull();
        await Assert.That(Provider.Database).IsNotNull();
        await Assert.That(Provider.Database.IsInitialized).IsTrue();
        await Assert.That(Provider.GetConnectionString()).IsEqualTo("Server=secondary;Database=other");
    }
}

#endregion
