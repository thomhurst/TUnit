using System.Reflection;

namespace TUnit.Core.Extensions;

public static class MetadataExtensions
{
    public static Type DeclaringType(this MethodMetadata method) => method.Class.Type;

    public static string MethodName(this MethodMetadata method) => method.Name;

    public static string DisplayName(this MethodMetadata method) => method.Name;

    public static bool IsGenericMethodDefinition(this MethodMetadata method) => method.GenericTypeCount > 0;

    public static MethodInfo GetReflectionInfo(this MethodMetadata method)
    {
        return method.Type.GetMethod(method.Name, method.Parameters.Select(x => x.Type).ToArray())!;
    }

    public static IEnumerable<Attribute> GetCustomAttributes(this MethodMetadata method)
    {
        return
        [
            ..method.GetReflectionInfo().GetCustomAttributes(),
            ..method.Type.GetCustomAttributes(),
            ..method.Type.Assembly.GetCustomAttributes()
        ];
    }
}
