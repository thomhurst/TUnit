using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

// ─── Base class for concrete hierarchy ──────────────────────────────────────

public class ConcreteBase
{
    public virtual string Process(string input) => $"base-{input}";
    public virtual int Compute(int x) => x + 1;
    public virtual string Tag { get; set; } = "base-tag";
    public virtual int Level { get; set; } = 1;
    public virtual event EventHandler<int>? ValueChanged;

    public void NotifyValueChanged(int value) => ValueChanged?.Invoke(this, value);
}

// ─── The kitchen-sink concrete class ────────────────────────────────────────

public class ConcreteKitchenSink : ConcreteBase
{
    private readonly string _name;

    public ConcreteKitchenSink() : this("default") { }
    public ConcreteKitchenSink(string name) { _name = name; }

    // ── Override of base virtual ──
    public override string Process(string input) => $"{_name}-{input}";

    // ── new method hiding base virtual ──
    public new int Compute(int x) => x * 10;

    // ── Override of base property ──
    public override string Tag { get; set; } = "concrete-tag";

    // ── new property hiding base ──
    public new int Level { get; set; } = 99;

    // ── Own virtual methods ──
    public virtual string Transform(string value) => value.ToUpperInvariant();
    public virtual int DoubleIt(int x) => x * 2;
    public virtual Task<string> TransformAsync(string value) => Task.FromResult(value.ToUpperInvariant());

    // ── Non-virtual method ──
    public string GetName() => _name;

    // ── Sealed override ──  (can't be re-overridden)
    // Note: 'sealed override' only valid if base declared it virtual
    // We seal Compute's base version via override + seal pattern isn't applicable here
    // since we used 'new'. Instead test a normal sealed method.
    public string NonOverridable() => "sealed-behavior";

    // ── Virtual method with ref/out ──
    public virtual bool TryGet(string key, out string value)
    {
        value = $"concrete-{key}";
        return true;
    }

    public virtual void Modify(ref int value)
    {
        value += 100;
    }

    // ── Virtual overloaded methods ──
    public virtual string Render(int x) => $"render-{x}";
    public virtual string Render(int x, int y) => $"render-{x}-{y}";

    // ── Virtual nullable return ──
    public virtual string? LookupOptional(string key) => key.Length > 0 ? key : null;

    // ── Virtual generic method ──
    public virtual T CreateDefault<T>() where T : new() => new T();

    // ── Virtual property (own) ──
    public virtual string Description { get; set; } = "default-desc";

    // ── Virtual params array ──
    public virtual int Total(params int[] values)
    {
        int sum = 0;
        foreach (var v in values) sum += v;
        return sum;
    }

    // ── Virtual tuple return ──
    public virtual (int Count, string Label) Describe() => (0, "base");

    // ── Virtual delegate return ──
    public virtual Func<int, int> GetMultiplier(int factor) => x => x * factor;

    // ── Virtual method with nullable reference param ──
    public virtual string Combine(string? prefix, string value) => $"{prefix ?? ""}{value}";
}

// ─── Interface with an internally-typed return and a concrete class that
//     implements it explicitly — regression shape for #5673
// ─────────────────────────────────────────────────────────────────────────────

public interface IHasHiddenInstance
{
    IHiddenState Instance { get; }
}

public interface IHiddenState
{
    string Marker { get; }
}

public sealed class HiddenState : IHiddenState
{
    public string Marker { get; init; } = "";
}

public class HasExplicitInstance : IHasHiddenInstance
{
    private readonly HiddenState _state;

    public HasExplicitInstance() : this(new HiddenState { Marker = "default" }) { }
    public HasExplicitInstance(HiddenState state) { _state = state; }

    // Explicit interface impl — *not* a virtual public member on the class.
    IHiddenState IHasHiddenInstance.Instance => _state;

    public virtual string Describe() => $"explicit:{_state.Marker}";
}

// ─── Tests ──────────────────────────────────────────────────────────────────

public class KitchenSinkConcreteTests
{
    // ── Override method: unconfigured → derived override ──

    [Test]
    public async Task Override_Method_Unconfigured_Uses_Derived()
    {
        var mock = ConcreteKitchenSink.Mock("test");

        // Process() is overridden in ConcreteKitchenSink: $"{_name}-{input}"
        await Assert.That(mock.Object.Process("data")).IsEqualTo("test-data");
    }

    // ── Override method: configured → mock override ──

    [Test]
    public async Task Override_Method_Configured_Uses_Mock()
    {
        var mock = ConcreteKitchenSink.Mock("test");
        mock.Process("data").Returns("mocked!");

        await Assert.That(mock.Object.Process("data")).IsEqualTo("mocked!");
    }

    // ── new method: hides base, NOT mockable ──

    [Test]
    public async Task New_Method_Uses_Derived_Implementation()
    {
        var mock = ConcreteKitchenSink.Mock();

        // Compute is 'new' — not virtual from ConcreteKitchenSink's perspective.
        // The mock calls ConcreteKitchenSink.Compute which is x * 10.
        await Assert.That(mock.Object.Compute(5)).IsEqualTo(50);
    }

    // ── Override property: unconfigured → derived ──

    [Test]
    public async Task Override_Property_Unconfigured_Uses_Derived_Default()
    {
        var mock = ConcreteKitchenSink.Mock();

        await Assert.That(mock.Object.Tag).IsEqualTo("concrete-tag");
    }

    // ── Override property: configured → mock ──

    [Test]
    public async Task Override_Property_Configured()
    {
        var mock = ConcreteKitchenSink.Mock();
        mock.Tag.Returns("mocked-tag");

        await Assert.That(mock.Object.Tag).IsEqualTo("mocked-tag");
    }

    // ── new property: hides base ──

    [Test]
    public async Task New_Property_Uses_Derived_Value()
    {
        var mock = ConcreteKitchenSink.Mock();

        // Level is 'new' — not virtual, always uses ConcreteKitchenSink's value
        await Assert.That(mock.Object.Level).IsEqualTo(99);
    }

    // ── Own virtual methods ──

    [Test]
    public async Task Own_Virtual_Method_Unconfigured_Uses_Base()
    {
        var mock = ConcreteKitchenSink.Mock();

        await Assert.That(mock.Object.Transform("hello")).IsEqualTo("HELLO");
    }

    [Test]
    public async Task Own_Virtual_Method_Configured()
    {
        var mock = ConcreteKitchenSink.Mock();
        mock.Transform("hello").Returns("custom");

        await Assert.That(mock.Object.Transform("hello")).IsEqualTo("custom");
    }

    [Test]
    public async Task Own_Virtual_Async_Method_Unconfigured()
    {
        var mock = ConcreteKitchenSink.Mock();

        var result = await mock.Object.TransformAsync("test");

        await Assert.That(result).IsEqualTo("TEST");
    }

    [Test]
    public async Task Own_Virtual_Async_Method_Configured()
    {
        var mock = ConcreteKitchenSink.Mock();
        mock.TransformAsync("test").Returns("async-mocked");

        await Assert.That(await mock.Object.TransformAsync("test")).IsEqualTo("async-mocked");
    }

    // ── Non-virtual method: always real ──

    [Test]
    public async Task Non_Virtual_Method_Returns_Real_Value()
    {
        var mock = ConcreteKitchenSink.Mock("myname");

        await Assert.That(mock.Object.GetName()).IsEqualTo("myname");
    }

    [Test]
    public async Task Non_Overridable_Method_Returns_Real_Value()
    {
        var mock = ConcreteKitchenSink.Mock();

        await Assert.That(mock.Object.NonOverridable()).IsEqualTo("sealed-behavior");
    }

    // ── Constructor parameter ──

    [Test]
    public async Task Default_Constructor_Works()
    {
        var mock = ConcreteKitchenSink.Mock();

        await Assert.That(mock.Object.GetName()).IsEqualTo("default");
    }

    [Test]
    public async Task Parameterized_Constructor_Works()
    {
        var mock = ConcreteKitchenSink.Mock("custom");

        await Assert.That(mock.Object.GetName()).IsEqualTo("custom");
    }

    // ── Out parameter ──

    [Test]
    public async Task Virtual_Out_Param_Unconfigured_Calls_Base()
    {
        var mock = ConcreteKitchenSink.Mock();

        var found = mock.Object.TryGet("key", out var value);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsEqualTo("concrete-key");
    }

    [Test]
    public async Task Virtual_Out_Param_Configured()
    {
        var mock = ConcreteKitchenSink.Mock();
        mock.TryGet("key").Returns(false).SetsOutValue("none");

        var found = mock.Object.TryGet("key", out var value);

        await Assert.That(found).IsFalse();
        await Assert.That(value).IsEqualTo("none");
    }

    // ── Ref parameter ──

    [Test]
    public async Task Virtual_Ref_Param_Unconfigured_Calls_Base()
    {
        var mock = ConcreteKitchenSink.Mock();
        int val = 5;

        mock.Object.Modify(ref val);

        await Assert.That(val).IsEqualTo(105); // base: += 100
    }

    // ── Overloaded virtual methods ──

    [Test]
    public async Task Overloaded_Virtual_Methods_Configurable_Independently()
    {
        var mock = ConcreteKitchenSink.Mock();
        mock.Render(1).Returns("one");
        // Leave 2-arg unconfigured → base

        await Assert.That(mock.Object.Render(1)).IsEqualTo("one");
        await Assert.That(mock.Object.Render(2, 3)).IsEqualTo("render-2-3");
    }

    // ── Nullable return ──

    [Test]
    public async Task Virtual_Nullable_Return_Unconfigured_Calls_Base()
    {
        var mock = ConcreteKitchenSink.Mock();

        await Assert.That(mock.Object.LookupOptional("abc")).IsEqualTo("abc");
        await Assert.That(mock.Object.LookupOptional("")).IsNull();
    }

    [Test]
    public async Task Virtual_Nullable_Return_Configured()
    {
        var mock = ConcreteKitchenSink.Mock();
        mock.LookupOptional(Any()).Returns("forced");

        await Assert.That(mock.Object.LookupOptional("")).IsEqualTo("forced");
    }

    // ── Own virtual property ──

    [Test]
    public async Task Own_Virtual_Property_Unconfigured()
    {
        var mock = ConcreteKitchenSink.Mock();

        await Assert.That(mock.Object.Description).IsEqualTo("default-desc");
    }

    [Test]
    public async Task Own_Virtual_Property_Configured()
    {
        var mock = ConcreteKitchenSink.Mock();
        mock.Description.Returns("mocked-desc");

        await Assert.That(mock.Object.Description).IsEqualTo("mocked-desc");
    }

    // ── Mixed: some configured, some base ──

    [Test]
    public async Task Mixed_Configured_And_Base_Behavior()
    {
        var mock = ConcreteKitchenSink.Mock("svc");
        mock.Process(Any()).Returns("intercepted");

        // Process → configured
        await Assert.That(mock.Object.Process("x")).IsEqualTo("intercepted");
        // Transform → base (not configured)
        await Assert.That(mock.Object.Transform("hello")).IsEqualTo("HELLO");
        // GetName → non-virtual, always base
        await Assert.That(mock.Object.GetName()).IsEqualTo("svc");
        // DoubleIt → base (not configured)
        await Assert.That(mock.Object.DoubleIt(7)).IsEqualTo(14);
    }

    // ── Verification ──

    [Test]
    public async Task Verify_Calls_On_Virtual_Methods()
    {
        var mock = ConcreteKitchenSink.Mock();
        mock.Transform(Any()).Returns("x");

        mock.Object.Transform("a");
        mock.Object.Transform("b");
        mock.Object.DoubleIt(5);

        mock.Transform(Any()).WasCalled(Times.Exactly(2));
        mock.DoubleIt(5).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    // ── Event from base class ──

    [Test]
    public async Task Base_Virtual_Event_Can_Be_Raised()
    {
        var mock = ConcreteKitchenSink.Mock();
        int? received = null;
        mock.Object.ValueChanged += (_, val) => received = val;

        mock.RaiseValueChanged(42);

        await Assert.That(received).IsEqualTo(42);
    }

    // ── Params virtual ──

    [Test]
    public async Task Virtual_Params_Unconfigured_Uses_Base()
    {
        var mock = ConcreteKitchenSink.Mock();

        await Assert.That(mock.Object.Total(1, 2, 3)).IsEqualTo(6);
    }

    [Test]
    public async Task Virtual_Params_Configured()
    {
        var mock = ConcreteKitchenSink.Mock();
        mock.Total(Any()).Returns(1000);

        await Assert.That(mock.Object.Total(1, 2, 3)).IsEqualTo(1000);
        await Assert.That(mock.Object.Total()).IsEqualTo(1000);
        mock.Total(Any()).WasCalled(Times.Exactly(2));
    }

    // ── Virtual tuple return ──

    [Test]
    public async Task Virtual_Tuple_Return_Configured()
    {
        var mock = ConcreteKitchenSink.Mock();
        mock.Describe().Returns((5, "mocked"));

        var (count, label) = mock.Object.Describe();

        await Assert.That(count).IsEqualTo(5);
        await Assert.That(label).IsEqualTo("mocked");
        mock.Describe().WasCalled(Times.Once);
    }

    // ── Virtual delegate return ──

    [Test]
    public async Task Virtual_Delegate_Return_Unconfigured_Uses_Base()
    {
        var mock = ConcreteKitchenSink.Mock();

        var fn = mock.Object.GetMultiplier(3);

        await Assert.That(fn(4)).IsEqualTo(12);
    }

    [Test]
    public async Task Virtual_Delegate_Return_Configured()
    {
        var mock = ConcreteKitchenSink.Mock();
        Func<int, int> tripler = x => x * 3;
        mock.GetMultiplier(5).Returns(tripler);

        var fn = mock.Object.GetMultiplier(5);

        await Assert.That(fn(4)).IsEqualTo(12);
        mock.GetMultiplier(5).WasCalled(Times.Once);
        mock.GetMultiplier(9).WasNeverCalled();
    }

    // ── Nullable reference param ──

    [Test]
    public async Task Virtual_Nullable_Reference_Param_Unconfigured_Uses_Base()
    {
        var mock = ConcreteKitchenSink.Mock();

        await Assert.That(mock.Object.Combine(null, "x")).IsEqualTo("x");
        await Assert.That(mock.Object.Combine("pre-", "x")).IsEqualTo("pre-x");
    }

    [Test]
    public async Task Virtual_Nullable_Reference_Param_Configured_And_Verified()
    {
        var mock = ConcreteKitchenSink.Mock();
        mock.Combine(IsNull<string>(), Any()).Returns("null-path");
        mock.Combine("p-", Any()).Returns("prefixed");

        await Assert.That(mock.Object.Combine(null, "x")).IsEqualTo("null-path");
        await Assert.That(mock.Object.Combine("p-", "x")).IsEqualTo("prefixed");

        mock.Combine(IsNull<string>(), Any()).WasCalled(Times.Once);
        mock.Combine("p-", Any()).WasCalled(Times.Once);
    }

    // ── Class with explicit interface impl of inaccessible-shaped member (#5673) ──

    [Test]
    public async Task Class_With_Explicit_Interface_Impl_Compiles_And_Inherits_Impl()
    {
        // Mock should compile despite the base explicitly implementing IHasHiddenInstance.Instance.
        // Accessing through the interface flows to the base's explicit impl.
        var mock = HasExplicitInstance.Mock();

        IHasHiddenInstance asInterface = mock.Object;
        await Assert.That(asInterface.Instance.Marker).IsEqualTo("default");

        // Own virtual on the class is still mockable AND verifiable.
        mock.Describe().Returns("mocked");
        await Assert.That(mock.Object.Describe()).IsEqualTo("mocked");
        await Assert.That(mock.Object.Describe()).IsEqualTo("mocked");
        mock.Describe().WasCalled(Times.Exactly(2));
    }
}
