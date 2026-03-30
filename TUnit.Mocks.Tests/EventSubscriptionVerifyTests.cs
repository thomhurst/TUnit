namespace TUnit.Mocks.Tests;

/// <summary>
/// US16 Tests: Event subscription verification.
/// </summary>
public class EventSubscriptionVerifyTests
{
    [Test]
    public async Task WasEventSubscribed_Returns_True_After_Subscribe()
    {
        // Arrange
        var mock = Mock.Of<ICustomEventService>();

        // Act
        mock.Object.OnStringAction += _ => { };

        // Assert
        await Assert.That(mock.Events.OnStringAction.WasSubscribed).IsTrue();
    }

    [Test]
    public async Task WasEventSubscribed_Returns_False_When_No_Subscription()
    {
        // Arrange
        var mock = Mock.Of<ICustomEventService>();

        // Assert
        await Assert.That(mock.Events.OnStringAction.WasSubscribed).IsFalse();
    }

    [Test]
    public async Task GetEventSubscriberCount_Tracks_Subscriptions()
    {
        // Arrange
        var mock = Mock.Of<ICustomEventService>();

        // Act
        mock.Object.OnStringAction += _ => { };
        mock.Object.OnStringAction += _ => { };

        // Assert
        await Assert.That(mock.Events.OnStringAction.SubscriberCount).IsEqualTo(2);
    }

    [Test]
    public async Task GetEventSubscriberCount_Decrements_On_Unsubscribe()
    {
        // Arrange
        var mock = Mock.Of<ICustomEventService>();
        Action<string> handler = _ => { };

        // Act
        mock.Object.OnStringAction += handler;
        mock.Object.OnStringAction += _ => { };
        mock.Object.OnStringAction -= handler;

        // Assert — 2 subscribes, 1 unsubscribe = 1 remaining
        await Assert.That(mock.Events.OnStringAction.SubscriberCount).IsEqualTo(1);
    }

    [Test]
    public async Task Different_Events_Tracked_Independently()
    {
        // Arrange
        var mock = Mock.Of<ICustomEventService>();

        // Act
        mock.Object.OnStringAction += _ => { };
        mock.Object.OnSimpleAction += () => { };
        mock.Object.OnSimpleAction += () => { };

        // Assert
        await Assert.That(mock.Events.OnStringAction.SubscriberCount).IsEqualTo(1);
        await Assert.That(mock.Events.OnSimpleAction.SubscriberCount).IsEqualTo(2);
        await Assert.That(mock.Events.OnMultiParamAction.WasSubscribed).IsFalse();
    }

    [Test]
    public async Task Reset_Clears_Subscription_History()
    {
        // Arrange
        var mock = Mock.Of<ICustomEventService>();
        mock.Object.OnStringAction += _ => { };

        // Act
        mock.Reset();

        // Assert
        await Assert.That(mock.Events.OnStringAction.WasSubscribed).IsFalse();
        await Assert.That(mock.Events.OnStringAction.SubscriberCount).IsEqualTo(0);
    }
}
