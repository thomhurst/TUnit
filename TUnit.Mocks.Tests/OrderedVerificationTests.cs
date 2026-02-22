using TUnit.Mocks;
using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

/// <summary>
/// US9 Integration Tests: Ordered call verification — verify calls occurred in expected sequence.
/// </summary>
public class OrderedVerificationTests
{
    [Test]
    public void Correct_Order_Passes()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // Act
        calc.Add(1, 2);
        calc.GetName();

        // Assert — verifying in the same order they were called should pass
        Mock.VerifyInOrder(() =>
        {
            mock.Verify.Add(1, 2).WasCalled();
            mock.Verify.GetName().WasCalled();
        });
    }

    [Test]
    public async Task Wrong_Order_Fails_With_Message()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // Act — call GetName first, then Add
        calc.GetName();
        calc.Add(1, 2);

        // Assert — verifying Add before GetName should fail because actual order was reversed
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            Mock.VerifyInOrder(() =>
            {
                mock.Verify.Add(1, 2).WasCalled();
                mock.Verify.GetName().WasCalled();
            });
        });

        await Assert.That(exception.Message).Contains("Ordered verification failed");
    }

    [Test]
    public void Cross_Mock_Ordering_Passes_When_Correct()
    {
        // Arrange
        var calcMock = Mock.Of<ICalculator>();
        var greeterMock = Mock.Of<IGreeter>();
        ICalculator calc = calcMock.Object;
        IGreeter greeter = greeterMock.Object;

        // Act — interleaved calls across two mocks
        calc.Add(1, 2);
        greeter.Greet("Alice");
        calc.GetName();

        // Assert — verify the cross-mock order
        Mock.VerifyInOrder(() =>
        {
            calcMock.Verify.Add(1, 2).WasCalled();
            greeterMock.Verify.Greet("Alice").WasCalled();
            calcMock.Verify.GetName().WasCalled();
        });
    }

    [Test]
    public async Task Cross_Mock_Ordering_Fails_When_Wrong()
    {
        // Arrange
        var calcMock = Mock.Of<ICalculator>();
        var greeterMock = Mock.Of<IGreeter>();
        ICalculator calc = calcMock.Object;
        IGreeter greeter = greeterMock.Object;

        // Act — greeter called first, then calculator
        greeter.Greet("Alice");
        calc.Add(1, 2);

        // Assert — verify wrong order: expect calc before greeter
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            Mock.VerifyInOrder(() =>
            {
                calcMock.Verify.Add(1, 2).WasCalled();
                greeterMock.Verify.Greet("Alice").WasCalled();
            });
        });

        await Assert.That(exception.Message).Contains("Ordered verification failed");
    }

    [Test]
    public void Single_Call_In_Ordered_Verification_Passes()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // Act
        calc.Add(1, 2);

        // Assert — single call trivially passes
        Mock.VerifyInOrder(() =>
        {
            mock.Verify.Add(1, 2).WasCalled();
        });
    }

    [Test]
    public async Task Error_Message_Describes_Expected_Vs_Actual_Order()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // Act — call Log then Add
        calc.Log("hello");
        calc.Add(1, 2);

        // Assert — verify wrong order: expect Add before Log
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            Mock.VerifyInOrder(() =>
            {
                mock.Verify.Add(1, 2).WasCalled();
                mock.Verify.Log("hello").WasCalled();
            });
        });

        // Verify the error message contains useful ordering information
        await Assert.That(exception.Message).Contains("Ordered verification failed");
        await Assert.That(exception.Message).Contains("out of order");
        await Assert.That(exception.Message).Contains("Expected calls in this order");
        await Assert.That(exception.Message).Contains("Actual call order");
    }

    [Test]
    public void Empty_Ordered_Verification_Passes()
    {
        // An empty verification block should not throw
        Mock.VerifyInOrder(() =>
        {
            // No verifications
        });
    }

    [Test]
    public async Task Missing_Call_Fails_With_Descriptive_Message()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // Act — only call Add
        calc.Add(1, 2);

        // Assert — verify Add then GetName, but GetName was never called
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            Mock.VerifyInOrder(() =>
            {
                mock.Verify.Add(1, 2).WasCalled();
                mock.Verify.GetName().WasCalled();
            });
        });

        await Assert.That(exception.Message).Contains("0 matching call(s) found");
    }

    [Test]
    public void Multiple_Calls_Same_Method_Correct_Order()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // Act
        calc.Add(1, 2);
        calc.Add(3, 4);

        // Assert — verify both in order
        Mock.VerifyInOrder(() =>
        {
            mock.Verify.Add(1, 2).WasCalled();
            mock.Verify.Add(3, 4).WasCalled();
        });
    }

    [Test]
    public async Task Multiple_Calls_Same_Method_Wrong_Order()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // Act — call with (3,4) first, then (1,2)
        calc.Add(3, 4);
        calc.Add(1, 2);

        // Assert — verify (1,2) before (3,4) should fail
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            Mock.VerifyInOrder(() =>
            {
                mock.Verify.Add(1, 2).WasCalled();
                mock.Verify.Add(3, 4).WasCalled();
            });
        });

        await Assert.That(exception.Message).Contains("Ordered verification failed");
    }

    [Test]
    public void VerifyInOrder_With_Times_Exactly_Multiple()
    {
        // Arrange — call Add(1,2) 3 times, then GetName once
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        calc.Add(1, 2);
        calc.Add(1, 2);
        calc.GetName();

        // Assert — verify Add was called exactly 3 times, then GetName once
        Mock.VerifyInOrder(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.Exactly(3));
            mock.Verify.GetName().WasCalled(Times.Once);
        });
    }

    [Test]
    public async Task VerifyInOrder_With_Times_Never_Throws_InvalidOperationException()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        // Assert — Times.Never is not allowed in VerifyInOrder
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            Mock.VerifyInOrder(() =>
            {
                mock.Verify.Add(1, 2).WasCalled(Times.Never);
            });
        });

        await Assert.That(exception.Message).Contains("Times.Never");
    }

    [Test]
    public async Task VerifyInOrder_With_Times_AtMost_Throws_InvalidOperationException()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        // Assert — Times.AtMost allows zero, not enforceable in ordered mode
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            Mock.VerifyInOrder(() =>
            {
                mock.Verify.Add(1, 2).WasCalled(Times.AtMost(3));
            });
        });

        await Assert.That(exception.Message).Contains("Times.AtMost");
    }

    [Test]
    public async Task VerifyInOrder_With_Times_Between_Zero_Min_Throws_InvalidOperationException()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        // Assert — Between(0, N) allows zero, not enforceable in ordered mode
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            Mock.VerifyInOrder(() =>
            {
                mock.Verify.Add(1, 2).WasCalled(Times.Between(0, 3));
            });
        });

        await Assert.That(exception.Message).Contains("Times.Between(0, N)");
    }

    [Test]
    public void VerifyInOrder_With_Times_AtLeast_Passes()
    {
        // Arrange — AtLeast is allowed since min > 0
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        calc.Add(1, 2);
        calc.Add(1, 2);
        calc.Add(1, 2);
        calc.GetName();

        // Assert — verify Add was called at least 2 times (actual: 3), then GetName
        Mock.VerifyInOrder(() =>
        {
            mock.Verify.Add(1, 2).WasCalled(Times.AtLeast(2));
            mock.Verify.GetName().WasCalled();
        });
    }

    [Test]
    public void VerifyInOrder_Marks_Calls_As_Verified_For_VerifyNoOtherCalls()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // Act
        calc.Add(1, 2);
        calc.GetName();

        // Assert — VerifyInOrder should mark calls as verified
        Mock.VerifyInOrder(() =>
        {
            mock.Verify.Add(1, 2).WasCalled();
            mock.Verify.GetName().WasCalled();
        });

        // This should pass because the calls above were verified in VerifyInOrder
        mock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task VerifyInOrder_Partial_Verification_Leaves_Unverified_Calls()
    {
        // Arrange
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        // Act — three calls
        calc.Add(1, 2);
        calc.GetName();
        calc.Log("hello");

        // Assert — only verify first two in order
        Mock.VerifyInOrder(() =>
        {
            mock.Verify.Add(1, 2).WasCalled();
            mock.Verify.GetName().WasCalled();
        });

        // Log("hello") was not verified, so this should fail
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            mock.VerifyNoOtherCalls();
        });

        await Assert.That(exception.Message).Contains("Log(hello)");
    }

    [Test]
    public void VerifyInOrder_Interleaved_Multi_Call_Correct_Group_Order()
    {
        // Regression test for group-based ordering:
        // A(1), B, A(2) — verifying A(Times.Exactly(2)) then B should fail
        // because one of A's calls is after B
        var mock = Mock.Of<ICalculator>();
        ICalculator calc = mock.Object;

        calc.Add(1, 2); // first A
        calc.GetName(); // B
        calc.Add(1, 2); // second A

        // This should fail because A's max sequence > B's min sequence
        var exception = Assert.Throws<MockVerificationException>(() =>
        {
            Mock.VerifyInOrder(() =>
            {
                mock.Verify.Add(1, 2).WasCalled(Times.Exactly(2));
                mock.Verify.GetName().WasCalled();
            });
        });
    }
}
