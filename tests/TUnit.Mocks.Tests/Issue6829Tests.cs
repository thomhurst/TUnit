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
    public async Task Wrapped_Instance_Keeps_Serving_An_Init_Only_Property()
    {
        var real = new WrappableInitOnlyProperty { Value = 11 };
        var mock = Mock.Wrap(real);

        await Assert.That(mock.Object.Value).IsEqualTo(11);

        mock.Value.Returns(33);

        await Assert.That(mock.Object.Value).IsEqualTo(33);
    }
}
