using System.Diagnostics;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.Aspire.Tests;

/// <summary>
/// Tests that verify the TUnit engine creates test activities correctly:
/// parent-child hierarchy (test → class → assembly → session), proper tags,
/// baggage for cross-boundary propagation, and TraceId shared within a class.
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

    [Test]
    public async Task TestCaseActivity_HasParentSpanId_FromClassActivity()
    {
        var testCase = Activity.Current!.Parent!;

        // The test case has a ParentSpanId linking it to the class activity.
        // Activity.Parent (object reference) may be null when explicit parentContext
        // is used, but ParentSpanId is always set for child activities.
        await Assert.That(testCase.ParentSpanId).IsNotEqualTo(default(ActivitySpanId));
    }

    [Test]
    public async Task TraceId_IsNonEmpty()
    {
        var traceId = Activity.Current!.TraceId.ToString();

        await Assert.That(traceId.Length).IsEqualTo(32);
        await Assert.That(traceId).IsNotEqualTo(new string('0', 32));
    }
}
