using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class OverrideResultsTests
{
    [Test, OverridePass]
    public void OverrideFailedTestToPassed()
    {
        throw new InvalidOperationException("This test should fail but will be overridden to passed");
    }

    [Test, OverrideToSkipped]
    public void OverrideFailedTestToSkipped()
    {
        throw new ArgumentException("This test should fail but will be overridden to skipped");
    }

    [Test, OverrideToFailed]
    public void OverridePassedTestToFailed()
    {
        // This test passes but will be overridden to failed
    }

    [Test, PreserveOriginalException]
    public void VerifyOriginalExceptionIsPreserved()
    {
        throw new InvalidOperationException("Original exception that should be preserved");
    }

    [Test, OverrideToSkippedWithSpecificReason]
    public void TestSkippedWithSpecificReason()
    {
        // This test will be overridden to skipped with a specific reason
    }

    [After(Class)]
    public static async Task AfterClass(ClassHookContext classHookContext)
    {
        await Assert.That(classHookContext.Tests.Count).IsEqualTo(5);

        // Test 1: Failed -> Passed
        var test1 = classHookContext.Tests.First(t => t.Metadata.TestDetails.TestName == "OverrideFailedTestToPassed");
        await Assert.That(test1.Execution.Result?.State).IsEqualTo(TestState.Passed);
        await Assert.That(test1.Execution.Result?.IsOverridden).IsTrue();
        await Assert.That(test1.Execution.Result?.OverrideReason).IsEqualTo("Overridden to passed");
        await Assert.That(test1.Execution.Result?.OriginalException).IsNotNull();
        await Assert.That(test1.Execution.Result?.OriginalException).IsTypeOf<InvalidOperationException>();

        // Test 2: Failed -> Skipped
        var test2 = classHookContext.Tests.First(t => t.Metadata.TestDetails.TestName == "OverrideFailedTestToSkipped");
        await Assert.That(test2.Execution.Result?.State).IsEqualTo(TestState.Skipped);
        await Assert.That(test2.Execution.Result?.IsOverridden).IsTrue();
        await Assert.That(test2.Execution.Result?.OverrideReason).IsEqualTo("Overridden to skipped");
        await Assert.That(test2.Execution.Result?.OriginalException).IsNotNull();
        await Assert.That(test2.Execution.Result?.OriginalException).IsTypeOf<ArgumentException>();

        // Test 3: Passed -> Failed
        var test3 = classHookContext.Tests.First(t => t.Metadata.TestDetails.TestName == "OverridePassedTestToFailed");
        await Assert.That(test3.Execution.Result?.State).IsEqualTo(TestState.Failed);
        await Assert.That(test3.Execution.Result?.IsOverridden).IsTrue();
        await Assert.That(test3.Execution.Result?.OverrideReason).IsEqualTo("Overridden to failed");

        // Test 4: Verify original exception preservation
        var test4 = classHookContext.Tests.First(t => t.Metadata.TestDetails.TestName == "VerifyOriginalExceptionIsPreserved");
        await Assert.That(test4.Execution.Result?.OriginalException?.Message).IsEqualTo("Original exception that should be preserved");

        // Test 5: Verify skip reason is displayed when overridden
        var test5 = classHookContext.Tests.First(t => t.Metadata.TestDetails.TestName == "TestSkippedWithSpecificReason");
        await Assert.That(test5.Execution.Result?.State).IsEqualTo(TestState.Skipped);
        await Assert.That(test5.Execution.Result?.IsOverridden).IsTrue();
        await Assert.That(test5.Execution.Result?.OverrideReason).IsEqualTo("test-skip foo bar baz.");
    }

    public class OverridePassAttribute : Attribute, ITestEndEventReceiver
    {
        public ValueTask OnTestEnd(TestContext afterTestContext)
        {
            afterTestContext.Execution.OverrideResult(TestState.Passed, "Overridden to passed");
            return default(ValueTask);
        }

        public int Order => 0;
    }

    public class OverrideToSkippedAttribute : Attribute, ITestEndEventReceiver
    {
        public ValueTask OnTestEnd(TestContext afterTestContext)
        {
            afterTestContext.Execution.OverrideResult(TestState.Skipped, "Overridden to skipped");
            return default(ValueTask);
        }

        public int Order => 0;
    }

    public class OverrideToFailedAttribute : Attribute, ITestEndEventReceiver
    {
        public ValueTask OnTestEnd(TestContext afterTestContext)
        {
            afterTestContext.Execution.OverrideResult(TestState.Failed, "Overridden to failed");
            return default(ValueTask);
        }

        public int Order => 0;
    }

    public class PreserveOriginalExceptionAttribute : Attribute, ITestEndEventReceiver
    {
        public ValueTask OnTestEnd(TestContext afterTestContext)
        {
            afterTestContext.Execution.OverrideResult(TestState.Passed, "Test passed after retry");
            return default(ValueTask);
        }

        public int Order => 0;
    }

    public class OverrideToSkippedWithSpecificReasonAttribute : Attribute, ITestEndEventReceiver
    {
        public ValueTask OnTestEnd(TestContext afterTestContext)
        {
            afterTestContext.Execution.OverrideResult(TestState.Skipped, "test-skip foo bar baz.");
            return default(ValueTask);
        }

        public int Order => 0;
    }
}
