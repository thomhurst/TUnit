using System.Net;
using System.Text;
using TUnit.Engine.Reporters.Html;

namespace TUnit.Engine.Reporters.Aggregation;

/// <summary>
/// Renders one Markdown summary block covering every suite persisted so far — used by the
/// cooperative in-engine merge (each finishing process rewrites the block; the last one
/// leaves the complete aggregate) and by the <c>tunit-report</c> tool's
/// <c>--github-summary</c>. Works purely on sidecar <see cref="ReportData"/> so it needs
/// no live test-node state.
/// </summary>
internal static class AggregatedSummaryWriter
{
    // Keeps the step summary within GitHub's 1 MB limit; GitHubReporter's per-suite
    // rendering references this same cap so both views truncate identically.
    internal const int MaxTestsPerGroup = 50;

    internal static string Render(
        IReadOnlyList<ReportData> suites,
        bool collapsible = true,
        string? serverUrl = null,
        string? mergedReportHint = null)
    {
        var labels = ReportDataMerger.BuildSuiteLabels(suites);

        var totals = new ReportSummary();
        // Bounds parsed once per suite; both the whole-run duration and each table row
        // derive from them, so no timestamp is parsed twice.
        var bounds = new (long StartMs, long EndMs)?[suites.Count];
        var earliest = long.MaxValue;
        var latest = long.MinValue;
        for (var i = 0; i < suites.Count; i++)
        {
            totals.Add(suites[i].Summary);
            bounds[i] = ReportDataMerger.ComputeWallClockBounds(suites[i]);
            if (bounds[i] is { } b)
            {
                if (b.StartMs < earliest) earliest = b.StartMs;
                if (b.EndMs > latest) latest = b.EndMs;
            }
        }

        var hasFailures = totals.TotalUnsuccessful > 0;
        var statusEmoji = hasFailures ? "❌" : "✅";
        var passRate = totals.Total > 0 ? (double)totals.Passed / totals.Total * 100 : 0;
        var wallMs = earliest == long.MaxValue
            ? suites.Max(static s => s.TotalDurationMs)
            : latest - earliest;
        var suiteWord = suites.Count == 1 ? "suite" : "suites";

        var sb = new StringBuilder();
        sb.AppendLine($"### {statusEmoji} TUnit Test Results — {suites.Count} {suiteWord}");
        sb.AppendLine();
        sb.AppendLine($"**{totals.Total} tests** across **{suites.Count} {suiteWord}** in **{FormatDuration(wallMs)}** — **{passRate:F1}%** passed");
        sb.AppendLine();

        if (totals.Passed != totals.Total)
        {
            var segments = new List<string> { $"✅ {totals.Passed} passed" };
            if (totals.Failed > 0) segments.Add($"❌ {totals.Failed} failed");
            if (totals.Skipped > 0) segments.Add($"⏭️ {totals.Skipped} skipped");
            if (totals.TimedOut > 0) segments.Add($"⏱️ {totals.TimedOut} timed out");
            if (totals.Cancelled > 0) segments.Add($"🚫 {totals.Cancelled} cancelled");
            sb.AppendLine(string.Join(" · ", segments));
            sb.AppendLine();
        }

        AppendSuiteTable(sb, suites, labels, bounds);

        if (totals.Flaky > 0)
        {
            AppendFlakySection(sb, suites, labels);
        }

        if (hasFailures)
        {
            AppendFailuresByCause(sb, suites, labels, collapsible, serverUrl);
        }

        if (!string.IsNullOrEmpty(mergedReportHint))
        {
            sb.AppendLine();
            sb.AppendLine($"> {mergedReportHint}");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        return sb.ToString();
    }

    private static void AppendSuiteTable(
        StringBuilder sb, IReadOnlyList<ReportData> suites, string[] labels, (long StartMs, long EndMs)?[] bounds)
    {
        var anyReportLink = suites.Any(static s => !string.IsNullOrEmpty(s.ArtifactUrl));

        sb.AppendLine(anyReportLink
            ? "| Suite | Tests | ✅ | ❌ | ⏭️ | Duration | Report |"
            : "| Suite | Tests | ✅ | ❌ | ⏭️ | Duration |");
        sb.AppendLine(anyReportLink
            ? "| --- | ---: | ---: | ---: | ---: | ---: | --- |"
            : "| --- | ---: | ---: | ---: | ---: | ---: |");

        for (var i = 0; i < suites.Count; i++)
        {
            var s = suites[i].Summary;
            var emoji = s.TotalUnsuccessful > 0 ? "❌" : "✅";
            // Wall clock derived from the suite's own test timestamps; TotalDurationMs is
            // only the fallback — it can come from a session span that measures differently.
            var duration = bounds[i] is { } b ? b.EndMs - b.StartMs : suites[i].TotalDurationMs;
            var row = $"| {emoji} `{labels[i]}` | {s.Total} | {s.Passed} | {s.TotalUnsuccessful} | {s.Skipped} | {FormatDuration(duration)} |";
            if (anyReportLink)
            {
                row += string.IsNullOrEmpty(suites[i].ArtifactUrl) ? " |" : $" [View]({suites[i].ArtifactUrl}) |";
            }
            sb.AppendLine(row);
        }
        sb.AppendLine();
    }

    private static void AppendFlakySection(StringBuilder sb, IReadOnlyList<ReportData> suites, string[] labels)
    {
        var flakyTests = new List<(string Suite, string Name, int Attempts, double DurationMs)>();
        for (var i = 0; i < suites.Count; i++)
        {
            foreach (var group in suites[i].Groups)
            {
                foreach (var test in group.Tests)
                {
                    if (test.RetryAttempt > 0 && test.Status == "passed")
                    {
                        flakyTests.Add((labels[i], $"{test.ClassName}.{test.DisplayName}", test.RetryAttempt + 1, test.DurationMs));
                    }
                }
            }
        }

        if (flakyTests.Count == 0)
        {
            return;
        }

        sb.AppendLine($"> **⚠️ {flakyTests.Count} flaky {(flakyTests.Count == 1 ? "test" : "tests")}** passed after retry:");
        foreach (var (suite, name, attempts, durationMs) in flakyTests)
        {
            sb.AppendLine($"> - `{name}` ({suite}) — {attempts} attempts ({FormatDuration(durationMs)})");
        }
        sb.AppendLine();
    }

    private static void AppendFailuresByCause(
        StringBuilder sb, IReadOnlyList<ReportData> suites, string[] labels, bool collapsible, string? serverUrl)
    {
        var failures = new List<(string Suite, ReportTestResult Test, ReportData Owner)>();
        for (var i = 0; i < suites.Count; i++)
        {
            foreach (var group in suites[i].Groups)
            {
                foreach (var test in group.Tests)
                {
                    if (test.Status is "failed" or "error" or "timedOut")
                    {
                        failures.Add((labels[i], test, suites[i]));
                    }
                }
            }
        }

        if (failures.Count == 0)
        {
            return;
        }

        var grouped = failures
            .GroupBy(static f => ExceptionLabel(f.Test))
            .OrderByDescending(static g => g.Count())
            .ToArray();

        var diagParts = grouped.Take(3).Select(static g =>
        {
            var topSuite = g.GroupBy(static x => x.Suite).OrderByDescending(static c => c.Count()).First();
            return $"{g.Count()} × `{g.Key}` in `{topSuite.Key}`";
        });
        sb.AppendLine($"> **Quick diagnosis:** {string.Join(", ", diagParts)}");
        sb.AppendLine();

        sb.AppendLine("#### Failures by Cause");
        sb.AppendLine();

        foreach (var group in grouped)
        {
            var entries = group.ToList();
            var label = $"{group.Key} ({entries.Count} {(entries.Count == 1 ? "test" : "tests")})";

            if (collapsible)
            {
                sb.AppendLine("<details>");
                sb.AppendLine($"<summary>{label}</summary>");
            }
            else
            {
                sb.AppendLine($"**{label}**");
            }

            sb.AppendLine();
            sb.AppendLine("| Test | Suite | Duration |");
            sb.AppendLine("| --- | --- | --- |");

            var displayCount = Math.Min(entries.Count, MaxTestsPerGroup);
            for (var i = 0; i < displayCount; i++)
            {
                var (suite, test, owner) = entries[i];
                var sourcePart = BuildSourceLink(test, owner, serverUrl) is { } link ? $" {link}" : "";
                sb.AppendLine($"| `{test.ClassName}.{test.DisplayName}`{sourcePart} | `{suite}` | {FormatDuration(test.DurationMs)} |");
            }

            if (entries.Count > MaxTestsPerGroup)
            {
                sb.AppendLine($"| *...and {entries.Count - MaxTestsPerGroup} more* | | |");
            }

            var commonError = entries
                .Select(static e => e.Test.Exception?.Message)
                .Where(static m => !string.IsNullOrWhiteSpace(m))
                .GroupBy(static m => m)
                .OrderByDescending(static g => g.Count())
                .FirstOrDefault()
                ?.Key;

            if (commonError is not null)
            {
                sb.AppendLine();
                sb.AppendLine("**Common error:**");
                sb.AppendLine($"<pre>{WebUtility.HtmlEncode(Truncate(commonError, 1000))}</pre>");
            }

            if (collapsible)
            {
                sb.AppendLine();
                sb.AppendLine("</details>");
            }

            sb.AppendLine();
        }
    }

    private static string ExceptionLabel(ReportTestResult test)
    {
        if (test.Status == "timedOut")
        {
            return "Timeout";
        }

        var type = test.Exception?.Type;
        if (string.IsNullOrEmpty(type))
        {
            return "Unknown";
        }

        // Sidecars store the exception's FullName; the summary groups by short name,
        // matching the per-suite GitHub summary.
        var lastDot = type!.LastIndexOf('.');
        return lastDot >= 0 ? type.Substring(lastDot + 1) : type;
    }

    private static string? BuildSourceLink(ReportTestResult test, ReportData owner, string? serverUrl)
    {
        if (string.IsNullOrEmpty(serverUrl)
            || string.IsNullOrEmpty(owner.RepositorySlug)
            || string.IsNullOrEmpty(owner.CommitSha)
            || string.IsNullOrEmpty(test.SourceRelativePath)
            || test.LineNumber is not { } line)
        {
            return null;
        }

        var fileName = Path.GetFileName(test.SourceRelativePath!);
        return $"[{fileName}:{line}]({serverUrl!.TrimEnd('/')}/{owner.RepositorySlug}/blob/{owner.CommitSha}/{test.SourceRelativePath}#L{line})";
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value.Substring(0, maxLength) + "…";

    internal static string FormatDuration(double milliseconds) => milliseconds switch
    {
        < 1 => "< 1ms",
        < 1000 => $"{milliseconds:F0}ms",
        < 60_000 => $"{milliseconds / 1000:F1}s",
        < 3_600_000 => $"{(int)(milliseconds / 60_000)}m {(int)(milliseconds % 60_000 / 1000)}s",
        _ => $"{(int)(milliseconds / 3_600_000)}h {(int)(milliseconds % 3_600_000 / 60_000)}m",
    };
}
