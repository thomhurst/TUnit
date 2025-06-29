using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Parameter)]
public class MatrixMethodAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TClass>(string methodName) : MatrixAttribute where TClass : class
{
    private static readonly BindingFlags BindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Static;

    public override object?[] GetObjects(object? instance)
    {
        return GetMethodValue(methodName, instance as TClass);
    }

    private static object?[] GetMethodValue(string methodName, TClass? instance)
    {
        var methodInfo = typeof(TClass).GetMethod(methodName, BindingFlags);

        if (methodInfo == null)
        {
            throw new Exception($"Method {methodName} not found on {typeof(TClass).Name}");
        }

        var result = instance != null
            ? methodInfo.Invoke(instance, null)
            : methodInfo.Invoke(null, null);

        if (result is object?[] objectArray)
        {
            return objectArray;
        }

        if (result is IEnumerable enumerable)
        {
            return [.. enumerable];
        }

        return [result];
    }
}
