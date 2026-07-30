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
