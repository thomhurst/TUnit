using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace TUnit.Aspire.Tests.Helpers;

internal sealed class OtlpTraceCaptureServer : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<CapturedRequest> _requests = new();
    private readonly object _waitersLock = new();
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
        if (_requests.FirstOrDefault(request => request.Path == path) is { } existing)
        {
            return existing;
        }

        var tcs = new TaskCompletionSource<CapturedRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiter = new PendingWait(path, tcs);

        lock (_waitersLock)
        {
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
            lock (_waitersLock)
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
            _requests.Enqueue(captured);
            SignalWaiters(captured);

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

    private void SignalWaiters(CapturedRequest captured)
    {
        PendingWait[] snapshot;
        lock (_waitersLock)
        {
            snapshot = _waiters.ToArray();
        }

        foreach (var waiter in snapshot)
        {
            if (waiter.Path == captured.Path)
            {
                waiter.Completion.TrySetResult(captured);
            }
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
