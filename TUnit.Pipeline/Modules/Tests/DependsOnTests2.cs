using FluentAssertions;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Parsers.NUnitTrx;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;
using File = ModularPipelines.FileSystem.File;

namespace TUnit.Pipeline.Modules.Tests;

public class DependsOnTests2 : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow;

        var file = Guid.NewGuid().ToString("N") + ".trx";
        
        await RunTestsWithFilter(context, 
            "/*/*/DependsOnTests2/*",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(2),
                result => result.Passed.Should().Be(0),
                result => result.Failed.Should().Be(3),
                result => result.Skipped.Should().Be(0),
                _ => (DateTime.UtcNow - start).Should().BeLessThan(TimeSpan.FromMinutes(1)),
                _ => (DateTime.UtcNow - start).Should().BeGreaterThan(TimeSpan.FromSeconds(30)),
            ],
            new RunOptions
            {
                AdditionalArguments = ["--report-trx", "--report-trx-filename", file],
            },  cancellationToken);

        var trxReport = new TrxParser().ParseTrxContents(await context.Git().RootDirectory.AssertExists().FindFile(x => x.Name == file).AssertExists().ReadAsync(cancellationToken));

        var test1Start = trxReport.UnitTestResults.First(x => x.TestName == "Test1").StartTime!.Value;
        var test2Start = trxReport.UnitTestResults.First(x => x.TestName == "Test2").StartTime!.Value;

        test2Start.Should().BeOnOrAfter(test1Start.AddSeconds(5));
        
        return null;
    }
}