using System.Collections;
using System.Collections.Generic;
using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

// ─── A secondary interface with same-named members for explicit impl collision ─

public interface IAltNamed
{
    /// <summary>Same name as IKitchenSink.GetId but returns string instead of int.</summary>
    string GetId();

    /// <summary>Same name as IKitchenSink.Tag but returns int instead of string.</summary>
    int Tag { get; }
}

// ─── The kitchen-sink interface ─────────────────────────────────────────────

public interface IKitchenSink : IEnumerable<string>, IAltNamed
{
    // ── Simple methods ──
    void Fire();
    int GetId();
    string Echo(string input);

    // ── Async methods (Task, Task<T>, ValueTask, ValueTask<T>) ──
    Task RunAsync();
    Task<int> ComputeAsync(int x);
    ValueTask PingAsync();
    ValueTask<string> ResolveAsync(string key);

    // ── Overloaded methods ──
    string Format(string value);
    string Format(int value);
    string Format(string value, string locale);

    // ── Generic method with constraint ──
    T Convert<T>(string input) where T : struct;

    // ── Method with default parameter ──
    string Greet(string name, string greeting = "Hello");

    // ── ref / out / in parameters ──
    bool TryParse(string input, out int result);
    void Swap(ref int value);
    int AddIn(in int a, in int b);

    // ── Nullable return ──
    int? FindIndex(string key);

    // ── Properties ──
    string Tag { get; }
    int ItemCount { get; set; }

    // ── Events ──
    event EventHandler<string> OnMessage;
    event Action OnPing;
}

// ─── Tests ──────────────────────────────────────────────────────────────────

public class KitchenSinkInterfaceTests
{
    // ── Simple void method ──

    [Test]
    public async Task Void_Method_Can_Be_Called()
    {
        var mock = IKitchenSink.Mock();

        mock.Object.Fire();

        mock.Fire().WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    // ── Return value method ──

    [Test]
    public async Task Return_Value_Method_Configurable()
    {
        var mock = IKitchenSink.Mock();
        mock.GetId().Returns(42);

        await Assert.That(mock.Object.GetId()).IsEqualTo(42);
    }

    // ── Passthrough method ──

    [Test]
    public async Task Echo_Method_With_Arg_Matching()
    {
        var mock = IKitchenSink.Mock();
        mock.Echo(Any()).Returns("default");
        mock.Echo("hello").Returns("HELLO");

        await Assert.That(mock.Object.Echo("hello")).IsEqualTo("HELLO");
        await Assert.That(mock.Object.Echo("other")).IsEqualTo("default");
    }

    // ── Async methods ──

    [Test]
    public async Task Async_Task_Method_Configurable()
    {
        var mock = IKitchenSink.Mock();
        // RunAsync returns Task — void async, just configure it doesn't throw
        await mock.Object.RunAsync();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Async_Task_T_Returns_Unwrapped()
    {
        var mock = IKitchenSink.Mock();
        mock.ComputeAsync(5).Returns(25);

        var result = await mock.Object.ComputeAsync(5);

        await Assert.That(result).IsEqualTo(25);
    }

    [Test]
    public async Task ValueTask_Void_Method_Configurable()
    {
        var mock = IKitchenSink.Mock();
        await mock.Object.PingAsync();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ValueTask_T_Returns_Unwrapped()
    {
        var mock = IKitchenSink.Mock();
        mock.ResolveAsync("key1").Returns("value1");

        var result = await mock.Object.ResolveAsync("key1");

        await Assert.That(result).IsEqualTo("value1");
    }

    // ── Overloaded methods ──

    [Test]
    public async Task Overloaded_Methods_Independently_Configurable()
    {
        var mock = IKitchenSink.Mock();
        mock.Format("raw").Returns("formatted-string");
        mock.Format(42).Returns("formatted-int");
        mock.Format("raw", "en-US").Returns("formatted-locale");

        await Assert.That(mock.Object.Format("raw")).IsEqualTo("formatted-string");
        await Assert.That(mock.Object.Format(42)).IsEqualTo("formatted-int");
        await Assert.That(mock.Object.Format("raw", "en-US")).IsEqualTo("formatted-locale");
    }

    // ── Generic method with struct constraint ──

    [Test]
    public async Task Generic_Struct_Constrained_Method()
    {
        var mock = IKitchenSink.Mock();
        mock.Convert<int>("123").Returns(123);

        await Assert.That(mock.Object.Convert<int>("123")).IsEqualTo(123);
    }

    // ── Default parameter ──

    [Test]
    public async Task Method_With_Default_Parameter()
    {
        var mock = IKitchenSink.Mock();
        mock.Greet("Alice", "Hello").Returns("Hello, Alice!");

        await Assert.That(mock.Object.Greet("Alice")).IsEqualTo("Hello, Alice!");
    }

    // ── Out parameters ──

    [Test]
    public async Task Out_Parameter_Method()
    {
        var mock = IKitchenSink.Mock();
        mock.TryParse("42").Returns(true).SetsOutResult(42);

        var success = mock.Object.TryParse("42", out var result);

        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo(42);
    }

    // ── Ref parameters ──

    [Test]
    public async Task Ref_Parameter_Method()
    {
        var mock = IKitchenSink.Mock();

        int val = 10;
        mock.Object.Swap(ref val);

        mock.Swap(Any()).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    // ── In parameters ──

    [Test]
    public async Task In_Parameter_Method()
    {
        var mock = IKitchenSink.Mock();
        mock.AddIn(3, 4).Returns(7);

        await Assert.That(mock.Object.AddIn(3, 4)).IsEqualTo(7);
    }

    // ── Nullable return type ──

    [Test]
    public async Task Nullable_Return_Unconfigured_Returns_Null()
    {
        var mock = IKitchenSink.Mock();

        await Assert.That(mock.Object.FindIndex("missing")).IsNull();
    }

    [Test]
    public async Task Nullable_Return_Configured()
    {
        var mock = IKitchenSink.Mock();
        mock.FindIndex("found").Returns(7);

        await Assert.That(mock.Object.FindIndex("found")).IsEqualTo(7);
    }

    // ── Properties ──

    [Test]
    public async Task Getter_Only_Property()
    {
        var mock = IKitchenSink.Mock();
        mock.Tag.Returns("beta");

        await Assert.That(mock.Object.Tag).IsEqualTo("beta");
    }

    [Test]
    public async Task Getter_Setter_Property()
    {
        var mock = IKitchenSink.Mock();
        mock.ItemCount.Returns(10);

        await Assert.That(mock.Object.ItemCount).IsEqualTo(10);

        mock.Object.ItemCount = 20;
        mock.ItemCount.Set(20).WasCalled(Times.Once);
    }

    // ── Events ──

    [Test]
    public async Task EventHandler_T_Can_Be_Raised()
    {
        var mock = IKitchenSink.Mock();
        string? received = null;
        mock.Object.OnMessage += (_, msg) => received = msg;

        mock.RaiseOnMessage("hello");

        await Assert.That(received).IsEqualTo("hello");
    }

    [Test]
    public async Task Action_Event_Can_Be_Raised()
    {
        var mock = IKitchenSink.Mock();
        bool fired = false;
        mock.Object.OnPing += () => fired = true;

        mock.RaiseOnPing();

        await Assert.That(fired).IsTrue();
    }

    // ── IEnumerable<string> inherited — explicit interface impl ──

    [Test]
    public async Task IEnumerable_GetEnumerator_Works()
    {
        var mock = IKitchenSink.Mock();
        var items = new List<string> { "a", "b" };
        mock.GetEnumerator().Returns(items.GetEnumerator());

        var result = new List<string>();
        foreach (var item in mock.Object)
        {
            result.Add(item);
        }

        await Assert.That(result).IsEquivalentTo(items);
    }

    [Test]
    public async Task Non_Generic_IEnumerable_Cast_Works()
    {
        var mock = IKitchenSink.Mock();
        var items = new List<string> { "x" };
        mock.GetEnumerator().Returns(items.GetEnumerator());

        IEnumerable nonGeneric = mock.Object;
        int count = 0;
        foreach (var _ in nonGeneric) count++;

        await Assert.That(count).IsEqualTo(1);
    }

    // ── Verification ──

    [Test]
    public async Task Multiple_Methods_Verify_Independently()
    {
        var mock = IKitchenSink.Mock();
        mock.Echo(Any()).Returns("ok");
        mock.GetId().Returns(1);

        mock.Object.Echo("a");
        mock.Object.Echo("b");
        mock.Object.GetId();

        mock.Echo(Any()).WasCalled(Times.Exactly(2));
        mock.GetId().WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }
}
