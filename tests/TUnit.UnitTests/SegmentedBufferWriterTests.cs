using System.Text.Json;
using TUnit.Engine.Reporters.Aggregation;

namespace TUnit.UnitTests;

public class SegmentedBufferWriterTests
{
    [Test]
    public async Task Empty_Writer_Produces_No_Bytes()
    {
        using var writer = new SegmentedBufferWriter();
        using var stream = new MemoryStream();

        writer.WriteTo(stream);

        await Assert.That(writer.Length).IsEqualTo(0L);
        await Assert.That(writer.ToArray()).IsEmpty();
        await Assert.That(stream.Length).IsEqualTo(0L);
    }

    [Test]
    [Arguments(1)]
    [Arguments(4096)]
    [Arguments(300_000)]
    public async Task Writes_Spanning_Many_Chunks_Round_Trip_In_Order(int writeSize)
    {
        // ~1.5MB total: several chunk rollovers, including writes larger than one chunk.
        var expected = new byte[1_500_000];
        new Random(42).NextBytes(expected);

        using var writer = new SegmentedBufferWriter();
        for (var offset = 0; offset < expected.Length; offset += writeSize)
        {
            var count = Math.Min(writeSize, expected.Length - offset);
            var span = writer.GetSpan(count);
            expected.AsSpan(offset, count).CopyTo(span);
            writer.Advance(count);
        }

        using var stream = new MemoryStream();
        writer.WriteTo(stream);

        await Assert.That(writer.Length).IsEqualTo((long)expected.Length);
        await Assert.That(writer.ToArray().AsSpan().SequenceEqual(expected)).IsTrue();
        await Assert.That(stream.ToArray().AsSpan().SequenceEqual(expected)).IsTrue();
    }

    [Test]
    public async Task Utf8JsonWriter_Output_Matches_Contiguous_Buffer()
    {
        using var segmented = new SegmentedBufferWriter();
        using var contiguous = new MemoryStream();

        WriteLargeDocument(segmented);
        WriteLargeDocument(contiguous);

        await Assert.That(segmented.ToArray().AsSpan().SequenceEqual(contiguous.ToArray())).IsTrue();

        static void WriteLargeDocument(object output)
        {
            using var w = output is Stream s ? new Utf8JsonWriter(s) : new Utf8JsonWriter((SegmentedBufferWriter)output);
            w.WriteStartArray();
            var longValue = new string('x', 100_000);
            for (var i = 0; i < 20_000; i++)
            {
                w.WriteStartObject();
                w.WriteString("id", "span-" + i);
                w.WriteNumber("durationMs", i * 1.25);
                if (i % 1000 == 0)
                {
                    // Values larger than a single Utf8JsonWriter growth step.
                    w.WriteString("output", longValue);
                }
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
    }

    [Test]
    public async Task Advance_Past_Requested_Span_Throws()
    {
        using var writer = new SegmentedBufferWriter();
        var available = writer.GetSpan(16).Length;

        await Assert.That(() => writer.Advance(available + 1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => writer.Advance(-1)).Throws<ArgumentOutOfRangeException>();
    }
}
