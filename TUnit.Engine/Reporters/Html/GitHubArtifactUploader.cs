using System.Net;
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

        // Step 1: CreateArtifact (deduplicate name on 409 conflict)
        var createUrl = $"{origin}/twirp/github.actions.results.api.v1.ArtifactService/CreateArtifact";
        string? signedUploadUrl = null;

        for (var nameAttempt = 0; nameAttempt < 3 && signedUploadUrl is null; nameAttempt++)
        {
            var artifactName = nameAttempt == 0
                ? fileName
                : $"{Path.GetFileNameWithoutExtension(fileName)}-{nameAttempt + 1}{Path.GetExtension(fileName)}";

            var createBody = BuildCreateArtifactJson(workflowRunBackendId, workflowJobRunBackendId, artifactName);

            signedUploadUrl = await RetryAsync(async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, createUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", runtimeToken);
                request.Content = new StringContent(createBody, Encoding.UTF8, "application/json");

                var response = await SharedHttpClient.SendAsync(request, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    // Artifact name already taken — outer loop will retry with a new name
                    return null;
                }

                await EnsureSuccessAsync(response, "CreateArtifact", cancellationToken);
                var json = await response.Content.ReadAsStringAsync(
#if NET
                    cancellationToken
#endif
                );
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.GetProperty("signed_upload_url").GetString();
            }, cancellationToken);
        }

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

        var uploadSucceeded = await RetryAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, signedUploadUrl);
            request.Content = new ByteArrayContent(fileBytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            request.Headers.Add("x-ms-blob-type", "BlockBlob");

            var response = await SharedHttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, "BlobUpload", cancellationToken);
            return true;
        }, cancellationToken);

        if (uploadSucceeded is not true)
        {
            Console.WriteLine("Warning: Blob upload failed — skipping artifact finalization");
            return null;
        }

        // Step 3: FinalizeArtifact
        var finalizeUrl = $"{origin}/twirp/github.actions.results.api.v1.ArtifactService/FinalizeArtifact";
        var finalizeBody = BuildFinalizeArtifactJson(workflowRunBackendId, workflowJobRunBackendId, fileName, fileBytes.Length, sha256Hash);

        var artifactId = await RetryAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, finalizeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", runtimeToken);
            request.Content = new StringContent(finalizeBody, Encoding.UTF8, "application/json");

            var response = await SharedHttpClient.SendAsync(request, cancellationToken);
            await EnsureSuccessAsync(response, "FinalizeArtifact", cancellationToken);
            var json = await response.Content.ReadAsStringAsync(
#if NET
                cancellationToken
#endif
            );
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("artifact_id").GetString();
        }, cancellationToken);

        return artifactId;
    }

    private static string BuildCreateArtifactJson(string runId, string jobId, string fileName)
    {
        using var ms = new MemoryStream();
        using var w = new Utf8JsonWriter(ms);
        w.WriteStartObject();
        w.WriteString("workflow_run_backend_id", runId);
        w.WriteString("workflow_job_run_backend_id", jobId);
        w.WriteString("name", fileName);
        w.WriteNumber("version", 7);
        w.WriteString("mime_type", "text/html");
        w.WriteEndObject();
        w.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static string BuildFinalizeArtifactJson(string runId, string jobId, string fileName, long size, string sha256Hash)
    {
        using var ms = new MemoryStream();
        using var w = new Utf8JsonWriter(ms);
        w.WriteStartObject();
        w.WriteString("workflow_run_backend_id", runId);
        w.WriteString("workflow_job_run_backend_id", jobId);
        w.WriteString("name", fileName);
        w.WriteString("size", size.ToString());
        w.WriteString("hash", $"sha256:{sha256Hash}");
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
                    Console.WriteLine($"Warning: GitHub artifact upload step failed after {MaxRetries} attempts: {ex.Message}");
                    return default;
                }

                var delay = (int)(BaseRetryMs * Math.Pow(RetryMultiplier, attempt));
                var jitter = Random.Shared.Next(0, 500);
                await Task.Delay(delay + jitter, cancellationToken);
            }
        }

        return default;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string step, CancellationToken cancellationToken)
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

        // Include response body in the exception message rather than logging per-attempt,
        // so only the final failure is surfaced by RetryAsync.
#if NET
        throw new HttpRequestException($"{step} returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
#else
        throw new HttpRequestException($"{step} returned {(int)response.StatusCode}: {body}");
#endif
    }

    private static bool IsRetryable(HttpRequestException ex)
    {
#if NET
        var statusCode = (int?)ex.StatusCode;
        return statusCode is not null && RetryableStatusCodes.Contains(statusCode.Value);
#else
        // On netstandard2.0, HttpRequestException doesn't have StatusCode.
        // Use message heuristic to avoid retrying non-retryable errors like 401/403.
        var msg = ex.Message;
        if (msg.Contains("401") || msg.Contains("403") ||
            msg.Contains("Unauthorized") || msg.Contains("Forbidden"))
        {
            return false;
        }

        return true;
#endif
    }
}
