using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using TUnit.Core;

namespace TUnit.Aspire.Telemetry;

/// <summary>
/// A lightweight OTLP/HTTP receiver that accepts telemetry from SUT processes
/// and correlates log records back to the originating test via TraceId.
/// </summary>
/// <remarks>
/// <para>
/// The receiver listens on a dynamic localhost port and accepts OTLP/HTTP protobuf
/// exports at <c>/v1/logs</c> and <c>/v1/traces</c>. It extracts TraceId from
/// incoming log records and looks up the corresponding test in
/// <see cref="TraceRegistry"/> to route logs to the correct test output.
/// </para>
/// <para>
/// Optionally forwards received telemetry to an upstream OTLP endpoint (e.g., the
/// Aspire dashboard) so both TUnit and the dashboard receive the data.
/// </para>
/// </remarks>
internal sealed class OtlpReceiver : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentBag<Task> _inflightRequests = [];
    private readonly HttpClient? _forwardingClient;
    private readonly string? _upstreamEndpoint;
    private Task? _listenTask;

    /// <summary>
    /// The port the receiver is listening on.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Creates a new OTLP receiver.
    /// </summary>
    /// <param name="upstreamEndpoint">
    /// Optional upstream OTLP endpoint to forward telemetry to (e.g., Aspire dashboard).
    /// If <c>null</c>, received telemetry is only used for correlation, not forwarded.
    /// </param>
    public OtlpReceiver(string? upstreamEndpoint = null)
    {
        _upstreamEndpoint = upstreamEndpoint?.TrimEnd('/');
        (_listener, Port) = CreateListener();

        if (_upstreamEndpoint is not null)
        {
            _forwardingClient = new HttpClient();
        }
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

            // Process each request without blocking the listen loop
            var task = Task.Run(() => ProcessRequestAsync(context));
            _inflightRequests.Add(task);
        }
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

            // Read the request body
            byte[] body;
            using (var ms = new MemoryStream())
            {
                await request.InputStream.CopyToAsync(ms).ConfigureAwait(false);
                body = ms.ToArray();
            }

            var path = request.Url?.AbsolutePath ?? "";

            if (path == "/v1/logs")
            {
                ProcessLogs(body);
            }

            // Forward to upstream if configured (fire-and-forget)
            if (_upstreamEndpoint is not null && _forwardingClient is not null)
            {
                _ = ForwardAsync(path, body, request.ContentType);
            }

            // Return 200 OK with empty protobuf response
            response.StatusCode = 200;
            response.ContentType = "application/x-protobuf";
            response.ContentLength64 = 0;
            response.Close();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[TUnit.Aspire] OTLP request processing failed: {ex.Message}");
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

    private static void ProcessLogs(byte[] body)
    {
        List<OtlpLogRecord> records;
        try
        {
            records = OtlpLogParser.Parse(body);
        }
        catch
        {
            // Malformed protobuf -- skip silently
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

    private async Task ForwardAsync(string path, byte[] body, string? contentType)
    {
        try
        {
            using var content = new ByteArrayContent(body);
            if (contentType is not null)
            {
                content.Headers.TryAddWithoutValidation("Content-Type", contentType);
            }

            await _forwardingClient!.PostAsync(
                $"{_upstreamEndpoint}{path}",
                content).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[TUnit.Aspire] OTLP forwarding to upstream failed: {ex.Message}");
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

        // Wait for any in-flight request processing to complete so we don't
        // access TraceRegistry or TestContext after they've been torn down.
        try
        {
            await Task.WhenAll(_inflightRequests).ConfigureAwait(false);
        }
        catch
        {
            // Best effort — individual failures already logged via Trace.WriteLine
        }

        _forwardingClient?.Dispose();
        _cts.Dispose();
    }

    /// <summary>
    /// Creates an <see cref="HttpListener"/> bound to a free port. Uses a retry loop to
    /// handle the TOCTOU window between discovering a free port and binding to it.
    /// </summary>
    private static (HttpListener Listener, int Port) CreateListener()
    {
        for (var attempt = 0; attempt < 10; attempt++)
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

        throw new InvalidOperationException("Could not bind OTLP listener after 10 attempts.");
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
