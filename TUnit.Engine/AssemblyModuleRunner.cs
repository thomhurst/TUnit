using System.Reflection;
using System.Runtime.CompilerServices;

namespace TUnit.Engine;

internal class AssemblyModuleRunner
{
    public static void RunModuleInitializers()
    {
        foreach (var assembly in GetAssemblies().Where(x => x.FullName?.StartsWith("System") != true))
        {
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        }
    }

    private static IEnumerable<Assembly> GetAssemblies()
    {
        var rootAssembly = Assembly.GetEntryAssembly()!;

        var visited = new HashSet<string>();            
        var queue = new Queue<Assembly>();

        queue.Enqueue(rootAssembly);

        while (queue.Count != 0)
        {
            var assembly = queue.Dequeue();

            yield return assembly;

            var references = assembly.GetReferencedAssemblies();
            
            foreach(var reference in references)
            {
                if (visited.Add(reference.FullName))
                {
                    try
                    {
                        queue.Enqueue(Assembly.Load(reference));
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
    }
}