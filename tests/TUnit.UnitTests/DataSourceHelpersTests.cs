using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

/// <summary>
/// Tests for <see cref="DataSourceHelpers.HandleTupleValue"/>, which unwraps a tuple value into
/// one factory per element. Each factory shares a single pre-unwrapped snapshot of the tuple.
/// </summary>
public class DataSourceHelpersTests
{
    [Test]
    public async Task HandleTupleValue_Tuple_ReturnsFactoryPerElement()
    {
        var factories = DataSourceHelpers.HandleTupleValue((1, "two", 3.0), shouldUnwrap: true);

        await Assert.That(factories.Length).IsEqualTo(3);
        await Assert.That(await factories[0]()).IsEqualTo((object?)1);
        await Assert.That(await factories[1]()).IsEqualTo((object?)"two");
        await Assert.That(await factories[2]()).IsEqualTo((object?)3.0);
    }

    [Test]
    public async Task HandleTupleValue_FactoriesReturnSameSnapshotOnRepeatedInvocation()
    {
        var factories = DataSourceHelpers.HandleTupleValue((1, 2), shouldUnwrap: true);

        // Each factory shares the pre-unwrapped snapshot, so repeated invocations are stable.
        await Assert.That(await factories[0]()).IsEqualTo(await factories[0]());
        await Assert.That(await factories[1]()).IsEqualTo((object?)2);
    }

    [Test]
    public async Task HandleTupleValue_NotUnwrapping_ReturnsSingleFactoryWithOriginalValue()
    {
        var value = (1, 2);
        var factories = DataSourceHelpers.HandleTupleValue(value, shouldUnwrap: false);

        await Assert.That(factories.Length).IsEqualTo(1);
        await Assert.That(await factories[0]()).IsEqualTo((object?)value);
    }

    [Test]
    public async Task HandleTupleValue_Null_ReturnsSingleNullFactory()
    {
        var factories = DataSourceHelpers.HandleTupleValue(null, shouldUnwrap: true);

        await Assert.That(factories.Length).IsEqualTo(1);
        await Assert.That(await factories[0]()).IsNull();
    }

    [Test]
    public async Task HandleTupleValue_NonTuple_ReturnsSingleFactory()
    {
        var factories = DataSourceHelpers.HandleTupleValue(42, shouldUnwrap: true);

        await Assert.That(factories.Length).IsEqualTo(1);
        await Assert.That(await factories[0]()).IsEqualTo((object?)42);
    }
}
