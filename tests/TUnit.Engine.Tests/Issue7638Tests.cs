using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7638Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("NestedTupleWithTrailingParametersTests", 4, 0)]
    [Arguments("ClosedNestedTupleConversionTests", 1, 0)]
    [Arguments("RepeatedTupleBindingTests", 3, 0)]
    [Arguments("WholeReferenceTupleSourceTests", 1, 0)]
    [Arguments("InferredReferenceTupleSourceTests", 1, 0)]
    [Arguments("WholeTupleWithTrailingParametersTests", 6, 0)]
    [Arguments("WrappedWholeTupleInferenceTests", 6, 0)]
    [Arguments("ExplicitWholeTupleInferenceTests", 8, 3)]
    [Arguments("ExplicitBareReferenceTupleInferenceTests", 1, 0)]
    [Arguments("ExplicitDirectTupleInferenceTests", 4, 0)]
    [Arguments("ExplicitWrappedTupleInferenceTests", 6, 3)]
    [Arguments("ExplicitSpreadColumnInferenceTests", 2, 0)]
    [Arguments("DerivedRowWholeTupleInferenceTests", 2, 0)]
    [Arguments("SingletonWholeTupleInferenceTests", 2, 0)]
    [Arguments("AsyncWholeTupleInferenceTests", 6, 3)]
    public async Task Whole_Tuple_Discovery_And_Failures_Agree(string className, int total, int failed)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7638/{className}/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(total),
                result => result.ResultSummary.Counters.Passed.ShouldBe(total - failed),
                result => result.ResultSummary.Counters.Failed.ShouldBe(failed)
            ]);
    }
}
