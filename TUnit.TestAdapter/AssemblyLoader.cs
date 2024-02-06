using System.Reflection;
using System.Runtime.Loader;
using TUnit.Core;
using TUnit.TestAdapter.Models;

namespace TUnit.TestAdapter;

public class AssemblyLoader
{
    internal AssemblyWithSource? LoadByPath(string source)
    {
        var rootedSource = Path.IsPathRooted(source) ? source : Path.Combine(Directory.GetCurrentDirectory(), source);
        
        if (!File.Exists(rootedSource))
        {
            return null;
        }

        try
        {
            return new AssemblyWithSource(source, rootedSource, AssemblyLoadContext.Default.LoadFromAssemblyPath(rootedSource));
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