using System.ComponentModel;

namespace TUnit.Assertions.Tests;

public class EventAssertionTests
{
    #region PropertyChanged Tests

    [Test]
    public void PropertyChanged_Passes_When_Property_Changes()
    {
        var obj = new NotifyingClass();

        Assert.PropertyChanged(obj, nameof(NotifyingClass.Name), () =>
        {
            obj.Name = "New Value";
        });
    }

    [Test]
    public async Task PropertyChanged_Fails_When_Property_Does_Not_Change()
    {
        var obj = new NotifyingClass();

        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
        {
            Assert.PropertyChanged(obj, nameof(NotifyingClass.Name), () =>
            {
                // Do nothing - property doesn't change
            });
            return Task.CompletedTask;
        });

        await Assert.That(exception.Message).Contains("Name");
    }

    [Test]
    public async Task PropertyChanged_Fails_When_Different_Property_Changes()
    {
        var obj = new NotifyingClass();

        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
        {
            Assert.PropertyChanged(obj, nameof(NotifyingClass.Name), () =>
            {
                obj.Age = 30; // Different property
            });
            return Task.CompletedTask;
        });

        await Assert.That(exception.Message).Contains("Name");
    }

    [Test]
    public async Task PropertyChangedAsync_Passes_When_Property_Changes()
    {
        var obj = new NotifyingClass();

        await Assert.PropertyChangedAsync(obj, nameof(NotifyingClass.Name), async () =>
        {
            await Task.Delay(1);
            obj.Name = "New Value";
        });
    }

    [Test]
    public async Task PropertyChangedAsync_Fails_When_Property_Does_Not_Change()
    {
        var obj = new NotifyingClass();

        await Assert.That(async () =>
        {
            await Assert.PropertyChangedAsync(obj, nameof(NotifyingClass.Name), async () =>
            {
                await Task.Delay(1);
                // Do nothing
            });
        }).Throws<AssertionException>();
    }

    #endregion

    #region Raises Tests

    [Test]
    public async Task Raises_Passes_When_Event_Is_Raised()
    {
        var obj = new EventRaisingClass();

        var result = Assert.Raises<CustomEventArgs>(
            handler => obj.CustomEvent += handler,
            handler => obj.CustomEvent -= handler,
            () => obj.RaiseCustomEvent("test"));

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Arguments.Value).IsEqualTo("test");
        await Assert.That(result.Sender).IsSameReferenceAs(obj);
    }

    [Test]
    public async Task Raises_Fails_When_Event_Is_Not_Raised()
    {
        var obj = new EventRaisingClass();

        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
        {
            Assert.Raises<CustomEventArgs>(
                handler => obj.CustomEvent += handler,
                handler => obj.CustomEvent -= handler,
                () =>
                {
                    // Don't raise the event
                });
            return Task.CompletedTask;
        });

        await Assert.That(exception.Message).Contains("CustomEventArgs");
    }

    [Test]
    public async Task RaisesAsync_Passes_When_Event_Is_Raised()
    {
        var obj = new EventRaisingClass();

        var result = await Assert.RaisesAsync<CustomEventArgs>(
            handler => obj.CustomEvent += handler,
            handler => obj.CustomEvent -= handler,
            async () =>
            {
                await Task.Delay(1);
                obj.RaiseCustomEvent("async test");
            });

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Arguments.Value).IsEqualTo("async test");
    }

    [Test]
    public async Task RaisesAsync_Fails_When_Event_Is_Not_Raised()
    {
        var obj = new EventRaisingClass();

        await Assert.That(async () =>
        {
            await Assert.RaisesAsync<CustomEventArgs>(
                handler => obj.CustomEvent += handler,
                handler => obj.CustomEvent -= handler,
                async () =>
                {
                    await Task.Delay(1);
                    // Don't raise the event
                });
        }).Throws<AssertionException>();
    }

    [Test]
    public async Task RaisesAny_Passes_When_Any_Event_Is_Raised()
    {
        var obj = new EventRaisingClass();

        var result = Assert.RaisesAny<EventArgs>(
            handler => obj.GenericEvent += handler,
            handler => obj.GenericEvent -= handler,
            () => obj.RaiseGenericEvent());

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Sender).IsSameReferenceAs(obj);
    }

    [Test]
    public async Task RaisesAny_Passes_For_Derived_EventArgs()
    {
        var obj = new EventRaisingClass();

        // RaisesAny should accept derived types
        var result = Assert.RaisesAny<EventArgs>(
            handler => obj.CustomEvent += (s, e) => handler(s, e),
            handler => { }, // Can't really unsubscribe this way, but it works for the test
            () => obj.RaiseCustomEvent("derived"));

        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task RaisesAny_Fails_When_No_Event_Is_Raised()
    {
        var obj = new EventRaisingClass();

        var exception = await Assert.ThrowsAsync<AssertionException>(() =>
        {
            Assert.RaisesAny<EventArgs>(
                handler => obj.GenericEvent += handler,
                handler => obj.GenericEvent -= handler,
                () =>
                {
                    // Don't raise the event
                });
            return Task.CompletedTask;
        });

        await Assert.That(exception.Message).Contains("EventArgs");
    }

    [Test]
    public async Task RaisesAnyAsync_Passes_When_Event_Is_Raised()
    {
        var obj = new EventRaisingClass();

        var result = await Assert.RaisesAnyAsync<EventArgs>(
            handler => obj.GenericEvent += handler,
            handler => obj.GenericEvent -= handler,
            async () =>
            {
                await Task.Delay(1);
                obj.RaiseGenericEvent();
            });

        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task RaisesAnyAsync_Fails_When_No_Event_Is_Raised()
    {
        var obj = new EventRaisingClass();

        await Assert.That(async () =>
        {
            await Assert.RaisesAnyAsync<EventArgs>(
                handler => obj.GenericEvent += handler,
                handler => obj.GenericEvent -= handler,
                async () =>
                {
                    await Task.Delay(1);
                    // Don't raise the event
                });
        }).Throws<AssertionException>();
    }

    #endregion

    #region Helper Classes

    private class NotifyingClass : INotifyPropertyChanged
    {
        private string? _name;
        private int _age;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string? Name
        {
            get => _name;
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public int Age
        {
            get => _age;
            set
            {
                _age = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Age)));
            }
        }
    }

    private class CustomEventArgs : EventArgs
    {
        public string Value { get; }

        public CustomEventArgs(string value)
        {
            Value = value;
        }
    }

    private class EventRaisingClass
    {
        public event EventHandler<CustomEventArgs>? CustomEvent;
        public event EventHandler<EventArgs>? GenericEvent;

        public void RaiseCustomEvent(string value)
        {
            CustomEvent?.Invoke(this, new CustomEventArgs(value));
        }

        public void RaiseGenericEvent()
        {
            GenericEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion
}
