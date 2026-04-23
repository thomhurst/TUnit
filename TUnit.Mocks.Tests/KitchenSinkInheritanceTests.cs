using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

// ─── Level 0: Root abstract ───────────────────────────────────────────────

public abstract class Level0Root
{
    public abstract string GetKind();
    public virtual string Greet(string name) => $"L0-hello-{name}";
    public virtual int GetPriority() => 0;
    public virtual string Tag { get; set; } = "L0";
    public abstract string Label { get; }

    public virtual event EventHandler<string>? StatusChanged;
    public void NotifyStatus(string status) => StatusChanged?.Invoke(this, status);

    // Virtual that will be sealed in Level1
    public virtual string GetVersion() => "v0";
}

// ─── Level 1: Intermediate abstract with overrides & sealed ───────────────

public abstract class Level1Middle : Level0Root
{
    private readonly string _region;

    protected Level1Middle() : this("default-region") { }
    protected Level1Middle(string region) { _region = region; }

    // Override of L0 abstract
    public override string GetKind() => $"L1-kind-{_region}";

    // Override of L0 virtual
    public override string Greet(string name) => $"L1-hi-{name}";

    // Sealed override — blocks Level2+ from overriding
    public sealed override string GetVersion() => "v1-sealed";

    // New abstract introduced at this level
    public abstract int ComputeScore(int x);

    // New virtual at this level
    public virtual string Format(int value) => $"L1-{value}";

    // Override L0 property
    public override string Tag { get; set; } = "L1";

    // 'new' hiding L0 GetPriority (NOT virtual from this level)
    public new int GetPriority() => 100;

    // Non-virtual getter using constructor param
    public string GetRegion() => _region;
}

// ─── Level 2: Concrete with mix of overrides & own virtuals ──────────────

public class Level2Leaf : Level1Middle
{
    private readonly int _id;

    public Level2Leaf() : this(0, "leaf-region") { }
    public Level2Leaf(int id) : this(id, "leaf-region") { }
    public Level2Leaf(int id, string region) : base(region) { _id = id; }

    // Override of L1 override of L0 abstract
    public override string GetKind() => $"L2-kind-{_id}";

    // Override of L0's abstract property, skipping L1
    public override string Label => $"L2-label-{_id}";

    // Override of L1 abstract
    public override int ComputeScore(int x) => x * _id;

    // Override of L1 virtual
    public override string Format(int value) => $"L2-fmt-{value}";

    // Override of L0 virtual (L1 also overrode it)
    public override string Greet(string name) => $"L2-yo-{name}";

    // Own new virtual
    public virtual string Describe() => $"leaf-{_id}";
    public virtual Task<int> GetScoreAsync(int x) => Task.FromResult(x + _id);

    // Own virtual property
    public virtual string Category { get; set; } = "default-cat";

    // Non-virtual method using field
    public int GetId() => _id;

    // Virtual with out param
    public virtual bool TryLookup(string key, out string value)
    {
        value = $"L2-{key}";
        return true;
    }

    // Virtual with ref param
    public virtual void Adjust(ref int value)
    {
        value += _id;
    }

    // Override L1 Tag property
    public override string Tag { get; set; } = "L2";
}

// ─── Interface hierarchy with `new`-redeclaration ───────────────────────────

public interface ILegacyService
{
    int Ping();
}

public interface IModernService : ILegacyService
{
    /// <summary>Hides ILegacyService.Ping with a wider return type.</summary>
    new long Ping();
}

// ─── Hierarchy where L1 explicitly implements an interface (#5673 shape) ────

public interface IScopeMarker
{
    string GetScope();
}

public abstract class ScopedBase : IScopeMarker
{
    // Explicit impl on the abstract base — the interface member is *not* a
    // virtual public member of the class, so a mock of a derived class must
    // not try to `override` it.
    string IScopeMarker.GetScope() => "base-scope";

    public abstract string GetName();
}

public class ScopedLeaf : ScopedBase
{
    private readonly string _tag;

    public ScopedLeaf() : this("leaf") { }
    public ScopedLeaf(string tag) { _tag = tag; }

    public override string GetName() => $"name-{_tag}";

    public virtual int Rank { get; set; } = 5;
}

// ─── Hierarchy where a BASE class satisfies an interface via virtual methods
//     and the LEAF (the type being mocked) inherits without redeclaring.
//     Confirms ProcessClassMembers recurses up the hierarchy and that the
//     interface-loop guard finds the inherited impl.
// ─────────────────────────────────────────────────────────────────────────────

public interface IInheritedShape
{
    int Area();
    string Name { get; set; }
    event EventHandler<int> Resized;
}

public class ShapeBase : IInheritedShape
{
    // Implicit interface impl via virtual members ON THE BASE — leaf inherits.
    public virtual int Area() => 10;
    public virtual string Name { get; set; } = "base-name";
    public virtual event EventHandler<int>? Resized;

    public void RaiseResized(int v) => Resized?.Invoke(this, v);
}

public class ShapeLeaf : ShapeBase
{
    // Deliberately does not redeclare anything — must inherit from ShapeBase,
    // and the mock must still be able to override Area/Name/Resized because
    // the class walk collected them via BaseType recursion.
}

// ─── Tests ──────────────────────────────────────────────────────────────────

public class KitchenSinkInheritanceTests
{
    // ── L0 abstract method overridden at L2 ──

    [Test]
    public async Task Abstract_Overridden_At_L2_Unconfigured()
    {
        var mock = Level2Leaf.Mock(42, "eu");

        await Assert.That(mock.Object.GetKind()).IsEqualTo("L2-kind-42");
    }

    [Test]
    public async Task Abstract_Overridden_At_L2_Configured()
    {
        var mock = Level2Leaf.Mock(42);
        mock.GetKind().Returns("mocked-kind");

        await Assert.That(mock.Object.GetKind()).IsEqualTo("mocked-kind");
    }

    // ── L0 virtual overridden at L1 and L2 ──

    [Test]
    public async Task Virtual_Overridden_At_Multiple_Levels_Uses_L2()
    {
        var mock = Level2Leaf.Mock();

        // Greet is overridden at both L1 and L2; L2 should win
        await Assert.That(mock.Object.Greet("Alice")).IsEqualTo("L2-yo-Alice");
    }

    [Test]
    public async Task Virtual_Overridden_At_Multiple_Levels_Configured()
    {
        var mock = Level2Leaf.Mock();
        mock.Greet("Alice").Returns("mocked-greet");

        await Assert.That(mock.Object.Greet("Alice")).IsEqualTo("mocked-greet");
    }

    // ── Sealed override at L1 — always uses L1 impl ──

    [Test]
    public async Task Sealed_Override_Always_Uses_Sealed_Impl()
    {
        var mock = Level2Leaf.Mock();

        // GetVersion is sealed at L1 — cannot be overridden, always "v1-sealed"
        await Assert.That(mock.Object.GetVersion()).IsEqualTo("v1-sealed");
    }

    // ── 'new' hiding: L1 hides L0.GetPriority ──

    [Test]
    public async Task New_Method_Uses_Hiding_Impl()
    {
        var mock = Level2Leaf.Mock();

        // GetPriority is 'new' at L1 — not virtual, always 100
        await Assert.That(mock.Object.GetPriority()).IsEqualTo(100);
    }

    // ── L1 abstract implemented at L2 ──

    [Test]
    public async Task L1_Abstract_Implemented_At_L2_Unconfigured()
    {
        var mock = Level2Leaf.Mock(5);

        await Assert.That(mock.Object.ComputeScore(10)).IsEqualTo(50); // 10 * 5
    }

    [Test]
    public async Task L1_Abstract_Implemented_At_L2_Configured()
    {
        var mock = Level2Leaf.Mock(5);
        mock.ComputeScore(10).Returns(999);

        await Assert.That(mock.Object.ComputeScore(10)).IsEqualTo(999);
    }

    // ── L1 virtual overridden at L2 ──

    [Test]
    public async Task L1_Virtual_Overridden_At_L2_Unconfigured()
    {
        var mock = Level2Leaf.Mock();

        await Assert.That(mock.Object.Format(42)).IsEqualTo("L2-fmt-42");
    }

    [Test]
    public async Task L1_Virtual_Overridden_At_L2_Configured()
    {
        var mock = Level2Leaf.Mock();
        mock.Format(42).Returns("custom-format");

        await Assert.That(mock.Object.Format(42)).IsEqualTo("custom-format");
    }

    // ── L2 own virtual method ──

    [Test]
    public async Task Own_Virtual_Unconfigured()
    {
        var mock = Level2Leaf.Mock(7);

        await Assert.That(mock.Object.Describe()).IsEqualTo("leaf-7");
    }

    [Test]
    public async Task Own_Virtual_Configured()
    {
        var mock = Level2Leaf.Mock();
        mock.Describe().Returns("custom-desc");

        await Assert.That(mock.Object.Describe()).IsEqualTo("custom-desc");
    }

    // ── L2 own virtual async method ──

    [Test]
    public async Task Own_Virtual_Async_Unconfigured()
    {
        var mock = Level2Leaf.Mock(3);

        await Assert.That(await mock.Object.GetScoreAsync(10)).IsEqualTo(13); // 10 + 3
    }

    [Test]
    public async Task Own_Virtual_Async_Configured()
    {
        var mock = Level2Leaf.Mock();
        mock.GetScoreAsync(10).Returns(77);

        await Assert.That(await mock.Object.GetScoreAsync(10)).IsEqualTo(77);
    }

    // ── Properties across hierarchy ──

    [Test]
    public async Task Property_Overridden_At_L2_Unconfigured()
    {
        var mock = Level2Leaf.Mock();

        await Assert.That(mock.Object.Tag).IsEqualTo("L2");
    }

    [Test]
    public async Task Property_Overridden_At_L2_Configured()
    {
        var mock = Level2Leaf.Mock();
        mock.Tag.Returns("custom-tag");

        await Assert.That(mock.Object.Tag).IsEqualTo("custom-tag");
    }

    [Test]
    public async Task Abstract_Property_Implemented_At_L2_Unconfigured()
    {
        var mock = Level2Leaf.Mock(99);

        await Assert.That(mock.Object.Label).IsEqualTo("L2-label-99");
    }

    [Test]
    public async Task Abstract_Property_Implemented_At_L2_Configured()
    {
        var mock = Level2Leaf.Mock();
        mock.Label.Returns("custom-label");

        await Assert.That(mock.Object.Label).IsEqualTo("custom-label");
    }

    [Test]
    public async Task Own_Virtual_Property_Unconfigured()
    {
        var mock = Level2Leaf.Mock();

        await Assert.That(mock.Object.Category).IsEqualTo("default-cat");
    }

    [Test]
    public async Task Own_Virtual_Property_Configured()
    {
        var mock = Level2Leaf.Mock();
        mock.Category.Returns("special");

        await Assert.That(mock.Object.Category).IsEqualTo("special");
    }

    // ── Constructor params forwarded through hierarchy ──

    [Test]
    public async Task Constructor_Params_Forwarded_Through_Hierarchy()
    {
        var mock = Level2Leaf.Mock(1, "us-west");

        // GetId → non-virtual, uses L2 _id field
        await Assert.That(mock.Object.GetId()).IsEqualTo(1);
        // GetRegion → non-virtual, uses L1 _region field
        await Assert.That(mock.Object.GetRegion()).IsEqualTo("us-west");
    }

    [Test]
    public async Task Default_Constructor()
    {
        var mock = Level2Leaf.Mock();

        await Assert.That(mock.Object.GetId()).IsEqualTo(0);
        await Assert.That(mock.Object.GetRegion()).IsEqualTo("leaf-region");
    }

    // ── Out param on L2 virtual ──

    [Test]
    public async Task Out_Param_Unconfigured()
    {
        var mock = Level2Leaf.Mock();

        var found = mock.Object.TryLookup("key", out var value);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsEqualTo("L2-key");
    }

    [Test]
    public async Task Out_Param_Configured()
    {
        var mock = Level2Leaf.Mock();
        mock.TryLookup("key").Returns(false).SetsOutValue("not-found");

        var found = mock.Object.TryLookup("key", out var value);

        await Assert.That(found).IsFalse();
        await Assert.That(value).IsEqualTo("not-found");
    }

    // ── Ref param on L2 virtual ──

    [Test]
    public async Task Ref_Param_Unconfigured()
    {
        var mock = Level2Leaf.Mock(10);
        int val = 5;

        mock.Object.Adjust(ref val);

        await Assert.That(val).IsEqualTo(15); // 5 + 10
    }

    // ── Event from L0 ──

    [Test]
    public async Task Event_From_Root_Can_Be_Raised()
    {
        var mock = Level2Leaf.Mock();
        string? received = null;
        mock.Object.StatusChanged += (_, s) => received = s;

        mock.RaiseStatusChanged("active");

        await Assert.That(received).IsEqualTo("active");
    }

    // ── Mixed: some configured, some base ──

    [Test]
    public async Task Mixed_Configured_And_Base_Across_Hierarchy()
    {
        var mock = Level2Leaf.Mock(7, "eu");

        // Configure some, leave others
        mock.GetKind().Returns("override");
        mock.Describe().Returns("custom");

        // Configured → mock values
        await Assert.That(mock.Object.GetKind()).IsEqualTo("override");
        await Assert.That(mock.Object.Describe()).IsEqualTo("custom");

        // Unconfigured → base impl
        await Assert.That(mock.Object.Format(5)).IsEqualTo("L2-fmt-5");
        await Assert.That(mock.Object.ComputeScore(3)).IsEqualTo(21); // 3 * 7
        await Assert.That(mock.Object.Greet("X")).IsEqualTo("L2-yo-X");

        // Sealed/non-virtual → always base
        await Assert.That(mock.Object.GetVersion()).IsEqualTo("v1-sealed");
        await Assert.That(mock.Object.GetId()).IsEqualTo(7);
        await Assert.That(mock.Object.GetRegion()).IsEqualTo("eu");
    }

    // ── Interface hierarchy with `new`-redeclaration ──

    [Test]
    public async Task Derived_Interface_New_Return_Resolves_Via_Static_Type()
    {
        var mock = IModernService.Mock();
        mock.Ping().Returns(long.MaxValue);

        // Static type → modern interface → long
        await Assert.That(mock.Object.Ping()).IsEqualTo(long.MaxValue);

        // Cast to legacy interface → int Ping() — unconfigured, smart default 0
        ILegacyService legacy = mock.Object;
        await Assert.That(legacy.Ping()).IsEqualTo(0);

        // Verification tracks the modern-shape call.
        mock.Ping().WasCalled(Times.Once);
    }

    // ── Hierarchy where L1 explicitly implements interface (#5673 shape) ──

    // ── Base-virtual-satisfies-interface: leaf inherits, mock still overrides ──

    [Test]
    public async Task Leaf_Inherits_Base_Virtual_That_Satisfies_Interface()
    {
        // ShapeLeaf declares nothing; ShapeBase provides virtual impls that satisfy
        // IInheritedShape. Confirms class-walk recursion (ProcessClassMembers walks
        // into ShapeBase) AND that the interface-loop guard skips the duplicate
        // interface-member emission once the inherited virtual has been collected.
        var mock = ShapeLeaf.Mock();

        // Unconfigured → inherited base virtual executes (value from ShapeBase).
        await Assert.That(mock.Object.Area()).IsEqualTo(10);
        await Assert.That(mock.Object.Name).IsEqualTo("base-name");

        // Configured → mock intercepts the inherited virtual.
        mock.Area().Returns(42);
        mock.Name.Returns("mocked-name");

        await Assert.That(mock.Object.Area()).IsEqualTo(42);
        await Assert.That(mock.Object.Name).IsEqualTo("mocked-name");

        // Calls through the interface route the same way.
        IInheritedShape asInterface = mock.Object;
        await Assert.That(asInterface.Area()).IsEqualTo(42);
        await Assert.That(asInterface.Name).IsEqualTo("mocked-name");

        // Inherited virtual event fires on the mock (Raise* helper is generated).
        int? raised = null;
        asInterface.Resized += (_, v) => raised = v;
        mock.RaiseResized(7);
        await Assert.That(raised).IsEqualTo(7);

        // Verification counts the inherited calls (two direct + one via interface).
        mock.Area().WasCalled(Times.Exactly(3));
        mock.Name.WasCalled(Times.Exactly(3));
    }

    [Test]
    public async Task Derived_Class_Inherits_Base_Explicit_Interface_Impl()
    {
        var mock = ScopedLeaf.Mock("x");

        // Abstract member overridden at L2 is mockable + verifiable.
        mock.GetName().Returns("mocked");
        await Assert.That(mock.Object.GetName()).IsEqualTo("mocked");
        mock.GetName().WasCalled(Times.Once);

        // Explicit interface impl on the abstract base flows through unchanged —
        // the mock must not attempt to override it.
        IScopeMarker asMarker = mock.Object;
        await Assert.That(asMarker.GetScope()).IsEqualTo("base-scope");

        // Own virtual property on derived class is still mockable + verifiable.
        mock.Rank.Returns(999);
        await Assert.That(mock.Object.Rank).IsEqualTo(999);
        await Assert.That(mock.Object.Rank).IsEqualTo(999);
        mock.Rank.WasCalled(Times.Exactly(2));
    }

    // ── Verification across hierarchy ──

    [Test]
    public async Task Verification_Works_Across_Hierarchy()
    {
        var mock = Level2Leaf.Mock();
        mock.GetKind().Returns("x");
        mock.Greet(Any()).Returns("y");
        mock.Describe().Returns("z");

        mock.Object.GetKind();
        mock.Object.GetKind();
        mock.Object.Greet("a");
        mock.Object.Describe();

        mock.GetKind().WasCalled(Times.Exactly(2));
        mock.Greet(Any()).WasCalled(Times.Once);
        mock.Describe().WasCalled(Times.Once);
        mock.Format(Any()).WasNeverCalled();
        await Assert.That(true).IsTrue();
    }
}
