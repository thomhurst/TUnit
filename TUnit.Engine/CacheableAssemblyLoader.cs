using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine;

internal class CacheableAssemblyLoader
{
    private readonly ConcurrentDictionary<string, CachedAssemblyInformation> _assemblies = new();

    public ICollection<CachedAssemblyInformation> CachedAssemblies => _assemblies.Values;

    public CachedAssemblyInformation GetOrLoadAssembly(string source)
    {
        var rootedSource = Path.IsPathRooted(source) ? source : Path.Combine(Directory.GetCurrentDirectory(), source);

        try
        {
            return _assemblies.GetOrAdd(rootedSource, _ => new CachedAssemblyInformation(Assembly.LoadFile(rootedSource)));
        }
        catch
        {
            return new CachedAssemblyInformation(Assembly.GetCallingAssembly());
        }
    }
}