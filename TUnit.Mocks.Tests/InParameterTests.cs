using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

// ─── Interfaces with in parameters ──────────────────────────────────────────

public interface ICalculatorWithIn
{
    int Add(in int a, in int b);
    void Log(in string message);
    double Compute(in int value, double factor);
}

public readonly struct Point
{
    public int X { get; init; }
    public int Y { get; init; }
}

public interface IGeometry
{
    double Distance(in Point a, in Point b);
    bool Contains(in Point point, int radius);
}

// ─── Tests ──────────────────────────────────────────────────────────────────

/// <summary>
/// Tests for 'in' (readonly ref) parameters — verifying argument matching,
/// returns, callbacks, throws, and verification all work correctly.
/// </summary>
public class InParameterTests
{
    [Test]
    public async Task In_Params_Returns_Value()
    {
        var mock = Mock.Of<ICalculatorWithIn>();
        mock.Add(1, 2).Returns(3);

        var result = mock.Object.Add(1, 2);

        await Assert.That(result).IsEqualTo(3);
    }

    [Test]
    public async Task In_Params_Arg_Any_Matching()
    {
        var mock = Mock.Of<ICalculatorWithIn>();
        mock.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);

        var result = mock.Object.Add(10, 20);

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task In_Params_Specific_Value_Matching()
    {
        var mock = Mock.Of<ICalculatorWithIn>();
        mock.Add(5, 10).Returns(15);
        mock.Add(1, 1).Returns(2);

        var r1 = mock.Object.Add(5, 10);
        var r2 = mock.Object.Add(1, 1);

        await Assert.That(r1).IsEqualTo(15);
        await Assert.That(r2).IsEqualTo(2);
    }

    [Test]
    public async Task In_Params_Void_Method()
    {
        var wasCalled = false;
        var mock = Mock.Of<ICalculatorWithIn>();
        mock.Log(Arg.Any<string>()).Callback(() => wasCalled = true);

        mock.Object.Log("hello");

        await Assert.That(wasCalled).IsTrue();
    }

    [Test]
    public void In_Params_Throws()
    {
        var mock = Mock.Of<ICalculatorWithIn>();
        mock.Add(Arg.Any<int>(), Arg.Any<int>()).Throws<InvalidOperationException>();

        Assert.Throws<InvalidOperationException>(() => mock.Object.Add(1, 2));
    }

    [Test]
    public async Task In_Params_Verify_WasCalled()
    {
        var mock = Mock.Of<ICalculatorWithIn>();
        mock.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(0);

        mock.Object.Add(1, 2);
        mock.Object.Add(3, 4);

        mock.Add(Arg.Any<int>(), Arg.Any<int>()).WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task In_Params_Verify_WasNeverCalled()
    {
        var mock = Mock.Of<ICalculatorWithIn>();

        mock.Add(1, 2).WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task In_Params_Mixed_With_Regular_Params()
    {
        var mock = Mock.Of<ICalculatorWithIn>();
        mock.Compute(Arg.Any<int>(), Arg.Any<double>()).Returns(99.5);

        var result = mock.Object.Compute(42, 2.0);

        await Assert.That(result).IsEqualTo(99.5);
    }

    [Test]
    public async Task In_Params_Specific_Mixed_Matching()
    {
        var mock = Mock.Of<ICalculatorWithIn>();
        mock.Compute(10, 2.5).Returns(25.0);
        mock.Compute(20, 3.0).Returns(60.0);

        var r1 = mock.Object.Compute(10, 2.5);
        var r2 = mock.Object.Compute(20, 3.0);

        await Assert.That(r1).IsEqualTo(25.0);
        await Assert.That(r2).IsEqualTo(60.0);
    }

    [Test]
    public async Task In_Struct_Params()
    {
        var mock = Mock.Of<IGeometry>();
        mock.Distance(Arg.Any<Point>(), Arg.Any<Point>()).Returns(5.0);

        var origin = new Point { X = 0, Y = 0 };
        var target = new Point { X = 3, Y = 4 };
        var result = mock.Object.Distance(origin, target);

        await Assert.That(result).IsEqualTo(5.0);
    }

    [Test]
    public async Task In_Struct_Mixed_With_Regular()
    {
        var mock = Mock.Of<IGeometry>();
        mock.Contains(Arg.Any<Point>(), Arg.Any<int>()).Returns(true);

        var center = new Point { X = 5, Y = 5 };
        var result = mock.Object.Contains(center, 10);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task In_Params_Callback_With_Args()
    {
        int capturedA = 0, capturedB = 0;
        var mock = Mock.Of<ICalculatorWithIn>();
        mock.Add(Arg.Any<int>(), Arg.Any<int>())
            .Callback((object?[] args) =>
            {
                capturedA = (int)args[0]!;
                capturedB = (int)args[1]!;
            })
            .Returns(0);

        mock.Object.Add(7, 8);

        await Assert.That(capturedA).IsEqualTo(7);
        await Assert.That(capturedB).IsEqualTo(8);
    }

    [Test]
    public async Task In_Params_Verify_Specific_Values()
    {
        var mock = Mock.Of<ICalculatorWithIn>();
        mock.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(0);

        mock.Object.Add(1, 2);
        mock.Object.Add(3, 4);

        mock.Add(1, 2).WasCalled(Times.Once);
        mock.Add(3, 4).WasCalled(Times.Once);
        mock.Add(5, 6).WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task In_String_Param_Matching()
    {
        var messages = new List<string>();
        var mock = Mock.Of<ICalculatorWithIn>();
        mock.Log(Arg.Any<string>()).Callback((object?[] args) => messages.Add((string)args[0]!));

        mock.Object.Log("first");
        mock.Object.Log("second");

        await Assert.That(messages).HasCount().EqualTo(2);
        await Assert.That(messages[0]).IsEqualTo("first");
        await Assert.That(messages[1]).IsEqualTo("second");
    }

    [Test]
    public async Task In_Params_Arg_Is_Predicate()
    {
        var mock = Mock.Of<ICalculatorWithIn>();
        mock.Add(Arg.Is<int>(x => x > 0), Arg.Is<int>(x => x > 0)).Returns(100);

        var r1 = mock.Object.Add(5, 10);
        await Assert.That(r1).IsEqualTo(100);

        // Negative values don't match the predicate — returns default (0)
        var r2 = mock.Object.Add(-1, 5);
        await Assert.That(r2).IsEqualTo(0);
    }
}
