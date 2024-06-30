using FluentAssertions;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Parsers.NUnitTrx;
using ModularPipelines.Extensions;
using ModularPipelines.Git.Extensions;

namespace TUnit.Pipeline.Modules.Tests;

public class ConflictingDependsOnTests2 : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/ConflictingDependsOnTests2/*",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(3),
                result => result.Passed.Should().Be(0),
                result => result.Failed.Should().Be(3),
                result => result.Skipped.Should().Be(0),
                result => result.TrxReport.UnitTestResults.First(x => x.TestName == "Test1").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test1 &gt; Test3 &gt; Test2 &gt; Test1"),
                result => result.TrxReport.UnitTestResults.First(x => x.TestName == "Test2").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test2 &gt; Test1 &gt; Test3 &gt; Test2"),
                result => result.TrxReport.UnitTestResults.First(x => x.TestName == "Test3").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test3 &gt; Test2 &gt; Test1 &gt; Test3"),
            ], cancellationToken);
    }
}