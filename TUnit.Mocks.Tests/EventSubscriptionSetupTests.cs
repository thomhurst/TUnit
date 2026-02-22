namespace TUnit.Mocks.Tests;

public interface INotifyService
{
    event EventHandler DataReady;
}

public class EventSubscriptionSetupTests
{
    [Test]
    public async Task OnSubscribe_Callback_Fires_When_Handler_Subscribes()
    {
        var mock = Mock.Of<INotifyService>();
        var callbackFired = false;

        mock.Events!.DataReady.OnSubscribe(() => callbackFired = true);

        mock.Object.DataReady += (sender, args) => { };

        await Assert.That(callbackFired).IsTrue();
    }

    [Test]
    public async Task OnUnsubscribe_Callback_Fires_When_Handler_Unsubscribes()
    {
        var mock = Mock.Of<INotifyService>();
        var callbackFired = false;

        mock.Events!.DataReady.OnUnsubscribe(() => callbackFired = true);

        EventHandler handler = (sender, args) => { };
        mock.Object.DataReady += handler;
        mock.Object.DataReady -= handler;

        await Assert.That(callbackFired).IsTrue();
    }

    [Test]
    public async Task Multiple_Subscriptions_Fire_Callback_Each_Time()
    {
        var mock = Mock.Of<INotifyService>();
        var callCount = 0;

        mock.Events!.DataReady.OnSubscribe(() => callCount++);

        mock.Object.DataReady += (sender, args) => { };
        mock.Object.DataReady += (sender, args) => { };
        mock.Object.DataReady += (sender, args) => { };

        await Assert.That(callCount).IsEqualTo(3);
    }

    [Test]
    public async Task No_Callback_Fires_When_None_Configured()
    {
        var mock = Mock.Of<INotifyService>();

        // Subscribe without configuring any callback — should not throw
        mock.Object.DataReady += (sender, args) => { };

        await Assert.That(mock.Events!.DataReady.WasSubscribed).IsTrue();
    }

    [Test]
    public async Task Subscribe_And_Unsubscribe_Callbacks_Work_Independently()
    {
        var mock = Mock.Of<INotifyService>();
        var subscribeCount = 0;
        var unsubscribeCount = 0;

        mock.Events!.DataReady.OnSubscribe(() => subscribeCount++);
        mock.Events!.DataReady.OnUnsubscribe(() => unsubscribeCount++);

        EventHandler handler = (sender, args) => { };
        mock.Object.DataReady += handler;
        mock.Object.DataReady -= handler;
        mock.Object.DataReady += handler;

        await Assert.That(subscribeCount).IsEqualTo(2);
        await Assert.That(unsubscribeCount).IsEqualTo(1);
    }

    [Test]
    public async Task Reset_Clears_Subscription_Callbacks()
    {
        var mock = Mock.Of<INotifyService>();
        var callbackFired = false;

        mock.Events!.DataReady.OnSubscribe(() => callbackFired = true);
        mock.Reset();

        mock.Object.DataReady += (sender, args) => { };

        await Assert.That(callbackFired).IsFalse();
    }
}
