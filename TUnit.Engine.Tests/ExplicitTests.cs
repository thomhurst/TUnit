using CliWrap;
using CliWrap.Buffered;
using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class ExplicitTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task ExplicitMethodTest_WithWildcardFilter_ShouldExcludeExplicitTests()
    {
        // When filtering with a wildcard that matches both explicit and non-explicit tests,
        // the explicit tests should be excluded
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._2755/Test1/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task ExplicitMethodTest_WithSpecificFilter_ShouldIncludeExplicitTest()
    {
        // When filtering specifically for an explicit test, it should run
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._2755/Test1/TestMethod2",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"), // Test is designed to fail
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task ExplicitClassTest_WithClassFilter_ShouldIncludeAllTestsInExplicitClass()
    {
        // When filtering for an explicit class, all tests in that class should run
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._2755/ExplicitClass/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }


    [Test]
    public async Task MixedClassTest_WithClassWildcard_ShouldExcludeOnlyExplicitMethods()
    {
        // When filtering a class with mixed explicit/non-explicit tests,
        // only non-explicit tests should run
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._2755/MixedTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2), // NormalTest and SkippedTest
                result => result.ResultSummary.Counters.Passed.ShouldBe(1), // NormalTest
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1) // SkippedTest
            ]);
    }

    [Test]
    public async Task MixedClassTest_WithIgnoreExplicitFlag_ShouldIncludeExplicitMethodsAlongsideNormal()
    {
        // With --ignore-explicit, [Explicit] tests participate in the run alongside
        // non-explicit tests. Skip still wins over Explicit.
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._2755/MixedTests/*",
            [
                // ExplicitTestInNormalClass throws, so the run is "Failed" overall
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4), // all four methods
                result => result.ResultSummary.Counters.Passed.ShouldBe(1), // NormalTest
                result => result.ResultSummary.Counters.Failed.ShouldBe(1), // ExplicitTestInNormalClass
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(2) // SkippedTest + ExplicitAndSkippedTest (Skip wins)
            ],
            new RunOptions().WithArgument("--ignore-explicit"));
    }

    [Test]
    public async Task ExplicitClassTest_WithIgnoreExplicitFlag_ShouldRunExplicitClass()
    {
        // With --ignore-explicit, the class-level [Explicit] path in IsExplicitTest
        // (via _explicitClassCache) should also be bypassed so all tests in
        // [Explicit] classes participate in the run.
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._2755/ExplicitClass/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ],
            new RunOptions().WithArgument("--ignore-explicit"));
    }

    [Test]
    public async Task NoFilter_WithIgnoreExplicitFlag_RunsExplicitTests()
    {
        // Exercises the FilterOutExplicitTests fast path: no --treenode-filter is
        // supplied, so MTP feeds a NopFilter to TestFilterService. With
        // --ignore-explicit, every test (explicit or not) should run.
        // Uses the standalone TUnit.TestProject.IgnoreExplicit fixture so we can
        // safely run with no filter. Reflection-only - the standalone fixture
        // isn't published as an AOT executable.
        Skip.When(testMode != TestMode.Reflection, "No-filter scenario only runs in Reflection mode.");

        var testProject = Sourcy.DotNet.Projects.TUnit_TestProject_IgnoreExplicit;
        var guid = Guid.NewGuid().ToString("N");
        var trxFilename = guid + ".trx";

        var command = Cli.Wrap("dotnet")
            .WithArguments(
                [
                    "run",
                    "--no-build",
                    "-f", "net10.0",
                    "--configuration", "Release",
                    "--",
                    "--ignore-explicit",
                    "--report-trx", "--report-trx-filename", trxFilename,
                    "--diagnostic-verbosity", "Debug",
                    "--diagnostic", "--diagnostic-file-prefix", $"log_{GetType().Name}_NoFilter_",
                    "--timeout", "5m"
                ]
            )
            .WithWorkingDirectory(testProject.DirectoryName!)
            .WithValidation(CommandResultValidation.None);

        var result = await command.ExecuteBufferedAsync();

        await TrxAsserter.AssertTrx(TestMode.Reflection, command, result,
            [
                run => run.ResultSummary.Outcome.ShouldBe("Completed"),
                run => run.ResultSummary.Counters.Total.ShouldBe(4),
                run => run.ResultSummary.Counters.Passed.ShouldBe(4),
                run => run.ResultSummary.Counters.Failed.ShouldBe(0),
                run => run.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ],
            trxFilename);
    }

}
