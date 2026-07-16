using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Tests that verify generic methods with [GenerateGenericTest] and [MethodDataSource] attributes
/// are properly discovered and executed.
/// </summary>
public class GenericMethodWithDataSourceTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task NonGenericClassWithGenericMethodAndDataSource_Should_Generate_Tests()
    {
        // This test verifies that a non-generic class with a generic method that has both
        // [GenerateGenericTest(typeof(int))] and [GenerateGenericTest(typeof(double))]
        // combined with [MethodDataSource(nameof(GetStrings))] generates 4 tests:
        // - GenericMethodWithDataSource<int>("hello")
        // - GenericMethodWithDataSource<int>("world")
        // - GenericMethodWithDataSource<double>("hello")
        // - GenericMethodWithDataSource<double>("world")
        await RunTestsWithFilter(
            "/*/*/Bug4440_NonGenericClassWithGenericMethodAndDataSource/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(4),
                result => result.ResultSummary.Counters.Passed.ShouldBe(4),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenericClassWithMethodDataSource_Should_Generate_Tests()
    {
        // Generic class with 2 types (string, object) and data source with 3 items (1, 2, 3)
        // Expected: 2 class types × 3 data items = 6 tests
        // The data source values differentiate the test names, so all 6 are unique.
        await RunTestsWithFilter(
            "/*/*/Bug4440_GenericClassWithMethodDataSource*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(6),
                result => result.ResultSummary.Counters.Passed.ShouldBe(6),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task FullyGenericWithDataSources_Should_Generate_Tests()
    {
        // Cartesian product: 2 class types × 2 method types × 2 data items = 8 tests
        // - Bug4440_GenericClassGenericMethodWithDataSources<string>.CartesianProduct<int>(true)
        // - Bug4440_GenericClassGenericMethodWithDataSources<string>.CartesianProduct<int>(false)
        // - Bug4440_GenericClassGenericMethodWithDataSources<string>.CartesianProduct<double>(true)
        // - Bug4440_GenericClassGenericMethodWithDataSources<string>.CartesianProduct<double>(false)
        // - Bug4440_GenericClassGenericMethodWithDataSources<object>.CartesianProduct<int>(true)
        // - Bug4440_GenericClassGenericMethodWithDataSources<object>.CartesianProduct<int>(false)
        // - Bug4440_GenericClassGenericMethodWithDataSources<object>.CartesianProduct<double>(true)
        // - Bug4440_GenericClassGenericMethodWithDataSources<object>.CartesianProduct<double>(false)
        await RunTestsWithFilter(
            "/*/*/Bug4440_GenericClassGenericMethodWithDataSources*/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(8),
                result => result.ResultSummary.Counters.Passed.ShouldBe(8),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task GenericMethodWithoutDataSource_Should_Work()
    {
        // Generic method with 3 type arguments (int, string, object) but NO data source.
        // Expected: 3 tests with unique names: GenericMethod_Should_Work<Int32>, <String>, <Object>
        await RunTestsWithFilter(
            "/*/*/Bug4440_NonGenericClassWithGenericMethod/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(3),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }
}
