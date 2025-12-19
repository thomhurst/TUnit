using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// Tests that exceptions thrown during data source initialization are properly propagated
/// and cause tests to fail with appropriate error messages.
/// See: https://github.com/thomhurst/TUnit/issues/4049
/// </summary>
public class DataSourceExceptionPropagationTests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task NestedInitializer_PropertyAccessFailure_FailsTestWithDataSourceException()
    {
        await RunTestsWithFilter(
            "/*/*/NestedInitializerExceptionPropagationTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result =>
                {
                    var errorMessage = result.Results.First().Output?.ErrorInfo?.Message;
                    errorMessage.ShouldNotBeNull("Expected an error message");
                    // Should identify the failing property
                    errorMessage.ShouldContain("Failed to access property 'NestedInitializer'");
                    // Should identify the type containing the failing property
                    errorMessage.ShouldContain("FailingNestedInitializerFactory");
                    // Should indicate when the failure occurred
                    errorMessage.ShouldContain("during object graph discovery");
                }
            ]);
    }

    [Test]
    public async Task Initializer_InitializeAsyncFailure_FailsTestWithException()
    {
        await RunTestsWithFilter(
            "/*/*/InitializerExceptionPropagationTests/*",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Failed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(0),
                result => result.ResultSummary.Counters.Failed.ShouldBe(1),
                result =>
                {
                    var errorMessage = result.Results.First().Output?.ErrorInfo?.Message;
                    errorMessage.ShouldNotBeNull("Expected an error message");
                    // Should contain the original exception message
                    errorMessage.ShouldContain("Simulated initialization failure");
                }
            ]);
    }
}
