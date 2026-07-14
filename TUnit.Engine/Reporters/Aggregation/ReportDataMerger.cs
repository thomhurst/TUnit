using System.Globalization;
using TUnit.Core;
using TUnit.Engine.Reporters.Html;

namespace TUnit.Engine.Reporters.Aggregation;

/// <summary>
/// Merges per-process <see cref="ReportData"/> sidecars (one per test suite / TFM / OS)
/// into a single <see cref="ReportData"/> that <c>HtmlReportGenerator</c> can render as
/// one combined report. Timing works across processes because test start times are
/// absolute wall-clock timestamps; the generator recomputes run bounds from all tests.
/// </summary>
internal static class ReportDataMerger
{
    internal static ReportData Merge(IReadOnlyList<ReportData> suites)
    {
        if (suites.Count == 0)
        {
            throw new ArgumentException("At least one suite is required", nameof(suites));
        }

        if (suites.Count == 1)
        {
            return suites[0];
        }

        // Deterministic output regardless of which process finished last.
        var ordered = suites
            .OrderBy(static s => s.AssemblyName, StringComparer.Ordinal)
            .ThenBy(static s => s.RuntimeVersion, StringComparer.Ordinal)
            .ThenBy(static s => s.OperatingSystem, StringComparer.Ordinal)
            .ToArray();

        var labels = BuildSuiteLabels(ordered);

        var summary = new ReportSummary();
        var groups = new List<ReportTestGroup>();
        var spans = new List<SpanData>();
        // Same class name can exist in several suites (multi-TFM runs of one assembly,
        // or genuinely duplicated names across projects) — suffix with the suite label
        // so the merged report keeps them apart instead of visually interleaving them.
        var classNameCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var suite in ordered)
        {
            foreach (var group in suite.Groups)
            {
                classNameCounts[group.ClassName] = classNameCounts.GetValueOrDefault(group.ClassName) + 1;
            }
        }

        for (var suiteIndex = 0; suiteIndex < ordered.Length; suiteIndex++)
        {
            var suite = ordered[suiteIndex];

            summary.Add(suite.Summary);

            foreach (var group in suite.Groups)
            {
                var className = classNameCounts[group.ClassName] > 1
                    ? $"{group.ClassName} [{labels[suiteIndex]}]"
                    : group.ClassName;

                groups.Add(new ReportTestGroup
                {
                    ClassName = className,
                    Namespace = group.Namespace,
                    Summary = group.Summary,
                    Tests = PrefixTestIds(group.Tests, suiteIndex, className),
                });
            }

            if (suite.Spans is { Length: > 0 } suiteSpans)
            {
                spans.AddRange(suiteSpans);
            }
        }

        return new ReportData
        {
            AssemblyName = $"{ordered.Length} Test Suites",
            MachineName = JoinDistinct(ordered, static s => s.MachineName),
            Timestamp = EarliestTimestamp(ordered),
            TUnitVersion = JoinDistinct(ordered, static s => s.TUnitVersion),
            OperatingSystem = JoinDistinct(ordered, static s => s.OperatingSystem),
            RuntimeVersion = JoinDistinct(ordered, static s => s.RuntimeVersion),
            Filter = JoinDistinctOrNull(ordered, static s => s.Filter),
            TotalDurationMs = ComputeWallClockDurationMs(ordered),
            Summary = summary,
            Groups = groups.ToArray(),
            Spans = spans.Count > 0 ? spans.ToArray() : null,
            CommitSha = FirstNonEmpty(ordered, static s => s.CommitSha),
            Branch = FirstNonEmpty(ordered, static s => s.Branch),
            PullRequestNumber = FirstNonEmpty(ordered, static s => s.PullRequestNumber),
            RepositorySlug = FirstNonEmpty(ordered, static s => s.RepositorySlug),
            SourceLinks = ordered.Select(static s => s.SourceLinks).FirstOrDefault(static l => l is not null),
        };
    }

    /// <summary>
    /// Display labels for suites, unique across the set: assembly name alone when unique,
    /// progressively disambiguated with runtime, OS and machine when suites collide
    /// (e.g. the same assembly run for net8.0 and net9.0).
    /// </summary>
    internal static string[] BuildSuiteLabels(IReadOnlyList<ReportData> suites)
    {
        var candidates = new Func<ReportData, string>[]
        {
            static s => s.AssemblyName,
            static s => $"{s.AssemblyName} ({s.RuntimeVersion})",
            static s => $"{s.AssemblyName} ({s.RuntimeVersion}, {s.OperatingSystem})",
            static s => $"{s.AssemblyName} ({s.RuntimeVersion}, {s.OperatingSystem}, {s.MachineName})",
        };

        foreach (var candidate in candidates)
        {
            var labels = new string[suites.Count];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unique = true;
            for (var i = 0; i < suites.Count; i++)
            {
                labels[i] = candidate(suites[i]);
                unique &= seen.Add(labels[i]);
            }
            if (unique)
            {
                return labels;
            }
        }

        // Truly identical metadata — fall back to an index suffix.
        var indexed = new string[suites.Count];
        for (var i = 0; i < suites.Count; i++)
        {
            indexed[i] = $"{suites[i].AssemblyName} #{i + 1}";
        }
        return indexed;
    }

    // Test UIDs are only unique within one process; the merged report uses them as
    // dictionary keys (lane assignment, span correlation), so prefix per suite.
    // ClassName is re-stamped so per-test class always matches its (possibly
    // disambiguated) group header.
    private static ReportTestResult[] PrefixTestIds(ReportTestResult[] tests, int suiteIndex, string className)
    {
        var result = new ReportTestResult[tests.Length];
        for (var i = 0; i < tests.Length; i++)
        {
            result[i] = tests[i] with
            {
                Id = $"s{suiteIndex}::{tests[i].Id}",
                ClassName = className,
            };
        }
        return result;
    }

    /// <summary>
    /// A suite's wall-clock bounds in unix ms, from its tests' absolute start timestamps;
    /// <see langword="null"/> when no test carries one. Computed once per suite so callers
    /// rendering both per-suite and whole-run durations parse each timestamp only once.
    /// </summary>
    internal static (long StartMs, long EndMs)? ComputeWallClockBounds(ReportData suite)
    {
        var earliest = long.MaxValue;
        var latest = long.MinValue;
        foreach (var group in suite.Groups)
        {
            foreach (var test in group.Tests)
            {
                if (HtmlReportGenerator.TryParseUnixMs(test.StartTime) is not { } startMs)
                {
                    continue;
                }

                var endMs = startMs + (long)Math.Round(test.DurationMs);
                if (startMs < earliest) earliest = startMs;
                if (endMs > latest) latest = endMs;
            }
        }
        return earliest == long.MaxValue ? null : (earliest, latest);
    }

    /// <summary>
    /// Wall-clock duration across all suites: latest test end minus earliest test start.
    /// Suites' own <see cref="ReportData.TotalDurationMs"/> values overlap when they ran
    /// in parallel, so summing them would overstate; when no test carries timestamps,
    /// fall back to the longest single suite.
    /// </summary>
    internal static double ComputeWallClockDurationMs(IReadOnlyList<ReportData> suites)
    {
        var earliest = long.MaxValue;
        var latest = long.MinValue;
        double maxSuiteDuration = 0;
        foreach (var suite in suites)
        {
            if (suite.TotalDurationMs > maxSuiteDuration)
            {
                maxSuiteDuration = suite.TotalDurationMs;
            }

            if (ComputeWallClockBounds(suite) is not { } bounds)
            {
                continue;
            }

            if (bounds.StartMs < earliest) earliest = bounds.StartMs;
            if (bounds.EndMs > latest) latest = bounds.EndMs;
        }

        return earliest == long.MaxValue ? maxSuiteDuration : latest - earliest;
    }

    // ReportData.Timestamp is a display string; parse with its exact write format
    // (HtmlReporter uses "dd MMM yyyy, HH:mm:ss 'UTC'") to pick the earliest suite.
    private static string EarliestTimestamp(IReadOnlyList<ReportData> suites)
    {
        var best = suites[0].Timestamp;
        var bestParsed = DateTimeOffset.MaxValue;
        foreach (var suite in suites)
        {
            if (DateTimeOffset.TryParseExact(suite.Timestamp, "dd MMM yyyy, HH:mm:ss 'UTC'",
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed)
                && parsed < bestParsed)
            {
                bestParsed = parsed;
                best = suite.Timestamp;
            }
        }
        return best;
    }

    private static List<string> DistinctNonEmpty(IReadOnlyList<ReportData> suites, Func<ReportData, string?> selector)
    {
        var distinct = new List<string>();
        foreach (var suite in suites)
        {
            var value = selector(suite);
            if (!string.IsNullOrEmpty(value) && !distinct.Contains(value!))
            {
                distinct.Add(value!);
            }
        }
        return distinct;
    }

    private static string JoinDistinct(IReadOnlyList<ReportData> suites, Func<ReportData, string?> selector)
    {
        var distinct = DistinctNonEmpty(suites, selector);
        return distinct.Count switch
        {
            0 => "",
            <= 3 => string.Join(", ", distinct),
            _ => $"{distinct[0]}, {distinct[1]} +{distinct.Count - 2} more",
        };
    }

    private static string? JoinDistinctOrNull(IReadOnlyList<ReportData> suites, Func<ReportData, string?> selector)
    {
        var distinct = DistinctNonEmpty(suites, selector);
        return distinct.Count == 0 ? null : string.Join("; ", distinct);
    }

    private static string? FirstNonEmpty(IReadOnlyList<ReportData> suites, Func<ReportData, string?> selector)
    {
        foreach (var suite in suites)
        {
            var value = selector(suite);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }
        return null;
    }
}
