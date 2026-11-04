using Shouldly;
using TUnit.Engine.Tests.Enums;
using TUnit.Tests.Shared;

namespace TUnit.Engine.Tests;

public class TupleConversionMatrixTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Binding()
    {
        // Run the entire matrix in one child process per mode, rather than starting a host per case.
        await RunTestsWithFilter("/*/TUnit.TestProject.TupleConversions/*/*",
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(TupleConversionMatrix.Cases.Length * 2),
            result =>
            {
                foreach (var row in TupleConversionMatrix.Cases)
                {
                    var binds = testMode == TestMode.AOT ? row.BindsAot : row.BindsReflection ?? row.Binds;
                    var tests = result.Results.Where(x => x.TestName.StartsWith(row.Name + "_", StringComparison.Ordinal)).ToArray();
                    tests.Length.ShouldBe(2, row.Name);
                    tests.Count(x => x.Outcome == "Passed").ShouldBe(binds ? 2 : 0, row.Name);
                    tests.Count(x => x.Outcome == "Failed").ShouldBe(binds ? 0 : 2, row.Name);
                    // Rejected rows must fail at binding, before the typed body's value assertion.
                    tests.Where(x => x.Outcome == "Failed").ShouldAllBe(x =>
                        x.Output!.ErrorInfo!.Message != null
                        && (x.Output.ErrorInfo.Message.Contains("InvalidCastException")
                            || x.Output.ErrorInfo.Message.Contains("ArgumentException"))
                        && !x.Output.ErrorInfo.Message.Contains("Wrong bound value"));
                }
            }
        ]);
    }
}
