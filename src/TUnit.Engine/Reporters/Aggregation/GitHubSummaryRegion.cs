using System.IO;
using System.Text;

namespace TUnit.Engine.Reporters.Aggregation;

/// <summary>
/// Maintains a single marked region inside a GitHub step summary file. The
/// <c>GITHUB_STEP_SUMMARY</c> file is per-step and freely rewritable until the step ends
/// (the runner reads it once, at step completion), so every finishing sibling process can
/// replace the region with a fresher aggregate — the last one to finish leaves the
/// complete summary, and no process needs to know whether it is last.
///
/// Safety: content written by anything else (user <c>echo &gt;&gt;</c> lines, other tools)
/// must never be lost. Only the region strictly between TUnit's own invisible HTML-comment
/// markers is ever replaced, the block is matched conservatively (see <see cref="Splice"/>),
/// and the rewrite goes through a temp file + atomic replace so a killed process cannot
/// leave a truncated summary.
/// </summary>
internal static class GitHubSummaryRegion
{
    internal const string StartMarker = "<!-- tunit:aggregated-summary:start -->";
    internal const string EndMarker = "<!-- tunit:aggregated-summary:end -->";

    /// <summary>
    /// GitHub truncates step summaries above 1 MiB. Lives here (rather than only in
    /// EngineDefaults, which isn't source-linked into the tool) so the engine and
    /// <c>tunit-report</c> enforce the same cap.
    /// </summary>
    internal const long MaxFileSizeInBytes = 1024 * 1024;

    /// <summary>
    /// Replaces the marked region in <paramref name="summaryFilePath"/> with
    /// <paramref name="content"/>, appending a new marked region when none exists.
    /// Returns false when the file is missing or would exceed
    /// <paramref name="maxFileSizeInBytes"/>. Callers hold the aggregation lock, which
    /// serialises all TUnit writers.
    /// </summary>
    internal static bool ReplaceOrAppend(string summaryFilePath, string content, long maxFileSizeInBytes = MaxFileSizeInBytes)
    {
        if (!File.Exists(summaryFilePath))
        {
            return false;
        }

        var existing = File.ReadAllText(summaryFilePath, Encoding.UTF8);
        var updated = Splice(existing, content);

        if (Encoding.UTF8.GetByteCount(updated) > maxFileSizeInBytes)
        {
            // Don't leave a previously-written (now stale) block standing — it would
            // silently misreport which suites ran. Swap the region for a short notice,
            // which also frees whatever space the old block occupied.
            updated = Splice(existing, OverflowNotice);
            if (Encoding.UTF8.GetByteCount(updated) > maxFileSizeInBytes)
            {
                Console.WriteLine("Skipping GitHub step summary update: the file is already at GitHub's 1MB size limit.");
                return false;
            }

            Console.WriteLine("The aggregated summary would exceed GitHub's 1MB step summary limit; wrote an overflow notice instead.");
            AtomicFile.WriteAllText(summaryFilePath, updated);
            return false;
        }

        AtomicFile.WriteAllText(summaryFilePath, updated);
        return true;
    }

    internal const string OverflowNotice =
        "> ⚠️ The TUnit aggregated summary was omitted because it would exceed GitHub's 1 MiB step-summary limit. See the merged HTML report for full results.";

    /// <summary>
    /// Splices <paramref name="content"/> into <paramref name="existing"/> as a marked block.
    /// The block to replace is matched conservatively: find the first end marker, then the
    /// last start marker before it — the span between them is guaranteed to be TUnit's own
    /// block, because foreign content is only ever appended after our end marker, never
    /// inside the pair. Any unpaired (torn) marker is left in place rather than risking a
    /// splice that swallows someone else's content; the fresh block is appended instead.
    /// </summary>
    internal static string Splice(string existing, string content)
    {
        var block = $"{StartMarker}\n{content}\n{EndMarker}\n";

        var end = existing.IndexOf(EndMarker, StringComparison.Ordinal);
        var start = end >= 0 ? existing.LastIndexOf(StartMarker, end, StringComparison.Ordinal) : -1;

        if (end < 0 || start < 0)
        {
            // No complete block yet (first writer, or a torn/foreign fragment): append.
            return existing.Length == 0 || existing.EndsWith("\n", StringComparison.Ordinal)
                ? existing + block
                : existing + "\n" + block;
        }

        var afterEnd = end + EndMarker.Length;
        // Swallow the newline our own block writes after the end marker.
        if (afterEnd < existing.Length && existing[afterEnd] == '\n')
        {
            afterEnd++;
        }

        return existing.Substring(0, start) + block + existing.Substring(afterEnd);
    }
}
