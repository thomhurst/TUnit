#pragma warning disable TPEXP

using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;
using Shouldly;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Engine.Reporters;
using TUnit.Engine.Reporters.Html;

namespace TUnit.Engine.Tests;

public class HtmlReporterTests
{
    [Test]
    public void HtmlReporter_Implements_IDataProducer()
    {
        var reporter = new HtmlReporter(new MockExtension());
        reporter.ShouldBeAssignableTo<Microsoft.Testing.Platform.Extensions.Messages.IDataProducer>();
    }

    [Test]
    public void HtmlReporter_DataTypesProduced_Contains_SessionFileArtifact()
    {
        var reporter = new HtmlReporter(new MockExtension());
        var producer = (Microsoft.Testing.Platform.Extensions.Messages.IDataProducer)reporter;
        producer.DataTypesProduced.ShouldContain(typeof(SessionFileArtifact));
    }

    [Test]
    public async Task PublishArtifactAsync_Publishes_SessionFileArtifact_When_SessionContext_Set_And_File_Exists()
    {
        // Arrange
        var reporter = new HtmlReporter(new MockExtension());
        var bus = new CapturingMessageBus();
        reporter.SetMessageBus(bus);

        var tempFile = Path.GetTempFileName();
        try
        {
            // Act
            await reporter.PublishArtifactAsync(tempFile, new SessionUid("test-session-1"), CancellationToken.None);

            // Assert
            bus.Published.Count.ShouldBe(1);
            var artifact = bus.Published[0].Data.ShouldBeOfType<Microsoft.Testing.Platform.Extensions.Messages.SessionFileArtifact>();
            artifact.FileInfo.FullName.ShouldBe(new FileInfo(tempFile).FullName);
            artifact.DisplayName.ShouldBe("HTML Test Report");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public async Task PublishArtifactAsync_Is_NoOp_When_MessageBus_Not_Injected()
    {
        var reporter = new HtmlReporter(new MockExtension());
        var bus = new CapturingMessageBus();
        // Intentionally not calling reporter.SetMessageBus(bus)

        var tempFile = Path.GetTempFileName();
        try
        {
            await reporter.PublishArtifactAsync(tempFile, new SessionUid("test-session-1"), CancellationToken.None);
            bus.Published.ShouldBeEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }


    [Test]
    public void FilterAdditionalTraceIds_Removes_Primary_Trace_CaseInsensitive()
    {
        var primary = "abcdef0123456789abcdef0123456789";
        var linked = "1111111111111111aaaaaaaaaaaaaaaa";
        var all = new[] { primary.ToUpperInvariant(), linked };

        var result = HtmlReporter.FilterAdditionalTraceIds(all, primary);

        result.ShouldBe(new[] { linked });
    }

    [Test]
    public void FilterAdditionalTraceIds_Returns_Input_When_Primary_Null()
    {
        var all = new[] { "aaaa", "bbbb" };

        var result = HtmlReporter.FilterAdditionalTraceIds(all, primaryTraceId: null);

        result.ShouldBeSameAs(all);
    }

    [Test]
    public void FilterAdditionalTraceIds_Returns_Input_When_No_Match()
    {
        var all = new[] { "aaaa", "bbbb" };

        var result = HtmlReporter.FilterAdditionalTraceIds(all, "cccc");

        result.ShouldBeSameAs(all);
    }

    [Test]
    public void FilterAdditionalTraceIds_Returns_Empty_When_Only_Primary()
    {
        var primary = "abcdef0123456789abcdef0123456789";

        var result = HtmlReporter.FilterAdditionalTraceIds(new[] { primary }, primary);

        result.ShouldBeEmpty();
    }

    [Test]
    public void OrderTestsForDisplay_SortsByStartTime_ThenName()
    {
        var later = CreateTestResultWithStartTime("Later", "2026-05-07T09:26:25.0000000Z");
        var earlier = CreateTestResultWithStartTime("Earlier", "2026-05-07T09:26:24.0000000Z");
        var sameTimeButLaterName = CreateTestResultWithStartTime("Zeta", "2026-05-07T09:26:24.0000000Z");

        var ordered = HtmlReporter.OrderTestsForDisplay([later, sameTimeButLaterName, earlier]);

        ordered.Select(static test => test.DisplayName).ShouldBe(["Earlier", "Zeta", "Later"]);
    }

    [Test]
    public void GenerateHtml_RoundTrips_TestBodySpans_AndChildren_Through_EmbeddedData()
    {
        // Pins the renderer's data contract: a "test body" span with a child span survives
        // serialisation into the embedded JSON under the owning test (matched by traceId),
        // with the design's per-test `spans[]` shape (`parent` rather than `parentSpanId`).
        const string traceId = "0123456789abcdef0123456789abcdef";
        var spans = new[]
        {
            new SpanData
            {
                TraceId = traceId, SpanId = "aaaaaaaaaaaaaaaa", Name = "test body",
                Source = "TUnit", Kind = "Internal", Status = "Ok",
            },
            new SpanData
            {
                TraceId = traceId, SpanId = "bbbbbbbbbbbbbbbb", ParentSpanId = "aaaaaaaaaaaaaaaa",
                Name = "wiremock-call", Source = "TUnit", Kind = "Client", Status = "Ok",
            },
        };

        var html = HtmlReportGenerator.GenerateHtml(new ReportData
        {
            AssemblyName = "Tests",
            MachineName = "machine",
            Timestamp = "2026-05-07T09:26:24.0000000Z",
            TUnitVersion = "1.0.0",
            OperatingSystem = "Linux",
            RuntimeVersion = ".NET 10.0",
            TotalDurationMs = 0,
            Summary = new ReportSummary(),
            Groups =
            [
                new ReportTestGroup
                {
                    ClassName = "SampleTests",
                    Namespace = "Tests",
                    Summary = new ReportSummary(),
                    Tests =
                    [
                        new ReportTestResult
                        {
                            Id = "t1", DisplayName = "t1", MethodName = "t1",
                            ClassName = "SampleTests", Status = "passed",
                            TraceId = traceId,
                        },
                    ],
                },
            ],
            Spans = spans,
        });

        var embedded = ExtractEmbeddedReportJson(html);
        embedded.ShouldContain("\"name\":\"test body\"");
        embedded.ShouldContain("\"name\":\"wiremock-call\"");
        embedded.ShouldContain("\"parent\":\"aaaaaaaaaaaaaaaa\"");
    }

    private static string ExtractEmbeddedReportJson(string html)
    {
        // The renderer reads gzipped+base64 JSON from <script id="report-data" type="application/octet-stream">
        // so the embedded data stays small for large suites.
        var match = Regex.Match(
            html,
            "<script id=\"report-data\"[^>]*>(?<payload>.*?)</script>",
            RegexOptions.Singleline);
        match.Success.ShouldBeTrue("Expected embedded report-data script in rendered HTML.");
        var payload = match.Groups["payload"].Value.Trim();
        var compressed = Convert.FromBase64String(payload);
        using var ms = new MemoryStream(compressed);
        using var gz = new GZipStream(ms, CompressionMode.Decompress);
        using var reader = new StreamReader(gz, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    [Test]
    public async Task PublishArtifactAsync_Publishes_With_Correct_SessionUid()
    {
        var reporter = new HtmlReporter(new MockExtension());
        var bus = new CapturingMessageBus();
        reporter.SetMessageBus(bus);

        var tempFile = Path.GetTempFileName();
        try
        {
            var uid = new SessionUid("my-session-42");
            await reporter.PublishArtifactAsync(tempFile, uid, CancellationToken.None);

            var artifact = bus.Published[0].Data.ShouldBeOfType<SessionFileArtifact>();
            artifact.SessionUid.Value.ShouldBe("my-session-42");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void ClassTimelineAttribute_Exposes_Mode_And_ScopeType()
    {
        var full = new ClassTimelineAttribute(TimelineMode.FullExecution);
        full.Mode.ShouldBe(TimelineMode.FullExecution);
        full.ScopeType.ShouldBe(typeof(ClassTimelineAttribute));

        var collapsed = new ClassTimelineAttribute(TimelineMode.Collapsed);
        collapsed.Mode.ShouldBe(TimelineMode.Collapsed);
    }

    [Test]
    public void GenerateHtml_RoundTrips_ClassTimeline_CustomProperty_OnTest()
    {
        // The JS reads group.tests[0].customProperties for tunit.report.timeline; this
        // pins the contract that the property survives serialisation into the embedded JSON.
        var html = HtmlReportGenerator.GenerateHtml(new ReportData
        {
            AssemblyName = "Tests",
            MachineName = "machine",
            Timestamp = "2026-05-07T09:26:24.0000000Z",
            TUnitVersion = "1.0.0",
            OperatingSystem = "Linux",
            RuntimeVersion = ".NET 10.0",
            TotalDurationMs = 0,
            Summary = new ReportSummary(),
            Groups =
            [
                new ReportTestGroup
                {
                    ClassName = "BddFlow",
                    Namespace = "Sample",
                    Summary = new ReportSummary(),
                    Tests =
                    [
                        new ReportTestResult
                        {
                            Id = "t1", DisplayName = "t1", MethodName = "t1",
                            ClassName = "BddFlow", Status = "passed",
                            CustomProperties =
                            [
                                new ReportKeyValue { Key = ClassTimelineAttribute.ClassTimelinePropertyKey, Value = nameof(TimelineMode.FullExecution) }
                            ],
                        },
                    ],
                },
            ],
        });

        var embedded = ExtractEmbeddedReportJson(html);
        embedded.ShouldContain("\"tunit.report.timeline\":\"FullExecution\"");
    }

    [Test]
    public void ExtractTestResult_SortsTestMetadataProperty_Into_Categories_And_CustomProperties()
    {
        // Regression: Microsoft.Testing.Platform's VSTestBridge convention emits categories as
        // TestMetadataProperty(name, "") — the category name lives in Key, Value is empty.
        // Traits/custom properties use the (key, value) form with a non-empty Value.
        // Earlier code keyed off IsNullOrEmpty(Key), which inverted both branches and silently
        // misclassified every category as a custom property — leaving the HTML report's
        // category-pill UI permanently empty.
        var node = new TestNode
        {
            Uid = new TestNodeUid("extract-1"),
            DisplayName = "Test",
            Properties = new PropertyBag(
                PassedTestNodeStateProperty.CachedInstance,
                new TestMethodIdentifierProperty(
                    @namespace: "TestNamespace",
                    assemblyFullName: "TestAssembly",
                    typeName: "SampleTests",
                    methodName: "Test",
                    parameterTypeFullNames: [],
                    returnTypeFullName: "System.Void",
                    methodArity: 0),
                new TestMetadataProperty("Async", string.Empty),
                new TestMetadataProperty("Integration", string.Empty),
                new TestMetadataProperty("Owner", "TeamA"))
        };

        var result = HtmlReporter.ExtractTestResult("extract-1", node, traceId: null, spanId: null, retryAttempt: 0, additionalTraceIds: null);

        result.Categories.ShouldBe(["Async", "Integration"], ignoreOrder: true);
        result.CustomProperties.ShouldNotBeNull();
        result.CustomProperties!.Length.ShouldBe(1);
        result.CustomProperties[0].Key.ShouldBe("Owner");
        result.CustomProperties[0].Value.ShouldBe("TeamA");
    }

    [Test]
    public void GenerateHtml_StripsSampleDataGeneratorBlock_FromShippedReports()
    {
        // The template carries a generateSampleData() preview block bounded by
        // /* SAMPLE_DATA_BEGIN ... SAMPLE_DATA_END */ markers; LoadAndStripTemplate
        // must remove it so the rendered report doesn't ship hundreds of lines of
        // CloudShop fixture data. This test pins that contract.
        var html = HtmlReportGenerator.GenerateHtml(new ReportData
        {
            AssemblyName = "Tests",
            MachineName = "machine",
            Timestamp = "2026-05-07T09:26:24.0000000Z",
            TUnitVersion = "1.0.0",
            OperatingSystem = "Linux",
            RuntimeVersion = ".NET 10.0",
            TotalDurationMs = 0,
            Summary = new ReportSummary(),
            Groups = [],
        });

        html.ShouldNotContain("SAMPLE_DATA_BEGIN");
        html.ShouldNotContain("SAMPLE_DATA_END");
        html.ShouldNotContain("function generateSampleData()");
        // Dev-machine / preview-only values must never leak into a shipped report.
        // These live outside the SAMPLE_DATA markers and used to be hardcoded into
        // the meta-strip; the JS now populates the strip from REPORT data at runtime.
        html.ShouldNotContain("CloudShop");
        html.ShouldNotContain("runnervmrw5os");
        html.ShouldNotContain("Ubuntu 24.04.4 LTS");
        html.ShouldNotContain("feat/test-categories-demo");
        html.ShouldNotContain("3e5d27d");
        html.ShouldNotContain("#5945");
    }

    [Test]
    public void GenerateHtml_SubstitutesTitleAndProjectPlaceholders()
    {
        // If the placeholders ever drift between template and generator the report
        // would ship literal "__REPORT_TITLE__" / "__REPORT_PROJECT__" — pin them.
        var html = HtmlReportGenerator.GenerateHtml(new ReportData
        {
            AssemblyName = "MyProject.Tests",
            MachineName = "machine",
            Timestamp = "2026-05-07T09:26:24.0000000Z",
            TUnitVersion = "1.0.0",
            OperatingSystem = "Linux",
            RuntimeVersion = ".NET 10.0",
            TotalDurationMs = 0,
            Summary = new ReportSummary(),
            Groups = [],
        });

        html.ShouldNotContain("__REPORT_TITLE__");
        html.ShouldNotContain("__REPORT_PROJECT__");
        html.ShouldContain("<title>Test Report — MyProject.Tests</title>");
        html.ShouldContain("id=\"projectName\">MyProject.Tests<");
    }

    [Test]
    public void GenerateHtml_EmitsAttemptsArray_WhenTestWasRetried()
    {
        // Per-attempt status/duration drives the renderer's flaky panel + per-test
        // Attempts strip. Pin that the array survives serialisation when present.
        var html = HtmlReportGenerator.GenerateHtml(new ReportData
        {
            AssemblyName = "Tests",
            MachineName = "machine",
            Timestamp = "2026-05-07T09:26:24.0000000Z",
            TUnitVersion = "1.0.0",
            OperatingSystem = "Linux",
            RuntimeVersion = ".NET 10.0",
            TotalDurationMs = 0,
            Summary = new ReportSummary(),
            Groups =
            [
                new ReportTestGroup
                {
                    ClassName = "FlakyTests",
                    Namespace = "Sample",
                    Summary = new ReportSummary(),
                    Tests =
                    [
                        new ReportTestResult
                        {
                            Id = "t1", DisplayName = "t1", MethodName = "t1",
                            ClassName = "FlakyTests", Status = "passed",
                            RetryAttempt = 1,
                            Attempts =
                            [
                                new ReportAttempt { Status = "failed", DurationMs = 120, ExceptionType = "System.TimeoutException", ExceptionMessage = "transient" },
                                new ReportAttempt { Status = "passed", DurationMs = 200 },
                            ],
                        },
                    ],
                },
            ],
        });

        var embedded = ExtractEmbeddedReportJson(html);
        embedded.ShouldContain("\"attempts\":[");
        embedded.ShouldContain("\"status\":\"fail\"");
        embedded.ShouldContain("\"status\":\"pass\"");
        embedded.ShouldContain("System.TimeoutException");
    }

    [Test]
    public void FilterEngineNotices_StripsTUnitPrefixedLines()
    {
        // Engine-emitted advisories ("[TUnit] External span cap reached…") are written
        // to Console.Error and get captured into the running test's stderr. Pin that
        // the strip removes prefix-matched lines but leaves user output alone.
        var stderr = "user error before\n[TUnit] External span cap of 100 reached; subsequent spans will be dropped.\nuser error after\nincidental [TUnit] mention in middle";
        var filtered = HtmlReportGenerator.FilterEngineNotices(stderr);
        filtered.ShouldBe("user error before\nuser error after\nincidental [TUnit] mention in middle");
    }

    [Test]
    public void FilterEngineNotices_ReturnsNull_WhenAllLinesStripped()
    {
        // When the test only ever wrote engine-advisory lines, the cleaned stderr
        // becomes null so the upstream "?? SkipReason ?? string.Empty" fallback fires
        // instead of an empty error block.
        var stderr = "[TUnit] notice one\n[TUnit] notice two";
        HtmlReportGenerator.FilterEngineNotices(stderr).ShouldBeNull();
    }

    [Test]
    public void FilterEngineNotices_PassesThroughWhenNoTUnitPrefix()
    {
        // Fast-path: no '[TUnit]' substring at all means we return the input unchanged.
        var stderr = "plain stderr with no engine notices";
        HtmlReportGenerator.FilterEngineNotices(stderr).ShouldBe(stderr);
    }

    private static ReportTestResult CreateTestResultWithStartTime(string displayName, string? startTime) => new()
    {
        Id = displayName,
        DisplayName = displayName,
        MethodName = displayName,
        ClassName = "SampleTests",
        Status = "passed",
        DurationMs = 1,
        StartTime = startTime,
        EndTime = startTime,
        RetryAttempt = 0,
    };

    [Test]
    public void GitHubSourceLink_Strips_Workspace_Prefix()
    {
        var relative = GitHubSourceLink.ToRepoRelativePath(
            @"C:\actions-runner\_work\TUnit\TUnit\src\Tests\SampleTests.cs",
            workspace: "C:/actions-runner/_work/TUnit/TUnit",
            repo: "thomhurst/TUnit");

        relative.ShouldBe("src/Tests/SampleTests.cs");
    }

    [Test]
    public void GitHubSourceLink_Strips_Workspace_Prefix_When_Workspace_Has_Backslashes()
    {
        // Workspace is passed in with Windows backslashes (un-normalized); the method
        // must normalize it internally so the prefix still matches.
        var relative = GitHubSourceLink.ToRepoRelativePath(
            @"C:\actions-runner\_work\TUnit\TUnit\src\Tests\SampleTests.cs",
            workspace: @"C:\actions-runner\_work\TUnit\TUnit",
            repo: "thomhurst/TUnit");

        relative.ShouldBe("src/Tests/SampleTests.cs");
    }

    [Test]
    public void GitHubSourceLink_Falls_Back_To_Repo_Name_When_No_Workspace()
    {
        // No workspace given; locate the repo name segment within the path instead.
        var relative = GitHubSourceLink.ToRepoRelativePath(
            "/home/user/code/TUnit/src/Tests/SampleTests.cs",
            workspace: null,
            repo: "thomhurst/TUnit");

        relative.ShouldBe("src/Tests/SampleTests.cs");
    }

    [Test]
    [Arguments(null, "owner/repo")]   // no file path
    [Arguments("/some/unrelated/path/File.cs", "owner/repo")] // repo name not in path, no workspace
    [Arguments("/x/repo/File.cs", null)] // no repo slug
    public void GitHubSourceLink_Returns_Null_When_Unresolvable(string? filePath, string? repo)
    {
        GitHubSourceLink.ToRepoRelativePath(filePath, workspace: null, repo: repo).ShouldBeNull();
    }

    [Test]
    public void ExtractTestResult_Populates_EndLineNumber_When_Span_Spans_Multiple_Lines()
    {
        var node = new TestNode
        {
            Uid = new TestNodeUid("src-1"),
            DisplayName = "Test",
            Properties = new PropertyBag(
                PassedTestNodeStateProperty.CachedInstance,
                new TestFileLocationProperty(
                    @"C:\repo\SampleTests.cs",
                    new LinePositionSpan(new LinePosition(12, 5), new LinePosition(20, 6))))
        };

        var result = HtmlReporter.ExtractTestResult("src-1", node, traceId: null, spanId: null, retryAttempt: 0, additionalTraceIds: null);

        result.LineNumber.ShouldBe(12);
        result.EndLineNumber.ShouldBe(20);
    }

    [Test]
    public void ExtractTestResult_Omits_EndLineNumber_When_End_Equals_Start()
    {
        // Reflection mode has no method end line, so the span collapses to a single line;
        // we emit null rather than a redundant endLine so the client falls back to a window.
        var node = new TestNode
        {
            Uid = new TestNodeUid("src-2"),
            DisplayName = "Test",
            Properties = new PropertyBag(
                PassedTestNodeStateProperty.CachedInstance,
                new TestFileLocationProperty(
                    @"C:\repo\SampleTests.cs",
                    new LinePositionSpan(new LinePosition(12, 0), new LinePosition(12, 0))))
        };

        var result = HtmlReporter.ExtractTestResult("src-2", node, traceId: null, spanId: null, retryAttempt: 0, additionalTraceIds: null);

        result.LineNumber.ShouldBe(12);
        result.EndLineNumber.ShouldBeNull();
    }

    [Test]
    public void GenerateHtml_RoundTrips_SourceLinks_And_Source_EndLine_And_RelativePath()
    {
        var html = HtmlReportGenerator.GenerateHtml(new ReportData
        {
            AssemblyName = "Tests",
            MachineName = "machine",
            Timestamp = "2026-05-07T09:26:24.0000000Z",
            TUnitVersion = "1.0.0",
            OperatingSystem = "Linux",
            RuntimeVersion = ".NET 10.0",
            TotalDurationMs = 0,
            Summary = new ReportSummary(),
            SourceLinks = new SourceLinkTemplates(
                "https://github.com/o/r/blob/sha/{path}#L{line}",
                "https://github.com/o/r/blob/sha/{path}#L{start}-L{end}",
                "https://raw.githubusercontent.com/o/r/sha/{path}"),
            Groups =
            [
                new ReportTestGroup
                {
                    ClassName = "SampleTests",
                    Namespace = "Tests",
                    Summary = new ReportSummary(),
                    Tests =
                    [
                        new ReportTestResult
                        {
                            Id = "t1", DisplayName = "t1", MethodName = "t1",
                            ClassName = "SampleTests", Status = "passed",
                            FilePath = @"C:\repo\src\SampleTests.cs",
                            LineNumber = 12,
                            EndLineNumber = 20,
                            SourceRelativePath = "src/SampleTests.cs",
                        },
                    ],
                },
            ],
        });

        var embedded = ExtractEmbeddedReportJson(html);
        embedded.ShouldContain("\"sourceLinks\":{");
        embedded.ShouldContain("\"lineUrl\":\"https://github.com/o/r/blob/sha/{path}#L{line}\"");
        embedded.ShouldContain("\"rawUrl\":\"https://raw.githubusercontent.com/o/r/sha/{path}\"");
        embedded.ShouldContain("\"endLine\":20");
        embedded.ShouldContain("\"relativePath\":\"src/SampleTests.cs\"");
    }

    [Test]
    public void SourceControlContext_GitHub_Builds_Blob_And_Raw_Templates()
    {
        var env = new Dictionary<string, string?>
        {
            ["GITHUB_ACTIONS"] = "true",
            ["GITHUB_SERVER_URL"] = "https://github.com",
            ["GITHUB_REPOSITORY"] = "thomhurst/TUnit",
            ["GITHUB_SHA"] = "abc123",
            ["GITHUB_WORKSPACE"] = "/work/TUnit",
        };

        var ctx = SourceControlContext.Detect(k => env.GetValueOrDefault(k));

        ctx.RepositorySlug.ShouldBe("thomhurst/TUnit");
        ctx.Links.ShouldNotBeNull();
        ctx.Links!.LineUrl.ShouldBe("https://github.com/thomhurst/TUnit/blob/abc123/{path}#L{line}");
        ctx.Links.RangeUrl.ShouldBe("https://github.com/thomhurst/TUnit/blob/abc123/{path}#L{start}-L{end}");
        // github.com raw is CORS-enabled, so a snippet template is provided.
        ctx.Links.RawUrl.ShouldBe("https://raw.githubusercontent.com/thomhurst/TUnit/abc123/{path}");
    }

    [Test]
    public void SourceControlContext_GitHubEnterprise_Omits_Raw_Template()
    {
        var env = new Dictionary<string, string?>
        {
            ["GITHUB_ACTIONS"] = "true",
            ["GITHUB_SERVER_URL"] = "https://github.acme.corp",
            ["GITHUB_REPOSITORY"] = "team/app",
            ["GITHUB_SHA"] = "deadbeef",
        };

        var ctx = SourceControlContext.Detect(k => env.GetValueOrDefault(k));

        ctx.Links.ShouldNotBeNull();
        ctx.Links!.LineUrl.ShouldBe("https://github.acme.corp/team/app/blob/deadbeef/{path}#L{line}");
        // Enterprise raw host CORS is unknown — link only, no inline snippet.
        ctx.Links.RawUrl.ShouldBeNull();
    }

    [Test]
    public void SourceControlContext_GitLab_Is_Link_Only()
    {
        var env = new Dictionary<string, string?>
        {
            ["GITLAB_CI"] = "true",
            ["CI_SERVER_URL"] = "https://gitlab.com",
            ["CI_PROJECT_PATH"] = "group/proj",
            ["CI_COMMIT_SHA"] = "f00d",
        };

        var ctx = SourceControlContext.Detect(k => env.GetValueOrDefault(k));

        ctx.Links.ShouldNotBeNull();
        ctx.Links!.LineUrl.ShouldBe("https://gitlab.com/group/proj/-/blob/f00d/{path}#L{line}");
        ctx.Links.RangeUrl.ShouldBe("https://gitlab.com/group/proj/-/blob/f00d/{path}#L{start}-{end}");
        // GitLab raw sends no CORS header, so inline snippets are not supported.
        ctx.Links.RawUrl.ShouldBeNull();
    }

    [Test]
    public void SourceControlContext_Bitbucket_Supports_Snippet()
    {
        var env = new Dictionary<string, string?>
        {
            ["BITBUCKET_BUILD_NUMBER"] = "42",
            ["BITBUCKET_REPO_FULL_NAME"] = "team/repo",
            ["BITBUCKET_COMMIT"] = "cafe",
        };

        var ctx = SourceControlContext.Detect(k => env.GetValueOrDefault(k));

        ctx.Links.ShouldNotBeNull();
        ctx.Links!.LineUrl.ShouldBe("https://bitbucket.org/team/repo/src/cafe/{path}#lines-{line}");
        ctx.Links.RangeUrl.ShouldBe("https://bitbucket.org/team/repo/src/cafe/{path}#lines-{start}:{end}");
        ctx.Links.RawUrl.ShouldBe("https://bitbucket.org/team/repo/raw/cafe/{path}");
    }

    [Test]
    public void SourceControlContext_NoCi_Is_Empty()
    {
        var ctx = SourceControlContext.Detect(_ => null);

        ctx.ShouldBe(SourceControlContext.Empty);
        ctx.Links.ShouldBeNull();
    }
}
