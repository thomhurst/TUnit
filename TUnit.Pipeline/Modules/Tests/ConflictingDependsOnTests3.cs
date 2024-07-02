using FluentAssertions;
using ModularPipelines.Context;

namespace TUnit.Pipeline.Modules.Tests;

public class ConflictingDependsOnTests3 : TestModule
{
    protected override async Task<TestResult?> ExecuteAsync(IPipelineContext context, CancellationToken cancellationToken)
    {
        return await RunTestsWithFilter(context, 
            "/*/*/ConflictingDependsOnTests3/*",
            [
                result => result.Successful.Should().BeFalse(),
                result => result.Total.Should().Be(5),
                result => result.Passed.Should().Be(0),
                result => result.Failed.Should().Be(5),
                result => result.Skipped.Should().Be(0),
                result => result.TrxReport.UnitTestResults.First(x => x.TestName == "Test1").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test1 > Test5 > Test4 > Test3 > Test2 > Test1"),
                result => result.TrxReport.UnitTestResults.First(x => x.TestName == "Test2").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test2 > Test1 > Test5 > Test4 > Test3 > Test2"),
                result => result.TrxReport.UnitTestResults.First(x => x.TestName == "Test3").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test3 > Test2 > Test1 > Test5 > Test4 > Test3"),
                result => result.TrxReport.UnitTestResults.First(x => x.TestName == "Test4").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test4 > Test3 > Test2 > Test1 > Test5 > Test4"),
                result => result.TrxReport.UnitTestResults.First(x => x.TestName == "Test5").Output?.ErrorInfo?.Message.Should().Contain("DependsOn Conflict: Test5 > Test4 > Test3 > Test2 > Test1 > Test5"),

            ]);
    }
}