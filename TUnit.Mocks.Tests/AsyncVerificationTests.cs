using TUnit.Mocks.Arguments;
using TUnit.Mocks.Assertions;

namespace TUnit.Mocks.Tests;

public class AsyncVerificationTests
{
    [Test]
    public async Task WasCalled_Times_Once_Passes()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2);

        await Assert.That(mock.Verify.Add(Arg.Any<int>(), Arg.Any<int>()))
            .WasCalled(Times.Once);
    }

    [Test]
    public async Task WasCalled_Times_Exactly_Passes()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2);
        _ = calc.Add(3, 4);
        _ = calc.Add(5, 6);

        await Assert.That(mock.Verify.Add(Arg.Any<int>(), Arg.Any<int>()))
            .WasCalled(Times.Exactly(3));
    }

    [Test]
    public async Task WasCalled_Wrong_Count_Fails()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2);

        await Assert.ThrowsAsync(async () =>
            await Assert.That(mock.Verify.Add(Arg.Any<int>(), Arg.Any<int>()))
                .WasCalled(Times.Exactly(5)));
    }

    [Test]
    public async Task WasNeverCalled_Passes_When_Not_Called()
    {
        var mock = Mock.Of<ICalculator>();

        await Assert.That(mock.Verify.Add(Arg.Any<int>(), Arg.Any<int>()))
            .WasNeverCalled();
    }

    [Test]
    public async Task WasNeverCalled_Fails_When_Called()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2);

        await Assert.ThrowsAsync(async () =>
            await Assert.That(mock.Verify.Add(Arg.Any<int>(), Arg.Any<int>()))
                .WasNeverCalled());
    }

    [Test]
    public async Task WasCalled_AtLeastOnce_Passes()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2);
        _ = calc.Add(3, 4);

        await Assert.That(mock.Verify.Add(Arg.Any<int>(), Arg.Any<int>()))
            .WasCalled(Times.AtLeastOnce);
    }

    [Test]
    public async Task Property_Getter_WasCalled_Via_Assert()
    {
        var mock = Mock.Of<IPropertyService>();
        mock.Setup.Name.Returns("Calculator");

        IPropertyService svc = mock.Object;
        _ = svc.Name;
        _ = svc.Name;

        await Assert.That(mock.Verify.Name)
            .WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Property_Setter_WasCalled_Via_Assert()
    {
        var mock = Mock.Of<IPropertyService>();

        IPropertyService svc = mock.Object;
        svc.Count = 10;

        await Assert.That(mock.Verify.Count.Setter)
            .WasCalled(Times.Once);
    }

    [Test]
    public async Task Property_Getter_WasNeverCalled_Via_Assert()
    {
        var mock = Mock.Of<IPropertyService>();

        await Assert.That(mock.Verify.Name)
            .WasNeverCalled();
    }

    [Test]
    public async Task Multiple_Verifications_In_Sequence()
    {
        var mock = Mock.Of<ICalculator>();
        mock.Setup.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(42);
        mock.Setup.GetName().Returns("test");

        ICalculator calc = mock.Object;
        _ = calc.Add(1, 2);
        _ = calc.GetName();
        _ = calc.GetName();

        await Assert.That(mock.Verify.Add(Arg.Any<int>(), Arg.Any<int>()))
            .WasCalled(Times.Once);

        await Assert.That(mock.Verify.GetName())
            .WasCalled(Times.Exactly(2));
    }
}
