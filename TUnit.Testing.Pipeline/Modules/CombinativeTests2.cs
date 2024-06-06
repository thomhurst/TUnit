using FluentAssertions;
using ModularPipelines.Context;
using ModularPipelines.DotNet;
using ModularPipelines.DotNet.Enums;

namespace TUnit.Testing.Pipeline.Modules;

public class CombinativeTests2 : TestModule
{
    protected override async Task<DotNetTestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/CombinativeTests/CombinativeTest_Two",
            [
                result => result.Successful.Should().BeTrue(),
                result => result.UnitTestResults.Should().HaveCount(48),
                result => result.UnitTestResults.Where(x => x.Outcome == TestOutcome.Passed).Should().HaveCount(48),
                result => result.UnitTestResults.Where(x => x.Outcome == TestOutcome.Failed).Should().HaveCount(0),
                result => result.UnitTestResults.Where(x => x.Outcome == TestOutcome.NotExecuted).Should().HaveCount(0)
            ], cancellationToken);
    }
}