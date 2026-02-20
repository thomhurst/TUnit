using TUnit.Mock;
using TUnit.Mock.Arguments;
using TUnit.Mock.Exceptions;
using TUnit.Mock.Verification;

namespace TUnit.Mock.Tests;

/// <summary>
/// Test classes used for partial mock tests.
/// </summary>
public abstract class AbstractService
{
    public abstract string GetName();
    public virtual int Calculate(int x) => x * 2;
    public string NonVirtualMethod() => "fixed";
}

public class ConcreteService
{
    public virtual string Greet(string name) => $"Hello, {name}!";
    public virtual int Add(int a, int b) => a + b;
}

public abstract class ServiceWithConstructor
{
    protected readonly string _prefix;

    protected ServiceWithConstructor(string prefix)
    {
        _prefix = prefix;
    }

    public abstract string Format(string value);
    public virtual string GetPrefix() => _prefix;
}

/// <summary>
/// US8 Integration Tests: Partial mocks of abstract and concrete classes.
/// </summary>
public class PartialMockTests
{
    [Test]
    public async Task OfPartial_Abstract_Class_Creates_Instance()
    {
        // Arrange & Act
        var mock = Mock.OfPartial<AbstractService>();

        // Assert
        await Assert.That(mock).IsNotNull();
        await Assert.That(mock.Object).IsNotNull();
    }

    [Test]
    public async Task Abstract_Method_Returns_Configured_Value()
    {
        // Arrange
        var mock = Mock.OfPartial<AbstractService>();
        mock.Setup.GetName().Returns("TestName");

        // Act
        var result = mock.Object.GetName();

        // Assert
        await Assert.That(result).IsEqualTo("TestName");
    }

    [Test]
    public async Task Unconfigured_Virtual_Method_Calls_Base_Implementation()
    {
        // Arrange
        var mock = Mock.OfPartial<AbstractService>();
        // GetName is abstract - configure it
        mock.Setup.GetName().Returns("TestName");
        // Calculate is virtual - do NOT configure it

        // Act
        var result = mock.Object.Calculate(5);

        // Assert - base implementation is x * 2
        await Assert.That(result).IsEqualTo(10);
    }

    [Test]
    public async Task Configured_Virtual_Method_Returns_Override_Instead_Of_Base()
    {
        // Arrange
        var mock = Mock.OfPartial<AbstractService>();
        mock.Setup.GetName().Returns("TestName");
        mock.Setup.Calculate(Arg.Any<int>()).Returns(42);

        // Act
        var result = mock.Object.Calculate(5);

        // Assert - should return configured value, not base (10)
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Concrete_Class_Partial_Mock_Calls_Base_When_Not_Configured()
    {
        // Arrange
        var mock = Mock.OfPartial<ConcreteService>();

        // Act - no setup, should call base
        var result = mock.Object.Greet("World");

        // Assert - base implementation
        await Assert.That(result).IsEqualTo("Hello, World!");
    }

    [Test]
    public async Task Concrete_Class_Override_Returns_Configured_Value()
    {
        // Arrange
        var mock = Mock.OfPartial<ConcreteService>();
        mock.Setup.Greet(Arg.Any<string>()).Returns("Mocked!");

        // Act
        var result = mock.Object.Greet("World");

        // Assert
        await Assert.That(result).IsEqualTo("Mocked!");
    }

    [Test]
    public async Task Concrete_Class_Multiple_Methods_Mixed_Setup()
    {
        // Arrange
        var mock = Mock.OfPartial<ConcreteService>();
        mock.Setup.Greet("Alice").Returns("Hi Alice!");
        // Don't setup Add - let it call base

        // Act
        var greetResult = mock.Object.Greet("Alice");
        var addResult = mock.Object.Add(3, 4);

        // Assert
        await Assert.That(greetResult).IsEqualTo("Hi Alice!");
        await Assert.That(addResult).IsEqualTo(7); // base: a + b
    }

    [Test]
    public async Task Constructor_Args_Passed_To_Base()
    {
        // Arrange
        var mock = Mock.OfPartial<ServiceWithConstructor>("PREFIX");
        mock.Setup.Format(Arg.Any<string>()).Returns("formatted");

        // Act - GetPrefix is virtual and unconfigured, so calls base which uses _prefix
        var prefix = mock.Object.GetPrefix();

        // Assert
        await Assert.That(prefix).IsEqualTo("PREFIX");
    }

    [Test]
    public async Task Constructor_Args_Abstract_Method_Override()
    {
        // Arrange
        var mock = Mock.OfPartial<ServiceWithConstructor>("test");
        mock.Setup.Format("value").Returns("test:value");

        // Act
        var result = mock.Object.Format("value");

        // Assert
        await Assert.That(result).IsEqualTo("test:value");
    }

    [Test]
    public async Task Non_Virtual_Method_Still_Works()
    {
        // Arrange
        var mock = Mock.OfPartial<AbstractService>();
        mock.Setup.GetName().Returns("Name");

        // Act - NonVirtualMethod is not virtual, so it runs the original implementation
        var result = mock.Object.NonVirtualMethod();

        // Assert
        await Assert.That(result).IsEqualTo("fixed");
    }

    [Test]
    public void Verify_Calls_On_Partial_Mock()
    {
        // Arrange
        var mock = Mock.OfPartial<ConcreteService>();
        mock.Setup.Greet(Arg.Any<string>()).Returns("Hi");

        // Act
        mock.Object.Greet("Alice");
        mock.Object.Greet("Bob");

        // Assert
        mock.Verify.Greet(Arg.Any<string>()).WasCalled(Times.Exactly(2));
    }

    [Test]
    public void Verify_Calls_On_Unconfigured_Virtual_Method()
    {
        // Arrange
        var mock = Mock.OfPartial<ConcreteService>();

        // Act - unconfigured, calls base
        mock.Object.Add(1, 2);
        mock.Object.Add(3, 4);

        // Assert - calls should still be recorded even when calling base
        mock.Verify.Add(Arg.Any<int>(), Arg.Any<int>()).WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Partial_Mock_With_Strict_Behavior_Abstract_Method_Throws_Without_Setup()
    {
        // Arrange
        var mock = Mock.OfPartial<AbstractService>(MockBehavior.Strict);

        // Act & Assert - abstract method with no setup in strict mode should throw
        var exception = Assert.Throws<MockStrictBehaviorException>(() =>
        {
            mock.Object.GetName();
        });

        await Assert.That(exception).IsNotNull();
    }

    [Test]
    public async Task Partial_Mock_With_Strict_Behavior_Virtual_Method_Calls_Base()
    {
        // Arrange - strict mode, but virtual methods should still call base when unconfigured
        var mock = Mock.OfPartial<ConcreteService>(MockBehavior.Strict);

        // Act - virtual method with no setup should call base (not throw)
        var result = mock.Object.Add(2, 3);

        // Assert
        await Assert.That(result).IsEqualTo(5);
    }
}
