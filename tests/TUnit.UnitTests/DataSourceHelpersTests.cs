using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

/// <summary>
/// Tests for <see cref="DataSourceHelpers.HandleTupleValue"/>, which unwraps a tuple value into
/// one factory per element. Each factory shares a single pre-unwrapped snapshot of the tuple.
/// </summary>
public class DataSourceHelpersTests
{
    private sealed record DerivedTupleRow : TestDataRow<(int, string)>
    {
        public DerivedTupleRow() : base((1, "tuple")) { }
    }

    [Test]
    public async Task RowTypeLookup_UnwrapsOneLayerIncludingDerivedRecords()
    {
        await Assert.That(TestDataRowUnwrapper.GetInnerDataType(typeof(DerivedTupleRow)))
            .IsEqualTo(typeof((int, string)));
        await Assert.That(TestDataRowUnwrapper.GetInnerDataType(typeof(TestDataRow<TestDataRow<(int, string)>>)))
            .IsEqualTo(typeof(TestDataRow<(int, string)>));
    }

    private static void OpenTuple<T>(Tuple<T, string> row) { }

    private static void ObjectTuple<T>(Tuple<T, object> row) { }

    [Test]
    public async Task NullRow_DoesNotMatchOpenTupleWithTrailingParameter()
    {
        var parameter = typeof(DataSourceHelpersTests).GetMethod(nameof(OpenTuple),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetParameters()[0].ParameterType;
        object?[] values = [null];
        var result = TUnit.Engine.Helpers.DataUnwrapper.UnwrapWithTypes(values,
            [new ParameterMetadata(parameter) { Name = "row", TypeInfo = new ConcreteType(parameter) },
                new ParameterMetadata(typeof(int)) { Name = "extra", TypeInfo = new ConcreteType(typeof(int)), CachedIsOptional = true }]);

        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That(result[0]).IsNull();
    }

    [Test]
    public async Task OpenTupleBinding_RequiresOuterAssignability()
    {
        var parameter = typeof(DataSourceHelpersTests).GetMethod(nameof(ObjectTuple),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetParameters()[0].ParameterType;
        var whole = Tuple.Create(1, new object());
        var nested = Tuple.Create(whole, "extra");
        await Assert.That(TupleHelper.MatchesOpenTuple(parameter, whole.GetType())).IsTrue();
        await Assert.That(TupleHelper.MatchesOpenTuple(parameter, Tuple.Create(1, "string").GetType())).IsFalse();
        await Assert.That(TupleHelper.MatchesOpenTuple(parameter, nested.GetType())).IsFalse();
        await Assert.That(nested.ToObjectArrayWithTypes([parameter, typeof(string)]).Length).IsEqualTo(2);
    }

    private static void RepeatedTuple<T>(Tuple<T, T> row) { }

    [Test]
    public async Task OpenTupleBinding_RequiresConsistentRepeatedParameters()
    {
        var parameter = typeof(DataSourceHelpersTests).GetMethod(nameof(RepeatedTuple),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetParameters()[0].ParameterType;
        var whole = Tuple.Create(1, 2);
        var nested = Tuple.Create(whole, Tuple.Create(7, "extra"));
        await Assert.That(TupleHelper.MatchesOpenTuple(parameter, whole.GetType())).IsTrue();
        await Assert.That(TupleHelper.MatchesOpenTuple(parameter, nested.GetType())).IsFalse();
        await Assert.That(TupleHelper.MatchesOpenTuple(parameter, Tuple.Create(1, 2L).GetType())).IsFalse();
        await Assert.That(nested.ToObjectArrayWithTypes([parameter, typeof(Tuple<int, string>)]).Length).IsEqualTo(2);
    }

    [Test]
    public async Task OpenTupleBinding_DistinguishesWholeAndNestedRows()
    {
        var parameter = typeof(DataSourceHelpersTests).GetMethod(nameof(OpenTuple),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetParameters()[0].ParameterType;
        var whole = Tuple.Create(1, "tuple");
        var nested = Tuple.Create(whole, 7);
        var mismatched = Tuple.Create(1, 2);

        await Assert.That(TupleHelper.MatchesOpenTuple(parameter, whole.GetType())).IsTrue();
        await Assert.That(TupleHelper.MatchesOpenTuple(parameter, nested.GetType())).IsFalse();
        await Assert.That(TupleHelper.MatchesOpenTuple(parameter, mismatched.GetType())).IsFalse();
        await Assert.That(whole.ToObjectArrayWithTypes([parameter, typeof(int)]).Length).IsEqualTo(1);
        var columns = nested.ToObjectArrayWithTypes([parameter, typeof(int)]);
        await Assert.That(columns.Length).IsEqualTo(2);
        await Assert.That(columns[0]).IsEqualTo((object)whole);
        await Assert.That(columns[1]).IsEqualTo((object)7);
    }

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
