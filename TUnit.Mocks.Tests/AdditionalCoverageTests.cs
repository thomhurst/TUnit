using TUnit.Mocks.Arguments;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Additional coverage tests for APIs and edge cases identified in the test coverage audit.
/// Covers: MockVerificationException, CallRecord, implicit conversion, DefaultValueProvider,
/// auto-mock error path, VerifyAll messages, invocation ordering, auto-track reset.
/// </summary>

// ─── MockVerificationException Properties ───────────────────────────────────

public class MockVerificationExceptionTests
{
    [Test]
    public async Task Exception_Properties_Populated_On_Verification_Failure()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Object.Add(1, 2);

        // Act — verify wrong count
        var ex = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.Exactly(5));
        });

        // Assert — all properties accessible
        await Assert.That(ex.ExpectedCall).Contains("Add");
        await Assert.That(ex.ExpectedTimes).IsEqualTo(Times.Exactly(5));
        await Assert.That(ex.ActualCount).IsEqualTo(1);
        await Assert.That(ex.ActualCalls).HasCount().EqualTo(1);
    }

    [Test]
    public async Task Exception_Message_Contains_Custom_Message()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();

        // Act — verify with custom message
        var ex = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.Once, "Calculator Add was expected");
        });

        await Assert.That(ex.Message).Contains("Calculator Add was expected");
        await Assert.That(ex.Message).Contains("Mock verification failed");
    }

    [Test]
    public async Task Exception_ActualCalls_Lists_All_Calls_To_Member()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Object.Add(1, 2);
        mock.Object.Add(3, 4);
        mock.Object.Add(5, 6);

        // Act — verify wrong specific args
        var ex = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(99, 99).WasCalled(Times.Once);
        });

        // Assert — all 3 actual calls to Add are listed
        await Assert.That(ex.ActualCalls).HasCount().EqualTo(3);
    }

    [Test]
    public async Task String_Message_Constructor_Sets_Default_Properties()
    {
        // The string-only constructor is used for ordered verification failures
        var ex = new MockVerificationException("Custom ordered failure message");
        await Assert.That(ex.Message).IsEqualTo("Custom ordered failure message");
        await Assert.That(ex.ExpectedCall).IsEmpty();
        await Assert.That(ex.ExpectedTimes).IsEqualTo(Times.Never);
        await Assert.That(ex.ActualCount).IsEqualTo(0);
        await Assert.That(ex.ActualCalls).HasCount().EqualTo(0);
    }
}

// ─── CallRecord Formatting ──────────────────────────────────────────────────

public class CallRecordTests
{
    [Test]
    public async Task FormatCall_Shows_Method_And_Args()
    {
        var record = new CallRecord(1, "Add", [1, 2], 1);
        await Assert.That(record.FormatCall()).IsEqualTo("Add(1, 2)");
    }

    [Test]
    public async Task FormatCall_Null_Arg_Shows_Null()
    {
        var record = new CallRecord(1, "GetData", [null], 1);
        await Assert.That(record.FormatCall()).IsEqualTo("GetData(null)");
    }

    [Test]
    public async Task FormatCall_No_Args_Shows_Empty_Parens()
    {
        var record = new CallRecord(1, "GetName", [], 1);
        await Assert.That(record.FormatCall()).IsEqualTo("GetName()");
    }

    [Test]
    public async Task FormatCall_String_Args()
    {
        var record = new CallRecord(1, "Greet", ["Alice"], 1);
        await Assert.That(record.FormatCall()).IsEqualTo("Greet(Alice)");
    }

    [Test]
    public async Task IsVerified_Defaults_To_False()
    {
        var record = new CallRecord(1, "M", [], 1);
        await Assert.That(record.IsVerified).IsFalse();
    }

    [Test]
    public async Task IsUnmatched_Defaults_To_False()
    {
        var record = new CallRecord(1, "M", [], 1);
        await Assert.That(record.IsUnmatched).IsFalse();
    }

    [Test]
    public async Task SequenceNumber_Is_Accessible()
    {
        var record = new CallRecord(1, "M", [], 42);
        await Assert.That(record.SequenceNumber).IsEqualTo(42);
    }
}

// ─── Object Property Access ─────────────────────────────────────────────────
// Note: C# does not allow user-defined implicit conversions to interface types,
// so Mock<T>.Object must be used explicitly when T is an interface.

public class MockObjectAccessTests
{
    [Test]
    public async Task Object_Property_Returns_Mock_Implementation()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(1, 2).Returns(3);

        var calc = mock.Object;
        var result = calc.Add(1, 2);

        await Assert.That(result).IsEqualTo(3);
    }

    [Test]
    public async Task Object_Property_Returns_Same_Reference()
    {
        var mock = Mock.Of<ICalculator>();

        var first = mock.Object;
        var second = mock.Object;

        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    [Test]
    public async Task IMock_ObjectInstance_Returns_Same_As_Object()
    {
        var mock = Mock.Of<ICalculator>();

        IMock imock = mock;
        var fromIMock = imock.ObjectInstance;
        var fromObject = mock.Object;

        await Assert.That(ReferenceEquals(fromIMock, fromObject)).IsTrue();
    }
}

// ─── GetAutoMock Error Path ─────────────────────────────────────────────────

public class AutoMockErrorPathTests
{
    [Test]
    public async Task GetAutoMock_Throws_When_No_AutoMock_Exists()
    {
        var mock = Mock.Of<ICalculator>();

        // No method has been called, so no auto-mock was created
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            mock.GetAutoMock<IGreeter>("NonExistentMethod");
        });

        await Assert.That(ex.Message).Contains("No auto-mock found");
        await Assert.That(ex.Message).Contains("NonExistentMethod");
    }
}

// ─── VerifyAll Error Messages ───────────────────────────────────────────────

public class VerifyAllMessageTests
{
    [Test]
    public async Task VerifyAll_Message_Includes_Matcher_Descriptions()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Is<int>(x => x > 0)).Returns(1);

        // Act — don't call the method

        // Assert
        var ex = Assert.Throws<MockVerificationException>(() => mock.VerifyAll());
        await Assert.That(ex.Message).Contains("Add(");
        await Assert.That(ex.Message).Contains("never invoked");
    }

    [Test]
    public async Task VerifyAll_Message_Lists_Multiple_Uninvoked_Setups()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(1, 2).Returns(3);
        mock.Setup.GetName().Returns("name");
        mock.Setup.Log("msg");

        // Act — don't call any methods

        // Assert
        var ex = Assert.Throws<MockVerificationException>(() => mock.VerifyAll());
        await Assert.That(ex.Message).Contains("Add(");
        await Assert.That(ex.Message).Contains("GetName()");
        await Assert.That(ex.Message).Contains("Log(");
    }

    [Test]
    public async Task VerifyAll_Passes_When_All_Setups_Invoked()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(1);
        mock.Setup.GetName().Returns("name");

        // Act — invoke all setups
        mock.Object.Add(1, 2);
        mock.Object.GetName();

        // Assert — no exception
        mock.VerifyAll();
        await Assert.That(true).IsTrue();
    }
}

// ─── Invocations Ordering ───────────────────────────────────────────────────

public class InvocationOrderingTests
{
    [Test]
    public async Task Invocations_Are_In_Call_Order()
    {
        var mock = Mock.Of<ICalculator>();

        mock.Object.Add(1, 2);
        mock.Object.Log("hello");
        mock.Object.GetName();
        mock.Object.Add(3, 4);

        await Assert.That(mock.Invocations).HasCount().EqualTo(4);
        await Assert.That(mock.Invocations[0].MemberName).IsEqualTo("Add");
        await Assert.That(mock.Invocations[1].MemberName).IsEqualTo("Log");
        await Assert.That(mock.Invocations[2].MemberName).IsEqualTo("GetName");
        await Assert.That(mock.Invocations[3].MemberName).IsEqualTo("Add");
    }

    [Test]
    public async Task Invocations_Sequence_Numbers_Are_Monotonically_Increasing()
    {
        var mock = Mock.Of<ICalculator>();

        mock.Object.Add(1, 2);
        mock.Object.GetName();
        mock.Object.Log("msg");

        for (int i = 1; i < mock.Invocations.Count; i++)
        {
            await Assert.That(mock.Invocations[i].SequenceNumber)
                .IsGreaterThan(mock.Invocations[i - 1].SequenceNumber);
        }
    }
}

// ─── Auto-Track Properties Reset ────────────────────────────────────────────

public interface ISettingsService
{
    string Theme { get; set; }
    int FontSize { get; set; }
}

public class AutoTrackPropertyResetTests
{
    [Test]
    public async Task Reset_Clears_Auto_Tracked_Property_Values()
    {
        var mock = Mock.Of<ISettingsService>();
        mock.SetupAllProperties();

        var svc = mock.Object;
        svc.Theme = "dark";
        svc.FontSize = 14;

        await Assert.That(svc.Theme).IsEqualTo("dark");
        await Assert.That(svc.FontSize).IsEqualTo(14);

        // Reset should clear tracked values
        mock.Reset();
        mock.SetupAllProperties();

        // After reset + re-enable auto-track, values are back to defaults
        await Assert.That(svc.Theme).IsNotEqualTo("dark");
        await Assert.That(svc.FontSize).IsEqualTo(0);
    }
}

// ─── WasCalled / WasNeverCalled with custom message ─────────────────────────

public class VerificationCustomMessageTests
{
    [Test]
    public async Task WasNeverCalled_With_Custom_Message()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Object.Add(1, 2);

        var ex = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasNeverCalled("Add should not have been called");
        });

        await Assert.That(ex.Message).Contains("Add should not have been called");
    }

    [Test]
    public async Task WasCalled_No_Args_With_Custom_Message()
    {
        var mock = Mock.Of<ICalculator>();
        // Don't call Add

        var ex = Assert.Throws<MockVerificationException>(() =>
        {
            mock.Verify.Add(1, 2).WasCalled("Expected Add to be called");
        });

        await Assert.That(ex.Message).Contains("Expected Add to be called");
    }
}

// ─── DefaultValueProvider on Mock<T> ────────────────────────────────────────

public class MockDefaultValueProviderPropertyTests
{
    private class FixedStringProvider : IDefaultValueProvider
    {
        public bool CanProvide(Type type) => type == typeof(string);
        public object? GetDefaultValue(Type type) => type == typeof(string) ? "CUSTOM" : null;
    }

    [Test]
    public async Task DefaultValueProvider_Get_Set_Roundtrip()
    {
        var mock = Mock.Of<IGreeter>();
        var provider = new FixedStringProvider();

        mock.DefaultValueProvider = provider;

        await Assert.That(mock.DefaultValueProvider).IsEqualTo(provider);
    }

    [Test]
    public async Task DefaultValueProvider_Affects_Unconfigured_Returns()
    {
        var mock = Mock.Of<IGreeter>(MockBehavior.Loose, new FixedStringProvider());

        // Unconfigured method should use the custom provider
        var result = mock.Object.Greet("test");

        await Assert.That(result).IsEqualTo("CUSTOM");
    }
}

// ─── Behavior Property ──────────────────────────────────────────────────────

public class MockBehaviorPropertyTests
{
    [Test]
    public async Task Loose_Mock_Has_Loose_Behavior()
    {
        var mock = Mock.Of<ICalculator>();
        await Assert.That(mock.Behavior).IsEqualTo(MockBehavior.Loose);
    }

    [Test]
    public async Task Strict_Mock_Has_Strict_Behavior()
    {
        var mock = Mock.Of<ICalculator>(MockBehavior.Strict);
        await Assert.That(mock.Behavior).IsEqualTo(MockBehavior.Strict);
    }
}
