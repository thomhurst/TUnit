using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6252
// An interface that hides a base member with `new` declares two distinct interface slots
// (IBase.SomeMethod and IDerived.SomeMethod). The generated wrapper forwards each member as an
// explicit interface impl, which satisfies only the slot it names — so the hidden base slot was
// left unimplemented and the build failed with CS0535. The wrapper must now forward both slots,
// for every member kind, and through every level of hiding / multiple-inheritance.

#region Test interfaces

public interface IShadowBase
{
    void Ping();
    int Value { get; }
    event System.Action Tick;
}

public interface IShadowDerived : IShadowBase
{
    new void Ping();
    new int Value { get; }
    new event System.Action Tick;
}

// Method with parameters and a non-void return — exercises argument + return forwarding.
public interface ICalcBase
{
    int Compute(int x);
}

public interface ICalcDerived : ICalcBase
{
    new int Compute(int x);
}

// Read/write property — both accessors must be forwarded for both slots.
public interface IRwPropBase
{
    string Name { get; set; }
}

public interface IRwPropDerived : IRwPropBase
{
    new string Name { get; set; }
}

// Indexer hiding.
public interface IIndexerBase
{
    int this[int index] { get; }
}

public interface IIndexerDerived : IIndexerBase
{
    new int this[int index] { get; }
}

// Three-level hiding — three distinct slots for one signature.
public interface ILevel1
{
    void Run();
}

public interface ILevel2 : ILevel1
{
    new void Run();
}

public interface ILevel3 : ILevel2
{
    new void Run();
}

// Diamond: two unrelated interfaces declare an identically-signed member; the combining
// interface inherits both. Same root cause (one impl satisfies both slots, wrapper must
// forward both) even though no `new` keyword is involved.
public interface IDiamondA
{
    void Go();
}

public interface IDiamondB
{
    void Go();
}

public interface IDiamondC : IDiamondA, IDiamondB
{
}

#endregion

public class Issue6252Tests
{
    [Test]
    public async Task Mocking_Interface_That_Hides_Base_Members_With_New_Compiles()
    {
        // Before the fix this file failed to compile: CS0535 'IShadowDerivedMock' does not
        // implement interface member 'IShadowBase.Ping()' (and Value / Tick).
        var mock = IShadowDerived.Mock();
        await Assert.That(mock).IsNotNull();
    }

    [Test]
    public async Task Wrapper_Is_Assignable_To_Both_Interfaces()
    {
        var mock = IShadowDerived.Mock();

        IShadowDerived asDerived = mock;
        IShadowBase asBase = mock;

        await Assert.That(asDerived).IsNotNull();
        await Assert.That(asBase).IsNotNull();
    }

    [Test]
    public async Task Wrapper_Forwards_Hidden_Void_Method_Through_Both_Slots()
    {
        var mock = IShadowDerived.Mock();

        ((IShadowDerived)mock).Ping();
        ((IShadowBase)mock).Ping();

        // Both slots dispatch to the single mocked member.
        mock.Ping().WasCalled(Times.Exactly(2));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Wrapper_Forwards_Hidden_Method_With_Args_And_Return_Through_Both_Slots()
    {
        var mock = ICalcDerived.Mock();
        mock.Compute(5).Returns(50);

        var viaDerived = ((ICalcDerived)mock).Compute(5);
        var viaBase = ((ICalcBase)mock).Compute(5);

        await Assert.That(viaDerived).IsEqualTo(50);
        await Assert.That(viaBase).IsEqualTo(50);
        mock.Compute(5).WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Wrapper_Forwards_Hidden_GetOnly_Property_Through_Both_Slots()
    {
        var mock = IShadowDerived.Mock();
        mock.Value.Returns(42);

        await Assert.That(((IShadowDerived)mock).Value).IsEqualTo(42);
        await Assert.That(((IShadowBase)mock).Value).IsEqualTo(42);
    }

    [Test]
    public async Task Wrapper_Forwards_Hidden_ReadWrite_Property_Through_Both_Slots()
    {
        var mock = IRwPropDerived.Mock();
        mock.Name.Returns("configured");

        // Getter forwarded for both slots.
        await Assert.That(((IRwPropDerived)mock).Name).IsEqualTo("configured");
        await Assert.That(((IRwPropBase)mock).Name).IsEqualTo("configured");

        // Setter forwarded for both slots (the explicit impl must declare both accessors,
        // or this would not compile / would throw).
        ((IRwPropDerived)mock).Name = "x";
        ((IRwPropBase)mock).Name = "y";
    }

    [Test]
    public async Task Wrapper_Forwards_Hidden_Indexer_Through_Both_Slots()
    {
        var mock = IIndexerDerived.Mock();
        mock.Item(0).Returns(100);

        await Assert.That(((IIndexerDerived)mock)[0]).IsEqualTo(100);
        await Assert.That(((IIndexerBase)mock)[0]).IsEqualTo(100);
        mock.Item(0).WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Wrapper_Forwards_Hidden_Event_Through_Both_Slots()
    {
        var mock = IShadowDerived.Mock();
        var derivedFired = 0;
        var baseFired = 0;

        // Subscribe through each slot — both add/remove forwards target the same backing event.
        ((IShadowDerived)mock).Tick += () => derivedFired++;
        ((IShadowBase)mock).Tick += () => baseFired++;

        mock.RaiseTick();

        await Assert.That(derivedFired).IsEqualTo(1);
        await Assert.That(baseFired).IsEqualTo(1);
    }

    [Test]
    public async Task Wrapper_Forwards_Method_Hidden_Across_Three_Levels()
    {
        var mock = ILevel3.Mock();

        ((ILevel3)mock).Run();
        ((ILevel2)mock).Run();
        ((ILevel1)mock).Run();

        mock.Run().WasCalled(Times.Exactly(3));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Wrapper_Forwards_Identical_Member_From_Multiple_Base_Interfaces()
    {
        var mock = IDiamondC.Mock();

        ((IDiamondA)mock).Go();
        ((IDiamondB)mock).Go();

        mock.Go().WasCalled(Times.Exactly(2));
        await Task.CompletedTask;
    }
}
