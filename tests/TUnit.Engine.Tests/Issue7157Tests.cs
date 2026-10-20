using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A non-shared class data source creates one instance per test, and each is disposed with its test.
/// The fetch the engine makes before it evaluates the method data sources no longer creates an extra
/// instance that nothing disposes (#7157).
/// </summary>
public class Issue7157Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Every_Non_Shared_Class_Data_Instance_Belongs_To_A_Test_And_Is_Disposed()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7157/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(13),
                result => result.ResultSummary.Counters.Passed.ShouldBe(13),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
