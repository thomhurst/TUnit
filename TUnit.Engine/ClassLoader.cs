using System.Reflection;

namespace TUnit.Engine;

internal class ClassLoader
{
    public IEnumerable<Type> GetAllTypes(Assembly[] assemblies)
    {
        return assemblies.SelectMany(LoadTypes);
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