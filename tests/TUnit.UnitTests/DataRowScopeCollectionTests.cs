using TUnit.Core;

namespace TUnit.UnitTests;

public class DataRowScopeCollectionTests
{
    [Test]
    public async Task ReleaseUsesReferenceIdentityAndLeavesUnrelatedScopesAlive()
    {
        var scopes = new DataRowScopeCollection();
        var first = new Scope();
        var second = new Scope();
        var firstValue = new EqualValue();
        scopes.RegisterObject(scopes.Register(first), firstValue);
        scopes.RegisterObject(scopes.Register(second), new EqualValue());

        await scopes.ReleaseAsync(firstValue);
        await Assert.That(first.DisposeCount).IsEqualTo(1);
        await Assert.That(second.DisposeCount).IsEqualTo(0);
        await scopes.CompleteAsync();
        await Assert.That(first.DisposeCount).IsEqualTo(1);
        await Assert.That(second.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task SharedResolvedObjectReleasesAllOwnersOnceAcrossClones()
    {
        var scopes = new DataRowScopeCollection();
        var first = new Scope();
        var second = new Scope();
        var value = new object();
        scopes.RegisterObject(scopes.Register(first), value);
        scopes.RegisterObject(scopes.Register(second), value);
        var clone = scopes.Clone();

        await clone.ReleaseAsync(value);
        await scopes.CompleteAsync();
        await clone.CompleteAsync();
        await Assert.That(first.DisposeCount).IsEqualTo(1);
        await Assert.That(second.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task ThrowingScopeDoesNotPreventOtherOwnersFromCompleting()
    {
        var scopes = new DataRowScopeCollection();
        var failed = new Scope { ThrowOnDispose = true };
        var successful = new Scope();
        scopes.Register(failed); // A factory may fail before returning an object.
        scopes.Register(successful);

        var errors = await scopes.CompleteAsync();
        await Assert.That(errors!.Count).IsEqualTo(1);
        await Assert.That(errors[0].Message).IsEqualTo("Scope failed");
        await Assert.That(failed.DisposeCount).IsEqualTo(1);
        await Assert.That(successful.DisposeCount).IsEqualTo(1);
        await Assert.That(await scopes.CompleteAsync()).IsNull();
    }

    private sealed class Scope : IDisposable
    {
        public int DisposeCount;
        public bool ThrowOnDispose;

        public void Dispose()
        {
            DisposeCount++;
            if (ThrowOnDispose)
            {
                throw new InvalidOperationException("Scope failed");
            }
        }
    }

    private sealed record EqualValue;
}
