using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Engine.Helpers;

internal static class AssemblyReferenceCache
{
    private static readonly ConcurrentDictionary<Assembly, AssemblyName[]> _assemblyCache = new();
    private static readonly ConcurrentDictionary<Type, Type[]> _interfaceCache = new();

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Assembly.GetReferencedAssemblies is reflection-based but safe for checking references")]
    public static AssemblyName[] GetReferencedAssemblies(Assembly assembly)
    {
        return _assemblyCache.GetOrAdd(assembly, static a => a.GetReferencedAssemblies());
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Type.GetInterfaces is reflection-based but safe for type checking")]
    public static Type[] GetInterfaces(Type type)
    {
        return _interfaceCache.GetOrAdd(type, static t => t.GetInterfaces());
    }
}
