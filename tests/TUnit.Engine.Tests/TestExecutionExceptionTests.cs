using Shouldly;
using TUnit.Core.Exceptions;

namespace TUnit.Engine.Tests;

public class TestExecutionExceptionTests
{
    [Test]
    public void TestExecutionException_WithSingleTestException_BuildsCorrectMessage()
    {
        var testException = new InvalidOperationException("Test failed");

        var exception = new TestExecutionException(testException, [], []);

        exception.Message.ShouldBe("Test failed: Test failed");
        exception.TestException.ShouldBe(testException);
        exception.HookExceptions.ShouldBeEmpty();
        exception.EventReceiverExceptions.ShouldBeEmpty();
        exception.InnerException.ShouldBe(testException);
    }

    [Test]
    public void TestExecutionException_WithSingleHookException_BuildsCorrectMessage()
    {
        var hookException = new InvalidOperationException("Hook failed");

        var exception = new TestExecutionException(null, [hookException], []);

        exception.Message.ShouldBe("Hook failed");
        exception.TestException.ShouldBeNull();
        exception.HookExceptions.Count.ShouldBe(1);
        exception.EventReceiverExceptions.ShouldBeEmpty();
        exception.InnerException.ShouldBe(hookException);
    }

    [Test]
    public void TestExecutionException_WithMultipleHookExceptions_BuildsCorrectMessage()
    {
        var hookException1 = new InvalidOperationException("Hook 1 failed");
        var hookException2 = new ArgumentException("Hook 2 failed");

        var exception = new TestExecutionException(null, [hookException1, hookException2], []);

        exception.Message.ShouldBe("Multiple hooks failed: Hook 1 failed; Hook 2 failed");
        exception.TestException.ShouldBeNull();
        exception.HookExceptions.Count.ShouldBe(2);
        exception.EventReceiverExceptions.ShouldBeEmpty();
        exception.InnerException.ShouldBeOfType<AggregateException>();
        var aggEx = (AggregateException)exception.InnerException!;
        aggEx.InnerExceptions.Count.ShouldBe(2);
    }

    [Test]
    public void TestExecutionException_WithSingleEventReceiverException_BuildsCorrectMessage()
    {
        var receiverException = new InvalidOperationException("Receiver failed");

        var exception = new TestExecutionException(null, [], [receiverException]);

        exception.Message.ShouldBe("Test end event receiver failed: Receiver failed");
        exception.TestException.ShouldBeNull();
        exception.HookExceptions.ShouldBeEmpty();
        exception.EventReceiverExceptions.Count.ShouldBe(1);
        exception.InnerException.ShouldBe(receiverException);
    }

    [Test]
    public void TestExecutionException_WithMultipleEventReceiverExceptions_BuildsCorrectMessage()
    {
        var receiverException1 = new InvalidOperationException("Receiver 1 failed");
        var receiverException2 = new ArgumentException("Receiver 2 failed");
        var receiverException3 = new NullReferenceException("Receiver 3 failed");

        var exception = new TestExecutionException(null, [], [receiverException1, receiverException2, receiverException3]);

        exception.Message.ShouldBe("3 test end event receivers failed: Receiver 1 failed; Receiver 2 failed; Receiver 3 failed");
        exception.TestException.ShouldBeNull();
        exception.HookExceptions.ShouldBeEmpty();
        exception.EventReceiverExceptions.Count.ShouldBe(3);
        exception.InnerException.ShouldBeOfType<AggregateException>();
        var aggEx = (AggregateException)exception.InnerException!;
        aggEx.InnerExceptions.Count.ShouldBe(3);
    }

    [Test]
    public void TestExecutionException_WithTestAndHookExceptions_BuildsCorrectMessage()
    {
        var testException = new InvalidOperationException("Test failed");
        var hookException = new ArgumentException("Hook failed");

        var exception = new TestExecutionException(testException, [hookException], []);

        exception.Message.ShouldBe("Test failed: Test failed | Hook failed");
        exception.TestException.ShouldBe(testException);
        exception.HookExceptions.Count.ShouldBe(1);
        exception.EventReceiverExceptions.ShouldBeEmpty();
        exception.InnerException.ShouldBeOfType<AggregateException>();
        var aggEx = (AggregateException)exception.InnerException!;
        aggEx.InnerExceptions.Count.ShouldBe(2);
    }

    [Test]
    public void TestExecutionException_WithTestAndMultipleHookExceptions_BuildsCorrectMessage()
    {
        var testException = new InvalidOperationException("Test failed");
        var hookException1 = new ArgumentException("Hook 1 failed");
        var hookException2 = new NullReferenceException("Hook 2 failed");

        var exception = new TestExecutionException(testException, [hookException1, hookException2], []);

        exception.Message.ShouldBe("Test failed: Test failed | Multiple hooks failed: Hook 1 failed; Hook 2 failed");
        exception.TestException.ShouldBe(testException);
        exception.HookExceptions.Count.ShouldBe(2);
        exception.EventReceiverExceptions.ShouldBeEmpty();
        exception.InnerException.ShouldBeOfType<AggregateException>();
        var aggEx = (AggregateException)exception.InnerException!;
        aggEx.InnerExceptions.Count.ShouldBe(3);
    }

    [Test]
    public void TestExecutionException_WithTestAndEventReceiverExceptions_BuildsCorrectMessage()
    {
        var testException = new InvalidOperationException("Test failed");
        var receiverException = new ArgumentException("Receiver failed");

        var exception = new TestExecutionException(testException, [], [receiverException]);

        exception.Message.ShouldBe("Test failed: Test failed | Test end event receiver failed: Receiver failed");
        exception.TestException.ShouldBe(testException);
        exception.HookExceptions.ShouldBeEmpty();
        exception.EventReceiverExceptions.Count.ShouldBe(1);
        exception.InnerException.ShouldBeOfType<AggregateException>();
        var aggEx = (AggregateException)exception.InnerException!;
        aggEx.InnerExceptions.Count.ShouldBe(2);
    }

    [Test]
    public void TestExecutionException_WithAllExceptionTypes_BuildsCorrectMessage()
    {
        var testException = new InvalidOperationException("Test failed");
        var hookException1 = new ArgumentException("Hook 1 failed");
        var hookException2 = new NullReferenceException("Hook 2 failed");
        var receiverException1 = new InvalidCastException("Receiver 1 failed");
        var receiverException2 = new System.TimeoutException("Receiver 2 failed");

        var exception = new TestExecutionException(
            testException,
            [hookException1, hookException2],
            [receiverException1, receiverException2]);

        exception.Message.ShouldBe(
            "Test failed: Test failed | " +
            "Multiple hooks failed: Hook 1 failed; Hook 2 failed | " +
            "2 test end event receivers failed: Receiver 1 failed; Receiver 2 failed");

        exception.TestException.ShouldBe(testException);
        exception.HookExceptions.Count.ShouldBe(2);
        exception.EventReceiverExceptions.Count.ShouldBe(2);
        exception.InnerException.ShouldBeOfType<AggregateException>();
        var aggEx = (AggregateException)exception.InnerException!;
        aggEx.InnerExceptions.Count.ShouldBe(5);
    }

    [Test]
    public void TestExecutionException_WithOnlyHookAndReceiverExceptions_BuildsCorrectMessage()
    {
        var hookException = new ArgumentException("Hook failed");
        var receiverException = new InvalidOperationException("Receiver failed");

        var exception = new TestExecutionException(null, [hookException], [receiverException]);

        exception.Message.ShouldBe("Hook failed | Test end event receiver failed: Receiver failed");
        exception.TestException.ShouldBeNull();
        exception.HookExceptions.Count.ShouldBe(1);
        exception.EventReceiverExceptions.Count.ShouldBe(1);
        exception.InnerException.ShouldBeOfType<AggregateException>();
        var aggEx = (AggregateException)exception.InnerException!;
        aggEx.InnerExceptions.Count.ShouldBe(2);
    }

    [Test]
    public void TestExecutionException_WithNoExceptions_BuildsEmptyMessage()
    {
        var exception = new TestExecutionException(null, [], []);

        exception.Message.ShouldBeEmpty();
        exception.TestException.ShouldBeNull();
        exception.HookExceptions.ShouldBeEmpty();
        exception.EventReceiverExceptions.ShouldBeEmpty();
        exception.InnerException.ShouldBeNull();
    }

    [Test]
    public void TestExecutionException_PreservesOriginalExceptionStackTrace()
    {
        var testException = CreateExceptionWithStackTrace();

        var exception = new TestExecutionException(testException, [], []);

        exception.InnerException.ShouldBe(testException);
        exception.InnerException!.StackTrace.ShouldNotBeNull();
    }

    private static Exception CreateExceptionWithStackTrace()
    {
        try
        {
            throw new InvalidOperationException("Test exception with stack trace");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
