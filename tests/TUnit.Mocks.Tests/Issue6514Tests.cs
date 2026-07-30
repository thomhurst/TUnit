using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6514
// A generic method like IFeatures.Get<T>() invoked by third-party code with a T the test
// assembly cannot name (internal to the SDK) used to fall through to null, because the source
// generator can only produce mocks for types nameable at compile time. Loose-mode mocks now
// fall back to a runtime-emitted stub (RuntimeStubGenerator): a functional recursive stub in a
// dynamic assembly named DynamicProxyGenAssembly2 with Castle's public key, so the ecosystem's
// existing InternalsVisibleTo grants for NSubstitute/Moq apply to TUnit.Mocks too.

#region Test types

public interface IStubFeatures
{
    T Get<T>();
}

// Simulates a type internal to a third-party SDK. The test project grants InternalsVisibleTo to
// DynamicProxyGenAssembly2 exactly like such SDKs do for Castle-based mocking libraries.
internal interface IInternalFeature
{
    string Name { get; }
}

// Never mocked anywhere in this assembly, so no source-generated factory is registered for it.
public interface INeverMockedFeature
{
    string Describe();
    INeverMockedNested Nested { get; }
    Task<int> CountAsync();
    IReadOnlyList<string> Items { get; }
}

public interface INeverMockedNested
{
    int Value { get; }
}

// A stubbed interface that itself declares a generic method — every instantiation shares one
// member slot, so cached defaults must be keyed by the closed return type too.
public interface INeverMockedGenericFeature
{
    T Resolve<T>();
}

// Review findings on #6519: indexer state must be keyed by the index arguments, and
// ValueTask<T> defaults must not trip the ambiguous (T)/(Task<T>) constructor pair when the
// inner default is null.
public sealed class StubDto
{
    public string? Name { get; set; }
}

public interface IIndexedFeature
{
    string this[int index] { get; set; }

    string this[string first, int second] { get; set; }

    ValueTask<StubDto> GetDtoAsync();
}

// Review finding on #6519 (round 4): the typed cache-miss handlers (1–8 args) must route
// through the same runtime-stub fallback as the object-array handler. IServiceProvider has no
// source-generated auto-mock factory (System.* interfaces are excluded), so only the runtime
// stub can serve it.
public interface ITypedPathFeatures
{
    IServiceProvider Resolve(int id);
}

// Distinct never-before-stubbed interfaces for the concurrent first-emission test — each
// first-touch runs EmitStubType, which must be serialized over the shared ModuleBuilder.
public interface IConcurrentStub0 { string Name { get; } }
public interface IConcurrentStub1 { string Name { get; } }
public interface IConcurrentStub2 { string Name { get; } }
public interface IConcurrentStub3 { string Name { get; } }
public interface IConcurrentStub4 { string Name { get; } }
public interface IConcurrentStub5 { string Name { get; } }
public interface IConcurrentStub6 { string Name { get; } }
public interface IConcurrentStub7 { string Name { get; } }

// Simulates the SDK-internal call site: code the test has no control over requesting types the
// test assembly could not configure.
public static class StubFeatureConsumer
{
    public static string DescribeInternal(IStubFeatures features)
    {
        var feature = features.Get<IInternalFeature>();
        return feature is null ? "null" : $"got:{feature.Name}";
    }
}

#endregion

[SkipIfNotDynamicCodeSupported("Runtime stubs require Reflection.Emit; on Native AOT the feature is inert and loose mocks keep the previous null default.")]
public class Issue6514Tests
{
    [Test]
    public async Task Generic_Get_With_Internal_Type_Argument_Returns_A_Functional_Stub()
    {
        var features = IStubFeatures.Mock();

        // "got:" — the stub exists and its string property returns "" rather than null.
        await Assert.That(StubFeatureConsumer.DescribeInternal(features.Object)).IsEqualTo("got:");
    }

    [Test]
    public async Task Generic_Get_With_Unregistered_Public_Interface_Returns_A_Stub()
    {
        var features = IStubFeatures.Mock();

        var stub = features.Object.Get<INeverMockedFeature>();

        await Assert.That(stub).IsNotNull();
        await Assert.That(stub.Describe()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Stub_Members_Recurse_Into_Further_Stubs()
    {
        var features = IStubFeatures.Mock();

        var stub = features.Object.Get<INeverMockedFeature>();

        await Assert.That(stub.Nested).IsNotNull();
        await Assert.That(stub.Nested.Value).IsEqualTo(0);
    }

    [Test]
    public async Task Stub_Task_Members_Return_Completed_Tasks()
    {
        var features = IStubFeatures.Mock();

        var stub = features.Object.Get<INeverMockedFeature>();
        var task = stub.CountAsync();

        // Accessing IsCompleted would throw if the stub had returned a null task.
        await Assert.That(task.IsCompleted).IsTrue();
        await Assert.That(await task).IsEqualTo(0);
    }

    [Test]
    public async Task Stub_Collection_Members_Return_Empty_Collections()
    {
        var features = IStubFeatures.Mock();

        var stub = features.Object.Get<INeverMockedFeature>();

        await Assert.That(stub.Items).IsNotNull();
        await Assert.That(stub.Items.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Stub_Member_Values_Are_Cached_For_Stable_Identity()
    {
        var features = IStubFeatures.Mock();

        var first = features.Object.Get<INeverMockedFeature>();
        var second = features.Object.Get<INeverMockedFeature>();

        await Assert.That(first).IsSameReferenceAs(second);
        await Assert.That(first.Nested).IsSameReferenceAs(second.Nested);
    }

    [Test]
    public async Task Registered_Interfaces_Still_Get_Real_Configurable_Mocks()
    {
        // IStubRegisteredFeature has a generated factory (mocked below), so the registry path
        // wins over runtime stubbing and the result is a real Mock the test can retrieve.
        _ = IStubRegisteredFeature.Mock();

        var features = IStubFeatures.Mock();
        var instance = features.Object.Get<IStubRegisteredFeature>();

        await Assert.That(instance).IsNotNull();

        var wrapper = Mock.Get(instance);
        wrapper.Tag.Returns("configured");

        await Assert.That(instance.Tag).IsEqualTo("configured");
    }

    [Test]
    public async Task Stub_Generic_Method_Keeps_Instantiations_Separate()
    {
        // Review finding on #6519: the per-slot return cache conflated generic instantiations —
        // after Resolve<int>() cached a boxed 0, Resolve<INeverMockedNested>() retrieved it and
        // the emitted unbox/cast threw InvalidCastException.
        var features = IStubFeatures.Mock();
        var stub = features.Object.Get<INeverMockedGenericFeature>();

        await Assert.That(stub.Resolve<int>()).IsEqualTo(0);
        await Assert.That(stub.Resolve<INeverMockedNested>()).IsNotNull();
        await Assert.That(stub.Resolve<string>()).IsEqualTo(string.Empty);

        // Same instantiation still returns the same cached instance.
        await Assert.That(stub.Resolve<INeverMockedNested>())
            .IsSameReferenceAs(stub.Resolve<INeverMockedNested>());
    }

    [Test]
    public async Task Same_Full_Name_From_Different_Assemblies_Get_Distinct_Stubs()
    {
        // Review finding on #6519: the auto-mock cache keyed by memberName + FullName string
        // conflated two types with identical full names from different assemblies — the second
        // call retrieved the first type's stub and the cast to TReturn threw.
        var typeA = EmitEmptyInterface("Issue6514.CollisionAssemblyA");
        var typeB = EmitEmptyInterface("Issue6514.CollisionAssemblyB");

        var features = IStubFeatures.Mock();
        var get = typeof(IStubFeatures).GetMethod(nameof(IStubFeatures.Get))!;

        var first = get.MakeGenericMethod(typeA).Invoke(features.Object, null);
        var second = get.MakeGenericMethod(typeB).Invoke(features.Object, null);

        await Assert.That(first).IsNotNull();
        await Assert.That(second).IsNotNull();
        await Assert.That(typeA.IsInstanceOfType(first!)).IsTrue();
        await Assert.That(typeB.IsInstanceOfType(second!)).IsTrue();
    }

    private static Type EmitEmptyInterface(string assemblyName)
    {
        var assembly = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
            new System.Reflection.AssemblyName(assemblyName),
            System.Reflection.Emit.AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("collision");
        var type = module.DefineType(
            "Issue6514.Collision.ISameFullName",
            System.Reflection.TypeAttributes.Public
            | System.Reflection.TypeAttributes.Interface
            | System.Reflection.TypeAttributes.Abstract);
        return type.CreateType()!;
    }

    [Test]
    public async Task Stub_Indexer_State_Is_Keyed_By_Index_Arguments()
    {
        var features = IStubFeatures.Mock();
        var stub = features.Object.Get<IIndexedFeature>();

        stub[1] = "one";
        stub[2] = "two";

        await Assert.That(stub[1]).IsEqualTo("one");
        await Assert.That(stub[2]).IsEqualTo("two");
        await Assert.That(stub[3]).IsEqualTo(string.Empty); // unset index keeps the default

        stub["a", 1] = "multi";

        await Assert.That(stub["a", 1]).IsEqualTo("multi");
        await Assert.That(stub["a", 2]).IsEqualTo(string.Empty);
        await Assert.That(stub["b", 1]).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Stub_ValueTask_Of_Reference_Type_Returns_Completed_Null()
    {
        var features = IStubFeatures.Mock();
        var stub = features.Object.Get<IIndexedFeature>();

        var task = stub.GetDtoAsync();

        await Assert.That(task.IsCompleted).IsTrue();
        await Assert.That(await task).IsNull();
    }

    [Test]
    public async Task Generic_Get_With_Value_Type_Argument_Still_Returns_Default()
    {
        var features = IStubFeatures.Mock();

        await Assert.That(features.Object.Get<int>()).IsEqualTo(0);
    }

    [Test]
    public async Task Strict_Mode_Still_Throws_For_Unconfigured_Generic_Calls()
    {
        var features = IStubFeatures.Mock(MockBehavior.Strict);

        await Assert.That(() => features.Object.Get<INeverMockedFeature>())
            .Throws<TUnit.Mocks.Exceptions.MockStrictBehaviorException>();
    }

    [Test]
    public async Task Typed_Handler_Cache_Miss_Falls_Back_To_Runtime_Stub()
    {
        var mock = ITypedPathFeatures.Mock();

        var provider = mock.Object.Resolve(42);

        await Assert.That(provider).IsNotNull();
        // Stubs are functional: members return defaults instead of throwing.
        await Assert.That(provider.GetService(typeof(string))).IsNull();
        // Same member + return type resolves to the same cached stub, argument values included.
        await Assert.That(mock.Object.Resolve(43)).IsSameReferenceAs(provider);
    }

    [Test]
    public async Task Concurrent_First_Time_Stub_Emission_Is_Thread_Safe()
    {
        // GetOrAdd only de-duplicates the factory per key: distinct interfaces first-touched in
        // parallel reach EmitStubType concurrently, and the shared ModuleBuilder must be guarded.
        var features = IStubFeatures.Mock();
        using var start = new ManualResetEventSlim(false);

        Task<object?> First(Func<object?> resolve) => Task.Run(() =>
        {
            start.Wait();
            return resolve();
        });

        var tasks = new[]
        {
            First(() => features.Object.Get<IConcurrentStub0>()),
            First(() => features.Object.Get<IConcurrentStub1>()),
            First(() => features.Object.Get<IConcurrentStub2>()),
            First(() => features.Object.Get<IConcurrentStub3>()),
            First(() => features.Object.Get<IConcurrentStub4>()),
            First(() => features.Object.Get<IConcurrentStub5>()),
            First(() => features.Object.Get<IConcurrentStub6>()),
            First(() => features.Object.Get<IConcurrentStub7>()),
        };

        start.Set();
        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            await Assert.That(result).IsNotNull();
        }
    }

    // Bare NotInParallel: this test flips a global setting, so nothing may run alongside it.
    [Test]
    [NotInParallel]
    public async Task Opt_Out_Restores_The_Previous_Null_Default()
    {
        TUnitMocksSettings.Default.RuntimeAutoStubs = false;
        try
        {
            var features = IStubFeatures.Mock();

            await Assert.That(features.Object.Get<INeverMockedFeature>()).IsNull();
        }
        finally
        {
            TUnitMocksSettings.Default.RuntimeAutoStubs = true;
        }
    }
}

public interface IStubRegisteredFeature
{
    string Tag { get; }
}
