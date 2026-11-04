using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

/// <summary>
/// Tests for <see cref="DataSourceHelpers.HandleTupleValue"/>, which unwraps a tuple value into
/// one factory per element. Each factory shares a single pre-unwrapped snapshot of the tuple.
/// </summary>
public class DataSourceHelpersTests
{
    [Test]
    [Arguments(null, false)]
    [Arguments("text", true)]
    public async Task SingletonReferenceTupleDecision_DoesNotAllocateElementArray(string? element, bool expectedFlatten)
    {
        var row = Tuple.Create(element);
        var parameter = new ParameterMetadata(typeof(object))
        {
            Name = "row",
            TypeInfo = new ConstructedGeneric(typeof(Tuple<>), [new GenericParameter(0, false, "T")])
        };

        // Warm type metadata and JIT before measuring the binding decision alone.
        for (var i = 0; i < 100; i++)
        {
            DataSourceHelpers.ShouldFlattenErasedTuple(row, parameter);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var flattened = false;
        for (var i = 0; i < 100; i++)
        {
            flattened = DataSourceHelpers.ShouldFlattenErasedTuple(row, parameter);
        }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        await Assert.That(flattened).IsEqualTo(expectedFlatten);
        await Assert.That(allocated).IsEqualTo(0L);
    }

    [Test]
    public async Task Typed_Tuple_Rows_Keep_Excess_Columns_For_Binding()
    {
        var row = ((object)(1, "two", 3)).ToObjectArrayWithTypes([typeof(int), typeof(string)]);
        await Assert.That(row).IsEquivalentTo(new object?[] { 1, "two", 3 });
    }

    [Test]
    public async Task Reference_Tuple_Rows_Preserve_Whole_And_Spread_Binding()
    {
        var tuple = Tuple.Create(1, "two");
        var whole = tuple.ToObjectArrayWithTypes([typeof(Tuple<int, string>)]);
        await Assert.That(ReferenceEquals(whole[0], tuple)).IsTrue();
        var spread = tuple.ToObjectArrayWithTypes([typeof(int), typeof(string)]);
        await Assert.That(spread).IsEquivalentTo(new object?[] { 1, "two" });

        var nested = Tuple.Create(new TestDataRow<int>(7));
        var nestedWhole = nested.ToObjectArrayWithTypes([typeof(Tuple<TestDataRow<int>>)]);
        await Assert.That(ReferenceEquals(nestedWhole[0], nested)).IsTrue();
        var nestedSpread = nested.ToObjectArrayWithTypes([typeof(TestDataRow<int>)]);
        await Assert.That(ReferenceEquals(nestedSpread[0], nested.Item1)).IsTrue();

        var wrapper = new TestDataRow<Tuple<TestDataRow<int>>>(nested);
        var container = ValueTuple.Create(wrapper).ToObjectArrayWithTypes([typeof(Tuple<TestDataRow<int>>)]);
        var (unwrapped, _) = TestDataRowUnwrapper.UnwrapArray(container, typeof(Tuple<TestDataRow<int>>));
        await Assert.That(ReferenceEquals(unwrapped[0], nested)).IsTrue();
    }

    [Test]
    public async Task UnwrapTuple_LongValueTuple_FlattensMultipleRestLevels()
    {
        object value = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, (15, 16));
        var elements = TupleHelper.UnwrapTupleUsingReflection(value);

        await Assert.That(elements.Length).IsEqualTo(15);
        await Assert.That(elements[7]).IsEqualTo((object)8);
        await Assert.That(elements[14]).IsEqualTo((object)(15, 16));
    }

    [Test]
    public async Task UnwrapTuple_LongReferenceTuple_FlattensRest()
    {
        object value = Tuple.Create(1, 2, 3, 4, 5, 6, 7, "eight");
        var elements = TupleHelper.UnwrapTupleUsingReflection(value);

        await Assert.That(elements.Length).IsEqualTo(8);
        await Assert.That(elements[7]).IsEqualTo((object)"eight");
    }

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
