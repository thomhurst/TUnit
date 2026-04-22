using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using TUnit.Core;
using TUnit.Engine.Reporters.Html;

namespace TUnit.OpenTelemetry.Receiver;

/// <summary>
/// A lightweight OTLP/HTTP receiver that accepts telemetry from SUT processes
/// and correlates log records back to the originating test via TraceId.
/// </summary>
/// <remarks>
/// <para>
/// The receiver listens on a dynamic localhost port and accepts OTLP/HTTP protobuf
/// exports at <c>/v1/logs</c> (parsed for correlation) and <c>/v1/traces</c>
/// (accepted but only forwarded, not parsed). It extracts TraceId from incoming
/// log records and looks up the corresponding test in <see cref="TraceRegistry"/>
/// to route logs to the correct test output.
/// </para>
/// <para>
/// Optionally forwards received telemetry to an upstream OTLP endpoint (e.g., the
/// Aspire dashboard) so both TUnit and the dashboard receive the data.
/// </para>
/// </remarks>
internal sealed class OtlpReceiver : IAsyncDisposable
{
    private const long MaxBodyBytes = 16 * 1024 * 1024; // 16 MB

    private static readonly HttpClient s_forwardingClient = new();

    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<int, Task> _inflightTasks = new();
    private string? _upstreamEndpoint;
    private Task? _listenTask;
    private int _taskIdCounter;
    private int _requestCount;

    /// <summary>
    /// The port the receiver is listening on.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// The number of POST requests successfully processed.
    /// </summary>
    internal int RequestCount => _requestCount;

    /// <summary>
    /// Creates a new OTLP receiver.
    /// </summary>
    /// <param name="upstreamEndpoint">
    /// Optional upstream OTLP endpoint to forward telemetry to (e.g., Aspire dashboard).
    /// If <c>null</c>, received telemetry is only used for correlation, not forwarded.
    /// </param>
    public OtlpReceiver(string? upstreamEndpoint = null)
    {
        (_listener, Port) = CreateListener();
        UpstreamEndpoint = upstreamEndpoint;
    }

    /// <summary>
    /// Optional upstream OTLP endpoint to forward telemetry to. May be assigned after
    /// construction — Aspire env-var callbacks resolve at resource startup, not at
    /// receiver-build time. Set to <c>null</c> to stop forwarding.
    /// </summary>
    public string? UpstreamEndpoint
    {
        get => Volatile.Read(ref _upstreamEndpoint);
        set => Volatile.Write(ref _upstreamEndpoint, value?.TrimEnd('/'));
    }

    /// <summary>
    /// Starts accepting OTLP requests.
    /// </summary>
    public void Start()
    {
        _listenTask = Task.Run(ListenLoop);
    }

    private async Task ListenLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException) when (_cts.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            TrackTask(Task.Run(() => ProcessRequestAsync(context)));
        }
    }

    /// <summary>
    /// Returns a task that completes when all in-flight request processing has finished.
    /// </summary>
    internal Task WhenIdle() => Task.WhenAll(_inflightTasks.Values);

    private void TrackTask(Task task)
    {
        var id = Interlocked.Increment(ref _taskIdCounter);
        _inflightTasks[id] = task;
        task.ContinueWith(_ => _inflightTasks.TryRemove(id, out Task? _), TaskContinuationOptions.ExecuteSynchronously);
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        if (_cts.IsCancellationRequested)
        {
            context.Response.StatusCode = 503;
            context.Response.Close();
            return;
        }

        try
        {
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod != "POST")
            {
                response.StatusCode = 405;
                response.Close();
                return;
            }

            if (request.ContentLength64 > MaxBodyBytes)
            {
                response.StatusCode = 413;
                response.Close();
                return;
            }

            // ContentLength64 is -1 for chunked; size-known path avoids MemoryStream growth copies.
            byte[] body;
            if (request.ContentLength64 >= 0)
            {
                body = new byte[request.ContentLength64];
                await request.InputStream.ReadExactlyAsync(body, _cts.Token).ConfigureAwait(false);
            }
            else
            {
                using var ms = new MemoryStream();
                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;
                while ((bytesRead = await request.InputStream.ReadAsync(buffer, _cts.Token).ConfigureAwait(false)) > 0)
                {
                    totalRead += bytesRead;
                    if (totalRead > MaxBodyBytes)
                    {
                        response.StatusCode = 413;
                        response.Close();
                        return;
                    }

                    ms.Write(buffer, 0, bytesRead);
                }

                body = ms.ToArray();
            }

            var path = request.Url?.AbsolutePath ?? "";

            if (path == "/v1/logs")
            {
                ProcessLogs(body);
            }
            else if (path == "/v1/traces")
            {
                ProcessTraces(body);
            }

            var upstream = Volatile.Read(ref _upstreamEndpoint);
            if (upstream is not null)
            {
                TrackTask(ForwardAsync(upstream, path, body, request.ContentType));
            }

            Interlocked.Increment(ref _requestCount);

            response.StatusCode = 200;
            response.ContentType = "application/x-protobuf";
            response.ContentLength64 = 0;
            response.Close();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[TUnit.OpenTelemetry] OTLP request processing failed: {ex.Message}");
            try
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch
            {
                // Best effort
            }
        }
    }

    private static void ProcessTraces(byte[] body)
    {
        var collector = ActivityCollector.Current;
        if (collector is null)
        {
            return;
        }

        IReadOnlyList<OtlpSpanRecord> spans;
        try
        {
            spans = OtlpTraceParser.Parse(body);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[TUnit.OpenTelemetry] Failed to parse /v1/traces body: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        foreach (var span in spans)
        {
            collector.IngestExternalSpan(ToSpanData(span));
        }
    }

    private static SpanData ToSpanData(OtlpSpanRecord span)
    {
        ReportKeyValue[]? tags = null;
        if (span.Attributes.Count > 0)
        {
            tags = new ReportKeyValue[span.Attributes.Count];
            for (var i = 0; i < span.Attributes.Count; i++)
            {
                tags[i] = new ReportKeyValue
                {
                    Key = span.Attributes[i].Key,
                    Value = span.Attributes[i].Value,
                };
            }
        }

        SpanEvent[]? events = null;
        if (span.Events.Count > 0)
        {
            events = new SpanEvent[span.Events.Count];
            for (var i = 0; i < span.Events.Count; i++)
            {
                var evt = span.Events[i];
                ReportKeyValue[]? evtTags = null;
                if (evt.Attributes.Count > 0)
                {
                    evtTags = new ReportKeyValue[evt.Attributes.Count];
                    for (var j = 0; j < evt.Attributes.Count; j++)
                    {
                        evtTags[j] = new ReportKeyValue
                        {
                            Key = evt.Attributes[j].Key,
                            Value = evt.Attributes[j].Value,
                        };
                    }
                }

                events[i] = new SpanEvent
                {
                    Name = evt.Name,
                    TimestampMs = evt.TimeUnixNano / 1_000_000.0,
                    Tags = evtTags,
                };
            }
        }

        SpanLink[]? links = null;
        if (span.Links.Count > 0)
        {
            links = new SpanLink[span.Links.Count];
            for (var i = 0; i < span.Links.Count; i++)
            {
                links[i] = new SpanLink
                {
                    TraceId = span.Links[i].TraceId,
                    SpanId = span.Links[i].SpanId,
                };
            }
        }

        var startMs = span.StartTimeUnixNano / 1_000_000.0;
        var endMs = span.EndTimeUnixNano / 1_000_000.0;

        return new SpanData
        {
            TraceId = span.TraceId,
            SpanId = span.SpanId,
            ParentSpanId = span.ParentSpanId,
            Name = span.Name,
            // SpanType classifies TUnit's own spans ("test_case", "test_suite", etc.)
            // and stays null for external spans — no analogue exists in OTLP.
            SpanType = null,
            Source = string.IsNullOrEmpty(span.ScopeName) ? span.ResourceName : span.ScopeName,
            Kind = MapSpanKind(span.Kind),
            StartTimeMs = startMs,
            DurationMs = endMs - startMs,
            Status = MapStatusCode(span.StatusCode),
            StatusMessage = string.IsNullOrEmpty(span.StatusMessage) ? null : span.StatusMessage,
            Tags = tags,
            Events = events,
            Links = links,
        };
    }

    private static string MapSpanKind(int kind) => kind switch
    {
        1 => "Internal",
        2 => "Server",
        3 => "Client",
        4 => "Producer",
        5 => "Consumer",
        _ => "Internal",
    };

    private static string MapStatusCode(int code) =>
        code is >= (int)ActivityStatusCode.Unset and <= (int)ActivityStatusCode.Error
            ? ((ActivityStatusCode)code).ToString()
            : nameof(ActivityStatusCode.Unset);

    private static void ProcessLogs(byte[] body)
    {
        List<OtlpLogRecord> records;
        try
        {
            records = OtlpLogParser.Parse(body);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[TUnit.OpenTelemetry] Failed to parse /v1/logs body: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        foreach (var record in records)
        {
            if (string.IsNullOrEmpty(record.TraceId))
            {
                continue;
            }

            // Look up the test context for this trace
            var contextId = TraceRegistry.GetContextId(record.TraceId);
            if (contextId is null)
            {
                continue;
            }

            var testContext = TestContext.GetById(contextId);
            if (testContext is null)
            {
                continue;
            }

            // Format and write to the test's output
            var severity = OtlpLogParser.FormatSeverity(record.SeverityNumber, record.SeverityText);
            var prefix = string.IsNullOrEmpty(record.ResourceName)
                ? ""
                : $"[{record.ResourceName}] ";

            testContext.Output.WriteLine($"{prefix}[{severity}] {record.Body}");
        }
    }

    private static async Task ForwardAsync(string upstream, string path, byte[] body, string? contentType)
    {
        try
        {
            using var content = new ByteArrayContent(body);
            if (contentType is not null)
            {
                content.Headers.TryAddWithoutValidation("Content-Type", contentType);
            }

            await s_forwardingClient.PostAsync(
                $"{upstream}{path}",
                content).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[TUnit.OpenTelemetry] OTLP forwarding to upstream failed: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);

        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch
        {
            // Best effort
        }

        if (_listenTask is not null)
        {
            try
            {
                await _listenTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        // Wait for any in-flight request/forwarding tasks to complete so we don't
        // access TraceRegistry or TestContext after they've been torn down.
        try
        {
            await Task.WhenAll(_inflightTasks.Values).ConfigureAwait(false);
        }
        catch
        {
            // Best effort — individual failures already logged via Trace.WriteLine
        }

        _cts.Dispose();
    }

    /// <summary>
    /// Creates an <see cref="HttpListener"/> bound to a free port. Uses a retry loop to
    /// handle the TOCTOU window between discovering a free port and binding to it.
    /// </summary>
    private static (HttpListener Listener, int Port) CreateListener() => LoopbackHttpListenerFactory.Create();
}

/// <summary>
/// Binds an <see cref="HttpListener"/> to a free loopback port, retrying if the
/// port is taken between probing and binding (TOCTOU window).
/// </summary>
internal static class LoopbackHttpListenerFactory
{
    private const int MaxPortBindingAttempts = 10;

    internal static (HttpListener Listener, int Port) Create()
    {
        for (var attempt = 0; attempt < MaxPortBindingAttempts; attempt++)
        {
            var port = FindFreePort();
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");

            try
            {
                listener.Start();
                return (listener, port);
            }
            catch (HttpListenerException)
            {
                // Port was taken between FindFreePort and Start — retry
            }
        }

        throw new InvalidOperationException($"Could not bind loopback HttpListener after {MaxPortBindingAttempts} attempts.");
    }

    private static int FindFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
