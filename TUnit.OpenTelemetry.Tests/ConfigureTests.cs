using OpenTelemetry;
using OpenTelemetry.Trace;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.OpenTelemetry.Tests;

public class ConfigureTests
{
    [Test]
    public async Task Configure_StoresUserCallback()
    {
        TUnitOpenTelemetry.ResetForTests();
        var called = false;
        TUnitOpenTelemetry.Configure(_ => called = true);

        TUnitOpenTelemetry.ApplyConfiguration(Sdk.CreateTracerProviderBuilder());

        await Assert.That(called).IsTrue();
    }

    [Test]
    public async Task Configure_MultipleCalls_AllInvoked()
    {
        TUnitOpenTelemetry.ResetForTests();
        var count = 0;
        TUnitOpenTelemetry.Configure(_ => count++);
        TUnitOpenTelemetry.Configure(_ => count++);

        TUnitOpenTelemetry.ApplyConfiguration(Sdk.CreateTracerProviderBuilder());

        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async Task Configure_NullCallback_Throws()
    {
        await Assert.That(() => TUnitOpenTelemetry.Configure(null!)).Throws<ArgumentNullException>();
    }
}
