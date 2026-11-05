using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7629Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("NestedTupleWithTrailingParametersTests", 4)]
    [Arguments("ClosedNestedTupleConversionTests", 1)]
    [Arguments("RepeatedTupleBindingTests", 3)]
    [Arguments("RepeatedConvertibleTupleTests", 2)]
    [Arguments("RepeatedNullableTupleTests", 6)]
    [Arguments("RepeatedNestedGenericTupleTests", 3)]
    [Arguments("OpenTupleAssignabilityTests", 3)]
    [Arguments("SingleTupleElementConversionTests", 18)]
    [Arguments("BoxedTupleElementConversionTests", 2)]
    [Arguments("WholeReferenceTupleSourceTests", 1)]
    [Arguments("InferredReferenceTupleSourceTests", 1)]
    [Arguments("TupleInterfaceCandidateIsolationTests", 2)]
    [Arguments("TupleNestedInterfaceBaseTests", 2)]
    [Arguments("TupleMultiParameterCandidateTests", 2)]
    [Arguments("TupleClosedCandidateArgumentTests", 2)]
    [Arguments("TupleVariantCandidateTests", 4)]
    [Arguments("TupleInvariantBaseCandidateTests", 2)]
    [Arguments("TupleConstrainedCandidateTests", 2)]
    [Arguments("NestedSingletonBoundaryTests", 2)]
    [Arguments("WrappedNestedSingletonInferenceTests", 2)]
    [Arguments("WrappedWholeTupleInferenceTests", 6)]
    [Arguments("SingletonTupleInferenceTests", 3)]
    [Arguments("OpenTupleRuntimeCastTests", 3)]
    [Arguments("WholeTupleWithTrailingParametersTests", 6)]
    public async Task Tuple_Inference_And_Conversion_Agree_In_Both_Modes(string className, int expectedCount)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7629/{className}/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedCount),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedCount),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    [Arguments("OrdinaryGenericBaseInferenceTests")]
    [Arguments("SpreadCrossFamilyTupleInferenceTests")]
    [Arguments("SpreadMatchingFamilyTupleInferenceTests")]
    public async Task Unsupported_Inference_Remains_Consistent_In_Both_Modes(string className)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7629/{className}/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1)
            ]);
    }
}
