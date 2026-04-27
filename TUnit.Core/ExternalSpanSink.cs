namespace TUnit.Core;

/// <summary>
/// Process-wide hook letting out-of-process span feeders (e.g. TUnit.OpenTelemetry's
/// OTLP receiver) push into TUnit.Engine's collector without OpenTelemetry referencing
/// Engine. Engine claims the slot at session start; first-wins semantics.
/// </summary>
internal static class ExternalSpanSink
{
    private static Action<SpanData>? _sink;

    // Volatile.Read pairs with the full fence in Interlocked.CompareExchange on the
    // Register/Unregister side; without it, weak memory models (ARM) could observe
    // a stale null after another thread has published a sink.
    public static Action<SpanData>? Current => Volatile.Read(ref _sink);

    public static void Register(Action<SpanData> sink)
    {
        Interlocked.CompareExchange(ref _sink, sink, null);
    }

    public static void Unregister(Action<SpanData> sink)
    {
        Interlocked.CompareExchange(ref _sink, null, sink);
    }
}
