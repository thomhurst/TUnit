using System.Reflection;

namespace TUnit.TestAdapter;

public class AssemblyLoader
{
    public IEnumerable<Assembly> GetLoadedAssemblies()
    {
        var entry = Assembly.GetEntryAssembly();

        if (entry is null)
        {
            yield break;
        }

        foreach (var assembly in Load(entry))
        {
            yield return assembly;
        }
    }

    private IEnumerable<Assembly> Load(Assembly assembly)
    {
        yield return assembly;

        foreach (var referencedAssembly in assembly.GetReferencedAssemblies())
        {
            foreach (var innerAssembly in Load(Assembly.Load(referencedAssembly)))
            {
                yield return innerAssembly;
            }
        }
    }
}