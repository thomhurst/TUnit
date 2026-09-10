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

            // Original → disambiguated class names for this suite, so the suite's spans
            // can be re-tagged to match: the report's per-class timelines are joined on
            // the span's class tag, and a renamed group would otherwise lose its timeline.
            Dictionary<string, string>? renamedClasses = null;

            foreach (var group in suite.Groups)
            {
                var className = group.ClassName;
                if (classNameCounts[className] > 1)
                {
                    var renamed = $"{className} [{labels[suiteIndex]}]";
                    (renamedClasses ??= new Dictionary<string, string>(StringComparer.Ordinal))[className] = renamed;
                    className = renamed;
                }

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
#if NET
                // Timelines only render on modern TFMs (span collection is #if NET), so
                // the retag is pointless elsewhere — spans pass through untouched there.
                if (renamedClasses is not null)
                {
                    foreach (var span in suiteSpans)
                    {
                        spans.Add(RetagClassSpan(span, renamedClasses));
                    }
                }
                else
#endif
                {
                    spans.AddRange(suiteSpans);
                }
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
            // Source-control metadata (and the link templates built from it) applies to the
            // whole merged report, so it is only kept when every suite that has it agrees —
            // merging sidecars from different commits/runs must not label every test with
            // the first suite's commit or link to the wrong revision.
            CommitSha = SingleDistinctOrNull(ordered, static s => s.CommitSha),
            Branch = SingleDistinctOrNull(ordered, static s => s.Branch),
            PullRequestNumber = SingleDistinctOrNull(ordered, static s => s.PullRequestNumber),
            RepositorySlug = SingleDistinctOrNull(ordered, static s => s.RepositorySlug),
            SourceLinks = SingleDistinctSourceLinksOrNull(ordered),
        };
    }

#if NET
    // The report's class-timeline join resolves a suite span's class as
    // `FindTagValue(TagTestClass) ?? Name`. Suite spans carry no TagTestClass; the
    // collector rewrites their Name from the TagTestSuiteName tag (the simple class
    // name), so the effective join key is Name — that's what must follow a rename.
    private static SpanData RetagClassSpan(SpanData span, Dictionary<string, string> renamedClasses)
    {
        if (!string.Equals(span.SpanType, TUnitActivitySource.SpanTestSuite, StringComparison.Ordinal)
            || !renamedClasses.TryGetValue(span.Name, out var renamed))
        {
            return span;
        }

        var tags = span.Tags;
        if (tags is { Length: > 0 })
        {
            for (var i = 0; i < tags.Length; i++)
            {
                if (string.Equals(tags[i].Key, TUnitActivitySource.TagTestSuiteName, StringComparison.Ordinal)
                    && string.Equals(tags[i].Value, span.Name, StringComparison.Ordinal))
                {
                    tags = (ReportKeyValue[])tags.Clone();
                    tags[i] = new ReportKeyValue { Key = tags[i].Key, Value = renamed };
                    break;
                }
            }
        }

        return span with { Name = renamed, Tags = tags };
    }
#endif

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

    /// <summary>The single value shared by every suite that has one; null when they disagree.</summary>
    private static string? SingleDistinctOrNull(IReadOnlyList<ReportData> suites, Func<ReportData, string?> selector)
    {
        var distinct = DistinctNonEmpty(suites, selector);
        return distinct.Count == 1 ? distinct[0] : null;
    }

    private static SourceLinkTemplates? SingleDistinctSourceLinksOrNull(IReadOnlyList<ReportData> suites)
    {
        SourceLinkTemplates? found = null;
        foreach (var suite in suites)
        {
            if (suite.SourceLinks is not { } links)
            {
                continue;
            }

            if (found is null)
            {
                found = links;
            }
            else if (found != links) // record value equality
            {
                return null;
            }
        }
        return found;
    }
}
