using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A class-level [MethodDataSource] that names a member of the generic test class itself is read on the
/// closed class, in both modes. A class whose types nothing infers fails on its own, without failing the
/// rest of the assembly in reflection mode (#7208).
/// </summary>
public class Issue7208Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    [Arguments("OwnClassMethodDataSource")]
    [Arguments("OwnClassPropertyDataSource")]
    [Arguments("OwnClassTypeParameterMethodDataSourceWithGenerateGenericTest")]
    [Arguments("OwnClassOverloadedMethodDataSource")]
    [Arguments("OwnClassSingleRowMethodDataSource")]
    [Arguments("OwnClassAsyncEnumerableMethodDataSource")]
    [Arguments("OwnClassTestDataRowIntMethodDataSourceWithGenerateGenericTest")]
    [Arguments("OwnClassArrayParameterMethodDataSourceWithGenerateGenericTest")]
    public async Task Instance_Is_Built_From_The_Own_Class_Member(string className)
    {
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7208/{className}/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    [Arguments("OwnClassTypeParameterMethodDataSourceWithArgumentsAndGenerateGenericTest")]
    [Arguments("OwnClassTypeParameterMethodDataSourceWithArgumentsAndGenerateGenericTestParameterless")]
    [Arguments("OwnClassObjectArrayMethodDataSourceWithArgumentsAndGenerateGenericTest")]
    [Arguments("OwnClassObjectTupleMethodDataSourceWithArgumentsAndGenerateGenericTest")]
    [Arguments("OwnClassTestDataRowObjectMethodDataSourceWithArgumentsAndGenerateGenericTest")]
    [Arguments("OwnClassConcreteMethodDataSourceWithArgumentsAndGenerateGenericTest")]
    [Arguments("OwnClassConcreteMethodDataSourceWithArguments")]
    [Arguments("OwnClassPartiallyGenericTupleMethodDataSourceWithArguments")]
    [Arguments("OwnClassTupleMethodDataSourceWithArguments")]
    public async Task Generate_Generic_Test_Instantiation_Inferred_By_Class_Arguments_Reads_The_Own_Class_Member(string className)
    {
        // Both modes run the [Arguments(1)] row and the own-class member's row on the int instantiation,
        // whether or not the test method has rows of its own
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7208/{className}/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Class_Arguments_Instantiation_Reads_A_Matching_Own_Class_Member_With_Method_Rows()
    {
        // Without [GenerateGenericTest]: the [Arguments(1)] row and the own-class member's row each run with
        // both method rows on the int instantiation, in both modes
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7208/OwnClassConcreteMethodDataSourceWithArgumentsAndMethodRows/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Repeated_Generate_Generic_Test_Reads_The_Own_Class_Member_Once()
    {
        // Same counts as with a single [GenerateGenericTest(typeof(int))]: the repeated attribute adds no rows.
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7208/OwnClassTypeParameterMethodDataSourceWithArgumentsAndRepeatedGenerateGenericTest/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    [Arguments("OwnClassMethodDataSourceWithSeveralConstructors")]
    [Arguments("OwnClassNullTestDataRowMethodDataSource")]
    [Arguments("OwnClassMethodDataSourceWithTestConstructor")]
    public async Task Rows_Infer_Types_From_The_Constructor_The_Instance_Is_Built_With(string className)
    {
        if (testMode != TestMode.Reflection)
        {
            await RunTestsWithFilter($"/*/TUnit.TestProject.Bugs._7208/{className}*/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result => result.Results.Single(x => x.Outcome == "Failed").Output?.ErrorInfo?.Message
                    .ShouldContain("cannot be inferred at compile time")
            ]);
            return;
        }

        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7208/{className}/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    [Arguments("OwnClassTupleMethodDataSourceWithTestConstructor")]
    [Arguments("OwnClassReferenceTupleMethodDataSourceWithGenerateGenericTest")]
    [Arguments("OwnClassMethodDataSourceWithTestConstructorAfterObjectConstructor")]
    public async Task Rows_Reach_The_Test_Constructor_Parameters(string className)
    {
        // The class parameter metadata comes from the [TestConstructor] and resolves its ParameterInfo there, and a
        // System.Tuple row spreads over the constructor like a value tuple, so each class runs one passing test in
        // both modes
        await RunTestsWithFilter(
            $"/*/TUnit.TestProject.Bugs._7208/{className}/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Overloads_Told_Apart_By_Arguments_Each_Read_On_Their_Own_Instantiation()
    {
        // Each instantiation runs its [Arguments] row and its own overload's row, not the other overload's, in both modes
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7208/OwnClassOverloadedMethodDataSourcesWithArguments/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task Uninferable_Own_Class_Member_Fails_Only_Its_Class()
    {
        // Source-gen reports inference failures for the four classes whose class arguments cannot be inferred
        // at compile time (#7355). Reflection resolves three at runtime and fails only the self-referencing
        // source. In both modes the remaining classes in the namespace still run.
        var reflection = testMode == TestMode.Reflection;

        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7208/*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(43),
                result => result.ResultSummary.Counters.Passed.ShouldBe(reflection ? 42 : 39),
                result => result.ResultSummary.Counters.Failed.ShouldBe(reflection ? 1 : 4),
                result => result.Results
                    .Where(x => x.Outcome == "Failed")
                    .Select(x => x.Output?.ErrorInfo?.Message)
                    .ShouldAllBe(message => message != null && message.Contains(reflection
                        ? "names a member of the class itself"
                        : "class type arguments cannot be inferred at compile time"))
            ]);
    }
}
