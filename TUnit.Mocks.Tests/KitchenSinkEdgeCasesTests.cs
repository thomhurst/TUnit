using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

#if NET8_0_OR_GREATER
// ─── T1. DIM on interface, class target without override (net8.0+ only) ──

public interface IHasDim
{
    string Say() => "dim-default";
}

public class ClassNoDimOverride : IHasDim
{
    public virtual int Rank { get; set; } = 10;
}
#endif

// ─── T2. Abstract class satisfies interface via abstract method ─────────────

public interface IHasCompute
{
    int Compute();
}

public abstract class AbstractComputeSatisfier : IHasCompute
{
    public abstract int Compute();
}

// ─── T3. Class re-implements interface on top of base's implicit impl ───────

public interface IReimplFoo
{
    string Foo();
}

public class ReimplBase : IReimplFoo
{
    public virtual string Foo() => "base-virtual";
}

public class ReimplDerived : ReimplBase, IReimplFoo
{
    // Re-implement explicitly on top of the inherited virtual impl.
    string IReimplFoo.Foo() => "derived-explicit";
}

// ─── T4. Four-level inheritance, interface at root ──────────────────────────

public interface IRootMarker
{
    int Compute();
}

public class DeepL0 : IRootMarker
{
    public virtual int Compute() => 0;
}

public class DeepL1 : DeepL0 { }
public class DeepL2 : DeepL1 { }
public class DeepL3 : DeepL2 { }

// ─── T5. Write-only property ────────────────────────────────────────────────

public interface IWriteOnlyValue
{
    int Value { set; }
    int Regular { get; set; }
}

// ─── T6. `abstract override` ─────────────────────────────────────────────────

public class AbsOverrideBase
{
    public virtual void Ping() { }
}

public abstract class AbsOverrideMid : AbsOverrideBase
{
    public abstract override void Ping();
}

// ─── T7. Two interfaces with same-name members, different returns ───────────

public interface IGetIdInt { int GetId(); }
public interface IGetIdString { string GetId(); }

public class DoubleInterfaceExplicit : IGetIdInt, IGetIdString
{
    int IGetIdInt.GetId() => 1;
    string IGetIdString.GetId() => "one";
    public virtual int OwnMember() => 0;
}

// ─── T8. Self-referential IEquatable<T> — `mock.Equals(...)` would resolve to
//     object.Equals via overload-resolution (extension methods can't beat instance
//     methods on object). Generator emits a disambiguating `EqualsOf(...)` helper
//     so the typed setup is reachable. Same disambiguation applies to GetHashCode
//     and ToString. (GetType is non-virtual on object so it cannot be overridden;
//     no helper is generated for it.)

public class SelfEquatable : IEquatable<SelfEquatable>
{
    public virtual bool Equals(SelfEquatable? other) => ReferenceEquals(this, other);
    public override bool Equals(object? obj) => obj is SelfEquatable s && Equals(s);
    public override int GetHashCode() => 0;
    public override string ToString() => "base";
}

// ─── T9. Nullable value types ────────────────────────────────────────────────

public interface INullableValues
{
    void Take(int? value);
    T? GetOrNull<T>(string key) where T : struct;
    int? MaybeValue { get; set; }
}

// ─── T10. IDisposable + IAsyncDisposable ─────────────────────────────────────

public abstract class DisposableService : IDisposable, IAsyncDisposable
{
    public abstract string Handle(string key);
    public virtual void Dispose() { }
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// ─── T11. Explicit event with custom add/remove on base ─────────────────────

public class BaseWithExplicitEvent : INotifyPropertyChanged
{
    private PropertyChangedEventHandler? _handler;
    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add => _handler += value;
        remove => _handler -= value;
    }
    public virtual string Name { get; set; } = "";

    public void RaiseInternal(string propName) => _handler?.Invoke(this, new PropertyChangedEventArgs(propName));
}

// ─── T12. Inner class inheriting closed generic ─────────────────────────────

public class OuterGenericForInner<T>
{
    public virtual T Get() => default!;
    public virtual void Put(T value) { }
}

public class InnerFromOuterString : OuterGenericForInner<string>
{
    public virtual int ExtraInt() => 0;
}

// ─── T13. `new`-redeclared property in derived interface ────────────────────

public interface ITagSmall
{
    short Tag { get; }
}

public interface ITagLarge : ITagSmall
{
    new long Tag { get; }
}

// ─── T14. Interface with indexer ────────────────────────────────────────────

public interface IHasIndexer
{
    string this[int index] { get; set; }
    int Regular { get; set; }
}

// T14b. Indexer with `in` parameter — exercises modifier forwarding
// in FormatIndexerParameterList. `in` is the only ref-kind C# permits on
// indexer parameters.
public interface IHasInIndexer
{
    string this[in int key] { get; }
}

// ─── T15 SKIPPED. Mocking a class that implements a static-abstract interface
//     hits the bridge builder, which treats the target as an interface ("Type
//     in interface list is not an interface"). Separate generator issue.

// ─── T16. IAsyncEnumerable with [EnumeratorCancellation] token ──────────────

public interface ICancellableStream
{
    IAsyncEnumerable<int> Stream(CancellationToken ct = default);
}

// ─── T17 SKIPPED. `required` members on a mock target produce CS9035
//     ("required member must be set in the object initializer") in the
//     generated factory. The factory would need to emit [SetsRequiredMembers]
//     on its constructor and skip initializing required members. Separate
//     generator fix.

// ─── T18. Member names matching C# reserved keywords (`class`, `event`, `namespace`) ─

// `record` is included as a CONTEXTUAL keyword — it does NOT require `@`-escaping (the
// C# compiler disambiguates by position). Kept here to confirm contextual keywords pass
// through the IdentifierEscaping helper unchanged.
public interface IEscapedNames
{
    int @class { get; }
    string @record();
    void @event(int @params);
    int @namespace(int @new, int @static);
}

// ─── T19. Obsolete member ───────────────────────────────────────────────────

public interface IHasObsolete
{
    [Obsolete("deprecated")]
    string OldMethod();

    string NewMethod();
}

// ─── T20. `ref readonly` return ─────────────────────────────────────────────

public class RefReadonlyReturn
{
    private int _value = 10;
    public virtual ref readonly int GetRef() => ref _value;
    public virtual int GetValue() => _value;
}

// ─── Tests ──────────────────────────────────────────────────────────────────

public class KitchenSinkEdgeCasesTests
{
    // ── T1 (net8.0+ only — DIM requires runtime support) ──

#if NET8_0_OR_GREATER
    [Test]
    public async Task T1_DIM_Class_Without_Override_Compiles_And_Routes_To_DIM()
    {
        var mock = ClassNoDimOverride.Mock();
        mock.Rank.Returns(99);

        // Own virtual is mockable.
        await Assert.That(mock.Object.Rank).IsEqualTo(99);
        mock.Rank.WasCalled(Times.Once);

        // DIM is only reachable via the interface cast; inherits the DIM body.
        IHasDim asInterface = mock.Object;
        await Assert.That(asInterface.Say()).IsEqualTo("dim-default");
    }
#endif

    // ── T2 ──

    [Test]
    public async Task T2_Abstract_Class_Satisfies_Interface_Via_Abstract_Method()
    {
        var mock = AbstractComputeSatisfier.Mock();
        mock.Compute().Returns(42);

        await Assert.That(mock.Object.Compute()).IsEqualTo(42);

        IHasCompute asInterface = mock.Object;
        await Assert.That(asInterface.Compute()).IsEqualTo(42);

        mock.Compute().WasCalled(Times.Exactly(2));
    }

    // ── T3 ──

    [Test]
    public async Task T3_Class_Reimplements_Interface_On_Top_Of_Base_Implicit_Impl()
    {
        var mock = ReimplDerived.Mock();
        mock.Foo().Returns("mocked-public");

        // Public path overrides the inherited virtual — mockable.
        await Assert.That(mock.Object.Foo()).IsEqualTo("mocked-public");

        // Interface cast hits the derived's explicit re-impl (not mockable).
        IReimplFoo asInterface = mock.Object;
        await Assert.That(asInterface.Foo()).IsEqualTo("derived-explicit");

        mock.Foo().WasCalled(Times.Once);
    }

    // ── T4 ──

    [Test]
    public async Task T4_Four_Level_Hierarchy_Interface_At_Root_Mockable()
    {
        var mock = DeepL3.Mock();
        mock.Compute().Returns(1234);

        await Assert.That(mock.Object.Compute()).IsEqualTo(1234);

        IRootMarker asInterface = mock.Object;
        await Assert.That(asInterface.Compute()).IsEqualTo(1234);

        mock.Compute().WasCalled(Times.Exactly(2));
    }

    // ── T5 ──

    [Test]
    public async Task T5_Write_Only_Property_Setter_Tracked()
    {
        var mock = IWriteOnlyValue.Mock();
        mock.Regular.Returns(1);

        mock.Object.Value = 42;
        mock.Object.Value = 100;
        _ = mock.Object.Regular;

        mock.Value.Set(42).WasCalled(Times.Once);
        mock.Value.Set(100).WasCalled(Times.Once);
        mock.Value.Set(Any()).WasCalled(Times.Exactly(2));
    }

    // ── T6 ──

    [Test]
    public async Task T6_Abstract_Override_Member_Mockable()
    {
        var mock = AbsOverrideMid.Mock();

        mock.Object.Ping();
        mock.Object.Ping();

        mock.Ping().WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }

    // ── T7 ──

    [Test]
    public async Task T7_Two_Interfaces_Same_Name_Different_Returns()
    {
        var mock = DoubleInterfaceExplicit.Mock();
        mock.OwnMember().Returns(42);

        await Assert.That(mock.Object.OwnMember()).IsEqualTo(42);

        // Each interface cast reaches the respective base explicit impl.
        IGetIdInt asInt = mock.Object;
        IGetIdString asString = mock.Object;
        await Assert.That(asInt.GetId()).IsEqualTo(1);
        await Assert.That(asString.GetId()).IsEqualTo("one");

        mock.OwnMember().WasCalled(Times.Once);
    }

    // ── T8 ──

    [Test]
    public async Task T8_Self_Referential_IEquatable_Mockable()
    {
        var mock = SelfEquatable.Mock();
        var other = new SelfEquatable();
        var unrelated = new SelfEquatable();

        // Setup via the disambiguated helper. Returns(...) must be reachable on the result.
        mock.EqualsOf(other).Returns(true);
        mock.EqualsOf(unrelated).Returns(false);

        // Direct call routes through the generated impl's Equals override to the engine.
        await Assert.That(mock.Object.Equals(other)).IsTrue();
        await Assert.That(mock.Object.Equals(unrelated)).IsFalse();

        // Interface-cast path resolves to the same underlying setup.
        IEquatable<SelfEquatable> asInterface = mock.Object;
        await Assert.That(asInterface.Equals(other)).IsTrue();
        await Assert.That(asInterface.Equals(unrelated)).IsFalse();

        // Verification: each setup tracks both the direct and interface-cast invocations.
        mock.EqualsOf(other).WasCalled(Times.Exactly(2));
        mock.EqualsOf(unrelated).WasCalled(Times.Exactly(2));

        var third = new SelfEquatable();
        mock.EqualsOf(third).WasNeverCalled();

        // GetHashCodeOf / ToStringOf — same disambiguation pattern.
        // GetType is not virtual on object so cannot be exercised; see class-level note.
        mock.GetHashCodeOf().Returns(7);
        mock.ToStringOf().Returns("mocked");

        await Assert.That(mock.Object.GetHashCode()).IsEqualTo(7);
        await Assert.That(mock.Object.ToString()).IsEqualTo("mocked");

        mock.GetHashCodeOf().WasCalled(Times.Once);
        mock.ToStringOf().WasCalled(Times.Once);
    }

    // ── T9 ──

    [Test]
    public async Task T9_Nullable_Value_Type_Parameter_And_Return()
    {
        var mock = INullableValues.Mock();
        mock.GetOrNull<int>("k").Returns((int?)42);
        mock.GetOrNull<int>("missing").Returns((int?)null);
        mock.MaybeValue.Returns((int?)99);

        mock.Object.Take(5);
        mock.Object.Take(null);

        await Assert.That(mock.Object.GetOrNull<int>("k")).IsEqualTo(42);
        await Assert.That(mock.Object.GetOrNull<int>("missing")).IsNull();
        await Assert.That(mock.Object.MaybeValue).IsEqualTo(99);

        mock.Take(Any<int?>()).WasCalled(Times.Exactly(2));
        mock.Take(5).WasCalled(Times.Once);
        mock.Take(IsNull<int?>()).WasCalled(Times.Once);
    }

    // ── T10 ──

    [Test]
    public async Task T10_Disposable_Services_Mockable()
    {
        var mock = DisposableService.Mock();
        mock.Handle("k").Returns("handled");

        await Assert.That(mock.Object.Handle("k")).IsEqualTo("handled");

        mock.Object.Dispose();
        await mock.Object.DisposeAsync();

        mock.Handle("k").WasCalled(Times.Once);
        mock.Dispose().WasCalled(Times.Once);
        mock.DisposeAsync().WasCalled(Times.Once);
    }

    // ── T11 ──

    [Test]
    public async Task T11_Class_With_Explicit_Event_Custom_Accessors_Mockable()
    {
        var mock = BaseWithExplicitEvent.Mock();
        mock.Name.Returns("mocked-name");

        await Assert.That(mock.Object.Name).IsEqualTo("mocked-name");
        mock.Name.WasCalled(Times.Once);

        // Explicit event with custom accessors is inherited as-is; attach through cast.
        string? captured = null;
        INotifyPropertyChanged asInpc = mock.Object;
        PropertyChangedEventHandler handler = (_, e) => captured = e.PropertyName;
        asInpc.PropertyChanged += handler;

        mock.Object.RaiseInternal("Name");

        await Assert.That(captured).IsEqualTo("Name");
        asInpc.PropertyChanged -= handler;
    }

    // ── T12 ──

    [Test]
    public async Task T12_Inner_Class_Inheriting_Closed_Generic_Mockable()
    {
        var mock = InnerFromOuterString.Mock();
        mock.Get().Returns("hello");
        mock.ExtraInt().Returns(7);

        await Assert.That(mock.Object.Get()).IsEqualTo("hello");
        await Assert.That(mock.Object.ExtraInt()).IsEqualTo(7);

        mock.Object.Put("x");

        mock.Get().WasCalled(Times.Once);
        mock.ExtraInt().WasCalled(Times.Once);
        mock.Put("x").WasCalled(Times.Once);
    }

    // ── T13 ──

    [Test]
    public async Task T13_Derived_Interface_New_Property_Redeclaration()
    {
        var mock = ITagLarge.Mock();
        mock.Tag.Returns(long.MaxValue);

        await Assert.That(mock.Object.Tag).IsEqualTo(long.MaxValue);

        // Cast to base interface — its `short Tag` is unconfigured → default 0.
        ITagSmall asSmall = mock.Object;
        await Assert.That(asSmall.Tag).IsEqualTo((short)0);
    }

    // ── T14 ──

    [Test]
    public async Task T14_Interface_With_Indexer_Compiles_Regular_Property_Works()
    {
        var mock = IHasIndexer.Mock();
        mock.Regular.Returns(123);

        await Assert.That(mock.Object.Regular).IsEqualTo(123);
        mock.Regular.WasCalled(Times.Once);
    }

    [Test]
    public async Task T14_Interface_With_Indexer_Get_Set_Configurable_And_Verifiable()
    {
        var mock = IHasIndexer.Mock();
        mock.Item(0).Returns("zero");
        mock.Item(1).Returns("one");

        await Assert.That(mock.Object[0]).IsEqualTo("zero");
        await Assert.That(mock.Object[1]).IsEqualTo("one");
        await Assert.That(mock.Object[0]).IsEqualTo("zero");

        mock.Object[5] = "five";
        mock.Object[5] = "five-again";
        mock.Object[6] = "six";

        // Distinct index values produce independent setups (verified by the get_*).
        mock.Item(0).WasCalled(Times.Exactly(2));
        mock.Item(1).WasCalled(Times.Once);

        // Setter verification per index value.
        mock.SetItem(5, Any<string>()).WasCalled(Times.Exactly(2));
        mock.SetItem(6, "six").WasCalled(Times.Once);
        mock.SetItem(Any<int>(), Any<string>()).WasCalled(Times.Exactly(3));
    }

    [Test]
    public async Task T14b_Indexer_With_In_Parameter_Compiles_And_Dispatches()
    {
        var mock = IHasInIndexer.Mock();
        mock.Item(7).Returns("seven");

        var k = 7;
        await Assert.That(mock.Object[in k]).IsEqualTo("seven");
        mock.Item(7).WasCalled(Times.Once);
    }

    // T15 test elided — see the SKIPPED note above the type declarations.

    // ── T16 ──

    [Test]
    public async Task T16_IAsyncEnumerable_With_CancellationToken_Param()
    {
        var mock = ICancellableStream.Mock();
        mock.Stream(Any<CancellationToken>()).Returns(Yield(1, 2, 3));

        var items = new List<int>();
        await foreach (var i in mock.Object.Stream())
        {
            items.Add(i);
        }

        await Assert.That(items).IsEquivalentTo(new[] { 1, 2, 3 });
        mock.Stream(Any<CancellationToken>()).WasCalled(Times.Once);

        static async IAsyncEnumerable<int> Yield(params int[] values)
        {
            foreach (var v in values)
            {
                await Task.Yield();
                yield return v;
            }
        }
    }

    // T17 test elided — see the SKIPPED note above the type declarations.

    // ── T18 ──

    [Test]
    public async Task T18_Member_Names_That_Are_Reserved_Keywords()
    {
        var mock = IEscapedNames.Mock();
        mock.@class.Returns(7);
        mock.@record().Returns("rec");
        mock.@namespace(Any<int>(), Any<int>()).Returns(123);

        await Assert.That(mock.Object.@class).IsEqualTo(7);
        await Assert.That(mock.Object.@record()).IsEqualTo("rec");
        await Assert.That(mock.Object.@namespace(1, 2)).IsEqualTo(123);
        mock.Object.@event(99);

        mock.@class.WasCalled(Times.Once);
        mock.@record().WasCalled(Times.Once);
        mock.@event(Any<int>()).WasCalled(Times.Once);
        mock.@namespace(Any<int>(), Any<int>()).WasCalled(Times.Once);
    }

    // ── T19 ──

    [Test]
    public async Task T19_Obsolete_Member_Can_Be_Mocked()
    {
#pragma warning disable CS0618 // deliberately exercising the obsolete member
        var mock = IHasObsolete.Mock();
        mock.OldMethod().Returns("old");
        mock.NewMethod().Returns("new");

        await Assert.That(mock.Object.OldMethod()).IsEqualTo("old");
        await Assert.That(mock.Object.NewMethod()).IsEqualTo("new");

        mock.OldMethod().WasCalled(Times.Once);
        mock.NewMethod().WasCalled(Times.Once);
#pragma warning restore CS0618
    }

    // ── T20 ──

    [Test]
    public async Task T20_Ref_Readonly_Return_Falls_Back_To_Base()
    {
        var mock = RefReadonlyReturn.Mock();
        mock.GetValue().Returns(77);

        // Non-ref return mockable as usual.
        await Assert.That(mock.Object.GetValue()).IsEqualTo(77);

        // ref-readonly return: generator may skip overriding (can't hook through engine).
        // Calling through base should still work without throwing.
        ref readonly var val = ref mock.Object.GetRef();
        await Assert.That(val).IsEqualTo(10);
    }
}
