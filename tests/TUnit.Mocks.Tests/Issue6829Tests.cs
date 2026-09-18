using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/discussions/6829
// The property model recorded only "has a setter", so an `init` accessor was emitted as a
// plain `set` — CS8854/CS8855, because an implementation has to match the slot's accessor
// kind exactly. Init-only members must be emitted as `init`, and because an init-only
// property is assignable only on `this`/`base` (CS8852), the wrap and wrapper-forward paths
// dispatch through the engine instead of assigning the underlying instance.

#region Test types

// The discussion's repro shape.
public interface IInitOnlyValue
{
    int Value { get; init; }
}

// Mixes accessor kinds so the plain `set` path stays covered alongside `init`.
public interface IMixedAccessorKinds
{
    int InitOnly { get; init; }
    string Mutable { get; set; }
    string ReadOnly { get; }
}

public interface IInitOnlyIndexer
{
    string this[int index] { get; init; }
}

public abstract class AbstractInitOnlyProperty
{
    public abstract int Value { get; init; }
    public abstract string this[int index] { get; init; }
}

public class VirtualInitOnlyProperty
{
    public virtual int Value { get; init; } = 7;
}

// A separate type for the wrap path: one type reached by both `.Mock()` and `Mock.Wrap`
// trips a duplicate-hint-name failure in the generator that predates this fix.
public class WrappableInitOnlyProperty
{
    public virtual int Value { get; init; } = 7;
}

// Same signature, different setter kinds. Neither slot can be dropped and one member cannot
// implement both, so the `set` slot becomes an explicit interface implementation that dispatches
// on the same member ids as the implicit `init` one.
public interface IInitSlot
{
    int V { get; init; }
    string this[int i] { get; init; }
}

public interface ISetSlot
{
    int V { get; set; }
    string this[int i] { get; set; }
}

public interface IBothSetterKinds : IInitSlot, ISetSlot;

// An explicit indexer that owns its member ids, rather than aliasing another one: the class
// implements the interface indexer non-virtually, so the mock can only intercept it by
// re-implementing the interface explicitly. Its setup surface must survive.
public interface IIndexedService
{
    string this[int index] { get; set; }
}

public class BlockingIndexerService : IIndexedService
{
    public string this[int index]
    {
        get => $"real-{index}";
        set { }
    }
}

#endregion

public class Issue6829Tests
{
    [Test]
    public async Task Interface_With_Init_Only_Property_Can_Be_Mocked()
    {
        var mock = IInitOnlyValue.Mock();

        await Assert.That(mock.Object.Value).IsEqualTo(0);
    }

    [Test]
    public async Task Init_Only_Property_Getter_Can_Be_Configured()
    {
        var mock = IInitOnlyValue.Mock();
        mock.Value.Returns(42);

        await Assert.That(mock.Object.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Init_Only_Property_Is_Reachable_Through_The_Typed_Wrapper()
    {
        var mock = IInitOnlyValue.Mock();
        mock.Value.Returns(13);

        // The wrapper implements IInitOnlyValue explicitly, including the init accessor.
        IInitOnlyValue asInterface = mock;

        await Assert.That(asInterface.Value).IsEqualTo(13);
    }

    [Test]
    public async Task Init_Only_Setter_Is_Verifiable_And_Unset_By_Default()
    {
        var mock = IInitOnlyValue.Mock();

        mock.Value.Setter.WasNeverCalled();
        mock.Value.Set(Arg.Any<int>()).WasNeverCalled();

        await Assert.That(mock.Object.Value).IsEqualTo(0);
    }

    [Test]
    public async Task Init_Only_And_Mutable_Accessors_Coexist()
    {
        var mock = IMixedAccessorKinds.Mock();
        mock.InitOnly.Returns(5);
        mock.Mutable.Returns("configured");
        mock.ReadOnly.Returns("readonly");

        mock.Object.Mutable = "assigned";

        await Assert.That(mock.Object.InitOnly).IsEqualTo(5);
        await Assert.That(mock.Object.ReadOnly).IsEqualTo("readonly");
        mock.Mutable.Set("assigned").WasCalled();
    }

    [Test]
    public async Task Interface_With_Init_Only_Indexer_Can_Be_Mocked()
    {
        var mock = IInitOnlyIndexer.Mock();
        mock.Item(1).Returns("one");

        await Assert.That(mock.Object[1]).IsEqualTo("one");
        mock.SetItem(Arg.Any<int>(), Arg.Any<string>()).WasNeverCalled();
    }

    [Test]
    public async Task Abstract_Class_With_Init_Only_Members_Can_Be_Mocked()
    {
        var mock = AbstractInitOnlyProperty.Mock();
        mock.Value.Returns(9);
        mock.Item(0).Returns("zero");

        await Assert.That(mock.Object.Value).IsEqualTo(9);
        await Assert.That(mock.Object[0]).IsEqualTo("zero");
    }

    [Test]
    public async Task Virtual_Init_Only_Property_Falls_Back_To_The_Base_Value()
    {
        var mock = VirtualInitOnlyProperty.Mock();

        await Assert.That(mock.Object.Value).IsEqualTo(7);

        mock.Value.Returns(21);

        await Assert.That(mock.Object.Value).IsEqualTo(21);
    }

    [Test]
    public async Task Clashing_Setter_Kinds_Serve_Both_Slots_From_One_Setup()
    {
        var mock = IBothSetterKinds.Mock();
        mock.V.Returns(5);
        mock.Item(2).Returns("two");

        // One setup covers both slots — the explicit `set` slot shares the implicit slot's ids.
        await Assert.That(((IInitSlot)mock.Object).V).IsEqualTo(5);
        await Assert.That(((ISetSlot)mock.Object).V).IsEqualTo(5);
        await Assert.That(((IInitSlot)mock.Object)[2]).IsEqualTo("two");
        await Assert.That(((ISetSlot)mock.Object)[2]).IsEqualTo("two");
    }

    [Test]
    public async Task Clashing_Setter_Kinds_Verify_Through_The_Shared_Member()
    {
        var mock = IBothSetterKinds.Mock();

        ((ISetSlot)mock.Object).V = 9;
        ((ISetSlot)mock.Object)[1] = "one";

        mock.V.Set(9).WasCalled();
        mock.SetItem(1, "one").WasCalled();

        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(2);
    }

    [Test]
    public async Task Clashing_Setter_Kinds_Are_Reachable_Through_The_Typed_Wrapper()
    {
        var mock = IBothSetterKinds.Mock();
        mock.V.Returns(4);

        IBothSetterKinds asInterface = mock;
        ((ISetSlot)asInterface).V = 7;

        await Assert.That(((IInitSlot)asInterface).V).IsEqualTo(4);
        mock.V.Set(7).WasCalled();
    }

    [Test]
    public async Task Explicit_Indexer_Owning_Its_Ids_Keeps_Its_Setup_Surface()
    {
        var mock = Mock.Of<BlockingIndexerService, IIndexedService>();
        mock.Item(1).Returns("mocked");

        await Assert.That(((IIndexedService)mock.Object)[1]).IsEqualTo("mocked");

        ((IIndexedService)mock.Object)[2] = "written";
        mock.SetItem(2, "written").WasCalled();
    }

    [Test]
    public async Task Wrapped_Instance_Keeps_Serving_An_Init_Only_Property()
    {
        var real = new WrappableInitOnlyProperty { Value = 11 };
        var mock = Mock.Wrap(real);

        await Assert.That(mock.Object.Value).IsEqualTo(11);

        mock.Value.Returns(33);

        await Assert.That(mock.Object.Value).IsEqualTo(33);
    }
}
