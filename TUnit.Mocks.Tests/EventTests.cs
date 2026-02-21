using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Test interface with events for mocking.
/// </summary>
public interface IEventService
{
    event EventHandler<string> OnMessage;
}

/// <summary>
/// US5 Integration Tests: Event raising.
/// </summary>
public class EventTests
{
    [Test]
    public async Task Raise_Event_Notifies_Subscriber()
    {
        // Arrange
        var mock = Mock.Of<IEventService>();
        string? receivedMessage = null;

        IEventService svc = mock.Object;
        svc.OnMessage += (sender, msg) => receivedMessage = msg;

        // Act — raise the event through the mock
        mock.Raise.OnMessage("Hello!");

        // Assert
        await Assert.That(receivedMessage).IsEqualTo("Hello!");
    }

    [Test]
    public async Task Raise_Event_Notifies_Multiple_Subscribers()
    {
        // Arrange
        var mock = Mock.Of<IEventService>();
        var messages = new List<string>();

        IEventService svc = mock.Object;
        svc.OnMessage += (sender, msg) => messages.Add("sub1:" + msg);
        svc.OnMessage += (sender, msg) => messages.Add("sub2:" + msg);

        // Act
        mock.Raise.OnMessage("test");

        // Assert
        await Assert.That(messages).Count().IsEqualTo(2);
        await Assert.That(messages[0]).IsEqualTo("sub1:test");
        await Assert.That(messages[1]).IsEqualTo("sub2:test");
    }

    [Test]
    public async Task Raise_Event_With_No_Subscribers_Does_Not_Throw()
    {
        // Arrange
        var mock = Mock.Of<IEventService>();

        // Act & Assert — should not throw when no subscribers
        mock.Raise.OnMessage("nobody listening");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Unsubscribe_From_Event_Stops_Notifications()
    {
        // Arrange
        var mock = Mock.Of<IEventService>();
        var callCount = 0;

        IEventService svc = mock.Object;
        EventHandler<string> handler = (sender, msg) => callCount++;
        svc.OnMessage += handler;

        // Act — raise once, unsubscribe, raise again
        mock.Raise.OnMessage("first");
        svc.OnMessage -= handler;
        mock.Raise.OnMessage("second");

        // Assert — only one notification
        await Assert.That(callCount).IsEqualTo(1);
    }
}
