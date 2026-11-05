using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7639Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("ExplicitSoleTupleInferenceTests", 7)]
    [Arguments("RepeatedConvertibleTupleTests", 2)]
    [Arguments("RepeatedNullableTupleTests", 6)]
    [Arguments("RepeatedNestedGenericTupleTests", 3)]
    [Arguments("ContainingGenericTypeInferenceTests", 2)]
    [Arguments("SingleTupleElementConversionTests", 18)]
    [Arguments("CrossFamilySoleTupleInferenceTests", 3)]
    [Arguments("BoxedTupleElementConversionTests", 2)]
    [Arguments("TupleInterfaceCandidateIsolationTests", 2)]
    [Arguments("NestedTupleCandidateBindingTests", 4)]
    [Arguments("TupleNestedInterfaceBaseTests", 2)]
    [Arguments("TupleMultiParameterCandidateTests", 2)]
    [Arguments("TupleClosedCandidateArgumentTests", 2)]
    [Arguments("TupleVariantCandidateTests", 4)]
    [Arguments("TupleInvariantBaseCandidateTests", 2)]
    [Arguments("TupleConstrainedCandidateTests", 2)]
    [Arguments("NestedSingletonBoundaryTests", 2)]
    [Arguments("WrappedNestedSingletonInferenceTests", 2)]
    [Arguments("SingletonTupleInferenceTests", 3)]
    [Arguments("OpenTupleRuntimeCastTests", 7)]
    [Arguments("OpenTupleArrayRuntimeCastTests", 4)]
    public async Task Tuple_Inference_And_Conversion_Agree_In_Both_Modes(string className, int expectedCount)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7639/{className}/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedCount),
                result => result.ResultSummary.Counters.Passed.ShouldBe(expectedCount),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    [Arguments("AmbiguousTupleInterfaceInferenceTests", 2)]
    [Arguments("OrdinaryGenericBaseInferenceTests", 1)]
    [Arguments("SpreadCrossFamilyTupleInferenceTests", 1)]
    [Arguments("SpreadMatchingFamilyTupleInferenceTests", 1)]
    public async Task Unsupported_Inference_Remains_Consistent_In_Both_Modes(string className, int expectedCount)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7639/{className}/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(expectedCount),
                result => result.ResultSummary.Counters.Failed.ShouldBe(expectedCount)
            ]);
    }
}
