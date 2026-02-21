using TUnit.Mock;
using TUnit.Mock.Arguments;
using TUnit.Mock.Exceptions;
using TUnit.Mock.Verification;

namespace TUnit.Mock.Tests;

/// <summary>
/// Interfaces for auto-mock testing — a chain of interface return types.
/// </summary>
public interface IServiceA
{
    IServiceB GetServiceB();
    string GetName();
}

public interface IServiceB
{
    IServiceC GetServiceC();
    int GetValue();
}

public interface IServiceC
{
    string GetDescription();
}

/// <summary>
/// Interface that returns another interface via async method.
/// </summary>
public interface IAsyncProvider
{
    Task<IServiceB> GetServiceAsync();
}

/// <summary>
/// Circular reference: INodeA references INodeB, INodeB references INodeA.
/// </summary>
public interface INodeA
{
    INodeB GetNext();
}

public interface INodeB
{
    INodeA GetPrevious();
}

/// <summary>
/// US1 Integration Tests: Recursive/auto mocking — interface return types automatically
/// return functional mocks instead of null.
/// </summary>
public class AutoMockTests
{
    [Test]
    public async Task Interface_Return_Type_Auto_Mocked()
    {
        // Arrange
        var mock = Mock.Of<IServiceA>();

        // Act — no setup for GetServiceB, should auto-mock
        var serviceB = mock.Object.GetServiceB();

        // Assert — should NOT be null
        await Assert.That(serviceB).IsNotNull();
    }

    [Test]
    public async Task Auto_Mocked_Return_Is_Functional()
    {
        // Arrange
        var mock = Mock.Of<IServiceA>();

        // Act
        var serviceB = mock.Object.GetServiceB();
        var value = serviceB.GetValue();

        // Assert — auto-mocked IServiceB returns default int (0)
        await Assert.That(value).IsEqualTo(0);
    }

    [Test]
    public async Task Same_Instance_On_Repeated_Calls()
    {
        // Arrange
        var mock = Mock.Of<IServiceA>();

        // Act
        var serviceB1 = mock.Object.GetServiceB();
        var serviceB2 = mock.Object.GetServiceB();

        // Assert — same cached instance
        await Assert.That(serviceB1).IsSameReferenceAs(serviceB2);
    }

    [Test]
    public async Task Nested_Chain_A_To_B_To_C()
    {
        // Arrange
        var mock = Mock.Of<IServiceA>();

        // Act — navigate the chain: A → B → C
        var serviceB = mock.Object.GetServiceB();
        var serviceC = serviceB.GetServiceC();

        // Assert — entire chain is auto-mocked
        await Assert.That(serviceC).IsNotNull();
    }

    [Test]
    public async Task Nested_Chain_Returns_Default_Values()
    {
        // Arrange
        var mock = Mock.Of<IServiceA>();

        // Act
        var serviceC = mock.Object.GetServiceB().GetServiceC();
        var description = serviceC.GetDescription();

        // Assert — default for string
        await Assert.That(description).IsEqualTo("");
    }

    [Test]
    public async Task Auto_Mock_Configurable_Via_GetAutoMock()
    {
        // Arrange
        var mock = Mock.Of<IServiceA>();

        // Trigger auto-mock creation
        var serviceB = mock.Object.GetServiceB();

        // Retrieve and configure the auto-mock
        var autoMock = mock.GetAutoMock<IServiceB>("GetServiceB");
        autoMock.Setup.GetValue().Returns(42);

        // Act
        var value = serviceB.GetValue();

        // Assert
        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public void Strict_Mode_Does_Not_Auto_Mock()
    {
        // Arrange
        var mock = Mock.Of<IServiceA>(MockBehavior.Strict);
        mock.Setup.GetName().Returns("test");

        // Act & Assert — strict mode throws for unconfigured method
        Assert.Throws<MockStrictBehaviorException>(() =>
        {
            mock.Object.GetServiceB();
        });
    }

    [Test]
    public async Task Explicit_Setup_Overrides_Auto_Mock()
    {
        // Arrange
        var mock = Mock.Of<IServiceA>();
        var customServiceB = Mock.Of<IServiceB>();
        customServiceB.Setup.GetValue().Returns(99);
        mock.Setup.GetServiceB().Returns(customServiceB.Object);

        // Act
        var serviceB = mock.Object.GetServiceB();
        var value = serviceB.GetValue();

        // Assert — explicit setup takes priority over auto-mock
        await Assert.That(value).IsEqualTo(99);
    }

    [Test]
    public async Task Auto_Mock_With_Circular_References()
    {
        // Arrange
        var mock = Mock.Of<INodeA>();

        // Act — navigate circular reference
        var nodeB = mock.Object.GetNext();

        // Assert — auto-mocked, not null
        await Assert.That(nodeB).IsNotNull();
    }

    [Test]
    public async Task Async_Method_Returns_Auto_Mock()
    {
        // Arrange
        var mock = Mock.Of<IAsyncProvider>();

        // Act — async method returning Task<IServiceB>
        var serviceB = await mock.Object.GetServiceAsync();

        // Assert — auto-mocked
        await Assert.That(serviceB).IsNotNull();
    }

    [Test]
    public async Task Non_Interface_Return_Type_Returns_Default()
    {
        // Arrange
        var mock = Mock.Of<IServiceA>();

        // Act — GetName returns string, not an interface
        var name = mock.Object.GetName();

        // Assert — returns smart default (empty string)
        await Assert.That(name).IsEqualTo("");
    }
}
