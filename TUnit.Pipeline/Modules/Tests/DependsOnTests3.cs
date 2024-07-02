using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Pipeline.Modules.Tests;

public class DependsOnTests3 : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/DependsOnTests3/*",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(3),
                result => result.Passed.Should().Be(3),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0),
                result =>
                {
                    var test1Start = result.TrxReport.UnitTestResults.First(x => x.TestName!.StartsWith("Test1")).StartTime!.Value;
                    var test2Start = result.TrxReport.UnitTestResults.First(x => x.TestName!.StartsWith("Test2")).StartTime!.Value;
                    var test3Start = result.TrxReport.UnitTestResults.First(x => x.TestName!.StartsWith("Test3")).StartTime!.Value;

                    test3Start.Should().BeOnOrAfter(test1Start.AddSeconds(1));
                    test3Start.Should().BeOnOrAfter(test2Start.AddSeconds(1));

                } 
            ], cancellationToken);
    }
}