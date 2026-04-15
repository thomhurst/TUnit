using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

// ─── Base class for multi-level hierarchy tests ─────────────────────────────

public abstract class AbstractGrandparent
{
    public virtual string Identify() => "grandparent";
    public virtual int Score { get; set; } = 10;
}

// ─── The kitchen-sink abstract class ────────────────────────────────────────

public abstract class AbstractKitchenSink : AbstractGrandparent
{
    private readonly string _prefix;

    protected AbstractKitchenSink() : this("default") { }
    protected AbstractKitchenSink(string prefix) { _prefix = prefix; }

    // ── Abstract members (MUST be configured or use default) ──
    public abstract string GetName();
    public abstract int ComputeAbstract(int x, int y);
    public abstract Task<string> GetNameAsync();

    // ── Abstract property ──
    public abstract string Label { get; set; }

    // ── Virtual methods (call base when unconfigured) ──
    public virtual int Multiply(int a, int b) => a * b;
    public virtual string Describe() => $"{_prefix}-described";
    public virtual Task<int> ComputeVirtualAsync(int x) => Task.FromResult(x * 3);

    // ── Non-virtual methods (always base, never mockable) ──
    public string GetPrefix() => _prefix;
    public int AlwaysFortyTwo() => 42;

    // ── Virtual property ──
    public virtual int Priority { get; set; } = 5;

    // ── Override of grandparent virtual ──
    public override string Identify() => "parent";

    // ── Protected virtual ──
    protected virtual int InternalCalc(int x) => x + 100;

    // ── Protected abstract ──
    protected abstract string FormatInternal(int value);

    // ── Public method that delegates to protected ──
    public string ProcessValue(int x)
    {
        var calc = InternalCalc(x);
        return FormatInternal(calc);
    }

    // ── Virtual methods with ref/out/in ──
    public virtual bool TryLookup(string key, out string value)
    {
        value = $"base-{key}";
        return true;
    }

    public virtual void Exchange(ref int value)
    {
        value = value * 2;
    }

    public virtual int SumIn(in int a, in int b) => a + b;

    // ── Virtual method with nullable return ──
    public virtual string? FindOptional(int id) => id > 0 ? $"item-{id}" : null;

    // ── Overloaded virtual methods ──
    public virtual string Render(string template) => $"base-render({template})";
    public virtual string Render(string template, int width) => $"base-render({template},{width})";

    // ── Virtual event ──
    public virtual event EventHandler<string>? StatusChanged;

    // ── Method that raises the event (tests can call) ──
    public void TriggerStatus(string status) => StatusChanged?.Invoke(this, status);

    // ── Generic virtual method ──
    public virtual T GetDefault<T>() where T : struct => default;
}

// ─── Tests ──────────────────────────────────────────────────────────────────

public class KitchenSinkAbstractTests
{
    // ── Abstract method: must be configured ──

    [Test]
    public async Task Abstract_Method_Returns_Configured_Value()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("TestName");

        await Assert.That(mock.Object.GetName()).IsEqualTo("TestName");
    }

    [Test]
    public async Task Abstract_Method_With_Args()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.ComputeAbstract(3, 4).Returns(12);

        await Assert.That(mock.Object.ComputeAbstract(3, 4)).IsEqualTo(12);
    }

    [Test]
    public async Task Abstract_Async_Method()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetNameAsync().Returns("AsyncName");

        var result = await mock.Object.GetNameAsync();

        await Assert.That(result).IsEqualTo("AsyncName");
    }

    // ── Abstract property ──

    [Test]
    public async Task Abstract_Property_Getter_Configurable()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.Label.Returns("MyLabel");

        await Assert.That(mock.Object.Label).IsEqualTo("MyLabel");
    }

    [Test]
    public async Task Abstract_Property_Setter_Verifiable()
    {
        var mock = AbstractKitchenSink.Mock();

        mock.Object.Label = "NewLabel";

        mock.Label.Set("NewLabel").WasCalled(Times.Once);
    }

    // ── Virtual method: unconfigured → calls base ──

    [Test]
    public async Task Virtual_Method_Unconfigured_Calls_Base()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x"); // satisfy abstract

        var result = mock.Object.Multiply(3, 4);

        await Assert.That(result).IsEqualTo(12); // base: a * b
    }

    // ── Virtual method: configured → override ──

    [Test]
    public async Task Virtual_Method_Configured_Overrides_Base()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");
        mock.Multiply(Any(), Any()).Returns(99);

        await Assert.That(mock.Object.Multiply(3, 4)).IsEqualTo(99);
    }

    // ── Virtual async method ──

    [Test]
    public async Task Virtual_Async_Method_Unconfigured_Calls_Base()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");

        var result = await mock.Object.ComputeVirtualAsync(5);

        await Assert.That(result).IsEqualTo(15); // base: x * 3
    }

    [Test]
    public async Task Virtual_Async_Method_Configured()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");
        mock.ComputeVirtualAsync(Any()).Returns(100);

        var result = await mock.Object.ComputeVirtualAsync(5);

        await Assert.That(result).IsEqualTo(100);
    }

    // ── Non-virtual method: always base ──

    [Test]
    public async Task Non_Virtual_Method_Always_Returns_Base()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");

        await Assert.That(mock.Object.AlwaysFortyTwo()).IsEqualTo(42);
    }

    // ── Constructor parameter usage ──

    [Test]
    public async Task Constructor_Param_Available_In_Base_Methods()
    {
        var mock = AbstractKitchenSink.Mock("custom");
        mock.GetName().Returns("x");

        // Non-virtual GetPrefix() uses _prefix
        await Assert.That(mock.Object.GetPrefix()).IsEqualTo("custom");
    }

    [Test]
    public async Task Virtual_Method_Uses_Constructor_Param_When_Unconfigured()
    {
        var mock = AbstractKitchenSink.Mock("ctx");
        mock.GetName().Returns("x");

        // Describe() base: $"{_prefix}-described"
        await Assert.That(mock.Object.Describe()).IsEqualTo("ctx-described");
    }

    // ── Virtual property ──

    [Test]
    public async Task Virtual_Property_Unconfigured_Returns_Base_Default()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");

        await Assert.That(mock.Object.Priority).IsEqualTo(5); // base default
    }

    [Test]
    public async Task Virtual_Property_Configured_Returns_Override()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");
        mock.Priority.Returns(99);

        await Assert.That(mock.Object.Priority).IsEqualTo(99);
    }

    // ── Override of grandparent virtual ──

    [Test]
    public async Task Override_Of_Grandparent_Virtual_Calls_Parent_Override()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");

        // Identify() overridden in AbstractKitchenSink → "parent"
        await Assert.That(mock.Object.Identify()).IsEqualTo("parent");
    }

    [Test]
    public async Task Override_Of_Grandparent_Virtual_Can_Be_Configured()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");
        mock.Identify().Returns("mocked");

        await Assert.That(mock.Object.Identify()).IsEqualTo("mocked");
    }

    // ── Grandparent property ──

    [Test]
    public async Task Grandparent_Virtual_Property_Unconfigured()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");

        await Assert.That(mock.Object.Score).IsEqualTo(10); // grandparent base
    }

    [Test]
    public async Task Grandparent_Virtual_Property_Configured()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");
        mock.Score.Returns(777);

        await Assert.That(mock.Object.Score).IsEqualTo(777);
    }

    // ── Protected virtual → calls base when unconfigured ──

    [Test]
    public async Task Protected_Virtual_Calls_Base_In_Delegation()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");
        mock.FormatInternal(Any()).Returns("formatted");

        // ProcessValue(10) → InternalCalc(10) = 110 → FormatInternal(110) = "formatted"
        await Assert.That(mock.Object.ProcessValue(10)).IsEqualTo("formatted");
    }

    [Test]
    public async Task Protected_Virtual_Can_Be_Configured()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");
        mock.InternalCalc(Any()).Returns(999);
        mock.FormatInternal(999).Returns("computed-999");

        await Assert.That(mock.Object.ProcessValue(10)).IsEqualTo("computed-999");
    }

    // ── Out parameters on virtual methods ──

    [Test]
    public async Task Virtual_Out_Param_Unconfigured_Calls_Base()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");

        var found = mock.Object.TryLookup("key", out var value);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsEqualTo("base-key");
    }

    [Test]
    public async Task Virtual_Out_Param_Configured()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");
        mock.TryLookup("key").Returns(true).SetsOutValue("mocked-value");

        var found = mock.Object.TryLookup("key", out var value);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsEqualTo("mocked-value");
    }

    // ── In parameters ──

    [Test]
    public async Task Virtual_In_Param_Unconfigured_Calls_Base()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");

        await Assert.That(mock.Object.SumIn(3, 4)).IsEqualTo(7);
    }

    [Test]
    public async Task Virtual_In_Param_Configured()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");
        mock.SumIn(Any(), Any()).Returns(100);

        await Assert.That(mock.Object.SumIn(3, 4)).IsEqualTo(100);
    }

    // ── Nullable return ──

    [Test]
    public async Task Virtual_Nullable_Return_Unconfigured()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");

        await Assert.That(mock.Object.FindOptional(5)).IsEqualTo("item-5");
        await Assert.That(mock.Object.FindOptional(-1)).IsNull();
    }

    [Test]
    public async Task Virtual_Nullable_Return_Configured()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");
        mock.FindOptional(Any()).Returns("always");

        await Assert.That(mock.Object.FindOptional(-1)).IsEqualTo("always");
    }

    // ── Overloaded virtual methods ──

    [Test]
    public async Task Overloaded_Virtual_Methods_Independent()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");
        mock.Render("t1").Returns("custom1");
        // Leave 2-arg overload unconfigured → base

        await Assert.That(mock.Object.Render("t1")).IsEqualTo("custom1");
        await Assert.That(mock.Object.Render("t2", 80)).IsEqualTo("base-render(t2,80)");
    }

    // ── Generic virtual method ──

    [Test]
    public async Task Generic_Virtual_Method_Unconfigured()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");

        await Assert.That(mock.Object.GetDefault<int>()).IsEqualTo(0);
        await Assert.That(mock.Object.GetDefault<bool>()).IsFalse();
    }

    // ── Verification on abstract and virtual methods ──

    [Test]
    public async Task Verify_Abstract_And_Virtual_Calls()
    {
        var mock = AbstractKitchenSink.Mock();
        mock.GetName().Returns("x");
        mock.ComputeAbstract(Any(), Any()).Returns(0);

        mock.Object.GetName();
        mock.Object.GetName();
        mock.Object.Multiply(1, 2);
        mock.Object.ComputeAbstract(1, 2);

        mock.GetName().WasCalled(Times.Exactly(2));
        mock.Multiply(Any(), Any()).WasCalled(Times.Once);
        mock.ComputeAbstract(1, 2).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }
}
