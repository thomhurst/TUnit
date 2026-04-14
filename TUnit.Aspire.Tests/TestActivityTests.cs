using System.Diagnostics;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Tests that verify the TUnit engine creates per-test activities correctly:
/// each test gets a unique TraceId (root activity), an ActivityLink back to
/// the class activity, registered baggage, and TraceRegistry entries.
/// </summary>
public class TestActivityTests
{
    [Test]
    public async Task ActivityCurrent_IsNotNull_DuringTestExecution()
    {
        await Assert.That(Activity.Current).IsNotNull();
    }

    [Test]
    public async Task ActivityCurrent_TraceId_IsRegisteredInTraceRegistry()
    {
        var activity = Activity.Current!;
        var traceId = activity.TraceId.ToString();

        await Assert.That(TraceRegistry.IsRegistered(traceId)).IsTrue();
    }

    [Test]
    public async Task ActivityCurrent_HasBaggage_WithTestContextId()
    {
        var activity = Activity.Current!;

        // Baggage traverses the parent chain: test body → test case
        var baggageValue = activity.GetBaggageItem(TUnitActivitySource.TagTestId);

        await Assert.That(baggageValue).IsNotNull();
        await Assert.That(baggageValue).IsEqualTo(TestContext.Current!.Id);
    }

    [Test]
    public async Task ActivityCurrent_HasParent_TestCaseActivity()
    {
        var activity = Activity.Current!;

        // Activity.Current during test execution is the "test body" span.
        // Its parent should be the "test case" span.
        await Assert.That(activity.Parent).IsNotNull();
        await Assert.That(activity.Parent!.DisplayName).IsEqualTo("test case");
    }

    [Test]
    public async Task TestCaseActivity_IsRoot_WithUniqueTraceId()
    {
        var testCase = Activity.Current!.Parent!;

        // The test case activity should be a root (no parent sharing its TraceId),
        // giving each test a unique TraceId for distributed tracing.
        if (testCase.Parent is not null)
        {
            // If there's a parent, it should have a DIFFERENT TraceId (the class's)
            await Assert.That(testCase.Parent.TraceId.ToString())
                .IsNotEqualTo(testCase.TraceId.ToString());
        }
    }

    [Test]
    public async Task TestCaseActivity_HasLinks_ToClassActivity()
    {
        var testCase = Activity.Current!.Parent!;
        var links = testCase.Links.ToList();

        // The engine creates an ActivityLink from test case → class activity
        await Assert.That(links.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task TestCaseActivity_HasExpectedTags()
    {
        var testCase = Activity.Current!.Parent!;

        var testCaseName = testCase.GetTagItem("test.case.name")?.ToString();
        var testClass = testCase.GetTagItem("tunit.test.class")?.ToString();
        var testMethod = testCase.GetTagItem("tunit.test.method")?.ToString();
        var testId = testCase.GetTagItem("tunit.test.id")?.ToString();
        var testNodeUid = testCase.GetTagItem("tunit.test.node_uid")?.ToString();

        await Assert.That(testCaseName).IsNotNull();
        await Assert.That(testClass).Contains(nameof(TestActivityTests));
        await Assert.That(testMethod).IsEqualTo(nameof(TestCaseActivity_HasExpectedTags));
        await Assert.That(testId).IsEqualTo(TestContext.Current!.Id);
        await Assert.That(testNodeUid).IsNotNull();
    }

    /// <summary>
    /// Two tests in the same class must have different TraceIds. This is the core property
    /// that enables per-test distributed tracing correlation.
    /// </summary>
    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    public async Task ParallelTests_EachGetUniqueTraceId(int instanceId)
    {
        var traceId = Activity.Current!.TraceId.ToString();

        // Store this test's TraceId
        UniqueTraceIdCollector.RecordTraceId(instanceId, traceId);

        await Assert.That(traceId.Length).IsEqualTo(32);
        await Assert.That(TraceRegistry.IsRegistered(traceId)).IsTrue();
    }

    [Test, DependsOn(nameof(ParallelTests_EachGetUniqueTraceId), [typeof(int)])]
    public async Task ParallelTests_AllTraceIdsAreDistinct()
    {
        var traceIds = UniqueTraceIdCollector.GetAllTraceIds();

        // Should have 5 entries (one per instance)
        await Assert.That(traceIds.Count).IsEqualTo(5);

        // All should be distinct — the whole point of per-test root activities
        var distinctCount = traceIds.Values.Distinct().Count();
        await Assert.That(distinctCount).IsEqualTo(5);
    }

    /// <summary>
    /// Thread-safe collector for verifying TraceId uniqueness across parameterized test instances.
    /// </summary>
    private static class UniqueTraceIdCollector
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, string> TraceIds = new();

        public static void RecordTraceId(int instanceId, string traceId)
            => TraceIds[instanceId] = traceId;

        public static System.Collections.Concurrent.ConcurrentDictionary<int, string> GetAllTraceIds()
            => TraceIds;
    }
}
