using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.OpenTelemetry.Tests;

public class AutoReceiverTests
{
    [Test]
    public async Task AutoReceiver_StartedByHook_ExposesEndpoint()
    {
        await Assert.That(AutoReceiver.Endpoint).IsNotNull();
        await Assert.That(AutoReceiver.Endpoint!).StartsWith("http://127.0.0.1:");
    }

    [Test]
    public async Task AutoReceiver_HasReceiverForTesting_True()
    {
        await Assert.That(AutoReceiver.HasReceiverForTesting).IsTrue();
    }
}
