using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Tests for the generated Mock wrapper type that implements the interface directly,
/// and the C# 14 static extension member IFoo.Mock() syntax.
/// </summary>
public class MockWrapperTypeTests
{
    [Test]
    public async Task Mock_Of_Returns_Wrapper_That_Implements_Interface()
    {
        var mock = Mock.Of<IGreeter>();

        // The runtime type should implement IGreeter directly
        await Assert.That(mock is IGreeter).IsTrue();
    }

    [Test]
    public async Task Wrapper_Can_Be_Cast_To_Interface()
    {
        var mock = Mock.Of<IGreeter>();
        mock.Greet(Arg.Any<string>()).Returns("Hello!");

        // Cast via the wrapper type (which IS the interface)
        var greeter = (IGreeter)mock;
        var result = greeter.Greet("World");

        await Assert.That(result).IsEqualTo("Hello!");
    }

    [Test]
    public async Task Wrapper_Can_Be_Passed_As_Interface_Parameter()
    {
        var mock = Mock.Of<IGreeter>();
        mock.Greet(Arg.Any<string>()).Returns("Hi!");

        // Pass mock directly to a method expecting the interface
        var result = AcceptGreeter((IGreeter)mock);

        await Assert.That(result).IsEqualTo("Hi!");
    }

    [Test]
    public async Task Static_Extension_Mock_Returns_Typed_Wrapper()
    {
        // Use the C# 14 static extension: IGreeter.Mock()
        var mock = IGreeter.Mock();

        mock.Greet(Arg.Any<string>()).Returns("Extension!");

        // mock IS an IGreeter — no cast or .Object needed
        IGreeter greeter = mock;
        var result = greeter.Greet("Test");

        await Assert.That(result).IsEqualTo("Extension!");
    }

    [Test]
    public async Task Static_Extension_Mock_Can_Be_Used_In_Collection()
    {
        var mock = IGreeter.Mock();
        mock.Greet(Arg.Any<string>()).Returns("Listed!");

        // Can be used directly in a collection of the interface type
        List<IGreeter> greeters = [mock];

        var result = greeters[0].Greet("X");
        await Assert.That(result).IsEqualTo("Listed!");
    }

    [Test]
    public async Task Static_Extension_Mock_With_Strict_Behavior()
    {
        var mock = IGreeter.Mock(MockBehavior.Strict);

        mock.Greet("Alice").Returns("Hello Alice");

        IGreeter greeter = mock;
        var result = greeter.Greet("Alice");

        await Assert.That(result).IsEqualTo("Hello Alice");
    }

    [Test]
    public async Task Static_Extension_Mock_Setup_And_Verify()
    {
        var mock = IGreeter.Mock();
        mock.Greet(Arg.Any<string>()).Returns("Verified!");

        IGreeter greeter = mock;
        greeter.Greet("Test");

        mock.Greet("Test").WasCalled();
    }

    [Test]
    public async Task Wrapper_Preserves_Mock_Functionality()
    {
        var mock = ICalculator.Mock();

        mock.Add(1, 2).Returns(3);
        mock.GetName().Returns("Calculator");

        ICalculator calc = mock;
        var sum = calc.Add(1, 2);
        var name = calc.GetName();

        await Assert.That(sum).IsEqualTo(3);
        await Assert.That(name).IsEqualTo("Calculator");
    }

    private static string AcceptGreeter(IGreeter greeter)
    {
        return greeter.Greet("Test");
    }
}
