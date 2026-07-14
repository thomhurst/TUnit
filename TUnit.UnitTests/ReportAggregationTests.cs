using TUnit.Core;
using TUnit.Engine.Reporters;
using TUnit.Engine.Reporters.Aggregation;
using TUnit.Engine.Reporters.Html;

namespace TUnit.UnitTests;

public class ReportDataJsonTests
{
    [Test]
    public async Task Serialize_Then_Deserialize_RoundTrips_AllFields()
    {
        var original = TestReportData.Build("My.Tests");

        var json = ReportDataJson.Serialize(original);
        var restored = ReportDataJson.TryDeserialize(json);

        await Assert.That(restored).IsNotNull();
        await Assert.That(restored!.AssemblyName).IsEqualTo("My.Tests");
        await Assert.That(restored.MachineName).IsEqualTo(original.MachineName);
        await Assert.That(restored.Timestamp).IsEqualTo(original.Timestamp);
        await Assert.That(restored.TUnitVersion).IsEqualTo(original.TUnitVersion);
        await Assert.That(restored.OperatingSystem).IsEqualTo(original.OperatingSystem);
        await Assert.That(restored.RuntimeVersion).IsEqualTo(original.RuntimeVersion);
        await Assert.That(restored.Filter).IsEqualTo(original.Filter);
        await Assert.That(restored.TotalDurationMs).IsEqualTo(original.TotalDurationMs);
        await Assert.That(restored.ArtifactUrl).IsEqualTo(original.ArtifactUrl);
        await Assert.That(restored.CommitSha).IsEqualTo(original.CommitSha);
        await Assert.That(restored.Branch).IsEqualTo(original.Branch);
        await Assert.That(restored.PullRequestNumber).IsEqualTo(original.PullRequestNumber);
        await Assert.That(restored.RepositorySlug).IsEqualTo(original.RepositorySlug);

        // Source-link templates must survive the sidecar, or merged reports lose
        // clickable source navigation (the template renders links from these, not
        // from commit/repository fields).
        await Assert.That(restored.SourceLinks).IsNotNull();
        await Assert.That(restored.SourceLinks!.LineUrl).IsEqualTo(original.SourceLinks!.LineUrl);
        await Assert.That(restored.SourceLinks.RangeUrl).IsEqualTo(original.SourceLinks.RangeUrl);
        await Assert.That(restored.SourceLinks.RawUrl).IsEqualTo(original.SourceLinks.RawUrl);

        await Assert.That(restored.Summary.Total).IsEqualTo(original.Summary.Total);
        await Assert.That(restored.Summary.Passed).IsEqualTo(original.Summary.Passed);
        await Assert.That(restored.Summary.Failed).IsEqualTo(original.Summary.Failed);
        await Assert.That(restored.Summary.Flaky).IsEqualTo(original.Summary.Flaky);

        await Assert.That(restored.Groups.Length).IsEqualTo(original.Groups.Length);
        var group = restored.Groups[0];
        await Assert.That(group.ClassName).IsEqualTo("CalculatorTests");
        await Assert.That(group.Namespace).IsEqualTo("My.Tests");

        var failing = group.Tests.Single(t => t.Status == "failed");
        await Assert.That(failing.Exception).IsNotNull();
        await Assert.That(failing.Exception!.Type).IsEqualTo("System.InvalidOperationException");
        await Assert.That(failing.Exception.InnerException).IsNotNull();
        await Assert.That(failing.Exception.InnerException!.Message).IsEqualTo("inner");
        await Assert.That(failing.Categories!).Contains("integration");
        await Assert.That(failing.CustomProperties![0].Key).IsEqualTo("owner");
        await Assert.That(failing.LineNumber).IsEqualTo(42);
        await Assert.That(failing.SourceRelativePath).IsEqualTo("tests/CalculatorTests.cs");

        var flaky = group.Tests.Single(t => t.RetryAttempt > 0);
        await Assert.That(flaky.Attempts!.Length).IsEqualTo(2);
        await Assert.That(flaky.Attempts[0].Status).IsEqualTo("failed");
        await Assert.That(flaky.Attempts[0].ExceptionType).IsEqualTo("HttpRequestException");

        await Assert.That(restored.Spans!.Length).IsEqualTo(1);
        var span = restored.Spans[0];
        await Assert.That(span.TraceId).IsEqualTo("trace-1");
        await Assert.That(span.Tags![0].Key).IsEqualTo("db.system");
        await Assert.That(span.Events![0].Name).IsEqualTo("query");
        await Assert.That(span.Links![0].SpanId).IsEqualTo("linked-span");
    }

    [Test]
    [Arguments("not json at all")]
    [Arguments("{}")] // no schemaVersion
    [Arguments("""{"schemaVersion": 999, "assemblyName": "x"}""")] // newer than we understand
    [Arguments("""{"schemaVersion": 999999999999, "assemblyName": "x"}""")] // not int-representable — must not throw
    [Arguments("""{"schemaVersion": 1.5, "assemblyName": "x"}""")] // not an integer — must not throw
    [Arguments("""{"schemaVersion": 1, "groups": [1]}""")] // wrong nested shape — must not throw
    [Arguments("""{"schemaVersion": 1, "groups": [{"tests": [{"attempts": [7]}]}]}""")] // deeper wrong shape
    [Arguments("[]")] // not an object
    public async Task TryDeserialize_Rejects_Invalid_Or_Incompatible_Input(string json)
    {
        await Assert.That(ReportDataJson.TryDeserialize(json)).IsNull();
    }

    [Test]
    public async Task TryDeserialize_Treats_OutOfRange_Numbers_As_Defaults()
    {
        var restored = ReportDataJson.TryDeserialize("""{"schemaVersion": 1, "assemblyName": "A", "totalDurationMs": 1e9999}""");
        await Assert.That(restored).IsNotNull();
        await Assert.That(restored!.TotalDurationMs).IsEqualTo(0d);
    }

    [Test]
    public async Task TryDeserialize_Ignores_Unknown_Properties()
    {
        var json = """{"schemaVersion":1,"assemblyName":"A","futureField":{"nested":[1,2,3]},"summary":{"total":1,"passed":1}}""";
        var restored = ReportDataJson.TryDeserialize(json);
        await Assert.That(restored).IsNotNull();
        await Assert.That(restored!.AssemblyName).IsEqualTo("A");
        await Assert.That(restored.Summary.Passed).IsEqualTo(1);
    }
}

public class ReportDataMergerTests
{
    [Test]
    public async Task Merge_SingleSuite_Is_Passthrough()
    {
        var suite = TestReportData.Build("Solo.Tests");
        var merged = ReportDataMerger.Merge([suite]);
        await Assert.That(merged).IsSameReferenceAs(suite);
    }

    [Test]
    public async Task Merge_Sums_Summaries_And_Concatenates_Groups()
    {
        var a = TestReportData.Build("A.Tests");
        var b = TestReportData.Build("B.Tests");

        var merged = ReportDataMerger.Merge([a, b]);

        await Assert.That(merged.AssemblyName).IsEqualTo("2 Test Suites");
        await Assert.That(merged.Summary.Total).IsEqualTo(a.Summary.Total + b.Summary.Total);
        await Assert.That(merged.Summary.Failed).IsEqualTo(a.Summary.Failed + b.Summary.Failed);
        await Assert.That(merged.Summary.Flaky).IsEqualTo(a.Summary.Flaky + b.Summary.Flaky);
        await Assert.That(merged.Groups.Length).IsEqualTo(a.Groups.Length + b.Groups.Length);
        await Assert.That(merged.Spans!.Length).IsEqualTo(2);
    }

    [Test]
    public async Task Merge_Disambiguates_Colliding_Class_Names_And_Test_Ids()
    {
        // Same assembly run twice (e.g. net8.0 + net9.0): identical class names and test ids.
        var net8 = TestReportData.Build("Same.Tests", runtimeVersion: ".NET 8.0.0");
        var net9 = TestReportData.Build("Same.Tests", runtimeVersion: ".NET 9.0.0");

        var merged = ReportDataMerger.Merge([net8, net9]);

        var classNames = merged.Groups.Select(g => g.ClassName).ToArray();
        await Assert.That(classNames.Distinct().Count()).IsEqualTo(classNames.Length);
        await Assert.That(classNames.All(c => c.Contains(".NET 8.0.0") || c.Contains(".NET 9.0.0"))).IsTrue();

        var ids = merged.Groups.SelectMany(g => g.Tests).Select(t => t.Id).ToArray();
        await Assert.That(ids.Distinct().Count()).IsEqualTo(ids.Length);

        // Per-test class name must match its group header, or the report groups them apart.
        foreach (var group in merged.Groups)
        {
            foreach (var test in group.Tests)
            {
                await Assert.That(test.ClassName).IsEqualTo(group.ClassName);
            }
        }
    }

    [Test]
    public async Task Merge_Keeps_Unique_Class_Names_Untouched()
    {
        var a = TestReportData.Build("A.Tests", className: "ATests");
        var b = TestReportData.Build("B.Tests", className: "BTests");

        var merged = ReportDataMerger.Merge([a, b]);

        await Assert.That(merged.Groups.Select(g => g.ClassName)).Contains("ATests");
        await Assert.That(merged.Groups.Select(g => g.ClassName)).Contains("BTests");
    }

    [Test]
    public async Task Merge_Retags_Class_Spans_When_Class_Names_Are_Disambiguated()
    {
        var net8 = TestReportData.Build("Same.Tests", runtimeVersion: ".NET 8.0.0", tagSpanWithClass: true);
        var net9 = TestReportData.Build("Same.Tests", runtimeVersion: ".NET 9.0.0", tagSpanWithClass: true);

        var merged = ReportDataMerger.Merge([net8, net9]);

        // Per-class timelines join spans on this tag; after a rename it must point at the
        // disambiguated group name, or the timeline silently disappears from the report.
        var mergedClassNames = merged.Groups.Select(g => g.ClassName).ToHashSet();
        var classTags = merged.Spans!
            .SelectMany(s => s.Tags ?? [])
            .Where(t => t.Key == "tunit.test.class")
            .Select(t => t.Value)
            .ToArray();

        await Assert.That(classTags).IsNotEmpty();
        foreach (var tag in classTags)
        {
            await Assert.That(mergedClassNames).Contains(tag);
        }
    }

    [Test]
    public async Task Merge_Keeps_SourceControl_Metadata_Only_When_Suites_Agree()
    {
        var sameCommit = ReportDataMerger.Merge([TestReportData.Build("A.Tests"), TestReportData.Build("B.Tests")]);
        await Assert.That(sameCommit.CommitSha).IsEqualTo("abc123");
        await Assert.That(sameCommit.SourceLinks).IsNotNull();

        // Different commits (e.g. merging sidecars from separate runs): a single top-level
        // template would link every test to the first suite's revision — omit instead.
        var mixed = ReportDataMerger.Merge([
            TestReportData.Build("A.Tests"),
            TestReportData.Build("B.Tests", commitSha: "def456"),
        ]);
        await Assert.That(mixed.CommitSha).IsNull();
        await Assert.That(mixed.SourceLinks).IsNull();
        await Assert.That(mixed.RepositorySlug).IsEqualTo("acme/repo"); // still unanimous
    }

    [Test]
    public async Task WallClock_Duration_Uses_Absolute_Bounds_Not_Sum()
    {
        // Two 10s suites overlapping by 5s => 15s wall clock, not 20s.
        var a = TestReportData.BuildWithSingleTest("A.Tests", start: "2026-07-14T10:00:00Z", durationMs: 10_000);
        var b = TestReportData.BuildWithSingleTest("B.Tests", start: "2026-07-14T10:00:05Z", durationMs: 10_000);

        var wall = ReportDataMerger.ComputeWallClockDurationMs([a, b]);
        await Assert.That(wall).IsEqualTo(15_000d);
    }

    [Test]
    public async Task WallClock_Duration_Falls_Back_To_Longest_Suite_Without_Timestamps()
    {
        var a = TestReportData.BuildWithSingleTest("A.Tests", start: null, durationMs: 100, totalDurationMs: 4000);
        var b = TestReportData.BuildWithSingleTest("B.Tests", start: null, durationMs: 100, totalDurationMs: 9000);

        var wall = ReportDataMerger.ComputeWallClockDurationMs([a, b]);
        await Assert.That(wall).IsEqualTo(9000d);
    }

    [Test]
    public async Task SuiteLabels_Disambiguate_Progressively()
    {
        var plain = ReportDataMerger.BuildSuiteLabels([
            TestReportData.Build("A.Tests"),
            TestReportData.Build("B.Tests"),
        ]);
        await Assert.That(plain).IsEquivalentTo(new[] { "A.Tests", "B.Tests" });

        var byRuntime = ReportDataMerger.BuildSuiteLabels([
            TestReportData.Build("Same.Tests", runtimeVersion: ".NET 8.0.0"),
            TestReportData.Build("Same.Tests", runtimeVersion: ".NET 9.0.0"),
        ]);
        await Assert.That(byRuntime[0]).IsEqualTo("Same.Tests (.NET 8.0.0)");
        await Assert.That(byRuntime[1]).IsEqualTo("Same.Tests (.NET 9.0.0)");
    }
}

public class GitHubSummaryRegionTests
{
    [Test]
    public async Task Splice_Into_Empty_File_Appends_Marked_Block()
    {
        var result = GitHubSummaryRegion.Splice("", "CONTENT");
        await Assert.That(result).IsEqualTo($"{GitHubSummaryRegion.StartMarker}\nCONTENT\n{GitHubSummaryRegion.EndMarker}\n");
    }

    [Test]
    public async Task Splice_Preserves_Foreign_Content_Before_And_After_Block()
    {
        var before = "## Coverage report\nlines: 87%\n";
        var withBlock = GitHubSummaryRegion.Splice(before, "V1");
        var after = withBlock + "## Lint results\nno issues\n";

        var updated = GitHubSummaryRegion.Splice(after, "V2");

        await Assert.That(updated).Contains("## Coverage report");
        await Assert.That(updated).Contains("## Lint results");
        await Assert.That(updated).Contains("V2");
        await Assert.That(updated).DoesNotContain("V1");
        // Exactly one block remains.
        await Assert.That(updated.Split([GitHubSummaryRegion.StartMarker], StringSplitOptions.None).Length).IsEqualTo(2);
    }

    [Test]
    public async Task Splice_Is_Idempotent_Across_Rewrites()
    {
        var once = GitHubSummaryRegion.Splice("existing\n", "A");
        var twice = GitHubSummaryRegion.Splice(once, "A");
        await Assert.That(twice).IsEqualTo(once);
    }

    [Test]
    public async Task Splice_Torn_Block_Never_Deletes_Foreign_Content()
    {
        // A writer died after emitting the start marker; another tool appended afterwards.
        var torn = $"{GitHubSummaryRegion.StartMarker}\npartial...\n## Other tool section\nimportant data\n";

        var updated = GitHubSummaryRegion.Splice(torn, "FRESH");

        await Assert.That(updated).Contains("## Other tool section");
        await Assert.That(updated).Contains("important data");
        await Assert.That(updated).Contains("FRESH");
    }

    [Test]
    public async Task Splice_After_Torn_Block_Replaces_Only_The_Complete_Block()
    {
        // Torn fragment, then foreign content, then a complete block from a later writer.
        var torn = $"{GitHubSummaryRegion.StartMarker}\npartial...\n## Foreign\nkeep me\n";
        var withComplete = GitHubSummaryRegion.Splice(torn, "OLD");

        var updated = GitHubSummaryRegion.Splice(withComplete, "NEW");

        await Assert.That(updated).Contains("keep me");
        await Assert.That(updated).Contains("NEW");
        await Assert.That(updated).DoesNotContain("OLD");
    }
}

public class AggregatedSummaryWriterTests
{
    [Test]
    public async Task Render_Contains_Totals_Suite_Rows_Flaky_And_Failures()
    {
        var suites = new[] { TestReportData.Build("A.Tests"), TestReportData.Build("B.Tests") };

        var markdown = AggregatedSummaryWriter.Render(suites, collapsible: true,
            serverUrl: "https://github.com", mergedReportHint: "hint-text");

        await Assert.That(markdown).Contains("TUnit Test Results — 2 suites");
        await Assert.That(markdown).Contains("**6 tests**");
        await Assert.That(markdown).Contains("| ❌ `A.Tests` |");
        await Assert.That(markdown).Contains("| ❌ `B.Tests` |");
        await Assert.That(markdown).Contains("flaky");
        await Assert.That(markdown).Contains("Failures by Cause");
        await Assert.That(markdown).Contains("InvalidOperationException");
        await Assert.That(markdown).Contains("hint-text");
        // Source link built from repositorySlug + commitSha + relative path.
        await Assert.That(markdown).Contains("https://github.com/acme/repo/blob/abc123/tests/CalculatorTests.cs#L42");
    }

    [Test]
    public async Task Render_AllPassing_Has_No_Failure_Section()
    {
        var suite = TestReportData.BuildWithSingleTest("Green.Tests", start: "2026-07-14T10:00:00Z", durationMs: 50);
        var markdown = AggregatedSummaryWriter.Render([suite]);

        await Assert.That(markdown).Contains("✅ TUnit Test Results — 1 suite");
        await Assert.That(markdown).DoesNotContain("Failures by Cause");
        await Assert.That(markdown).DoesNotContain("Quick diagnosis");
    }
}

public class ReportAggregatorEnvironmentTests
{
    private static Func<string, string?> Env(params (string Key, string Value)[] vars)
        => key => vars.FirstOrDefault(v => v.Key == key).Value;

    private static readonly (string, string)[] GitHubActionsEnv =
    [
        ("GITHUB_ACTIONS", "true"),
        ("RUNNER_TEMP", Path.GetTempPath()),
        ("GITHUB_RUN_ID", "123"),
        ("GITHUB_RUN_ATTEMPT", "1"),
        ("GITHUB_JOB", "test"),
    ];

    [Test]
    public async Task Aggregation_Defaults_On_When_Shared_Directory_Is_Resolvable()
    {
        var aggregator = ReportAggregator.TryCreateFromEnvironment(Env(GitHubActionsEnv));

        await Assert.That(aggregator).IsNotNull();
        await Assert.That(aggregator!.Mode).IsEqualTo(AggregationMode.Cooperative);
        await Assert.That(aggregator.Directory).Contains("tunit-aggregate");
        await Assert.That(aggregator.Directory).Contains("run-123-1-test");
    }

    [Test]
    public async Task Aggregation_Defaults_Off_When_No_Shared_Directory_Resolvable()
    {
        // Plain local run: no CI vars, no explicit directory — must silently no-op.
        var aggregator = ReportAggregator.TryCreateFromEnvironment(Env());
        await Assert.That(aggregator).IsNull();
    }

    [Test]
    [Arguments("off")]
    [Arguments("false")]
    [Arguments("0")]
    [Arguments("no")]
    [Arguments("disabled")]
    [Arguments("none")]
    public async Task Aggregation_Can_Be_Disabled_Explicitly(string value)
    {
        var aggregator = ReportAggregator.TryCreateFromEnvironment(
            Env([("TUNIT_AGGREGATE_REPORTS", value), .. GitHubActionsEnv]));
        await Assert.That(aggregator).IsNull();
    }

    [Test]
    public async Task Defer_Mode_Is_Selected_By_Value()
    {
        var dir = Path.Combine(Path.GetTempPath(), "tunit-agg-tests");
        var aggregator = ReportAggregator.TryCreateFromEnvironment(
            Env(("TUNIT_AGGREGATE_REPORTS", "defer"), ("TUNIT_AGGREGATE_DIR", dir)));

        await Assert.That(aggregator).IsNotNull();
        await Assert.That(aggregator!.Mode).IsEqualTo(AggregationMode.Defer);
        await Assert.That(aggregator.Directory).IsEqualTo(Path.GetFullPath(dir));
    }

    [Test]
    public async Task Explicit_Directory_Wins_Over_GitHub_Derived_Directory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "explicit-agg-dir");
        var aggregator = ReportAggregator.TryCreateFromEnvironment(
            Env([("TUNIT_AGGREGATE_DIR", dir), .. GitHubActionsEnv]));

        await Assert.That(aggregator).IsNotNull();
        await Assert.That(aggregator!.Directory).IsEqualTo(Path.GetFullPath(dir));
    }
}

public class HtmlReporterSidecarPathTests
{
    [Test]
    public async Task Default_Report_Name_Maps_To_Sidecar_Name()
    {
        var path = HtmlReporter.GetSidecarPath(Path.Combine("TestResults", "My.Tests-linux-net10-report.html"));
        await Assert.That(path).IsEqualTo(Path.Combine("TestResults", "My.Tests-linux-net10.tunit-report.json"));
    }

    [Test]
    public async Task Custom_Report_Name_Swaps_Extension()
    {
        var path = HtmlReporter.GetSidecarPath(Path.Combine("out", "custom.html"));
        await Assert.That(path).IsEqualTo(Path.Combine("out", "custom.tunit-report.json"));
    }
}

/// <summary>Builders for realistic ReportData graphs used across the aggregation tests.</summary>
internal static class TestReportData
{
    internal static ReportData Build(
        string assemblyName,
        string runtimeVersion = ".NET 10.0.0",
        string className = "CalculatorTests",
        string commitSha = "abc123",
        bool tagSpanWithClass = false)
        => new()
        {
            AssemblyName = assemblyName,
            MachineName = "test-machine",
            Timestamp = "14 Jul 2026, 10:00:00 UTC",
            TUnitVersion = "1.2.3",
            OperatingSystem = "Linux",
            RuntimeVersion = runtimeVersion,
            Filter = "/*/*/CalculatorTests/*",
            TotalDurationMs = 1234.5,
            ArtifactUrl = "https://github.com/acme/repo/actions/runs/1/artifacts/2",
            CommitSha = commitSha,
            Branch = "main",
            PullRequestNumber = "17",
            RepositorySlug = "acme/repo",
            SourceLinks = new SourceLinkTemplates(
                $"https://github.com/acme/repo/blob/{commitSha}/{{path}}#L{{line}}",
                $"https://github.com/acme/repo/blob/{commitSha}/{{path}}#L{{start}}-L{{end}}",
                $"https://raw.githubusercontent.com/acme/repo/{commitSha}/{{path}}"),
            Summary = new ReportSummary { Total = 3, Passed = 2, Failed = 1, Flaky = 1 },
            Groups =
            [
                new ReportTestGroup
                {
                    ClassName = className,
                    Namespace = "My.Tests",
                    Summary = new ReportSummary { Total = 3, Passed = 2, Failed = 1, Flaky = 1 },
                    Tests =
                    [
                        new ReportTestResult
                        {
                            Id = "test-1",
                            DisplayName = "Adds",
                            MethodName = "Adds",
                            ClassName = className,
                            Status = "passed",
                            DurationMs = 10,
                            StartTime = "2026-07-14T10:00:01.000Z",
                        },
                        new ReportTestResult
                        {
                            Id = "test-2",
                            DisplayName = "Divides",
                            MethodName = "Divides",
                            ClassName = className,
                            Status = "failed",
                            DurationMs = 20,
                            StartTime = "2026-07-14T10:00:02.000Z",
                            Exception = new ReportExceptionData
                            {
                                Type = "System.InvalidOperationException",
                                Message = "boom",
                                StackTrace = "at CalculatorTests.Divides()",
                                InnerException = new ReportExceptionData { Type = "System.Exception", Message = "inner" },
                            },
                            Categories = ["integration"],
                            CustomProperties = [new ReportKeyValue { Key = "owner", Value = "platform" }],
                            FilePath = "C:/repo/tests/CalculatorTests.cs",
                            LineNumber = 42,
                            EndLineNumber = 48,
                            SourceRelativePath = "tests/CalculatorTests.cs",
                        },
                        new ReportTestResult
                        {
                            Id = "test-3",
                            DisplayName = "RetriesThenPasses",
                            MethodName = "RetriesThenPasses",
                            ClassName = className,
                            Status = "passed",
                            DurationMs = 30,
                            StartTime = "2026-07-14T10:00:03.000Z",
                            RetryAttempt = 1,
                            Attempts =
                            [
                                new ReportAttempt { Status = "failed", DurationMs = 25, ExceptionType = "HttpRequestException", ExceptionMessage = "timeout" },
                                new ReportAttempt { Status = "passed", DurationMs = 30 },
                            ],
                        },
                    ],
                },
            ],
            Spans =
            [
                new SpanData
                {
                    TraceId = "trace-1",
                    SpanId = "span-1",
                    ParentSpanId = null,
                    Name = "SELECT",
                    SpanType = "db",
                    Source = "Npgsql",
                    Kind = "Client",
                    StartTimeMs = 1_752_487_201_000,
                    DurationMs = 5,
                    Status = "Ok",
                    Tags = tagSpanWithClass
                        ? [new ReportKeyValue { Key = "db.system", Value = "postgresql" }, new ReportKeyValue { Key = "tunit.test.class", Value = className }]
                        : [new ReportKeyValue { Key = "db.system", Value = "postgresql" }],
                    Events = [new SpanEvent { Name = "query", TimestampMs = 1_752_487_201_001 }],
                    Links = [new SpanLink { TraceId = "trace-2", SpanId = "linked-span" }],
                },
            ],
        };

    internal static ReportData BuildWithSingleTest(string assemblyName, string? start, double durationMs, double totalDurationMs = 0)
        => new()
        {
            AssemblyName = assemblyName,
            MachineName = "test-machine",
            Timestamp = "14 Jul 2026, 10:00:00 UTC",
            TUnitVersion = "1.2.3",
            OperatingSystem = "Linux",
            RuntimeVersion = ".NET 10.0.0",
            TotalDurationMs = totalDurationMs > 0 ? totalDurationMs : durationMs,
            Summary = new ReportSummary { Total = 1, Passed = 1 },
            Groups =
            [
                new ReportTestGroup
                {
                    ClassName = "SingleTests",
                    Namespace = assemblyName,
                    Summary = new ReportSummary { Total = 1, Passed = 1 },
                    Tests =
                    [
                        new ReportTestResult
                        {
                            Id = "only",
                            DisplayName = "Only",
                            MethodName = "Only",
                            ClassName = "SingleTests",
                            Status = "passed",
                            DurationMs = durationMs,
                            StartTime = start,
                        },
                    ],
                },
            ],
        };
}
