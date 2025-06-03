using System;
using System.Runtime.CompilerServices;

namespace TUnit.Core;

internal class DependencyTracker : IDisposable
{
    private readonly ConditionalWeakTable<object, object> _dependencies = new();
    private int _dependencyCount;

    public void TrackDependency(object dependency)
    {
        if (dependency == null)
            return;

        _dependencies.GetOrCreateValue(dependency);
        _dependencyCount++;
    }

    public int Count => _dependencyCount;

    public void Dispose()
    {
        // ConditionalWeakTable doesn't need explicit cleanup - it handles GC automatically
        // Just reset the count
        _dependencyCount = 0;
    }
}
