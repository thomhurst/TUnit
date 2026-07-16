using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace TUnit.AspNetCore.Interception;

/// <summary>
/// Middleware that captures HTTP request/response exchanges for test assertions.
/// </summary>
public sealed class HttpExchangeCaptureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HttpExchangeCapture _capture;

    public HttpExchangeCaptureMiddleware(RequestDelegate next, HttpExchangeCapture capture)
    {
        _next = next;
        _capture = capture;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        // Capture request
        var capturedRequest = await CaptureRequestAsync(context.Request);

        // Buffer response body if capturing
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();

        if (_capture.CaptureResponseBody)
        {
            context.Response.Body = responseBodyStream;
        }

        try
        {
            await _next(context);
        }
        finally
        {
            // Ensure response body stream is restored even on exception
            if (_capture.CaptureResponseBody)
            {
                context.Response.Body = originalBodyStream;
            }
        }

        stopwatch.Stop();

        // Capture response
        string? responseBody = null;
        if (_capture.CaptureResponseBody)
        {
            responseBodyStream.Position = 0;
            responseBody = await ReadBodyAsync(responseBodyStream, _capture.MaxBodySize);

            // Copy back to original stream
            responseBodyStream.Position = 0;
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }

        var capturedResponse = CaptureResponse(context.Response, responseBody);

        var exchange = new CapturedHttpExchange
        {
            Timestamp = timestamp,
            Duration = stopwatch.Elapsed,
            Request = capturedRequest,
            Response = capturedResponse
        };

        _capture.Add(exchange);
    }

    private async Task<CapturedRequest> CaptureRequestAsync(HttpRequest request)
    {
        string? body = null;

        if (_capture.CaptureRequestBody && request.ContentLength > 0)
        {
            request.EnableBuffering();
            body = await ReadBodyAsync(request.Body, _capture.MaxBodySize);
            request.Body.Position = 0;
        }

        return new CapturedRequest
        {
            Method = request.Method,
            Path = request.Path.Value ?? "/",
            QueryString = request.QueryString.HasValue ? request.QueryString.Value?.TrimStart('?') : null,
            Headers = CaptureHeaders(request.Headers),
            ContentType = request.ContentType,
            ContentLength = request.ContentLength,
            Body = body
        };
    }

    private static CapturedResponse CaptureResponse(HttpResponse response, string? body)
    {
        return new CapturedResponse
        {
            StatusCode = (HttpStatusCode)response.StatusCode,
            Headers = CaptureHeaders(response.Headers),
            ContentType = response.ContentType,
            ContentLength = response.ContentLength,
            Body = body
        };
    }

    private static Dictionary<string, string?[]> CaptureHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string?[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers)
        {
            result[header.Key] = header.Value.ToArray();
        }

        return result;
    }

    private static async Task<string> ReadBodyAsync(Stream stream, int maxSize)
    {
        var bufferSize = Math.Min(maxSize, 81920); // 80KB chunks
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            var builder = new StringBuilder();
            int totalRead = 0;

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(bufferSize, maxSize - totalRead)))) > 0)
            {
                builder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                totalRead += bytesRead;

                if (totalRead >= maxSize)
                {
                    builder.Append("... [truncated]");
                    break;
                }
            }

            return builder.ToString();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
