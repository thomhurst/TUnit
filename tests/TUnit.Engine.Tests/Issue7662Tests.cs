using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7662Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("InheritedOnly", 1)]
    [Arguments("InheritedAndDeclared", 2)]
    [Arguments("WithoutInheritance", 1)]
    [Arguments("HidesInheritedTest", 3)]
    public async Task Generic_Classes_Honor_InheritsTests(string className, int expectedCount)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7662/{className}*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedCount),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedCount),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
