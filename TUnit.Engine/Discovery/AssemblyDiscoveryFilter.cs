using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Discovery;

internal static class AssemblyDiscoveryFilter
{
    public static bool IsExcludedFromTestDiscovery(Assembly assembly) =>
        SourceRegistrar.IsAssemblyExcludedFromDiscovery(assembly);
}
