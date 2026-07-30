using FakeSdk;
using TUnit.Mocks;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.InternalsAccess.Tests;

// Experimental #6514 "Tier 2": IInternalBindingsFeature is internal to the FakeSdk assembly,
// which grants NO InternalsVisibleTo (not to this assembly, not to DynamicProxyGenAssembly2).
// With TUnitMocksExperimentalInternalsAccess enabled, the compiler sees a publicized copy of the
// reference and IgnoresAccessChecksTo makes it valid at runtime — so the type is nameable here,
// the source generator mocks it like any public interface, and setups/verification are fully
// typed. This exceeds what runtime-proxy libraries offer: they can auto-substitute such a type
// but can never let the test configure or verify it, because the test cannot write its name.

public class InternalsAccessTests
{
    [Test]
    public async Task Internal_Interface_Is_Nameable_And_Mockable()
    {
        var bindings = IInternalBindingsFeature.Mock();
        bindings.InvocationResult.Returns("configured");

        await Assert.That(bindings.Object.InvocationResult).IsEqualTo("configured");
    }

    [Test]
    public async Task Sdk_Internal_Generic_Request_Receives_The_Configured_Mock()
    {
        var bindings = IInternalBindingsFeature.Mock();
        bindings.InvocationResult.Returns("from-tier2");

        var features = IFeatureCollection.Mock();
        features.Get<IInternalBindingsFeature>().Returns(bindings.Object);

        // The generic request happens inside the SDK, not in this assembly.
        await Assert.That(SdkRuntime.DescribeInvocation(features.Object)).IsEqualTo("from-tier2");

        // Typed verification of a call whose type argument is internal to another assembly.
        features.Get<IInternalBindingsFeature>().WasCalled(Times.Once);
    }

    [Test]
    public async Task Typed_Setup_With_Matchers_On_Internal_Member()
    {
        var bindings = IInternalBindingsFeature.Mock();
        bindings.Compute(Any<int>()).Returns(seed => seed * 2);

        var features = IFeatureCollection.Mock();
        features.Get<IInternalBindingsFeature>().Returns(bindings.Object);

        await Assert.That(SdkRuntime.RunComputation(features.Object, 21)).IsEqualTo(42);

        bindings.Compute(21).WasCalled(Times.Once);
        bindings.Compute(99).WasNeverCalled();
    }

    [Test]
    public async Task Manual_Implementation_Of_The_Internal_Interface_Loads_And_Runs()
    {
        // Proves IgnoresAccessChecksTo is honored at type-load time for hand-written
        // implementations too, not just generated mocks.
        var features = IFeatureCollection.Mock();
        features.Get<IInternalBindingsFeature>().Returns(new ManualBindings());

        await Assert.That(SdkRuntime.DescribeInvocation(features.Object)).IsEqualTo("manual");
        await Assert.That(SdkRuntime.RunComputation(features.Object, 5)).IsEqualTo(6);
    }

    private sealed class ManualBindings : IInternalBindingsFeature
    {
        public string InvocationResult { get; set; } = "manual";

        public int Compute(int seed) => seed + 1;
    }
}
