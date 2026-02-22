using TUnit.Mocks.Arguments;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Interfaces for testing multiple interface mocking.
/// </summary>
public interface IMultiLogger
{
    void Log(string message);
    string LastMessage { get; }
}

public interface IMultiDisposable
{
    void Dispose();
    bool IsDisposed { get; }
}

public interface IMultiSerializable
{
    string Serialize();
    void Deserialize(string data);
}

public interface IMultiCloneable
{
    object Clone();
    bool CanClone { get; }
}

/// <summary>
/// US13 Tests: Multiple interface mocking (Mock.Of&lt;T1, T2&gt;()).
/// </summary>
public class MultipleInterfaceTests
{
    [Test]
    public async Task Mock_Of_Two_Interfaces_Creates_Mock()
    {
        // Arrange & Act
        var mock = Mock.Of<IMultiLogger, IMultiDisposable>();

        // Assert — mock implements both interfaces
        await Assert.That(mock.Object).IsNotNull();
        await Assert.That(mock.Object is IMultiLogger).IsTrue();
        await Assert.That(mock.Object is IMultiDisposable).IsTrue();
    }

    [Test]
    public async Task Can_Setup_Methods_From_Primary_Interface()
    {
        // Arrange
        var mock = Mock.Of<IMultiLogger, IMultiDisposable>();
        mock.Setup.Log(Arg.Any<string>());

        // Act
        var logger = mock.Object;
        logger.Log("test message");

        // Assert — call recorded and verifiable
        mock.Verify.Log(Arg.Is("test message")).WasCalled();
    }

    [Test]
    public async Task Can_Cast_To_Secondary_Interface()
    {
        // Arrange
        var mock = Mock.Of<IMultiLogger, IMultiDisposable>();

        // Act — cast to secondary interface
        var disposable = (IMultiDisposable)mock.Object;

        // Assert — cast succeeds and methods work (return defaults in loose mode)
        await Assert.That(disposable).IsNotNull();
        await Assert.That(disposable.IsDisposed).IsFalse(); // default bool
    }

    [Test]
    public async Task Secondary_Interface_Methods_Return_Defaults()
    {
        // Arrange
        var mock = Mock.Of<IMultiLogger, IMultiSerializable>();

        // Act — use secondary interface methods
        var serializable = (IMultiSerializable)mock.Object;
        var result = serializable.Serialize();

        // Assert — returns smart default (empty string for string)
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Mock_Of_Three_Interfaces()
    {
        // Arrange
        var mock = Mock.Of<IMultiLogger, IMultiDisposable, IMultiSerializable>();

        // Assert — implements all three
        await Assert.That(mock.Object is IMultiLogger).IsTrue();
        await Assert.That(mock.Object is IMultiDisposable).IsTrue();
        await Assert.That(mock.Object is IMultiSerializable).IsTrue();
    }

    [Test]
    public async Task Multi_Mock_Shares_Single_Engine()
    {
        // Arrange
        var mock = Mock.Of<IMultiLogger, IMultiDisposable>();
        mock.Setup.Log(Arg.Any<string>());

        // Act — call primary and secondary interface methods
        mock.Object.Log("hello");
        ((IMultiDisposable)mock.Object).Dispose();

        // Assert — all calls recorded in invocations
        await Assert.That(mock.Invocations).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Mock_Of_Four_Interfaces()
    {
        // Arrange
        var mock = Mock.Of<IMultiLogger, IMultiDisposable, IMultiSerializable, IMultiCloneable>();

        // Assert — implements all four
        await Assert.That(mock.Object is IMultiLogger).IsTrue();
        await Assert.That(mock.Object is IMultiDisposable).IsTrue();
        await Assert.That(mock.Object is IMultiSerializable).IsTrue();
        await Assert.That(mock.Object is IMultiCloneable).IsTrue();
    }

    [Test]
    public async Task Mock_Of_Four_Interfaces_Can_Use_Primary()
    {
        // Arrange
        var mock = Mock.Of<IMultiLogger, IMultiDisposable, IMultiSerializable, IMultiCloneable>();
        mock.Setup.Log(Arg.Any<string>());

        // Act
        mock.Object.Log("hello from four-interface mock");

        // Assert
        mock.Verify.Log("hello from four-interface mock").WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Mock_Of_Four_Interfaces_Can_Cast_To_All()
    {
        // Arrange
        var mock = Mock.Of<IMultiLogger, IMultiDisposable, IMultiSerializable, IMultiCloneable>();

        // Act — cast to each secondary interface and call methods
        var disposable = (IMultiDisposable)mock.Object;
        disposable.Dispose();

        var serializable = (IMultiSerializable)mock.Object;
        var serialized = serializable.Serialize();

        var cloneable = (IMultiCloneable)mock.Object;
        var canClone = cloneable.CanClone;

        // Assert — all operations succeeded
        await Assert.That(disposable).IsNotNull();
        await Assert.That(serialized).IsEmpty(); // default string
        await Assert.That(canClone).IsFalse(); // default bool
    }

    [Test]
    public async Task Mock_Of_Four_Interfaces_Tracks_All_Calls()
    {
        // Arrange
        var mock = Mock.Of<IMultiLogger, IMultiDisposable, IMultiSerializable, IMultiCloneable>();

        // Act — call methods from all interfaces
        mock.Object.Log("msg");
        ((IMultiDisposable)mock.Object).Dispose();
        ((IMultiSerializable)mock.Object).Serialize();
        ((IMultiCloneable)mock.Object).Clone();

        // Assert — all 4 calls tracked
        await Assert.That(mock.Invocations).Count().IsEqualTo(4);
    }

    [Test]
    public async Task Mock_Of_Two_Interfaces_With_Strict_Behavior()
    {
        // Arrange
        var mock = Mock.Of<IMultiLogger, IMultiDisposable>(MockBehavior.Strict);

        // Assert — strict behavior inherited
        await Assert.That(mock.Behavior).IsEqualTo(MockBehavior.Strict);
    }
}
