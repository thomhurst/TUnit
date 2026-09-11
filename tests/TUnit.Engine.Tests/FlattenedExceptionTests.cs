using Shouldly;
using TUnit.Engine.Exceptions;

namespace TUnit.Engine.Tests;

// Regression coverage for https://github.com/thomhurst/TUnit/issues/1327: IDE clients only receive
// Exception.Message and Exception.StackTrace, so the inner-exception chain must be folded into them.
public class FlattenedExceptionTests
{
    [Test]
    public void Wrap_ExceptionWithoutInner_ReturnsSameInstance()
    {
        var exception = Throw(() => new InvalidOperationException("leaf"));

        var wrapped = FlattenedException.Wrap(exception);

        wrapped.ShouldBeSameAs(exception);
    }

    [Test]
    public void Wrap_ExceptionWithInner_ReturnsFlattenedException()
    {
        var exception = CreateNestedException();

        var wrapped = FlattenedException.Wrap(exception);

        wrapped.ShouldBeOfType<FlattenedException>();
    }

    [Test]
    public void Message_ListsEveryInnerExceptionOutermostFirst()
    {
        var exception = CreateNestedException();

        var wrapped = FlattenedException.Wrap(exception);

        wrapped.Message.ShouldBe(string.Join(Environment.NewLine,
            "Thrown from Method1",
            " ---> System.ArgumentException: Thrown from Method2",
            " ---> System.InvalidOperationException: Thrown from Method3"));
    }

    [Test]
    public void StackTrace_ContainsFramesOfEveryInnerException()
    {
        var exception = CreateNestedException();

        var wrapped = FlattenedException.Wrap(exception);

        var stackTrace = wrapped.StackTrace!;
        stackTrace.ShouldContain($"{nameof(FlattenedExceptionTests)}.{nameof(Method1)}()");
        stackTrace.ShouldContain($"{nameof(FlattenedExceptionTests)}.{nameof(Method2)}()");
        stackTrace.ShouldContain($"{nameof(FlattenedExceptionTests)}.{nameof(Method3)}()");
        stackTrace.ShouldContain("--- Inner exception stack trace (System.ArgumentException) ---");
        stackTrace.ShouldContain("--- Inner exception stack trace (System.InvalidOperationException) ---");
    }

    [Test]
    public void StackTrace_OrdersOuterFramesBeforeInnerFrames()
    {
        var exception = CreateNestedException();

        var wrapped = FlattenedException.Wrap(exception);

        var stackTrace = wrapped.StackTrace!;
        var method1 = stackTrace.IndexOf($"{nameof(Method1)}()", StringComparison.Ordinal);
        var method2 = stackTrace.IndexOf($"{nameof(Method2)}()", StringComparison.Ordinal);
        var method3 = stackTrace.IndexOf($"{nameof(Method3)}()", StringComparison.Ordinal);

        method1.ShouldBeLessThan(method2);
        method2.ShouldBeLessThan(method3);
    }

    [Test]
    public void InnerException_IsNull_SoChainWalkersDoNotDuplicateOutput()
    {
        var exception = CreateNestedException();

        var wrapped = FlattenedException.Wrap(exception);

        wrapped.InnerException.ShouldBeNull();
    }

    [Test]
    public void Unwrap_ReturnsOriginalException()
    {
        var exception = CreateNestedException();

        var wrapped = FlattenedException.Wrap(exception);

        TUnitFailedException.Unwrap(wrapped).ShouldBeSameAs(exception);
    }

    [Test]
    public void ToString_ReportsOriginalTypeMessageAndFoldedStackTrace()
    {
        var exception = CreateNestedException();

        var wrapped = FlattenedException.Wrap(exception);

        var text = wrapped.ToString();
        text.ShouldStartWith("System.Exception: Thrown from Method1");
        text.ShouldContain(" ---> System.InvalidOperationException: Thrown from Method3");
        text.ShouldContain($"{nameof(Method3)}()");
        text.ShouldNotContain("TUnit.Engine.Exceptions.FlattenedException");
    }

    [Test]
    public void AggregateException_IncludesEverySiblingAndTheirInnerChains()
    {
        var first = Throw(() => new InvalidOperationException("first"));
        var second = Throw(() => new ArgumentException("second", Throw(() => new FormatException("second-inner"))));
        var aggregate = new AggregateException("aggregate", first, second);

        var wrapped = FlattenedException.Wrap(aggregate);

        wrapped.Message.ShouldContain(" ---> System.InvalidOperationException: first");
        wrapped.Message.ShouldContain(" ---> System.ArgumentException: second");
        wrapped.Message.ShouldContain(" ---> System.FormatException: second-inner");
        wrapped.StackTrace!.ShouldContain("--- Inner exception stack trace (System.InvalidOperationException) ---");
        wrapped.StackTrace!.ShouldContain("--- Inner exception stack trace (System.ArgumentException) ---");
        wrapped.StackTrace!.ShouldContain("--- Inner exception stack trace (System.FormatException) ---");
    }

    [Test]
    public void InnerExceptionWithoutStackTrace_ListedInMessage_NoFramelessSeparator()
    {
        // Inner exception constructed but never thrown: its StackTrace is null, so the type is
        // carried by the message line only.
        var exception = Throw(() => new Exception("outer", new InvalidOperationException("never thrown")));

        var wrapped = FlattenedException.Wrap(exception);

        wrapped.Message.ShouldContain(" ---> System.InvalidOperationException: never thrown");
        wrapped.StackTrace.ShouldBe(exception.StackTrace);
    }

    [Test]
    public void AssertMultipleShape_DoesNotRepeatMemberMessages()
    {
        // Assert.Multiple throws AssertionException(joinedMessages, new AggregateException(members)):
        // every member message is already in the outer message and no member was ever thrown.
        var first = new InvalidOperationException("Expected 1 but was 2");
        var second = new InvalidOperationException("Expected true but was false");
        var exception = Throw(() => new Exception(
            $"Expected 1 but was 2{Environment.NewLine}{Environment.NewLine}Expected true but was false",
            new AggregateException(first, second)));

        var wrapped = FlattenedException.Wrap(exception);

        wrapped.Message.ShouldBe(exception.Message);
        wrapped.StackTrace.ShouldBe(exception.StackTrace);
    }

    [Test]
    public void InnerAggregate_GetsNoLineOfItsOwn_MembersAreListed()
    {
        var first = Throw(() => new InvalidOperationException("first"));
        var second = Throw(() => new ArgumentException("second"));
        var exception = Throw(() => new Exception("wrapper", new AggregateException(first, second)));

        var wrapped = FlattenedException.Wrap(exception);

        wrapped.Message.ShouldBe(string.Join(Environment.NewLine,
            "wrapper",
            " ---> System.InvalidOperationException: first",
            " ---> System.ArgumentException: second"));
        wrapped.Message.ShouldNotContain("System.AggregateException");
        wrapped.StackTrace!.ShouldContain("--- Inner exception stack trace (System.InvalidOperationException) ---");
        wrapped.StackTrace!.ShouldContain("--- Inner exception stack trace (System.ArgumentException) ---");
        wrapped.StackTrace!.ShouldNotContain("(System.AggregateException)");
    }

    [Test]
    public void InnerMessageEmbeddedInParentMessage_NotRepeated_DeeperCauseStillListed()
    {
        // Hook wrappers splice the cause into their own message: "BeforeTest hook failed: boom".
        var cause = Throw(() => new InvalidOperationException("boom", Throw(() => new FormatException("root cause"))));
        var exception = Throw(() => new Exception("BeforeTest hook failed: boom", cause));

        var wrapped = FlattenedException.Wrap(exception);

        wrapped.Message.ShouldBe(string.Join(Environment.NewLine,
            "BeforeTest hook failed: boom",
            " ---> System.FormatException: root cause"));
        wrapped.StackTrace!.ShouldContain("--- Inner exception stack trace (System.InvalidOperationException) ---");
        wrapped.StackTrace!.ShouldContain("--- Inner exception stack trace (System.FormatException) ---");
    }

    [Test]
    public void RootAggregate_MembersListedEvenThoughAggregateMessageEmbedsThem()
    {
        var first = Throw(() => new InvalidOperationException("first"));
        var second = Throw(() => new ArgumentException("second"));
        var aggregate = Throw(() => new AggregateException("two failures", first, second));

        var wrapped = FlattenedException.Wrap(aggregate);

        wrapped.Message.ShouldStartWith(aggregate.Message);
        wrapped.Message.ShouldContain(" ---> System.InvalidOperationException: first");
        wrapped.Message.ShouldContain(" ---> System.ArgumentException: second");
    }

    [Test]
    public void ConsoleWrapper_FiltersInnerStackTracesLikeTheOuterOne()
    {
        var inner = new StackTraceOverrideException("inner", string.Join(Environment.NewLine,
            "   at MyApp.Tests.UserTests.TestGetUser() in C:\\src\\Tests.cs:line 15",
            "   at TUnit.Core.RunHelpers.RunAsync()",
            "   at TUnit.Engine.TestExecutor.ExecuteAsync()"));
        var wrapper = new TestFailedException(Throw(() => new Exception("outer", inner)));

        var stackTrace = FlattenedException.CombineStackTraces(wrapper);

        stackTrace.ShouldStartWith(wrapper.StackTrace);
        stackTrace.ShouldContain("MyApp.Tests.UserTests.TestGetUser");
        stackTrace.ShouldNotContain("TUnit.Core.RunHelpers");
        stackTrace.ShouldNotContain("TUnit.Engine.TestExecutor");
    }

    [Test]
    public void Wrap_CopiesExceptionData()
    {
        var exception = CreateNestedException();
        exception.Data["assert.expected"] = "1";
        exception.Data["assert.actual"] = "2";

        var wrapped = FlattenedException.Wrap(exception);

        wrapped.Data["assert.expected"].ShouldBe("1");
        wrapped.Data["assert.actual"].ShouldBe("2");
    }

    [Test]
    public void ConsoleWrapperAroundAggregate_FoldsEverySiblingFromWrappedOriginal()
    {
        // TestFailedException (the console-mode wrapper) exposes only the first aggregate member as
        // InnerException; folding must read the chain from WrappedException instead.
        var first = Throw(() => new InvalidOperationException("first"));
        var second = Throw(() => new ArgumentException("second", Throw(() => new FormatException("second-inner"))));
        var aggregate = Throw(() => new AggregateException("aggregate", first, second));
        var wrapper = new TestFailedException(aggregate);

        var message = FlattenedException.CombineMessages(wrapper);
        var stackTrace = FlattenedException.CombineStackTraces(wrapper);

        message.ShouldStartWith(wrapper.Message);
        message.ShouldContain(" ---> System.InvalidOperationException: first");
        message.ShouldContain(" ---> System.ArgumentException: second");
        message.ShouldContain(" ---> System.FormatException: second-inner");

        stackTrace.ShouldStartWith(wrapper.StackTrace);
        stackTrace.ShouldContain("--- Inner exception stack trace (System.InvalidOperationException) ---");
        stackTrace.ShouldContain("--- Inner exception stack trace (System.ArgumentException) ---");
        stackTrace.ShouldContain("--- Inner exception stack trace (System.FormatException) ---");
    }

    [Test]
    public void AlreadyFlattened_CombineIsNoOp()
    {
        var wrapped = FlattenedException.Wrap(CreateNestedException());

        FlattenedException.CombineMessages(wrapped).ShouldBe(wrapped.Message);
        FlattenedException.CombineStackTraces(wrapped).ShouldBe(wrapped.StackTrace);
        FlattenedException.Wrap(wrapped).ShouldBeSameAs(wrapped);
    }

    private static Exception CreateNestedException()
    {
        try
        {
            Method1();
            throw new InvalidOperationException("Method1 should have thrown");
        }
        catch (Exception e) when (e.Message == "Thrown from Method1")
        {
            return e;
        }
    }

    private static void Method1()
    {
        try
        {
            Method2();
        }
        catch (Exception e)
        {
            throw new Exception("Thrown from Method1", e);
        }
    }

    private static void Method2()
    {
        try
        {
            Method3();
        }
        catch (Exception e)
        {
            throw new ArgumentException("Thrown from Method2", e);
        }
    }

    private static void Method3()
    {
        throw new InvalidOperationException("Thrown from Method3");
    }

    private sealed class StackTraceOverrideException(string message, string stackTrace) : Exception(message)
    {
        public override string StackTrace { get; } = stackTrace;
    }

    private static Exception Throw(Func<Exception> factory)
    {
        try
        {
            throw factory();
        }
        catch (Exception e)
        {
            return e;
        }
    }
}
