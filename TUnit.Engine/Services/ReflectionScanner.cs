using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Polyfills;

namespace TUnit.Engine.Services;

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
internal static class ReflectionScanner
{
    private static HashSet<Type>? _types;

    public static HashSet<Type> GetTypes()
    {
        return _types ??= AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => x.GetReferencedAssemblies().Select(ra => ra.Name).Contains("TUnit.Core"))
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    return e.Types.OfType<Type>();
                }
            })
            .Where(x => x.Name == "NestedClassDataSourceDrivenTests3")
            .ToHashSet();
    }
}
