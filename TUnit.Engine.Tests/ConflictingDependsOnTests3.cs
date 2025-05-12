using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class ConflictingDependsOnTests3(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Test()
    {
        await RunTestsWithFilter(
            "/*/*/ConflictingDependsOnTests3/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(5),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(5),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0),
                result => result.Results.First(x => x.TestName == "Test1").Output?.ErrorInfo?.Message.ShouldContain("DependsOn Conflict: ConflictingDependsOnTests3.Test1 > ConflictingDependsOnTests3.Test5 > ConflictingDependsOnTests3.Test4 > ConflictingDependsOnTests3.Test3 > ConflictingDependsOnTests3.Test2 > ConflictingDependsOnTests3.Test1"),
                result => result.Results.First(x => x.TestName == "Test2").Output?.ErrorInfo?.Message.ShouldContain("DependsOn Conflict: ConflictingDependsOnTests3.Test2 > ConflictingDependsOnTests3.Test1 > ConflictingDependsOnTests3.Test5 > ConflictingDependsOnTests3.Test4 > ConflictingDependsOnTests3.Test3 > ConflictingDependsOnTests3.Test2"),
                result => result.Results.First(x => x.TestName == "Test3").Output?.ErrorInfo?.Message.ShouldContain("DependsOn Conflict: ConflictingDependsOnTests3.Test3 > ConflictingDependsOnTests3.Test2 > ConflictingDependsOnTests3.Test1 > ConflictingDependsOnTests3.Test5 > ConflictingDependsOnTests3.Test4 > ConflictingDependsOnTests3.Test3"),
                result => result.Results.First(x => x.TestName == "Test4").Output?.ErrorInfo?.Message.ShouldContain("DependsOn Conflict: ConflictingDependsOnTests3.Test4 > ConflictingDependsOnTests3.Test3 > ConflictingDependsOnTests3.Test2 > ConflictingDependsOnTests3.Test1 > ConflictingDependsOnTests3.Test5 > ConflictingDependsOnTests3.Test4"),
                result => result.Results.First(x => x.TestName == "Test5").Output?.ErrorInfo?.Message.ShouldContain("DependsOn Conflict: ConflictingDependsOnTests3.Test5 > ConflictingDependsOnTests3.Test4 > ConflictingDependsOnTests3.Test3 > ConflictingDependsOnTests3.Test2 > ConflictingDependsOnTests3.Test1 > ConflictingDependsOnTests3.Test5"),
            ]);
    }
}