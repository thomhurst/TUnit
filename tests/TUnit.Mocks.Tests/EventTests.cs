using System.ComponentModel;
using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Regression interface for #5423: multiple multi-parameter events on the
/// same interface previously caused CS0128 in the generated RaiseEvent switch.
/// </summary>
// Public because the mock generator emits public extension types that take this interface as a parameter.
public interface IMultiEventNotifier : INotifyPropertyChanging, INotifyPropertyChanged
{
}

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
        var mock = IEventService.Mock();
        string? receivedMessage = null;

        IEventService svc = mock.Object;
        svc.OnMessage += (sender, msg) => receivedMessage = msg;

        // Act — raise the event through the mock
        mock.RaiseOnMessage("Hello!");

        // Assert
        await Assert.That(receivedMessage).IsEqualTo("Hello!");
    }

    [Test]
    public async Task Raise_Event_Notifies_Multiple_Subscribers()
    {
        // Arrange
        var mock = IEventService.Mock();
        var messages = new List<string>();

        IEventService svc = mock.Object;
        svc.OnMessage += (sender, msg) => messages.Add("sub1:" + msg);
        svc.OnMessage += (sender, msg) => messages.Add("sub2:" + msg);

        // Act
        mock.RaiseOnMessage("test");

        // Assert
        await Assert.That(messages).Count().IsEqualTo(2);
        await Assert.That(messages[0]).IsEqualTo("sub1:test");
        await Assert.That(messages[1]).IsEqualTo("sub2:test");
    }

    [Test]
    public async Task Raise_Event_With_No_Subscribers_Does_Not_Throw()
    {
        // Arrange
        var mock = IEventService.Mock();

        // Act & Assert — should not throw when no subscribers
        mock.RaiseOnMessage("nobody listening");
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Unsubscribe_From_Event_Stops_Notifications()
    {
        // Arrange
        var mock = IEventService.Mock();
        var callCount = 0;

        IEventService svc = mock.Object;
        EventHandler<string> handler = (sender, msg) => callCount++;
        svc.OnMessage += handler;

        // Act — raise once, unsubscribe, raise again
        mock.RaiseOnMessage("first");
        svc.OnMessage -= handler;
        mock.RaiseOnMessage("second");

        // Assert — only one notification
        await Assert.That(callCount).IsEqualTo(1);
    }

    /// <summary>
    /// Regression test for #5423. An interface inheriting two interfaces, each
    /// declaring a multi-parameter event, must produce a generated mock that
    /// compiles and dispatches each event independently through RaiseEvent.
    /// </summary>
    [Test]
    public async Task Mock_With_Multiple_Multi_Parameter_Events_Compiles_And_Raises()
    {
        var mock = IMultiEventNotifier.Mock();
        var notifier = mock.Object;

        string? changingProperty = null;
        string? changedProperty = null;
        notifier.PropertyChanging += (_, e) => changingProperty = e.PropertyName;
        notifier.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

        mock.RaisePropertyChanging(notifier, new PropertyChangingEventArgs("Foo"));
        mock.RaisePropertyChanged(notifier, new PropertyChangedEventArgs("Bar"));

        await Assert.That(changingProperty).IsEqualTo("Foo");
        await Assert.That(changedProperty).IsEqualTo("Bar");
    }
}
