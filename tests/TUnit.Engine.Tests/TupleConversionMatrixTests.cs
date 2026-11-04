using Shouldly;
using TUnit.Engine.Tests.Enums;
using TUnit.Tests.Shared;

namespace TUnit.Engine.Tests;

public class TupleConversionMatrixTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Binding()
    {
        await AssertBinding(generatedJit: false);

        // The reflection pipeline already builds the JIT executable. Also run it without
        // --reflection so source-generated binding has an independent regression check.
        if (testMode == TestMode.Reflection)
        {
            await AssertBinding(generatedJit: true);
        }
    }

    private async Task AssertBinding(bool generatedJit)
    {
        // Run the entire matrix in one child process per mode, rather than starting a host per case.
        List<Action<TrxTools.TrxParser.TestRun>> assertions =
        [
            result => result.ResultSummary.Counters.Total.ShouldBe(TupleConversionMatrix.Cases.Length * 2),
            result =>
            {
                foreach (var row in TupleConversionMatrix.Cases)
                {
                    var binds = generatedJit ? row.Binds
                        : testMode == TestMode.AOT ? row.BindsAot
                        : row.BindsReflection ?? row.Binds;
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
        ];

        const string filter = "/*/TUnit.TestProject.TupleConversions/*/*";
        if (generatedJit)
        {
            await RunTestsWithSourceGeneration(filter, assertions);
        }
        else
        {
            await RunTestsWithFilter(filter, assertions);
        }
    }
}
