namespace TUnit.Mock.Tests;

/// <summary>
/// Interfaces with custom delegate events for testing.
/// </summary>
public interface ICustomEventService
{
    event Action OnSimpleAction;
    event Action<string> OnStringAction;
    event Action<string, int> OnMultiParamAction;
    event EventHandler<string> OnStandardEvent;
}

/// <summary>
/// US16 Tests: Custom delegate event support.
/// </summary>
public class CustomDelegateEventTests
{
    [Test]
    public async Task Raise_Action_Event()
    {
        // Arrange
        var mock = Mock.Of<ICustomEventService>();
        var wasCalled = false;

        mock.Object.OnSimpleAction += () => wasCalled = true;

        // Act
        mock.Raise!.OnSimpleAction();

        // Assert
        await Assert.That(wasCalled).IsTrue();
    }

    [Test]
    public async Task Raise_Action_String_Event()
    {
        // Arrange
        var mock = Mock.Of<ICustomEventService>();
        string? receivedValue = null;

        mock.Object.OnStringAction += value => receivedValue = value;

        // Act
        mock.Raise!.OnStringAction("hello");

        // Assert
        await Assert.That(receivedValue).IsEqualTo("hello");
    }

    [Test]
    public async Task Raise_MultiParam_Action_Event()
    {
        // Arrange
        var mock = Mock.Of<ICustomEventService>();
        string? receivedName = null;
        int receivedAge = 0;

        mock.Object.OnMultiParamAction += (name, age) =>
        {
            receivedName = name;
            receivedAge = age;
        };

        // Act
        mock.Raise!.OnMultiParamAction("Alice", 30);

        // Assert
        await Assert.That(receivedName).IsEqualTo("Alice");
        await Assert.That(receivedAge).IsEqualTo(30);
    }

    [Test]
    public async Task Raise_Standard_EventHandler_Still_Works()
    {
        // Arrange
        var mock = Mock.Of<ICustomEventService>();
        string? receivedValue = null;

        mock.Object.OnStandardEvent += (sender, e) => receivedValue = e;

        // Act
        mock.Raise!.OnStandardEvent("world");

        // Assert
        await Assert.That(receivedValue).IsEqualTo("world");
    }

    [Test]
    public async Task Multiple_Subscribers_All_Notified()
    {
        // Arrange
        var mock = Mock.Of<ICustomEventService>();
        var results = new List<string>();

        mock.Object.OnStringAction += val => results.Add("sub1:" + val);
        mock.Object.OnStringAction += val => results.Add("sub2:" + val);

        // Act
        mock.Raise!.OnStringAction("test");

        // Assert
        await Assert.That(results).Count().IsEqualTo(2);
        await Assert.That(results[0]).IsEqualTo("sub1:test");
        await Assert.That(results[1]).IsEqualTo("sub2:test");
    }

    [Test]
    public async Task Unsubscribe_Stops_Notifications()
    {
        // Arrange
        var mock = Mock.Of<ICustomEventService>();
        var callCount = 0;

        Action<string> handler = _ => callCount++;
        mock.Object.OnStringAction += handler;

        // Act — raise, unsubscribe, raise again
        mock.Raise!.OnStringAction("first");
        mock.Object.OnStringAction -= handler;
        mock.Raise!.OnStringAction("second");

        // Assert
        await Assert.That(callCount).IsEqualTo(1);
    }

    [Test]
    public async Task No_Subscribers_Does_Not_Throw()
    {
        // Arrange
        var mock = Mock.Of<ICustomEventService>();

        // Act & Assert — should not throw
        mock.Raise!.OnSimpleAction();
        mock.Raise!.OnStringAction("nobody");
        mock.Raise!.OnMultiParamAction("nobody", 0);
    }
}
