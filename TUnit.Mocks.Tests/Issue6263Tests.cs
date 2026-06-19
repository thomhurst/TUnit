using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6263
// When a derived interface hides a base property/indexer with `new` and a DIFFERENT accessor set
// (e.g. base { get; }, derived new { get; set; }), the two slots are distinct. The shared impl holds
// the merged accessors, but each explicit wrapper forward must emit only the accessors ITS slot
// declares — otherwise the forward adds an accessor the interface lacks and the build fails with
// CS0550. Both asymmetry directions are covered (adding and dropping an accessor in the derived slot).

#region Test interfaces

// Issue's exact case: base get-only, derived adds a setter.
public interface IGetOnlyBase
{
    string SomeProperty { get; }
}

public interface IAddSetterDerived : IGetOnlyBase
{
    new string SomeProperty { get; set; }
}

// Reverse asymmetry: base read/write, derived drops the setter.
public interface IReadWriteBase
{
    string Prop { get; set; }
}

public interface IDropSetterDerived : IReadWriteBase
{
    new string Prop { get; }
}

// Asymmetric indexer: base get-only, derived adds a setter.
public interface IGetOnlyIndexerBase
{
    int this[int index] { get; }
}

public interface IAddSetterIndexerDerived : IGetOnlyIndexerBase
{
    new int this[int index] { get; set; }
}

#endregion

public class Issue6263Tests
{
    [Test]
    public async Task Mocking_Asymmetric_New_Hidden_Property_Compiles()
    {
        // Before the fix this file failed to compile: CS0550 'IAddSetterDerivedMock.IGetOnlyBase
        // .SomeProperty.set' adds an accessor not found in interface member 'IGetOnlyBase.SomeProperty'.
        var mock = IAddSetterDerived.Mock();
        await Assert.That(mock).IsNotNull();
    }

    [Test]
    public async Task AddedSetter_Getter_Forwards_Through_Both_Slots()
    {
        var mock = IAddSetterDerived.Mock();
        mock.SomeProperty.Returns("configured");

        // The base slot is get-only; only the derived slot exposes the setter.
        await Assert.That(((IAddSetterDerived)mock).SomeProperty).IsEqualTo("configured");
        await Assert.That(((IGetOnlyBase)mock).SomeProperty).IsEqualTo("configured");

        // Setter exists on the derived slot only — must not throw / must compile.
        ((IAddSetterDerived)mock).SomeProperty = "x";
    }

    [Test]
    public async Task DroppedSetter_Setter_Still_Reachable_Through_Base_Slot()
    {
        // Reverse direction: derived slot is get-only, base slot keeps the setter.
        var mock = IDropSetterDerived.Mock();
        mock.Prop.Returns("configured");

        await Assert.That(((IDropSetterDerived)mock).Prop).IsEqualTo("configured");
        await Assert.That(((IReadWriteBase)mock).Prop).IsEqualTo("configured");

        // Setter exists on the base slot only.
        ((IReadWriteBase)mock).Prop = "y";
    }

    [Test]
    public async Task Asymmetric_New_Hidden_Indexer_Forwards_Through_Both_Slots()
    {
        var mock = IAddSetterIndexerDerived.Mock();
        mock.Item(0).Returns(100);

        await Assert.That(((IAddSetterIndexerDerived)mock)[0]).IsEqualTo(100);
        await Assert.That(((IGetOnlyIndexerBase)mock)[0]).IsEqualTo(100);

        // Setter on the derived slot only.
        ((IAddSetterIndexerDerived)mock)[0] = 5;
    }
}
