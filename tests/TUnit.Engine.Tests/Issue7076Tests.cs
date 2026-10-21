using System.Reflection;
using Shouldly;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Discovery;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

public class Issue7076Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Arguments_Skip_Only_Skips_Its_Own_Row()
    {
        await RunTestsWithFilter(
            "/*/*/SkippableArgumentTests/Test1",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(3),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task Arguments_DisplayName_Only_Applies_To_Its_Own_Row()
    {
        await RunTestsWithFilter(
            "/*/*/ArgumentsRowSkipTests/DisplayNameOnlyAppliesToItsOwnRow",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public async Task ClassArguments_Skip_Only_Skips_Its_Own_Row()
    {
        await RunTestsWithFilter(
            "/*/*/ClassArgumentsRowSkipTests/OnlyMarkedClassRowIsSkipped",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task InheritedArguments_Skip_Only_Skips_Its_Own_Row()
    {
        await RunTestsWithFilter(
            "/*/*/InheritedArgumentsRowSkipTests/OnlyMarkedInheritedRowIsSkipped",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(1)
            ]);
    }

    [Test]
    public async Task ClassConstructor_Uses_Method_DataSource()
    {
        await RunTestsWithFilter(
            "/*/*/ClassConstructorDataSourceTest/MethodDataSourceIsPassedToClassConstructor",
            [
                result => result.ResultSummary.Outcome.ShouldBe("Completed"),
                result => result.ResultSummary.Counters.Total.ShouldBe(1),
                result => result.ResultSummary.Counters.Passed.ShouldBe(1),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0),
                result => result.ResultSummary.Counters.NotExecuted.ShouldBe(0)
            ]);
    }

    [Test]
    public Task Failed_Metadata_Filters_Method_DataSources()
    {
        var method = typeof(FailedMetadataDataSourceTest).GetMethod(nameof(FailedMetadataDataSourceTest.Test))!;
        var collectorType = typeof(ReflectionTestDataCollector);
        var exception = new InvalidOperationException("discovery failed");
        var factories = new (string Name, object?[] Arguments)[]
        {
            ("CreateFailedTestMetadata", [typeof(FailedMetadataDataSourceTest), method, exception]),
            ("CreateFailedMethodGenericMetadata", [typeof(FailedMetadataDataSourceTest), method, "generic resolution failed"]),
            ("CreateFailedTestMetadataForDynamicBuilder", [typeof(FailedMetadataDataSourceTest), method, exception])
        };

        foreach (var (name, arguments) in factories)
        {
            var factory = collectorType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
            factory.ShouldNotBeNull(name);
            var metadata = (TestMetadata)factory.Invoke(null, arguments)!;
            metadata.AttributeFactory().OfType<IDataSourceAttribute>().ShouldBeEmpty(name);
        }

        return Task.CompletedTask;
    }

    private sealed class FailedMetadataDataSourceTest
    {
        [Arguments(1)]
        [Arguments(2, Skip = "wip")]
        public static void Test(int value)
        {
        }
    }
}
