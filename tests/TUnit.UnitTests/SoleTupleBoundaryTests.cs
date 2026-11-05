using TUnit.Core.Helpers;
using TUnit.Engine.Helpers;

namespace TUnit.UnitTests;

public class SoleTupleBoundaryTests
{
    private static void OpenNestedTuple<T>(Tuple<Tuple<T>> row) { }

    [Test]
    public async Task WrappedNestedSingleton_PreservesPayloadForReconstruction()
    {
        var payload = Tuple.Create(new ValueTuple<int>(7));
        var wrapper = new TestDataRow<Tuple<ValueTuple<int>>>(payload) { DisplayName = "nested payload" };
        var parameterType = typeof(SoleTupleBoundaryTests).GetMethod(nameof(OpenNestedTuple),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetParameters()[0].ParameterType;
        ParameterMetadata[] parameters =
        [
            new(parameterType) { Name = "row", TypeInfo = new ConcreteType(parameterType) }
        ];

        var result = DataUnwrapper.UnwrapWithTypesAndMetadata([wrapper], parameters);

        await Assert.That(result.Data.Length).IsEqualTo(1);
        await Assert.That(ReferenceEquals(result.Data[0], payload)).IsTrue();
        await Assert.That(result.Metadata!.DisplayName).IsEqualTo("nested payload");
    }

    [Test]
    public async Task UnwrappedNestedSingleton_PreservesOuterBoundary()
    {
        var payload = Tuple.Create(new ValueTuple<int>(7));
        var parameterType = typeof(ValueTuple<Tuple<int>>);
        ParameterMetadata[] parameters =
        [
            new(parameterType) { Name = "row", TypeInfo = new ConcreteType(parameterType) }
        ];

        var result = DataUnwrapper.UnwrapWithTypes([payload], parameters);

        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That(ReferenceEquals(result[0], payload)).IsTrue();
    }

    [Test]
    public async Task WrappedTupleWithoutExpectedParameter_StillSpreadsPayload()
    {
        var (data, metadata) = TestDataRowUnwrapper.UnwrapArray([new TestDataRow<(int, string)>((7, "value"))]);

        await Assert.That(data.Length).IsEqualTo(2);
        await Assert.That(data[0]).IsEqualTo((object)7);
        await Assert.That(data[1]).IsEqualTo((object)"value");
        await Assert.That(metadata).IsNotNull();
    }
}
