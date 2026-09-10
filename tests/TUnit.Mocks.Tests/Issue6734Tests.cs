using TUnit.Mocks;
using TUnit.Mocks.Exceptions;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6734
public class Issue6734Tests
{
    [Test]
    public async Task Parameterless_Constructor_Records_Virtual_Call_And_Preserves_Base_Behavior()
    {
        var mock = ConstructorCallbackClient.Mock();

        await Assert.That(MockRegistry.GetEngine(mock).GetAllCalls()).Count().IsEqualTo(1);
        await Assert.That(mock.Object.BaseCalls).IsEqualTo(1);

        mock.Object.Call("default");
        mock.Call("default").WasCalled(Times.Exactly(2));
        await Assert.That(mock.Object.BaseCalls).IsEqualTo(2);
    }

    [Test]
    public async Task Constructor_Overloads_Forward_Arguments_And_Use_The_Same_Engine_After_Construction()
    {
        var text = ConstructorCallbackClient.Mock("text");
        var number = ConstructorCallbackClient.Mock(42);

        text.Call("text").WasCalled(Times.Once);
        number.Call("42").WasCalled(Times.Once);
        text.Call("text").Callback(() => { });
        text.Object.Call("text");

        text.Call("text").WasCalled(Times.Exactly(2));
        await Assert.That(text.Object.BaseCalls).IsEqualTo(1);
        await Assert.That(number.Object.BaseCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Abstract_Constructor_Callbacks_Use_Loose_Defaults_And_Record_Calls()
    {
        var mock = AbstractConstructorCallbackClient.Mock();

        mock.Call().WasCalled(Times.Once);
        mock.Read().WasCalled(Times.Once);
        await Assert.That(mock.Object.InitialValue).IsEqualTo(0);
        mock.Read().Returns(42);
        await Assert.That(mock.Object.Read()).IsEqualTo(42);
        mock.Read().WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Abstract_Constructor_Callback_Respects_Strict_Behavior()
    {
        await Assert.That(() => Mock.Of<AbstractConstructorCallbackClient>(MockBehavior.Strict))
            .Throws<MockStrictBehaviorException>();
    }

    [Test]
    public async Task Virtual_Constructor_Callback_Preserves_Strict_Partial_Mock_Base_Fallback()
    {
        var mock = Mock.Of<ConstructorCallbackClient>(MockBehavior.Strict);

        mock.Call("default").WasCalled(Times.Once);
        await Assert.That(mock.Object.BaseCalls).IsEqualTo(1);
    }

    [Test]
    public async Task Constructor_Can_Read_And_Write_Virtual_Properties_And_Indexers()
    {
        var mock = ConstructorAccessorClient.Mock();

        await Assert.That(mock.Object.InitialProperty).IsEqualTo(12);
        await Assert.That(mock.Object.InitialIndexer).IsEqualTo(34);
        var names = MockRegistry.GetEngine(mock).GetAllCalls().Select(call => call.MemberName).ToArray();
        await Assert.That(names.SequenceEqual(new[] { "set_Value", "get_Value", "set_Item", "get_Item" })).IsTrue();
        mock.Value.Returns(99);
        await Assert.That(mock.Object.Value).IsEqualTo(99);
    }

    [Test]
    public async Task Constructor_Can_Read_And_Write_Abstract_Properties_And_Indexers()
    {
        var mock = AbstractConstructorAccessorClient.Mock();

        await Assert.That(mock.Object.InitialProperty).IsEqualTo(0);
        await Assert.That(mock.Object.InitialIndexer).IsEqualTo(0);
        var names = MockRegistry.GetEngine(mock).GetAllCalls().Select(call => call.MemberName).ToArray();
        await Assert.That(names.SequenceEqual(new[] { "set_Value", "get_Value", "set_Item", "get_Item" })).IsTrue();
    }

    [Test]
    public async Task Constructor_Event_Subscriptions_Survive_Construction()
    {
        var mock = ConstructorEventClient.Mock();

        await Assert.That(MockRegistry.GetEngine(mock).GetEventSubscriberCount("Changed")).IsEqualTo(1);
        mock.RaiseChanged();
        await Assert.That(mock.Object.Notifications).IsEqualTo(1);
        mock.Object.Unsubscribe();
        await Assert.That(MockRegistry.GetEngine(mock).GetEventSubscriberCount("Changed")).IsEqualTo(0);
        mock.RaiseChanged();
        await Assert.That(mock.Object.Notifications).IsEqualTo(1);
    }

    [Test]
    public async Task Virtual_Constructor_Event_Subscriptions_Are_Recorded()
    {
        var mock = VirtualConstructorEventClient.Mock();

        await Assert.That(MockRegistry.GetEngine(mock).GetEventSubscriberCount("Changed")).IsEqualTo(1);
        mock.RaiseChanged();
        await Assert.That(mock.Object.Notifications).IsEqualTo(1);
        mock.Object.Unsubscribe();
        await Assert.That(MockRegistry.GetEngine(mock).GetEventSubscriberCount("Changed")).IsEqualTo(0);
        mock.RaiseChanged();
        await Assert.That(mock.Object.Notifications).IsEqualTo(1);
    }

    [Test]
    public async Task Constructor_Receives_Usable_Async_Defaults()
    {
        var mock = ConstructorAsyncClient.Mock();

        await mock.Object.InitialTask;
        await Assert.That(await mock.Object.InitialValueTask).IsEqualTo(0);
        mock.CallAsync().WasCalled(Times.Once);
        mock.ReadAsync().WasCalled(Times.Once);
    }

    [Test]
    public async Task Inherited_Constructor_Callback_Dispatches_To_Most_Derived_Base_Method()
    {
        var mock = DerivedConstructorCallbackClient.Mock();

        mock.Call("default").WasCalled(Times.Once);
        await Assert.That(mock.Object.BaseCalls).IsEqualTo(10);
    }

    [Test]
    public async Task Closed_Generic_Class_Constructor_Callback_Is_Recorded()
    {
        var mock = GenericConstructorCallbackClient<string>.Mock("value");

        mock.Call("value").WasCalled(Times.Once);
        await Assert.That(mock.Object.InitialValue).IsEqualTo("value");
    }

    [Test]
    public async Task Multi_Interface_Class_Mock_Records_Constructor_Callback()
    {
        var mock = Mock.Of<ConstructorCallbackClient, IConstructorExtra>();

        await Assert.That(mock.Object.BaseCalls).IsEqualTo(1);
        await Assert.That(MockRegistry.GetEngine(mock).GetAllCalls().Single().MemberName).IsEqualTo("Call");
    }

    [Test]
    public async Task Nested_Construction_Keeps_Each_Instances_Call_History()
    {
        Mock<NestedConstructorCallbackClient>? inner = null;
        var outer = NestedConstructorCallbackClient.Mock("outer", () =>
            inner = NestedConstructorCallbackClient.Mock("inner", (Action?)null));

        outer.Call("outer").WasCalled(Times.Exactly(2));
        inner!.Call("inner").WasCalled(Times.Exactly(2));
        await Assert.That(MockRegistry.GetEngine(outer).GetAllCalls()).Count().IsEqualTo(2);
        await Assert.That(MockRegistry.GetEngine(inner!).GetAllCalls()).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Throwing_Nested_Constructor_Does_Not_Corrupt_Outer_Or_Later_Mocks()
    {
        var expected = new InvalidOperationException("constructor failure");
        Exception? actual = null;
        var outer = NestedConstructorCallbackClient.Mock("outer", () =>
        {
            try
            {
                _ = NestedConstructorCallbackClient.Mock("failed", () => throw expected);
            }
            catch (InvalidOperationException exception)
            {
                actual = exception;
            }
        });
        var later = NestedConstructorCallbackClient.Mock("later", (Action?)null);

        await Assert.That(actual).IsSameReferenceAs(expected);
        outer.Call("outer").WasCalled(Times.Exactly(2));
        later.Call("later").WasCalled(Times.Exactly(2));
        await Assert.That(MockRegistry.GetEngine(outer).GetAllCalls()).Count().IsEqualTo(2);
        await Assert.That(MockRegistry.GetEngine(later).GetAllCalls()).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Concurrent_Construction_Keeps_Engines_Isolated()
    {
        var mocks = await Task.WhenAll(Enumerable.Range(0, 128).Select(index => Task.Run(() =>
            ConstructorCallbackClient.Mock(index))));

        for (var index = 0; index < mocks.Length; index++)
        {
            mocks[index].Call(index.ToString()).WasCalled(Times.Once);
            await Assert.That(MockRegistry.GetEngine(mocks[index]).GetAllCalls()).Count().IsEqualTo(1);
        }
    }

    [Test]
    public async Task Wrap_Initializes_Engine_And_Wrapped_Instance_Before_Base_Callbacks()
    {
        var instance = new WrappedConstructorCallbackClient();
        var mock = Mock.Wrap(instance);

        await Assert.That(instance.BaseCalls).IsEqualTo(2);
        await Assert.That(MockRegistry.GetEngine(mock).GetAllCalls()).Count().IsEqualTo(1);
        mock.Object.Call();
        mock.Call().WasCalled(Times.Exactly(2));
        await Assert.That(instance.BaseCalls).IsEqualTo(3);
    }

    [Test]
    public async Task Wrap_Constructor_Accessors_Delegate_To_Wrapped_Instance()
    {
        var instance = new WrappedConstructorAccessorClient(42);
        var mock = Mock.Wrap(instance);

        await Assert.That(instance.Value).IsEqualTo(7);
        await Assert.That(instance[0]).IsEqualTo(8);
        await Assert.That(mock.Object.InitialProperty).IsEqualTo(7);
        await Assert.That(mock.Object.InitialIndexer).IsEqualTo(8);
        await Assert.That(instance.Seed).IsEqualTo(42);
        await Assert.That(mock.Object.Seed).IsEqualTo(0);
        await Assert.That(MockRegistry.GetEngine(mock).GetAllCalls()).Count().IsEqualTo(4);
    }

    [Test]
    public async Task Wrap_Strict_Constructor_Callback_Throws_Strict_Exception()
    {
        var instance = new WrappedConstructorCallbackClient();
        await Assert.That(() => Mock.Wrap(MockBehavior.Strict, instance)).Throws<MockStrictBehaviorException>();
    }

    [Test]
    public async Task Nested_Wrap_Construction_Keeps_Wrapped_Instances_And_Engines_Isolated()
    {
        var innerInstance = new NestedWrappedConstructorClient();
        var outerInstance = new NestedWrappedConstructorClient();
        Mock<NestedWrappedConstructorClient>? inner = null;
        outerInstance.OnCall = () => inner = Mock.Wrap(innerInstance);

        var outer = Mock.Wrap(outerInstance);

        await Assert.That(outerInstance.BaseCalls).IsEqualTo(2);
        await Assert.That(innerInstance.BaseCalls).IsEqualTo(2);
        outer.Call().WasCalled(Times.Once);
        inner!.Call().WasCalled(Times.Once);
    }
}

public class ConstructorCallbackClient
{
    public int BaseCalls { get; protected set; }
    public ConstructorCallbackClient() => Call("default");
    public ConstructorCallbackClient(string value) => Call(value);
    public ConstructorCallbackClient(int value) => Call(value.ToString());
    public virtual void Call(string value) => BaseCalls++;
}

public class DerivedConstructorCallbackClient : ConstructorCallbackClient
{
    public override void Call(string value) => BaseCalls += 10;
}

public abstract class AbstractConstructorCallbackClient
{
    protected AbstractConstructorCallbackClient()
    {
        Call();
        InitialValue = Read();
    }
    public int InitialValue { get; }
    public abstract void Call();
    public abstract int Read();
}

public class ConstructorAccessorClient
{
    private int _item;
    public ConstructorAccessorClient()
    {
        Value = 12;
        InitialProperty = Value;
        this[0] = 34;
        InitialIndexer = this[0];
    }
    public int InitialProperty { get; }
    public int InitialIndexer { get; }
    public virtual int Value { get; set; }
    public virtual int this[int index] { get => _item; set => _item = value; }
}

public abstract class AbstractConstructorAccessorClient
{
    protected AbstractConstructorAccessorClient()
    {
        Value = 12;
        InitialProperty = Value;
        this[0] = 34;
        InitialIndexer = this[0];
    }
    public int InitialProperty { get; }
    public int InitialIndexer { get; }
    public abstract int Value { get; set; }
    public abstract int this[int index] { get; set; }
}

public abstract class ConstructorEventClient
{
    protected ConstructorEventClient()
    {
        Changed += Handle;
        Changed -= Handle;
        Changed += Handle;
    }
    public int Notifications { get; private set; }
    public abstract event Action Changed;
    private void Handle() => Notifications++;
    public void Unsubscribe() => Changed -= Handle;
}

public abstract class ConstructorAsyncClient
{
    protected ConstructorAsyncClient()
    {
        InitialTask = CallAsync();
        InitialValueTask = ReadAsync();
    }
    public Task InitialTask { get; }
    public ValueTask<int> InitialValueTask { get; }
    public abstract Task CallAsync();
    public abstract ValueTask<int> ReadAsync();
}

public class VirtualConstructorEventClient
{
    public VirtualConstructorEventClient()
    {
        Changed += Handle;
        Changed -= Handle;
        Changed += Handle;
    }
    public int Notifications { get; private set; }
    public virtual event Action? Changed;
    private void Handle() => Notifications++;
    public void Unsubscribe() => Changed -= Handle;
    public void Notify() => Changed?.Invoke();
}

public class GenericConstructorCallbackClient<T>
{
    public GenericConstructorCallbackClient(T value) => InitialValue = Call(value);
    public T InitialValue { get; }
    public virtual T Call(T value) => value;
}

public interface IConstructorExtra
{
    void Extra();
}

public class NestedConstructorCallbackClient
{
    public NestedConstructorCallbackClient(string value, Action? nested)
    {
        Call(value);
        nested?.Invoke();
        Call(value);
    }
    public virtual void Call(string value) { }
}

public class WrappedConstructorCallbackClient
{
    public WrappedConstructorCallbackClient() => Call();
    public int BaseCalls { get; private set; }
    public virtual void Call() => BaseCalls++;
}

public class WrappedConstructorAccessorClient
{
    private int _item;
    public WrappedConstructorAccessorClient(int seed)
    {
        Value = 7;
        InitialProperty = Value;
        this[0] = 8;
        InitialIndexer = this[0];
        Seed = seed;
    }
    public int Seed { get; }
    public int InitialProperty { get; }
    public int InitialIndexer { get; }
    public virtual int Value { get; set; }
    public virtual int this[int index] { get => _item; set => _item = value; }
}

public class NestedWrappedConstructorClient
{
    public NestedWrappedConstructorClient() => Call();
    public int BaseCalls { get; private set; }
    public Action? OnCall { get; set; }
    public virtual void Call()
    {
        BaseCalls++;
        OnCall?.Invoke();
    }
}
