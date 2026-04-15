using System.Diagnostics;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Tests that verify the TUnit engine creates test activities correctly:
/// parent-child hierarchy (test body → test case), proper tags,
/// baggage for cross-boundary propagation, Activity Links to the class activity,
/// and unique TraceIds per test case.
/// </summary>
public class TestActivityTests
{
    [Test]
    public async Task ActivityCurrent_IsNotNull_DuringTestExecution()
    {
        await Assert.That(Activity.Current).IsNotNull();
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
    public async Task TestCaseActivity_SharesTraceId_WithTestBody()
    {
        var testBody = Activity.Current!;
        var testCase = testBody.Parent!;

        // Parent-child spans share the same TraceId (W3C spec).
        // The test body is a child of the test case.
        await Assert.That(testBody.TraceId.ToString())
            .IsEqualTo(testCase.TraceId.ToString());
    }

    [Test]
    public async Task TestCaseActivity_HasExpectedTags()
    {
        var testCase = Activity.Current!.Parent!;

        var testCaseName = testCase.GetTagItem(TUnitActivitySource.TagTestCaseName)?.ToString();
        var testClass = testCase.GetTagItem(TUnitActivitySource.TagTestClass)?.ToString();
        var testMethod = testCase.GetTagItem(TUnitActivitySource.TagTestMethod)?.ToString();
        var testId = testCase.GetTagItem(TUnitActivitySource.TagTestId)?.ToString();
        var testNodeUid = testCase.GetTagItem(TUnitActivitySource.TagTestNodeUid)?.ToString();

        await Assert.That(testCaseName).IsNotNull();
        await Assert.That(testClass).Contains(nameof(TestActivityTests));
        await Assert.That(testMethod).IsEqualTo(nameof(TestCaseActivity_HasExpectedTags));
        await Assert.That(testId).IsEqualTo(TestContext.Current!.Id);
        await Assert.That(testNodeUid).IsNotNull();
    }

    [Test]
    public async Task TestCaseActivity_HasActivityLink_ToClassActivity()
    {
        var testCase = Activity.Current!.Parent!;

        // Each test case starts its own W3C trace (so ParentSpanId is default).
        // Instead, an Activity Link references the class activity for correlation.
        // This prevents all tests in a class from sharing one giant trace in backends
        // like Seq/Jaeger while still preserving the logical hierarchy.
        var link = await Assert.That(testCase.Links.ToList()).HasSingleItem();

        await Assert.That(link.Context.TraceId).IsNotEqualTo(default(ActivityTraceId));
        await Assert.That(link.Context.SpanId).IsNotEqualTo(default(ActivitySpanId));
    }

    [Test]
    public async Task TraceId_IsNonEmpty()
    {
        var traceId = Activity.Current!.TraceId.ToString();

        await Assert.That(traceId.Length).IsEqualTo(32);
        await Assert.That(traceId).IsNotEqualTo(new string('0', 32));
    }

    [Test]
    public async Task TraceId_IsRegisteredInTraceRegistry()
    {
        var traceId = Activity.Current!.TraceId.ToString();

        // The engine registers the test's TraceId in TraceRegistry at activity creation.
        // This enables the OTLP receiver to correlate SUT logs back to this test
        // without synthetic TraceId generation — pure natural OTEL propagation.
        await Assert.That(TraceRegistry.IsRegistered(traceId)).IsTrue();
        await Assert.That(TraceRegistry.GetContextId(traceId)).IsEqualTo(TestContext.Current!.Id);
    }
}
