using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine;

internal class CacheableAssemblyLoader
{
    private readonly ConcurrentDictionary<string, CachedAssemblyInformation> _assemblies = new();

    public ICollection<CachedAssemblyInformation> CachedAssemblies => _assemblies.Values;

    public CachedAssemblyInformation GetOrLoadAssembly(string assemblyFullName)
    {
        return _assemblies.GetOrAdd(assemblyFullName,
            _ => new CachedAssemblyInformation(Assembly.Load(assemblyFullName)));
    }
}