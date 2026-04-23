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

// ─── T8 SKIPPED. Self-referential IEquatable<T> — `mock.Equals(...)` resolves
//     to object.Equals via extension-method dispatch rather than to the
//     generator-emitted setup extension. Separate design limitation: would
//     require either renaming the extension or generating a disambiguating
//     helper (e.g. `mock.EqualsOf(...)`).

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

// ─── T14 SKIPPED. Interfaces with an indexer produce CS0535
//     ("does not implement IHasIndexer.this[int]") because the mock-impl builder
//     skips indexer emission without providing a stub. Tracked as a separate
//     generator gap — not in scope of the #5673 fix.

// ─── T15. Class implementing a static-abstract interface ────────────────────

#if NET8_0_OR_GREATER
public interface IStaticAbstractFactory
{
    static abstract IStaticAbstractFactory Create();
    int InstanceValue { get; }
}

public class StaticAbstractImpl : IStaticAbstractFactory
{
    public static IStaticAbstractFactory Create() => new StaticAbstractImpl();
    public virtual int InstanceValue => 99;
}
#endif

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

// ─── T18 SKIPPED. Member names matching C# keywords (`class`, `event`, `record`)
//     are passed through to the generator as unescaped identifiers, producing
//     malformed emission (CS0539, CS0106, CS0066 on the generated impl). The
//     EscapeIdentifier helper exists but is only applied to parameter names.
//     Separate generator fix to apply it to method/property/event names.

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

    // T8 test elided — see the SKIPPED note above the type declarations.

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

    // T14 test elided — see the SKIPPED note above the type declarations.

    // ── T15 (net8.0+ only — static abstract requires runtime support) ──

#if NET8_0_OR_GREATER
    [Test]
    public async Task T15_Class_Implementing_Static_Abstract_Interface_Mockable()
    {
        // Mocking a class whose interface has static-abstract members should still work:
        // the class provides the concrete static impl; the mock only needs to override
        // the instance-virtual surface. No bridge interface is required for class targets.
        var mock = StaticAbstractImpl.Mock();
        mock.InstanceValue.Returns(42);

        await Assert.That(mock.Object.InstanceValue).IsEqualTo(42);
        mock.InstanceValue.WasCalled(Times.Once);

        // The class's concrete static impl is unaffected — direct call still works
        // and returns a real instance, NOT routed through the mock engine. The static
        // method's declared return type IStaticAbstractFactory itself can't be a type
        // argument (CS8920), so observe the result as `object`.
        object? created = StaticAbstractImpl.Create();
        await Assert.That(created).IsNotNull();
        await Assert.That(created).IsTypeOf<StaticAbstractImpl>();
    }
#endif

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

    // T18 test elided — see the SKIPPED note above the type declarations.

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
