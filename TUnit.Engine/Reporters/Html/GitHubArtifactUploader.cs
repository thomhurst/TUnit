using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TUnit.Engine.Reporters.Html;

internal static class GitHubArtifactUploader
{
    private const int MaxRetries = 5;
    private const int BaseRetryMs = 3000;
    private const double RetryMultiplier = 1.5;

    private static readonly HashSet<int> RetryableStatusCodes = [429, 500, 502, 503, 504];
    private static readonly HttpClient SharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    internal static async Task<string?> UploadAsync(
        string filePath,
        string runtimeToken,
        string resultsUrl,
        CancellationToken cancellationToken)
    {
        var (workflowRunBackendId, workflowJobRunBackendId) = ExtractBackendIds(runtimeToken);
        if (workflowRunBackendId is null || workflowJobRunBackendId is null)
        {
            Console.WriteLine("Warning: Could not extract backend IDs from ACTIONS_RUNTIME_TOKEN");
            return null;
        }

        var origin = new Uri(resultsUrl).GetLeftPart(UriPartial.Authority);
        var fileName = Path.GetFileName(filePath);

        // Step 1: CreateArtifact
        var createUrl = $"{origin}/twirp/github.actions.results.api.v1.ArtifactService/CreateArtifact";
        var createBody = BuildCreateArtifactJson(workflowRunBackendId, workflowJobRunBackendId, fileName);

        var signedUploadUrl = await RetryAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, createUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", runtimeToken);
            request.Content = new StringContent(createBody, Encoding.UTF8, "application/json");

            var response = await SharedHttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrLogAsync(response, "CreateArtifact", cancellationToken);
            var json = await response.Content.ReadAsStringAsync(
#if NET
                cancellationToken
#endif
            );
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("signedUploadUrl").GetString()!;
        }, cancellationToken);

        if (signedUploadUrl is null)
        {
            Console.WriteLine("Warning: CreateArtifact failed — could not obtain signed upload URL");
            return null;
        }

        // Step 2: Upload blob + compute SHA256
#if NET
        var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var sha256Hash = Convert.ToHexStringLower(SHA256.HashData(fileBytes));
#else
        var fileBytes = File.ReadAllBytes(filePath);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(fileBytes);
        var sha256Hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
#endif

        await RetryAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, signedUploadUrl);
            request.Content = new ByteArrayContent(fileBytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            request.Headers.Add("x-ms-blob-type", "BlockBlob");

            var response = await SharedHttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrLogAsync(response, "BlobUpload", cancellationToken);
            return true;
        }, cancellationToken);

        // Step 3: FinalizeArtifact
        var finalizeUrl = $"{origin}/twirp/github.actions.results.api.v1.ArtifactService/FinalizeArtifact";
        var finalizeBody = BuildFinalizeArtifactJson(workflowRunBackendId, workflowJobRunBackendId, fileName, fileBytes.Length, sha256Hash);

        var artifactId = await RetryAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, finalizeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", runtimeToken);
            request.Content = new StringContent(finalizeBody, Encoding.UTF8, "application/json");

            var response = await SharedHttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessOrLogAsync(response, "FinalizeArtifact", cancellationToken);
            var json = await response.Content.ReadAsStringAsync(
#if NET
                cancellationToken
#endif
            );
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("artifactId").GetString();
        }, cancellationToken);

        return artifactId;
    }

    private static string BuildCreateArtifactJson(string runId, string jobId, string fileName)
    {
        using var ms = new MemoryStream();
        using var w = new Utf8JsonWriter(ms);
        w.WriteStartObject();
        w.WriteString("workflowRunBackendId", runId);
        w.WriteString("workflowJobRunBackendId", jobId);
        w.WriteString("name", fileName);
        w.WriteNumber("version", 7);
        w.WriteStartObject("mimeType");
        w.WriteString("value", "text/html");
        w.WriteEndObject();
        w.WriteEndObject();
        w.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static string BuildFinalizeArtifactJson(string runId, string jobId, string fileName, long size, string sha256Hash)
    {
        using var ms = new MemoryStream();
        using var w = new Utf8JsonWriter(ms);
        w.WriteStartObject();
        w.WriteString("workflowRunBackendId", runId);
        w.WriteString("workflowJobRunBackendId", jobId);
        w.WriteString("name", fileName);
        w.WriteString("size", size.ToString());
        w.WriteStartObject("hash");
        w.WriteString("value", $"sha256:{sha256Hash}");
        w.WriteEndObject();
        w.WriteEndObject();
        w.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static (string? RunId, string? JobId) ExtractBackendIds(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2)
            {
                return (null, null);
            }

            // Base64url decode the payload
            var payload = parts[1];
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2:
                    payload += "==";
                    break;
                case 3:
                    payload += "=";
                    break;
            }

            var bytes = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(bytes);
            var scp = doc.RootElement.GetProperty("scp").GetString();

            if (scp is null)
            {
                return (null, null);
            }

            // scp may be space-separated list of scopes; find "Actions.Results:{runId}:{jobId}"
            foreach (var scope in scp.Split(' '))
            {
                if (!scope.StartsWith("Actions.Results:", StringComparison.Ordinal))
                {
                    continue;
                }

                var colonParts = scope.Split(':');
                if (colonParts.Length >= 3)
                {
                    return (colonParts[1], colonParts[2]);
                }
            }

            return (null, null);
        }
        catch (Exception)
        {
            return (null, null);
        }
    }

    private static async Task<T?> RetryAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException ex) when (IsRetryable(ex))
            {
                if (attempt == MaxRetries - 1)
                {
                    Console.WriteLine($"Warning: GitHub artifact upload step failed after {MaxRetries} attempts");
                    return default;
                }

                var delay = (int)(BaseRetryMs * Math.Pow(RetryMultiplier, attempt));
                var jitter = Random.Shared.Next(0, 500);
                await Task.Delay(delay + jitter, cancellationToken);
            }
        }

        return default;
    }

    private static async Task EnsureSuccessOrLogAsync(HttpResponseMessage response, string step, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(
#if NET
            cancellationToken
#endif
        );

        Console.WriteLine($"Warning: {step} returned {(int)response.StatusCode}: {body}");
        response.EnsureSuccessStatusCode();
    }

    private static bool IsRetryable(HttpRequestException ex)
    {
#if NET
        var statusCode = (int?)ex.StatusCode;
        return statusCode is not null && RetryableStatusCodes.Contains(statusCode.Value);
#else
        // On netstandard2.0, HttpRequestException doesn't have StatusCode — retry on any failure
        return true;
#endif
    }
}
