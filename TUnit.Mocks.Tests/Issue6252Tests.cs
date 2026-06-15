using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6252
// An interface that hides a base member with `new` declares two distinct interface slots
// (IBase.SomeMethod and IDerived.SomeMethod). The generated wrapper forwards each member as an
// explicit interface impl, which satisfies only the slot it names — so the hidden base slot was
// left unimplemented and the build failed with CS0535. The wrapper must now forward both slots.
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
    public async Task Wrapper_Forwards_Hidden_Method_Through_Both_Interface_Slots()
    {
        var mock = IShadowDerived.Mock();

        // Drive the wrapper's explicit forwards via each interface slot.
        ((IShadowDerived)mock).Ping();
        ((IShadowBase)mock).Ping();

        // Both slots dispatch to the single mocked member.
        mock.Ping().WasCalled(Times.Exactly(2));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Wrapper_Forwards_Hidden_Property_Through_Both_Interface_Slots()
    {
        var mock = IShadowDerived.Mock();
        mock.Value.Returns(42);

        await Assert.That(((IShadowDerived)mock).Value).IsEqualTo(42);
        await Assert.That(((IShadowBase)mock).Value).IsEqualTo(42);
    }
}
