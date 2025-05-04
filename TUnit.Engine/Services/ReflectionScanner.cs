using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Engine.Services;

[SuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
public class ReflectionScanner
{
    public static Type[] GetTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    return e.Types.OfType<Type>().ToArray();
                }
            })
            .ToArray();
    }
}