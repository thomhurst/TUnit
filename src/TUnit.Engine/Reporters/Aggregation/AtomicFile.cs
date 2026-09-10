using System.IO;
using System.Text;

namespace TUnit.Engine.Reporters.Aggregation;

/// <summary>
/// Stage-to-temp-then-swap file writes for the aggregation pipeline: concurrent readers
/// never observe a torn file, and a process killed mid-write cannot leave a truncated
/// destination (which for the GitHub step summary would mean losing other tools' content,
/// not just ours). Single implementation shared by the sidecar, merged-report and
/// summary-region writers — and source-linked into TUnit.Reporting.Tool.
/// </summary>
internal static class AtomicFile
{
    internal static void WriteAllBytes(string path, byte[] bytes)
    {
        var tempPath = TempPathFor(path);
        File.WriteAllBytes(tempPath, bytes);
        if (!TrySwap(tempPath, path))
        {
            File.WriteAllBytes(path, bytes);
        }
    }

    internal static void WriteAllText(string path, string content)
    {
        var tempPath = TempPathFor(path);
        File.WriteAllText(tempPath, content, Encoding.UTF8);
        if (!TrySwap(tempPath, path))
        {
            File.WriteAllText(path, content, Encoding.UTF8);
        }
    }

    private static string TempPathFor(string path)
        => path + "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp";

    // False = the swap isn't possible on this filesystem (e.g. some network mounts);
    // callers fall back to an in-place write, accepting the small tear window.
    private static bool TrySwap(string tempPath, string path)
    {
        try
        {
#if NET
            File.Move(tempPath, path, overwrite: true);
#else
            // No overwriting Move downlevel; Replace is atomic but requires the
            // destination to exist. Never delete-then-move — a crash between the two
            // would lose the existing file's content entirely.
            if (File.Exists(path))
            {
                File.Replace(tempPath, path, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, path);
            }
#endif
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            try
            {
                File.Delete(tempPath);
            }
            catch (IOException)
            {
                // Best effort; a stray .tmp is harmless.
            }
            return false;
        }
    }
}
