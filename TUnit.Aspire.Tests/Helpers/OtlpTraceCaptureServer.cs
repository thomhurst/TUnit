using System.Diagnostics;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace TUnit.Aspire.Tests.Helpers;

internal sealed class OtlpTraceCaptureServer : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentQueue<CapturedRequest> _requests = new();
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
        var sw = Stopwatch.StartNew();

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (_requests.FirstOrDefault(request => request.Path == path) is { } request)
            {
                return request;
            }

            await Task.Delay(50);
        }

        var receivedPaths = string.Join(", ", _requests.Select(static request => request.Path));
        throw new TimeoutException(
            $"Timed out waiting for OTLP request to '{path}'. Received: [{receivedPaths}]");
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

            _requests.Enqueue(new CapturedRequest(
                context.Request.Url?.AbsolutePath ?? string.Empty,
                ms.ToArray()));

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

    private static (HttpListener Listener, int Port) CreateListener()
    {
        using var tcpListener = new TcpListener(IPAddress.Loopback, 0);
        tcpListener.Start();
        var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
        tcpListener.Stop();

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        return (listener, port);
    }
}

internal sealed record CapturedRequest(string Path, byte[] Body);
