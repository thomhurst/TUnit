using System.Net;
using System.Net.Sockets;

namespace TUnit.Aspire.Tests.Helpers;

internal sealed class OtlpTraceCaptureServer : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    // Single lock guards both _requests and _waiters so that ProcessRequestAsync's
    // enqueue-and-signal happens atomically with WaitForRequestAsync's check-and-register.
    // Without this, a request that arrives between the check and registration would be
    // silently dropped (signalled to an empty waiter list).
    private readonly object _stateLock = new();
    private readonly List<CapturedRequest> _requests = new();
    private readonly List<PendingWait> _waiters = new();
    private Task? _listenTask;

    public int Port { get; }

    public OtlpTraceCaptureServer()
    {
        (_listener, Port) = CreateListener();
    }

    public void Start()
    {
        _listenTask = Task.Run(ListenLoopAsync);
    }

    public async Task<CapturedRequest> WaitForRequestAsync(string path, int timeoutMs = 5000)
    {
        var tcs = new TaskCompletionSource<CapturedRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiter = new PendingWait(path, tcs);

        lock (_stateLock)
        {
            foreach (var existing in _requests)
            {
                if (existing.Path == path)
                {
                    return existing;
                }
            }

            _waiters.Add(waiter);
        }

        using var cts = new CancellationTokenSource(timeoutMs);
        using var registration = cts.Token.Register(static state =>
        {
            var w = (PendingWait)state!;
            w.Completion.TrySetException(new TimeoutException(
                $"Timed out waiting for OTLP request to '{w.Path}'."));
        }, waiter);

        try
        {
            return await tcs.Task;
        }
        finally
        {
            lock (_stateLock)
            {
                _waiters.Remove(waiter);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();

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
                await _listenTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _cts.Dispose();
    }

    private async Task ListenLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException) when (_cts.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => ProcessRequestAsync(context));
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        try
        {
            using var ms = new MemoryStream();
            await context.Request.InputStream.CopyToAsync(ms, _cts.Token);

            var captured = new CapturedRequest(
                context.Request.Url?.AbsolutePath ?? string.Empty,
                ms.ToArray());
            EnqueueAndSignal(captured);

            context.Response.StatusCode = 200;
            context.Response.ContentLength64 = 0;
            context.Response.Close();
        }
        catch
        {
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

    private void EnqueueAndSignal(CapturedRequest captured)
    {
        PendingWait[] matched;
        lock (_stateLock)
        {
            _requests.Add(captured);
            matched = _waiters
                .Where(w => w.Path == captured.Path)
                .ToArray();
        }

        // Signal outside the lock — TrySetResult can run continuations synchronously
        // when the awaiter has not yet suspended, which would otherwise re-enter our lock.
        foreach (var waiter in matched)
        {
            waiter.Completion.TrySetResult(captured);
        }
    }

    // HttpListener has no port-0 bind; probe TcpListener for a free port and retry on the
    // unavoidable TOCTOU race against another process binding it before HttpListener starts.
    private static (HttpListener Listener, int Port) CreateListener()
    {
        const int maxAttempts = 10;
        HttpListenerException? lastError = null;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            int port;
            using (var tcpListener = new TcpListener(IPAddress.Loopback, 0))
            {
                tcpListener.Start();
                port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
                tcpListener.Stop();
            }

            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            try
            {
                listener.Start();
                return (listener, port);
            }
            catch (HttpListenerException ex)
            {
                lastError = ex;
                ((IDisposable)listener).Dispose();
            }
        }

        throw new InvalidOperationException(
            $"Failed to bind a loopback HttpListener after {maxAttempts} attempts.", lastError);
    }

    private sealed record PendingWait(string Path, TaskCompletionSource<CapturedRequest> Completion);
}

internal sealed record CapturedRequest(string Path, byte[] Body);
