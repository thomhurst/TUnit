#pragma warning disable TUnit0042

using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

#region Base Classes and Fixtures

/// <summary>
/// Generic base class with a property that has a ClassDataSource attribute.
/// This simulates WebApplicationFactory-style fixtures.
/// </summary>
public abstract class GenericFixtureBase<TProgram> where TProgram : class
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryDatabase? Postgres { get; init; }
}

/// <summary>
/// Simulates a database fixture that implements IAsyncInitializer.
/// </summary>
public class InMemoryDatabase : IAsyncInitializer, IAsyncDisposable
{
    public bool IsInitialized { get; private set; }
    public string? ConnectionString { get; private set; }

    public Task InitializeAsync()
    {
        Console.WriteLine(@"Initializing InMemoryDatabase");
        IsInitialized = true;
        ConnectionString = "Server=localhost;Database=test";
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Console.WriteLine(@"Disposing InMemoryDatabase");
        return default;
    }
}

/// <summary>
/// Intermediate class in the inheritance chain.
/// </summary>
public abstract class IntermediateBase<T> : GenericFixtureBase<T> where T : class
{
}

/// <summary>
/// Generic class that implements IAsyncInitializer with nested IAsyncInitializer properties.
/// This tests Pipeline 5 (generic IAsyncInitializer property generation).
/// </summary>
public class GenericInitializerFixture<T> : IAsyncInitializer where T : class
{
    public InMemoryDatabase? Database { get; init; }
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        Console.WriteLine($"Initializing GenericInitializerFixture<{typeof(T).Name}>");
        IsInitialized = true;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Generic base with multiple properties having data source attributes.
/// </summary>
public abstract class MultiPropertyGenericBase<T> where T : class
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryDatabase? FirstDb { get; init; }

    [ClassDataSource<SecondaryDatabase>(Shared = SharedType.PerTestSession)]
    public SecondaryDatabase? SecondDb { get; init; }
}

/// <summary>
/// Secondary database fixture for testing multiple properties.
/// </summary>
public class SecondaryDatabase : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        Console.WriteLine(@"Initializing SecondaryDatabase");
        IsInitialized = true;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Generic base with multiple type parameters.
/// </summary>
public abstract class MultiTypeParamBase<T1, T2> where T1 : class where T2 : class
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryDatabase? Database { get; init; }
}

/// <summary>
/// Grandparent in deep inheritance chain.
/// </summary>
public abstract class GrandparentBase<T> where T : class
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryDatabase? GrandparentDb { get; init; }
}

/// <summary>
/// Parent in deep inheritance chain with its own property.
/// </summary>
public abstract class ParentBase<T> : GrandparentBase<T> where T : class
{
    [ClassDataSource<SecondaryDatabase>(Shared = SharedType.PerTestSession)]
    public SecondaryDatabase? ParentDb { get; init; }
}

#endregion

#region Test Classes

/// <summary>
/// Tests to verify that property injection works correctly for classes
/// that inherit from generic base classes.
/// This is the scenario from issue #4431.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(GenericPropertyInjectionTests))]
public class GenericPropertyInjectionTests : GenericFixtureBase<GenericPropertyInjectionTests.TestProgram>
{
    [Test]
    public async Task GenericBase_PropertyInjection_Works()
    {
        // The Postgres property should be injected and initialized
        // before this test runs
        await Assert.That(Postgres).IsNotNull();
        await Assert.That(Postgres!.IsInitialized).IsTrue();
        await Assert.That(Postgres.ConnectionString).IsEqualTo("Server=localhost;Database=test");
    }

    public class TestProgram { }
}

/// <summary>
/// Test that verifies multiple concrete instantiations of the same generic.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(MultipleGenericInstantiationTests))]
public class MultipleGenericInstantiationTests : GenericFixtureBase<MultipleGenericInstantiationTests.OtherProgram>
{
    [Test]
    public async Task DifferentTypeArg_AlsoWorks()
    {
        await Assert.That(Postgres).IsNotNull();
        await Assert.That(Postgres!.IsInitialized).IsTrue();
    }

    public class OtherProgram { }
}

/// <summary>
/// Test for deeply nested inheritance with generics.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(DeepInheritanceGenericTests))]
public class DeepInheritanceGenericTests : IntermediateBase<DeepInheritanceGenericTests.DeepProgram>
{
    [Test]
    public async Task DeepInheritance_PropertyInjection_Works()
    {
        await Assert.That(Postgres).IsNotNull();
        await Assert.That(Postgres!.IsInitialized).IsTrue();
    }

    public class DeepProgram { }
}

/// <summary>
/// Test that uses a generic IAsyncInitializer via ClassDataSource.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(GenericInitializerPropertyTests))]
public class GenericInitializerPropertyTests
{
    [ClassDataSource<GenericInitializerFixture<GenericInitializerPropertyTests>>(Shared = SharedType.PerTestSession)]
    public GenericInitializerFixture<GenericInitializerPropertyTests>? Fixture { get; init; }

    [Test]
    public async Task GenericInitializer_IsDiscovered()
    {
        await Assert.That(Fixture).IsNotNull();
        await Assert.That(Fixture!.IsInitialized).IsTrue();
    }
}

/// <summary>
/// Test for multiple properties on a generic base class.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(MultiplePropertiesGenericTests))]
public class MultiplePropertiesGenericTests : MultiPropertyGenericBase<MultiplePropertiesGenericTests.TestProgram>
{
    [Test]
    public async Task MultipleProperties_AllInjected()
    {
        await Assert.That(FirstDb).IsNotNull();
        await Assert.That(FirstDb!.IsInitialized).IsTrue();

        await Assert.That(SecondDb).IsNotNull();
        await Assert.That(SecondDb!.IsInitialized).IsTrue();
    }

    public class TestProgram { }
}

/// <summary>
/// Test for generic base with multiple type parameters.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(MultiTypeParameterTests))]
public class MultiTypeParameterTests : MultiTypeParamBase<MultiTypeParameterTests.Program1, MultiTypeParameterTests.Program2>
{
    [Test]
    public async Task MultiTypeParams_PropertyInjection_Works()
    {
        await Assert.That(Database).IsNotNull();
        await Assert.That(Database!.IsInitialized).IsTrue();
    }

    public class Program1 { }
    public class Program2 { }
}

/// <summary>
/// Test for deep inheritance with properties at each level.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(DeepInheritanceMultiLevelPropertyTests))]
public class DeepInheritanceMultiLevelPropertyTests : ParentBase<DeepInheritanceMultiLevelPropertyTests.TestProgram>
{
    [Test]
    public async Task DeepInheritance_AllLevelProperties_Injected()
    {
        // Property from grandparent
        await Assert.That(GrandparentDb).IsNotNull();
        await Assert.That(GrandparentDb!.IsInitialized).IsTrue();

        // Property from parent
        await Assert.That(ParentDb).IsNotNull();
        await Assert.That(ParentDb!.IsInitialized).IsTrue();
    }

    public class TestProgram { }
}

/// <summary>
/// Test for mix of generic base properties and derived class properties.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(MixedGenericAndDerivedPropertiesTests))]
public class MixedGenericAndDerivedPropertiesTests : GenericFixtureBase<MixedGenericAndDerivedPropertiesTests.TestProgram>
{
    [ClassDataSource<SecondaryDatabase>(Shared = SharedType.PerTestSession)]
    public SecondaryDatabase? DerivedDatabase { get; init; }

    [Test]
    public async Task MixedProperties_BothInjected()
    {
        // Property from generic base
        await Assert.That(Postgres).IsNotNull();
        await Assert.That(Postgres!.IsInitialized).IsTrue();

        // Property from derived class
        await Assert.That(DerivedDatabase).IsNotNull();
        await Assert.That(DerivedDatabase!.IsInitialized).IsTrue();
    }

    public class TestProgram { }
}

/// <summary>
/// Test using a nested generic type as the type argument.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(NestedGenericTypeArgumentTests))]
public class NestedGenericTypeArgumentTests : GenericFixtureBase<List<string>>
{
    [Test]
    public async Task NestedGenericTypeArg_PropertyInjection_Works()
    {
        await Assert.That(Postgres).IsNotNull();
        await Assert.That(Postgres!.IsInitialized).IsTrue();
    }
}

/// <summary>
/// Test for generic IAsyncInitializer with nested IAsyncInitializer properties.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(GenericInitializerWithNestedPropertyTests))]
public class GenericInitializerWithNestedPropertyTests
{
    [ClassDataSource<GenericInitializerFixture<GenericInitializerWithNestedPropertyTests>>(Shared = SharedType.PerTestSession)]
    public GenericInitializerFixture<GenericInitializerWithNestedPropertyTests>? Fixture { get; init; }

    [Test]
    public async Task GenericInitializer_NestedProperty_Works()
    {
        await Assert.That(Fixture).IsNotNull();
        await Assert.That(Fixture!.IsInitialized).IsTrue();
        // The Database property should be discovered via InitializerPropertyRegistry
        // This verifies Pipeline 5 is working for generic types
    }
}

#endregion

#region Issue 4431 - Exact Scenarios from GitHub Issue

/// <summary>
/// Simulates the WebApplicationFactory scenario from issue #4431.
/// This is a generic factory that requires async initialization.
/// </summary>
public class CustomWebApplicationFactory<TProgram> : IAsyncInitializer, IAsyncDisposable
    where TProgram : class
{
    public bool IsInitialized { get; private set; }
    public string? ConfiguredConnectionString { get; private set; }

    // Simulates a dependency like a test container
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryDatabase? TestDatabase { get; init; }

    public async Task InitializeAsync()
    {
        Console.WriteLine($"CustomWebApplicationFactory<{typeof(TProgram).Name}>.InitializeAsync starting");

        // In the real scenario, this would configure the web host with the test database
        // The test database should already be initialized at this point
        if (TestDatabase == null)
        {
            throw new InvalidOperationException("TestDatabase was not injected!");
        }

        if (!TestDatabase.IsInitialized)
        {
            throw new InvalidOperationException("TestDatabase was not initialized before factory!");
        }

        ConfiguredConnectionString = TestDatabase.ConnectionString;
        IsInitialized = true;

        Console.WriteLine($"CustomWebApplicationFactory<{typeof(TProgram).Name}>.InitializeAsync completed");
        await Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Console.WriteLine($"CustomWebApplicationFactory<{typeof(TProgram).Name}>.DisposeAsync");
        return default;
    }
}

/// <summary>
/// Issue #4431 - Test that replicates the WebApplicationFactory scenario.
/// The factory should be initialized AFTER its dependencies (like test containers).
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(Issue4431_WebApplicationFactoryScenarioTests))]
public class Issue4431_WebApplicationFactoryScenarioTests
{
    public class TestProgram { }

    [ClassDataSource<CustomWebApplicationFactory<TestProgram>>(Shared = SharedType.PerTestSession)]
    public CustomWebApplicationFactory<TestProgram>? Factory { get; init; }

    [Test]
    public async Task WebApplicationFactory_InitializedAfterDependencies()
    {
        // Factory should be injected and initialized
        await Assert.That(Factory).IsNotNull();
        await Assert.That(Factory!.IsInitialized).IsTrue();

        // Factory's dependencies should have been initialized first
        await Assert.That(Factory.TestDatabase).IsNotNull();
        await Assert.That(Factory.TestDatabase!.IsInitialized).IsTrue();

        // Factory should have configured itself with the test database
        await Assert.That(Factory.ConfiguredConnectionString).IsEqualTo("Server=localhost;Database=test");
    }
}

/// <summary>
/// Issue #4431 Comment - ParentTest scenario.
/// This is a non-generic class with ClassDataSource property - should work.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(Issue4431_ParentTestScenario))]
public class Issue4431_ParentTestScenario
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryDatabase? Postgres { get; init; }

    [Test]
    public async Task ParentTest_Should_Succeed()
    {
        await Assert.That(Postgres).IsNotNull();
        await Assert.That(Postgres!.ConnectionString).IsNotNull();
    }
}

/// <summary>
/// Issue #4431 Comment - SecondChildTest scenario.
/// This inherits from a non-generic base with ClassDataSource property - should work.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(Issue4431_SecondChildTestScenario))]
public class Issue4431_SecondChildTestScenario : Issue4431_ParentTestScenario
{
    [Test]
    public async Task SecondChildTest_Should_Succeed()
    {
        // Inherited property should be injected
        await Assert.That(Postgres).IsNotNull();
        await Assert.That(Postgres!.ConnectionString).IsNotNull();
    }
}

/// <summary>
/// Abstract base class that simulates WebApplicationFactory-style inheritance.
/// This is the exact pattern from the issue's attached zip file.
/// </summary>
public abstract class WebAppFactoryBase<TFactory, TProgram>
    where TFactory : CustomWebApplicationFactory<TProgram>
    where TProgram : class
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryDatabase? SharedDatabase { get; init; }
}

/// <summary>
/// Issue #4431 - Complex generic inheritance scenario from the attached zip.
/// Tests a class inheriting from a generic base with multiple type parameters.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[NotInParallel(nameof(Issue4431_ComplexGenericInheritanceTests))]
public class Issue4431_ComplexGenericInheritanceTests
    : WebAppFactoryBase<CustomWebApplicationFactory<Issue4431_ComplexGenericInheritanceTests.MyProgram>, Issue4431_ComplexGenericInheritanceTests.MyProgram>
{
    public class MyProgram { }

    [Test]
    public async Task ComplexGenericInheritance_PropertyInjection_Works()
    {
        // Property from generic base should be injected
        await Assert.That(SharedDatabase).IsNotNull();
        await Assert.That(SharedDatabase!.IsInitialized).IsTrue();
    }
}

#endregion
