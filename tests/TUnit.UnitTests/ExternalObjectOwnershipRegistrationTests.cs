using TUnit.Core;

namespace TUnit.UnitTests;

public class ExternalObjectOwnershipRegistrationTests
{
    [Test]
    public async Task Repeated_Registration_Can_Be_Fully_Revoked_And_Registered_Again()
    {
        var instance = new object();
        var owner = new DataRowScopeCollection().Register(new Scope());
        ExternalObjectOwnership.Register(instance, owner);
        ExternalObjectOwnership.Register(instance, owner);
        ExternalObjectOwnership.Unregister(instance, owner);
        await Assert.That(ExternalObjectOwnership.IsOwned(instance)).IsFalse();

        ExternalObjectOwnership.Register(instance, owner);
        await Assert.That(ReferenceEquals(ExternalObjectOwnership.FindOwner(instance), owner)).IsTrue();
        GC.KeepAlive(owner);
    }

    [Test]
    public async Task Revoking_One_Shared_Owner_Preserves_The_Other()
    {
        var instance = new object();
        var first = new DataRowScopeCollection().Register(new Scope());
        var second = new DataRowScopeCollection().Register(new Scope());
        ExternalObjectOwnership.Register(instance, first);
        ExternalObjectOwnership.Register(instance, second);
        ExternalObjectOwnership.Unregister(instance, second);
        await Assert.That(ReferenceEquals(ExternalObjectOwnership.FindOwner(instance), first)).IsTrue();
        ExternalObjectOwnership.Unregister(instance, second);
        ExternalObjectOwnership.Unregister(instance, first);
        await Assert.That(ExternalObjectOwnership.IsOwned(instance)).IsFalse();
        GC.KeepAlive(first);
        GC.KeepAlive(second);
    }

    private sealed class Scope : IDisposable
    {
        public void Dispose() { }
    }
}
