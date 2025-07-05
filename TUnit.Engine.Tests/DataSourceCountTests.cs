using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class DataSourceCountTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task MultipleDataSourcesTests_ShouldHaveFourTestInstances()
    {
        await RunTestsWithFilter(
            "/*/*/MultipleDataSourcesTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
    
    [Test]
    public async Task MultipleClassDataGeneratorsTests_ShouldHaveTwoTestInstances()
    {
        await RunTestsWithFilter(
            "/*/*/MultipleClassDataGeneratorsTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
    
    [Test]
    public async Task ArgumentsWithClassDataSourceTests_ShouldHaveFourTestInstances()
    {
        await RunTestsWithFilter(
            "/*/*/ArgumentsWithClassDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
    
    [Test]
    public async Task MethodDataSourceWithArgumentsTests_ShouldHaveFiveTestInstances()
    {
        await RunTestsWithFilter(
            "/*/*/MethodDataSourceWithArgumentsTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(5),
                result => result.ResultSummary.Counters.Passed.ShouldBe(5),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
    
    [Test]
    public async Task ClassDataSourceWithMethodDataSourceTests_ShouldHaveSixTestInstances()
    {
        await RunTestsWithFilter(
            "/*/*/ClassDataSourceWithMethodDataSourceTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(6),
                result => result.ResultSummary.Counters.Passed.ShouldBe(6),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
    
    [Test]
    public async Task AllDataSourcesCombinedTests_ShouldHaveTwentyTestInstances()
    {
        await RunTestsWithFilter(
            "/*/*/AllDataSourcesCombinedTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(20),
                result => result.ResultSummary.Counters.Passed.ShouldBe(20),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}