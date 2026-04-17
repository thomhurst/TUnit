#if NET
using System.Diagnostics;
using TUnit.Core;

namespace TUnit.UnitTests;

// Tests mutate DistributedContextPropagator.Current (process-global) — must not run concurrently.
[NotInParallel(nameof(PropagatorAlignmentTests))]
public class PropagatorAlignmentTests
{
    [Test]
    public async Task ModuleInitializer_Replaces_Default_Legacy_Propagator()
    {
        // Module init runs on first touch of any TUnit.Core type, so by now the default
        // LegacyPropagator must already be gone; otherwise cross-process baggage breaks.
        var current = DistributedContextPropagator.Current.GetType().FullName;
        await Assert.That(current).IsNotEqualTo("System.Diagnostics.LegacyPropagator");
    }

    [Test]
    public async Task AlignIfDefault_Leaves_Custom_Propagator_Untouched()
    {
        var original = DistributedContextPropagator.Current;
        var custom = DistributedContextPropagator.CreatePassThroughPropagator();

        try
        {
            DistributedContextPropagator.Current = custom;
            PropagatorAlignment.AlignIfDefault();
            await Assert.That(DistributedContextPropagator.Current).IsSameReferenceAs(custom);
        }
        finally
        {
            DistributedContextPropagator.Current = original;
        }
    }

    [Test]
    public async Task AlignIfDefault_Does_Not_Replace_Existing_W3C_Propagator()
    {
        var original = DistributedContextPropagator.Current;
        var w3c = PropagatorAlignment.CreateW3CPropagator();

        try
        {
            DistributedContextPropagator.Current = w3c;
            PropagatorAlignment.AlignIfDefault();
            await Assert.That(DistributedContextPropagator.Current).IsSameReferenceAs(w3c);
        }
        finally
        {
            DistributedContextPropagator.Current = original;
        }
    }
}
#endif
