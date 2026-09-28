using System.Buffers;
using System.Text.Json;

namespace TUnit.Engine.Reporters.Aggregation;

/// <summary>
/// Writes the elements of a large JSON array on several threads. Reports are serialized after the
/// last test finishes, on the critical path of the run, while the thread pool is otherwise idle.
/// Each slice of elements is written by its own <see cref="Utf8JsonWriter"/> into its own buffer and
/// the slices are appended in order, so the output is byte-identical to writing the elements one by one.
/// Source-linked into TUnit.Reporting.Tool alongside the serializers that use it.
/// </summary>
internal static class ParallelJsonArrayWriter
{
    /// <summary>
    /// Writes <paramref name="count"/> elements into the array that <paramref name="writer"/> has just
    /// opened with <see cref="Utf8JsonWriter.WriteStartArray()"/>. <paramref name="buffer"/> must be the
    /// writer's output and <paramref name="options"/> its options. The caller's next call on
    /// <paramref name="writer"/> must be <see cref="Utf8JsonWriter.WriteEndArray"/>.
    /// </summary>
    public static void WriteElements(
        Utf8JsonWriter writer,
        SegmentedBufferWriter buffer,
        JsonWriterOptions options,
        int count,
        int minElementsPerSlice,
        Action<Utf8JsonWriter, int> writeElement)
    {
        var sliceCount = Math.Min(Environment.ProcessorCount, count / minElementsPerSlice);
        if (sliceCount < 2)
        {
            for (var i = 0; i < count; i++)
            {
                writeElement(writer, i);
            }

            return;
        }

        // Without validation a slice writer accepts several top-level values, and it still separates
        // them with commas exactly as it would inside an array.
        var sliceOptions = options;
        sliceOptions.SkipValidation = true;
        var slices = new SegmentedBufferWriter?[sliceCount];
        try
        {
            Parallel.For(0, sliceCount, slice =>
            {
                var sliceBuffer = new SegmentedBufferWriter();
                slices[slice] = sliceBuffer;
                using var sliceWriter = new Utf8JsonWriter(sliceBuffer, sliceOptions);
                var end = (int)((long)count * (slice + 1) / sliceCount);
                for (var i = (int)((long)count * slice / sliceCount); i < end; i++)
                {
                    writeElement(sliceWriter, i);
                }
            });

            // Bytes appended to the buffer directly bypass the writer, which still considers the array
            // empty. Closing the array needs no separator, which is why that must be the next call.
            writer.Flush();
            for (var slice = 0; slice < sliceCount; slice++)
            {
                if (slice > 0)
                {
                    buffer.Write(","u8);
                }

                slices[slice]!.WriteTo(buffer);
            }
        }
        finally
        {
            foreach (var slice in slices)
            {
                slice?.Dispose();
            }
        }
    }
}
