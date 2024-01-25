using System.Reflection;

namespace TUnit.TestAdapter;

internal class AssemblyLoader
{
    internal Assembly? LoadByPath(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            return null;
        }

        try
        {
            return Assembly.LoadFrom(assemblyPath);
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