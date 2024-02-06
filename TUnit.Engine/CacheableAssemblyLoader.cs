using System.Collections.Concurrent;
using System.Reflection;

namespace TUnit.Engine;

internal class CacheableAssemblyLoader
{
    private readonly ConcurrentDictionary<string, Assembly> _assemblies = new();

    public Assembly? GetOrLoadAssembly(string source)
    {
        var rootedSource = Path.IsPathRooted(source) ? source : Path.Combine(Directory.GetCurrentDirectory(), source);

        try
        {
            return _assemblies.GetOrAdd(rootedSource, _ => Assembly.LoadFile(rootedSource));
        }
        catch
        {
            return null;
        }
    }
}