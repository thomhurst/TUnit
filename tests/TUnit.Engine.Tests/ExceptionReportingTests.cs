using Microsoft.Testing.Platform.Extensions.Messages;
using Shouldly;
using TUnit.Assertions.Exceptions;

namespace TUnit.Engine.Tests;

public class ExceptionReportingTests
{
    [Test]
    public void IdeTimeoutExplanation_IncludesEveryAggregateMember()
    {
        var exception = new AggregateException("Timeout and cleanup failures",
            new TaskCanceledException("Custom cancellation diagnostic",
                new InvalidOperationException("Inner diagnostic")),
            new FormatException("Cleanup sibling diagnostic"));

        var state = TUnitMessageBus.GetFailureStateProperty(exception,
            TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100), isConsole: false);

        var timeout = state.ShouldBeOfType<TimeoutTestNodeStateProperty>();
        timeout.Explanation.ShouldStartWith("[Timeout] Test timed out after 50ms");
        timeout.Explanation.ShouldContain("Custom cancellation diagnostic");
        timeout.Explanation.ShouldContain("System.InvalidOperationException: Inner diagnostic");
        timeout.Explanation.ShouldContain("System.FormatException: Cleanup sibling diagnostic");
    }

    [Test]
    public void IdeTimeoutExplanation_IncludesCancellationMessageWithoutInnerException()
    {
        var exception = new TaskCanceledException("Custom cancellation diagnostic");

        var state = TUnitMessageBus.GetFailureStateProperty(exception,
            TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100), isConsole: false);

        state.ShouldBeOfType<TimeoutTestNodeStateProperty>().Explanation.ShouldBe(
            $"[Timeout] Test timed out after 50ms{Environment.NewLine}Custom cancellation diagnostic");
    }

    [Test]
    public void ConsoleTimeoutExplanation_KeepsImmediateDiagnostic()
    {
        var exception = new TaskCanceledException("Custom cancellation diagnostic",
            new InvalidOperationException("Inner diagnostic", new FormatException("Root cause")));

        var state = TUnitMessageBus.GetFailureStateProperty(exception,
            TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100), isConsole: true);

        var timeout = state.ShouldBeOfType<TimeoutTestNodeStateProperty>();
        timeout.Exception.ShouldBeSameAs(exception);
        timeout.Explanation.ShouldBe($"[Timeout] Test timed out after 50ms{Environment.NewLine}Inner diagnostic");
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public void IdeAggregateAssertion_PreservesFirstMembersDiffAndAllSiblingMessages(bool nested)
    {
        Exception first = new AssertionException("First assertion failure");
        first.Data["assert.expected"] = "1";
        first.Data["assert.actual"] = "2";
        if (nested)
        {
            first = new AggregateException(first, new Exception("Nested sibling diagnostic"));
        }

        var exception = new AggregateException(first, new Exception("Cleanup sibling diagnostic"));

        var state = TUnitMessageBus.GetFailureStateProperty(exception, null, TimeSpan.Zero, isConsole: false);

        var failed = state.ShouldBeOfType<FailedTestNodeStateProperty>();
        failed.Exception!.Data["assert.expected"].ShouldBe("1");
        failed.Exception.Data["assert.actual"].ShouldBe("2");
        failed.Explanation.ShouldNotBeNull();
        failed.Explanation.ShouldContain("[Assertion Failure]");
        failed.Explanation.ShouldContain("First assertion failure");
        failed.Explanation.ShouldContain("System.Exception: Cleanup sibling diagnostic");
        if (nested)
        {
            failed.Explanation.ShouldContain("System.Exception: Nested sibling diagnostic");
        }
    }
}
