using FluentAssertions;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Parsers.NUnitTrx;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;

namespace TUnit.Pipeline.Modules.Tests;

public class ConflictingDependsOnTests : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        var file = Guid.NewGuid().ToString("N") + ".trx";
        
        return await RunTestsWithFilter(context, 
            "/*/*/ConflictingDependsOnTests/*",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(2),
                result => result.Passed.Should().Be(0),
                result => result.Failed.Should().Be(2),
                result => result.Skipped.Should().Be(0),
                result => result.TrxReport.UnitTestResults.First(x => x.TestName == "Test1").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test1 &gt; Test2 &gt; Test1"),
                result => result.TrxReport.UnitTestResults.First(x => x.TestName == "Test2").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test2 &gt; Test1 &gt; Test2"),

            ], cancellationToken);
    }
}