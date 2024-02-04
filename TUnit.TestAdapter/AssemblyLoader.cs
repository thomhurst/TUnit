using System.Reflection;
using System.Runtime.Loader;

namespace TUnit.TestAdapter;

public class AssemblyLoader
{
    internal Assembly? LoadByPath(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            return null;
        }

        try
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        }
        catch
        {
            return null;
        }
    }

    internal Assembly? LoadByName(AssemblyName assemblyName)
    {
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch
        {
            return null;
        }
    }
}