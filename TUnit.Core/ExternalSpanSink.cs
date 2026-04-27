namespace TUnit.Core;

/// <summary>
/// Process-wide hook letting out-of-process span feeders (e.g. TUnit.OpenTelemetry's
/// OTLP receiver) push into TUnit.Engine's collector without OpenTelemetry referencing
/// Engine. Engine claims the slot at session start; first-wins semantics.
/// </summary>
internal static class ExternalSpanSink
{
    private static Action<SpanData>? _sink;

    public static Action<SpanData>? Current => _sink;

    public static void Register(Action<SpanData> sink)
    {
        Interlocked.CompareExchange(ref _sink, sink, null);
    }

    public static void Unregister(Action<SpanData> sink)
    {
        Interlocked.CompareExchange(ref _sink, null, sink);
    }
}
