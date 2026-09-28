using System.Text;
using System.Text.Json;
using TUnit.Engine.Reporters.Aggregation;

namespace TUnit.UnitTests;

public class ParallelJsonArrayWriterTests
{
    private static readonly JsonWriterOptions Options = new() { Indented = false };

    [Test]
    [Arguments(0, 1)]
    [Arguments(1, 1)]
    [Arguments(7, 1)]
    [Arguments(10_000, 10)]
    [Arguments(10_000, 5_000)]
    [Arguments(10_000, 100_000)]
    public async Task Output_Matches_Sequential_Serialization(int count, int minElementsPerSlice)
    {
        var expected = Serialize(count, minElementsPerSlice: null);
        var actual = Serialize(count, minElementsPerSlice);

        await Assert.That(actual).IsEqualTo(expected);
        using var document = JsonDocument.Parse(actual);
        await Assert.That(document.RootElement.GetProperty("items").GetArrayLength()).IsEqualTo(count);
    }

    // Writes an object whose "items" array is filled one element at a time when
    // minElementsPerSlice is null, and through ParallelJsonArrayWriter otherwise.
    private static string Serialize(int count, int? minElementsPerSlice)
    {
        using var buffer = new SegmentedBufferWriter();
        using (var w = new Utf8JsonWriter(buffer, Options))
        {
            w.WriteStartObject();
            w.WriteString("before", "value");
            w.WritePropertyName("items");
            w.WriteStartArray();
            if (minElementsPerSlice is { } min)
            {
                ParallelJsonArrayWriter.WriteElements(w, buffer, Options, count, min, WriteElement);
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    WriteElement(w, i);
                }
            }
            w.WriteEndArray();
            w.WriteString("after", "value");
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteElement(Utf8JsonWriter w, int i)
    {
        w.WriteStartObject();
        w.WriteNumber("index", i);
        w.WriteString("name", $"Test{i} \"quoted\" \\ <tag>");
        w.WritePropertyName("nested");
        w.WriteStartArray();
        w.WriteNumberValue(i * 0.5);
        w.WriteNullValue();
        w.WriteEndArray();
        w.WriteEndObject();
    }
}
