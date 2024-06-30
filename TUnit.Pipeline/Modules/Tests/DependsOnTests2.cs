using System.Text.Json;
using FluentAssertions;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Parsers.NUnitTrx;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;

namespace TUnit.Pipeline.Modules.Tests;

public class DependsOnTests2 : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var file = Guid.NewGuid().ToString("N") + ".trx";
        
        await RunTestsWithFilter(context, 
            "/*/*/DependsOnTests2/*",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.Total.Should().Be(2),
                result => result.Passed.Should().Be(2),
                result => result.Failed.Should().Be(0),
                result => result.Skipped.Should().Be(0),
            ],
            new RunOptions
            {
                AdditionalArguments = ["--report-trx", "--report-trx-filename", file],
            },  cancellationToken);

        var trxReport = new TrxParser().ParseTrxContents(await context.Git().RootDirectory.AssertExists().FindFile(x => x.Name == file).AssertExists().ReadAsync(cancellationToken));

        var test1Start = trxReport.UnitTestResults.FirstOrDefault(x => x.TestName == "Test1")?.StartTime!.Value ?? throw new Exception($"Test1 not found: {JsonSerializer.Serialize(trxReport)}");
        var test2Start = trxReport.UnitTestResults.FirstOrDefault(x => x.TestName == "Test2")?.StartTime!.Value ?? throw new Exception($"Test2 not found: {JsonSerializer.Serialize(trxReport)}");

        test2Start.Should().BeOnOrAfter(test1Start.AddSeconds(5));
        
        return null;
    }
}