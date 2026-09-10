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

// Review finding on #6519 (round 5, rebutted): assigning null to a writable stub property or
// indexer must round-trip. ConcurrentDictionary rejects null KEYS, not values — the stub keys
// by slot int / index tuple, so storing a null value is fine; this pins that behavior.
public interface INullAssignableFeature
{
    string? Name { get; set; }

    string? this[int index] { get; set; }
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

public interface ISingleEmissionStub { string Name { get; } }

// By-ref returns are an unsupported stub shape: TryCreateStub fails and the engine caches the
// miss as null.
public interface IByRefFeature
{
    ref int Counter();
}

// Custom modifiers are part of the CLR signature the emitted MethodImpl must match: an init
// setter carries modreq(IsExternalInit), an `in` parameter modreq(InAttribute). Dropping either
// during emission makes CreateType reject the stub type.
public interface ICustomModifierFeature
{
    string Name { get; init; }

    int Sum(in int left, in int right);
}

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
    public async Task Stub_Writable_Property_And_Indexer_Round_Trip_Null()
    {
        var features = IStubFeatures.Mock();
        var stub = features.Object.Get<INullAssignableFeature>();

        // Overwrite a real value with null so the setter genuinely stores null rather than
        // leaving the slot untouched, then confirm the getter hands the null back instead of
        // throwing or falling through to the "" default.
        stub.Name = "set";
        stub.Name = null;
        await Assert.That(stub.Name).IsNull();

        stub[1] = "set";
        stub[1] = null;
        await Assert.That(stub[1]).IsNull();
        await Assert.That(stub[2]).IsEqualTo(string.Empty); // untouched index keeps the default
    }

    [Test]
    public async Task Stub_Supports_Init_Only_Properties_And_In_Parameters()
    {
        var features = IStubFeatures.Mock();

        // Emission must preserve modreq(IsExternalInit) / modreq(InAttribute) — a dropped
        // modifier fails CreateType, the miss is cached, and this returns null instead.
        var stub = features.Object.Get<ICustomModifierFeature>();

        await Assert.That(stub).IsNotNull();
        await Assert.That(stub.Name).IsEqualTo(string.Empty);
        await Assert.That(stub.Sum(1, 2)).IsEqualTo(0);
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

    [Test]
    public async Task Concurrent_Same_Interface_First_Touch_Emits_A_Single_Stub_Type()
    {
        // GetOrAdd may run the value factory once per contender for the SAME key and keep only
        // one result — with a plain Type? value, racing first-touches of one interface would
        // each emit a permanent dynamic type. The Lazy cache must collapse them to exactly one.
        using var start = new ManualResetEventSlim(false);

        var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            // Distinct mocks so every engine's own auto-mock cache misses and all of them race
            // into RuntimeStubGenerator for the same interface.
            var features = IStubFeatures.Mock();
            start.Wait();
            return (object?)features.Object.Get<ISingleEmissionStub>();
        })).ToArray();

        start.Set();
        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            await Assert.That(result).IsNotNull();
        }

        var emittedTypes = GetLoadedTypes(results[0]!.GetType().Assembly)
            .Where(t => t.Name.Contains(nameof(ISingleEmissionStub)))
            .ToList();
        await Assert.That(emittedTypes.Count).IsEqualTo(1);

        // Other tests may be mid-emission in the shared dynamic assembly while this enumerates
        // it — their half-built TypeBuilders throw; every ISingleEmissionStub stub is fully
        // created by this point, so the loaded subset is complete for the assertion.
        static IEnumerable<Type> GetLoadedTypes(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t is not null)!;
            }
        }
    }

    // Review finding on #6519: a cached null records a definitive miss (stub disabled,
    // unavailable, or failed) — TryGetAutoMock must not report it as a hit with a null mock,
    // which would violate NotNullWhen(true) and hand callers a null to dereference.
    [Test]
    public async Task TryGetAutoMock_Returns_False_For_A_Cached_Miss()
    {
        var features = IStubFeatures.Mock();
        await Assert.That(features.Object.Get<IByRefFeature>()).IsNull();

        var engine = ((IMockEngineAccess<IStubFeatures>)features).Engine;

        await Assert.That(engine.TryGetAutoMock("Get", typeof(IByRefFeature), out var mock)).IsFalse();
        await Assert.That(mock).IsNull();
    }

    // Review finding on #6519: the auto-mock cache re-key to (memberName, Type) removed the
    // public TryGetAutoMock(string, out IMock?) CLR signature — a binary break for assemblies
    // compiled against the previous shape. The legacy overload must keep resolving entries by
    // the old memberName + "|" + returnType.FullName key format.
    [Test]
    public async Task Legacy_String_TryGetAutoMock_Overload_Resolves_Cached_Auto_Mocks()
    {
        var features = IStubFeatures.Mock();
        var stub = features.Object.Get<INeverMockedFeature>();

        var engine = ((IMockEngineAccess<IStubFeatures>)features).Engine;
        var legacyKey = "Get|" + typeof(INeverMockedFeature).FullName;

        await Assert.That(engine.TryGetAutoMock(legacyKey, out var cached)).IsTrue();
        await Assert.That(cached!.ObjectInstance).IsSameReferenceAs(stub!);

        await Assert.That(engine.TryGetAutoMock("Get|No.Such.Type", out _)).IsFalse();
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
