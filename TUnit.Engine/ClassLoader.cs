using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine;

internal class ClassLoader
{
    public IEnumerable<Type> GetAllTypes(AssemblyWithSource[] assemblies)
    {
        return assemblies.SelectMany(x => LoadTypes(x.Assembly));
    }

    private static IEnumerable<Type> LoadTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException reflectionTypeLoadException)
        {
            return reflectionTypeLoadException.Types.OfType<Type>();
        }
        catch
        {
            return [];
        }
    }
}