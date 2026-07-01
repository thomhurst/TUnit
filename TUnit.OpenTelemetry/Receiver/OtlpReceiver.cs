using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using TUnit.Core;

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
    private const string DrainWindowEnvVar = "TUNIT_OTLP_DRAIN_MS";
    private const string DiagnosticsDumpEnvVar = "TUNIT_OTLP_DEBUG";

    private static readonly HttpClient s_forwardingClient = new();

    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<int, Task> _inflightTasks = new();
    private readonly OtlpReceiverDiagnostics _diagnostics = new();

    // service.name values seen on incoming OTLP *log* records. Recorded for every parsed log
    // record regardless of trace-id match — presence here means OTLP logging reached us at all.
    // TUnit.Aspire uses it to tell a resource that exported correlated logs from one that only
    // wrote raw console output, so it can hint at missing OpenTelemetry log export in the SUT.
    private readonly ConcurrentDictionary<string, byte> _seenLogServiceNames = new(StringComparer.OrdinalIgnoreCase);

    // Cached so passing it to the parser doesn't allocate a delegate per /v1/logs request.
    private readonly Action<string> _recordSeenLogService;

    private string? _upstreamEndpoint;
    private IReadOnlyDictionary<string, string>? _upstreamHeaders;
    private Task? _listenTask;
    private int _taskIdCounter;

    /// <summary>
    /// The port the receiver is listening on.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Total POST requests received, including those rejected (e.g. gRPC content-type)
    /// before signal-specific processing. Use the per-signal counters
    /// (<c>LogsRequests</c>, <c>TracesRequests</c>, <c>MetricsRequests</c>, <c>GrpcRejected</c>)
    /// on <see cref="OtlpReceiverDiagnostics"/> for finer-grained breakdown.
    /// </summary>
    internal int RequestCount => _diagnostics.TotalRequests;

    /// <summary>
    /// Per-counter snapshot of receiver activity. Useful in tests and when diagnosing
    /// silent drops in user environments via <see cref="WriteDiagnosticsSummary"/>.
    /// </summary>
    internal OtlpReceiverDiagnostics Diagnostics => _diagnostics;

    /// <summary>
    /// Returns <c>true</c> if at least one OTLP log record carrying the given <c>service.name</c>
    /// resource attribute has been received (whether or not its trace id matched a registered
    /// test). Used by TUnit.Aspire to distinguish a resource that exported telemetry from one
    /// that only wrote to the console, so it can hint at missing OpenTelemetry log export.
    /// </summary>
    internal bool HasSeenLogsFrom(string serviceName) => _seenLogServiceNames.ContainsKey(serviceName);

    /// <summary>Snapshot of all <c>service.name</c> values seen on incoming OTLP log records.</summary>
    internal IReadOnlyCollection<string> SeenLogServiceNames => _seenLogServiceNames.Keys.ToArray();

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
        _recordSeenLogService = name => _seenLogServiceNames.TryAdd(name, 0);
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
    /// Optional headers attached to upstream forwarded requests. The Aspire dashboard
    /// gates its OTLP endpoints on an <c>x-otlp-api-key</c> header; without it, forwarding
    /// returns 401 and the dashboard never sees the SUT's spans.
    /// </summary>
    public IReadOnlyDictionary<string, string>? UpstreamHeaders
    {
        get => Volatile.Read(ref _upstreamHeaders);
        set => Volatile.Write(ref _upstreamHeaders, value);
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

    /// <summary>
    /// Waits for the receiver to go quiet — no in-flight tasks and no new POSTs for a short
    /// stable window — up to <paramref name="window"/>. Intended for the session-end boundary
    /// where the SUT's <c>BatchSpanProcessor</c> may still have unflushed spans queued; without
    /// this, fast tests can finish and tear down their AppHost before exporters drain, dropping
    /// the trailing telemetry the report is meant to show.
    /// </summary>
    /// <remarks>
    /// Best-effort heuristic: the drain returns as soon as no new requests arrive for the
    /// stable window. Spans the SUT exports after the drain returns can still be missed —
    /// this isn't an explicit OTel flush, since exporters in another process can't be
    /// signalled directly. Increase <c>TUNIT_OTLP_DRAIN_MS</c> if your exporter's batch
    /// schedule is longer than the default 2s.
    /// <para>
    /// The internal stable window (250&#160;ms of inactivity) is fixed; only the total cap
    /// is configurable via <c>TUNIT_OTLP_DRAIN_MS</c>.
    /// </para>
    /// </remarks>
    /// <param name="window">Maximum total time to wait. Defaults to <see cref="DefaultDrainWindow"/>.</param>
    /// <param name="cancellationToken">Stops the wait early.</param>
    public async Task DrainAsync(TimeSpan? window = null, CancellationToken cancellationToken = default)
    {
        var stableFor = TimeSpan.FromMilliseconds(250);
        var totalWindow = window ?? DefaultDrainWindow;
        var clock = Stopwatch.StartNew();

        // A request that's been sent over TCP but not yet pulled by GetContextAsync is
        // invisible to both _inflightTasks and _diagnostics.TotalRequests — there's no
        // hook between kernel TCP queue and HttpListener's accept loop. A single 250ms
        // idle window can therefore return while a request is still on the wire. Require
        // two consecutive idle windows (~500ms) so an in-transit POST has a chance to
        // surface before drain declares quiet.
        var consecutiveIdleWindows = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var beforeCount = Volatile.Read(ref _diagnostics.TotalRequests);

            try
            {
                await WhenIdle().ConfigureAwait(false);
            }
            catch
            {
                // Individual request failures already logged via Trace.WriteLine; the drain
                // is best-effort and shouldn't surface them.
            }

            var remaining = totalWindow - clock.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                return;
            }

            try
            {
                await Task.Delay(stableFor < remaining ? stableFor : remaining, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            var afterCount = Volatile.Read(ref _diagnostics.TotalRequests);
            if (afterCount == beforeCount && _inflightTasks.IsEmpty)
            {
                if (++consecutiveIdleWindows >= 2)
                {
                    return;
                }
            }
            else
            {
                consecutiveIdleWindows = 0;
            }
        }
    }

    /// <summary>
    /// Default drain window applied by <see cref="DrainAsync"/> when no value is supplied.
    /// Honours the <c>TUNIT_OTLP_DRAIN_MS</c> environment variable, captured once at type
    /// init so repeated reads don't pay env-var lookup cost.
    /// </summary>
    public static TimeSpan DefaultDrainWindow { get; } = ResolveDefaultDrainWindow();

    private static TimeSpan ResolveDefaultDrainWindow()
    {
        var raw = Environment.GetEnvironmentVariable(DrainWindowEnvVar);
        if (!string.IsNullOrEmpty(raw)
            && int.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var ms)
            && ms >= 0)
        {
            return TimeSpan.FromMilliseconds(ms);
        }

        return TimeSpan.FromSeconds(2);
    }

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

            var path = request.Url?.AbsolutePath ?? "";

            if (LooksLikeGrpc(request.ContentType, path))
            {
                // HttpListener is HTTP/1.1-only — most gRPC clients won't even reach us, but
                // h2c-fallback or grpc-web requests can. Reject explicitly with 415 so the
                // SUT exporter logs an error instead of silently retrying, and surface the
                // mismatch in the diagnostic dump. Fix is to set
                // OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf (already injected by
                // TUnit.Aspire on every ProjectResource).
                Interlocked.Increment(ref _diagnostics.GrpcRejected);
                Interlocked.Increment(ref _diagnostics.TotalRequests);
                // Only record the path under "unknown" when path itself triggered detection;
                // otherwise we'd add /v1/traces to the unknown-paths map on every gRPC-by-
                // content-type rejection, which is actively misleading in the dump.
                if (path.StartsWith("/opentelemetry.proto.collector.", StringComparison.Ordinal))
                {
                    _diagnostics.RecordUnknownPath(path);
                }

                response.StatusCode = 415;
                response.ContentType = "text/plain";
                ReadOnlySpan<byte> msg = "TUnit OTLP receiver does not support gRPC. Set OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf.\n"u8;
                response.ContentLength64 = msg.Length;
                response.OutputStream.Write(msg);
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

            if (path == "/v1/logs")
            {
                Interlocked.Increment(ref _diagnostics.LogsRequests);
                ProcessLogs(body);
            }
            else if (path == "/v1/traces")
            {
                Interlocked.Increment(ref _diagnostics.TracesRequests);
                ProcessTraces(body, _diagnostics);
            }
            else if (path == "/v1/metrics")
            {
                // Standard OTLP/HTTP signal — accepted and forwarded upstream, but we don't
                // render metrics in the report so there's nothing to parse.
                Interlocked.Increment(ref _diagnostics.MetricsRequests);
            }
            else
            {
                Interlocked.Increment(ref _diagnostics.OtherRequests);
                _diagnostics.RecordUnknownPath(path);
            }

            var upstream = Volatile.Read(ref _upstreamEndpoint);
            if (upstream is not null)
            {
                TrackTask(ForwardAsync(upstream, path, body, request.ContentType));
            }

            Interlocked.Increment(ref _diagnostics.TotalRequests);

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

    private static void ProcessTraces(byte[] body, OtlpReceiverDiagnostics diag)
    {
        var sink = ExternalSpanSink.Current;
        if (sink is null)
        {
            Interlocked.Increment(ref diag.TracesNoSink);
            return;
        }

        IReadOnlyList<OtlpSpanRecord> spans;
        try
        {
            spans = OtlpTraceParser.Parse(body);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref diag.TracesParseFailures);
            Trace.WriteLine($"[TUnit.OpenTelemetry] Failed to parse /v1/traces body: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        Interlocked.Add(ref diag.TracesSpansParsed, spans.Count);

        // Dedupe registration attempts per batch — without this, every span in an
        // already-known trace would bump the diagnostic counter, making
        // "50 spans, all in a known trace" indistinguishable from "50 spans, 50 misses".
        var registrationAttempted = spans.Count > 1
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : null;

        foreach (var span in spans)
        {
            if (registrationAttempted is null || registrationAttempted.Add(span.TraceId))
            {
                RegisterDerivedTrace(span, diag);
            }

            sink(ToSpanData(span));
        }
    }

    private static void RegisterDerivedTrace(OtlpSpanRecord span, OtlpReceiverDiagnostics diag)
    {
        if (TraceRegistry.IsRegistered(span.TraceId))
        {
            Interlocked.Increment(ref diag.TracesAlreadyRegistered);
            return;
        }

        foreach (var link in span.Links)
        {
            if (TraceRegistry.TryRegisterDerivedTrace(span.TraceId, link.TraceId))
            {
                Interlocked.Increment(ref diag.TracesRegisteredViaLink);
                return;
            }
        }

        Interlocked.Increment(ref diag.TracesNoMatch);
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

    private void ProcessLogs(byte[] body)
    {
        List<OtlpLogRecord> records;
        try
        {
            // The callback records the source service for *every* incoming log record — including
            // ones the parser drops for lacking a trace id — so the Aspire missing-telemetry hint
            // sees that OTLP logging reached us even from a resource that only emits untraced logs.
            records = OtlpLogParser.Parse(body, _recordSeenLogService);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _diagnostics.LogsParseFailures);
            Trace.WriteLine($"[TUnit.OpenTelemetry] Failed to parse /v1/logs body: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        Interlocked.Add(ref _diagnostics.LogsRecordsParsed, records.Count);

        foreach (var record in records)
        {
            if (string.IsNullOrEmpty(record.TraceId))
            {
                Interlocked.Increment(ref _diagnostics.LogsRecordsNoTraceId);
                continue;
            }

            // Look up the test context for this trace
            var contextId = TraceRegistry.GetContextId(record.TraceId);
            if (contextId is null)
            {
                Interlocked.Increment(ref _diagnostics.LogsRecordsTraceNotRegistered);
                continue;
            }

            var testContext = TestContext.GetById(contextId);
            if (testContext is null)
            {
                Interlocked.Increment(ref _diagnostics.LogsRecordsTestContextMissing);
                continue;
            }

            // Format and write to the test's output
            var severity = OtlpLogParser.FormatSeverity(record.SeverityNumber, record.SeverityText);
            var prefix = string.IsNullOrEmpty(record.ResourceName)
                ? ""
                : $"[{record.ResourceName}] ";

            testContext.Output.WriteLine($"{prefix}[{severity}] {record.Body}");

            // When the SUT logged an exception, the OTLP body is usually just the message
            // template — the actual stack trace lives in the exception.* attributes. Surface it
            // so a failing test shows *why* it failed, not only that an error was logged.
            if (record.HasException)
            {
                testContext.Output.WriteLine($"{prefix}{record.FormatException()}");
            }

            Interlocked.Increment(ref _diagnostics.LogsRecordsRouted);
        }
    }

    private async Task ForwardAsync(string upstream, string path, byte[] body, string? contentType)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{upstream}{path}");
            request.Content = new ByteArrayContent(body);
            if (contentType is not null)
            {
                request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
            }

            var headers = Volatile.Read(ref _upstreamHeaders);
            if (headers is not null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            using var response = await s_forwardingClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                Interlocked.Increment(ref _diagnostics.UpstreamForwardSuccess);
            }
            else
            {
                Interlocked.Increment(ref _diagnostics.UpstreamForwardFailures);
                // Strip CR/LF from ReasonPhrase before logging — it's an HTTP response header
                // value and could otherwise inject newlines into the trace output, breaking
                // log parsing tools (CodeQL log-injection rule).
                var safePhrase = response.ReasonPhrase?.Replace('\r', ' ').Replace('\n', ' ');
                Trace.WriteLine($"[TUnit.OpenTelemetry] Upstream {path} returned {(int)response.StatusCode} {safePhrase}.");
            }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _diagnostics.UpstreamForwardFailures);
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

        // Distinguish "SUT didn't export anything" from "SUT exported but TUnit dropped it
        // on the floor" — opt-in via TUNIT_OTLP_DEBUG=1 so production runs stay quiet.
        if (IsDiagnosticsDumpEnabled())
        {
            try
            {
                Console.Error.WriteLine(_diagnostics.FormatSummary(Port));
            }
            catch
            {
                // Best effort — Console may be redirected/closed in some hosts
            }
        }

        _cts.Dispose();
    }

    /// <summary>
    /// Returns <c>true</c> if a request looks like gRPC — either by content-type
    /// (<c>application/grpc</c>, <c>application/grpc-web</c>, <c>application/grpc+proto</c>)
    /// or by an OTLP gRPC service path (<c>/opentelemetry.proto.collector.{trace,logs,metrics}.v1.*Service/Export</c>).
    /// </summary>
    private static bool LooksLikeGrpc(string? contentType, string path)
    {
        if (contentType is not null && contentType.StartsWith("application/grpc", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // OTLP gRPC service paths all live under /opentelemetry.proto.collector. We don't
        // need to match the full service+method to recognise the misroute.
        return path.StartsWith("/opentelemetry.proto.collector.", StringComparison.Ordinal);
    }

    private static bool IsDiagnosticsDumpEnabled()
    {
        var value = Environment.GetEnvironmentVariable(DiagnosticsDumpEnvVar);
        return !string.IsNullOrEmpty(value)
            && !string.Equals(value, "0", StringComparison.Ordinal)
            && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Writes a one-line-per-counter summary of receiver activity. Intended for the
    /// diagnostic dump path — call manually from a test to inspect counters.
    /// </summary>
    internal void WriteDiagnosticsSummary(TextWriter writer)
    {
        writer.Write(_diagnostics.FormatSummary(Port));
    }

    /// <summary>
    /// Creates an <see cref="HttpListener"/> bound to a free port. Uses a retry loop to
    /// handle the TOCTOU window between discovering a free port and binding to it.
    /// </summary>
    private static (HttpListener Listener, int Port) CreateListener() => LoopbackHttpListenerFactory.Create();
}

/// <summary>
/// Counter snapshot for an <see cref="OtlpReceiver"/>. Helps diagnose silent drops by
/// distinguishing between requests that never arrived, parse failures, and spans/logs
/// that arrived but didn't match any registered test trace.
/// </summary>
internal sealed class OtlpReceiverDiagnostics
{
    private const int MaxTrackedUnknownPaths = 16;

    private readonly ConcurrentDictionary<string, int> _unknownPaths = new(StringComparer.Ordinal);

    // Counters are mutated via Interlocked from the listener task and read at session end.
    // No snapshot consistency across fields — readers may observe a partially-updated batch.
    // Acceptable for a best-effort diagnostic dump; do not use for live decision-making.
    // Fields are internal (rather than public) because ref access requires assembly-internal
    // visibility at most, and OtlpReceiver lives in the same assembly.
    internal int TotalRequests;
    internal int LogsRequests;
    internal int TracesRequests;
    internal int MetricsRequests;
    internal int OtherRequests;
    internal int GrpcRejected;

    internal int LogsParseFailures;
    internal int LogsRecordsParsed;
    internal int LogsRecordsNoTraceId;
    internal int LogsRecordsTraceNotRegistered;
    internal int LogsRecordsTestContextMissing;
    internal int LogsRecordsRouted;

    internal int TracesNoSink;
    internal int TracesParseFailures;
    internal int TracesSpansParsed;
    // Per-trace (deduped per batch): how many distinct traces fell into each bucket
    // when ProcessTraces attempted registration.
    internal int TracesAlreadyRegistered;
    internal int TracesRegisteredViaLink;
    internal int TracesNoMatch;

    // Upstream forwarding (Aspire dashboard etc.) — distinguish "didn't try" from
    // "tried and the dashboard rejected" so users can tell whether the proxy step
    // is actually reaching the dashboard.
    internal int UpstreamForwardSuccess;
    internal int UpstreamForwardFailures;

    /// <summary>
    /// Records a request path that didn't match any known OTLP signal endpoint. Caps the
    /// distinct-path set at <see cref="MaxTrackedUnknownPaths"/> to avoid unbounded growth
    /// if a misbehaving client cycles through generated URLs.
    /// </summary>
    public void RecordUnknownPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            path = "(empty)";
        }

        // Soft cap: ContainsKey/Count are checked outside the dictionary lock, so concurrent
        // callers may push a few entries past MaxTrackedUnknownPaths under a burst. Acceptable
        // for a diagnostic-only path — exact enforcement isn't worth the contention.
        if (_unknownPaths.ContainsKey(path) || _unknownPaths.Count < MaxTrackedUnknownPaths)
        {
            _unknownPaths.AddOrUpdate(path, 1, static (_, c) => c + 1);
        }
    }

    public string FormatSummary(int port)
    {
        // Keep the layout grep-friendly so users can quickly paste a snippet into an issue.
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[TUnit.OpenTelemetry] OtlpReceiver diagnostics summary (port {port}):");
        sb.AppendLine($"  requests.total                       = {TotalRequests}");
        sb.AppendLine($"  requests.v1_logs                     = {LogsRequests}");
        sb.AppendLine($"  requests.v1_traces                   = {TracesRequests}");
        sb.AppendLine($"  requests.v1_metrics                  = {MetricsRequests}");
        sb.AppendLine($"  requests.grpc_rejected               = {GrpcRejected}");
        sb.AppendLine($"  requests.other_path                  = {OtherRequests}");
        foreach (var entry in _unknownPaths)
        {
            sb.AppendLine($"    other_path[{entry.Key}] = {entry.Value}");
        }

        sb.AppendLine($"  logs.parse_failures                  = {LogsParseFailures}");
        sb.AppendLine($"  logs.records_parsed                  = {LogsRecordsParsed}");
        sb.AppendLine($"  logs.records_no_trace_id             = {LogsRecordsNoTraceId}");
        sb.AppendLine($"  logs.records_trace_not_registered    = {LogsRecordsTraceNotRegistered}");
        sb.AppendLine($"  logs.records_test_context_missing    = {LogsRecordsTestContextMissing}");
        sb.AppendLine($"  logs.records_routed_to_test          = {LogsRecordsRouted}");
        sb.AppendLine($"  traces.no_sink_registered            = {TracesNoSink}");
        sb.AppendLine($"  traces.parse_failures                = {TracesParseFailures}");
        sb.AppendLine($"  traces.spans_parsed                  = {TracesSpansParsed}");
        sb.AppendLine($"  traces.unique_already_registered     = {TracesAlreadyRegistered}");
        sb.AppendLine($"  traces.unique_registered_via_link    = {TracesRegisteredViaLink}");
        sb.AppendLine($"  traces.unique_no_match               = {TracesNoMatch}");
        sb.AppendLine($"  upstream.forward_success             = {UpstreamForwardSuccess}");
        sb.AppendLine($"  upstream.forward_failures            = {UpstreamForwardFailures}");
        return sb.ToString();
    }
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

    internal static int FindFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
