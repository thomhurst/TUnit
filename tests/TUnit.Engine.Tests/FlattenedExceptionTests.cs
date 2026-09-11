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
    public void InnerExceptionWithoutStackTrace_StillListedInMessageAndStackTrace()
    {
        // Inner exception constructed but never thrown: its StackTrace is null.
        var exception = Throw(() => new Exception("outer", new InvalidOperationException("never thrown")));

        var wrapped = FlattenedException.Wrap(exception);

        wrapped.Message.ShouldContain(" ---> System.InvalidOperationException: never thrown");
        wrapped.StackTrace!.ShouldContain("--- Inner exception stack trace (System.InvalidOperationException) ---");
        wrapped.StackTrace!.ShouldContain(nameof(Throw));
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
