using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class WeakReferenceAssertionTests
{
    [Test]
    public async Task Test_WeakReference_IsAlive()
    {
        var target = new object();
        var weakRef = new WeakReference(target);
        await Assert.That(weakRef).IsAlive();
        GC.KeepAlive(target);
    }

    [Test]
    public async Task Test_WeakReference_IsNotAlive()
    {
        var weakRef = CreateWeakReferenceToCollectedObject();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        await Assert.That(weakRef).IsNotAlive();
    }

    [Test]
    public async Task Test_WeakReference_DoesNotTrackResurrection()
    {
        var target = new object();
        var weakRef = new WeakReference(target, trackResurrection: false);
        await Assert.That(weakRef).DoesNotTrackResurrection();
        GC.KeepAlive(target);
    }

    [Test]
    public async Task Test_WeakReference_TrackResurrection()
    {
        var target = new object();
        var weakRef = new WeakReference(target, trackResurrection: true);
        await Assert.That(weakRef).TrackResurrection();
        GC.KeepAlive(target);
    }

    private static WeakReference CreateWeakReferenceToCollectedObject()
    {
        var temp = new object();
        return new WeakReference(temp);
    }
}
