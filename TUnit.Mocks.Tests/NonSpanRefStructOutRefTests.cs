#if NET9_0_OR_GREATER

using System.Text;
using System.Text.Json;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

// ─── Test ref structs and interfaces ────────────────────────────────────────

public ref struct Marker
{
    public int Value;
    public byte Tag;
}

public interface IMarkerProducer
{
    void Produce(out Marker marker);
    bool Mutate(int seed, ref Marker marker);
}

public interface IJsonReaderProducer
{
    void GetReader(out Utf8JsonReader reader);
}

public interface IMixedRefStruct
{
    void DoMixed(out int count, out Marker marker, ref ReadOnlySpan<byte> buffer);
}

// Generic interface with a non-span ref struct out param — the closed-signature delegate
// setter can't be emitted here (would need `allows ref struct` on a generic delegate),
// so SetsOutMarker is intentionally not generated. The fluent wrapper for this method
// is suppressed entirely (matching pre-feature behavior for this shape).
public interface IGenericProducer<T>
{
    void Produce(out Marker marker);
}

public class NonSpanRefStructOutRefTests
{
    [Test]
    public async Task Out_UserDefinedRefStruct_Assignment()
    {
        var mock = IMarkerProducer.Mock();
        mock.Produce().SetsOutMarker((out Marker m) => m = new Marker { Value = 42, Tag = 0xAB });

        mock.Object.Produce(out var marker);
        // Hoist before any await — ref structs can't cross an async state-machine boundary.
        var value = marker.Value;
        var tag = marker.Tag;

        await Assert.That(value).IsEqualTo(42);
        await Assert.That(tag).IsEqualTo((byte)0xAB);
    }

    [Test]
    public async Task Ref_UserDefinedRefStruct_Mutation_Observed()
    {
        var mock = IMarkerProducer.Mock();
        var input = new Marker { Value = 1, Tag = 0 };

        mock.Mutate(Arg.Any<int>(), RefStructArg<Marker>.Any)
            .Returns(true)
            .SetsRefMarker((ref Marker m) =>
            {
                m.Value *= 10;
                m.Tag = 0xFF;
            });

        var result = mock.Object.Mutate(5, ref input);

        var value = input.Value;
        var tag = input.Tag;
        await Assert.That(result).IsTrue();
        await Assert.That(value).IsEqualTo(10);
        await Assert.That(tag).IsEqualTo((byte)0xFF);
    }

    [Test]
    public async Task Out_Utf8JsonReader_Bcl_RefStruct()
    {
        var mock = IJsonReaderProducer.Mock();
        var payload = Encoding.UTF8.GetBytes("{\"v\":7}");

        mock.GetReader().SetsOutReader((out Utf8JsonReader r) => r = new Utf8JsonReader(payload));

        mock.Object.GetReader(out var reader);
        var read = reader.Read();
        var tokenType = reader.TokenType;

        await Assert.That(read).IsTrue();
        await Assert.That(tokenType).IsEqualTo(JsonTokenType.StartObject);
    }

    [Test]
    public async Task Out_Multiple_Mixed_OutInt_OutRefStruct_RefSpan()
    {
        var mock = IMixedRefStruct.Mock();

        mock.DoMixed(RefStructArg<ReadOnlySpan<byte>>.Any)
            .SetsOutCount(99)
            .SetsOutMarker((out Marker m) => m = new Marker { Value = 7, Tag = 1 })
            .SetsRefBuffer(new ReadOnlySpan<byte>([4, 5, 6]));

        var buf = new ReadOnlySpan<byte>([1, 2, 3]);
        mock.Object.DoMixed(out var count, out var marker, ref buf);

        var markerValue = marker.Value;
        var markerTag = marker.Tag;
        var bufLen = buf.Length;
        var b0 = buf[0];

        await Assert.That(count).IsEqualTo(99);
        await Assert.That(markerValue).IsEqualTo(7);
        await Assert.That(markerTag).IsEqualTo((byte)1);
        await Assert.That(bufLen).IsEqualTo(3);
        await Assert.That(b0).IsEqualTo((byte)4);
    }

    [Test]
    public async Task Out_UserDefinedRefStruct_NoSetter_LeavesDefault()
    {
        var mock = IMarkerProducer.Mock();
        mock.Produce().Returns();

        mock.Object.Produce(out var marker);
        var value = marker.Value;
        var tag = marker.Tag;

        await Assert.That(value).IsEqualTo(0);
        await Assert.That(tag).IsEqualTo((byte)0);
    }

    // Pins the generic-mock boundary: the source generator cannot emit a closed-signature
    // delegate setter for a non-span ref struct out/ref param on a generic mock type
    // (would require `allows ref struct` on a generic delegate). The mock still works,
    // the out param just falls through to its default value with no way to override it.
    [Test]
    public async Task Out_NonSpanRefStruct_OnGenericMock_LeavesDefault()
    {
        var mock = IGenericProducer<int>.Mock();

        mock.Object.Produce(out var marker);
        var value = marker.Value;
        var tag = marker.Tag;

        await Assert.That(value).IsEqualTo(0);
        await Assert.That(tag).IsEqualTo((byte)0);
    }
}

#endif
