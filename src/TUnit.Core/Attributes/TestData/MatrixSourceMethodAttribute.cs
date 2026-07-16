using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Parameter)]
public class MatrixMethodAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TClass>(string methodName) : MatrixAttribute where TClass : class
{
    private static readonly BindingFlags BindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Static;

    public override object?[] GetObjects(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return GetMethodValue(methodName, dataGeneratorMetadata);
    }

    private static object?[] GetMethodValue(string methodName, DataGeneratorMetadata dataGeneratorMetadata)
    {
        var instance = dataGeneratorMetadata.TestClassInstance as TClass;

        var methodInfo = typeof(TClass).GetMethods(BindingFlags).SingleOrDefault(x => x.Name == methodName)
            ?? typeof(TClass).GetMethod(methodName, BindingFlags);

        if (methodInfo == null)
        {
            throw new Exception($"Method {methodName} not found on {typeof(TClass).Name}");
        }

        var result = methodInfo.IsStatic
            ? methodInfo.Invoke(null, null)
            : methodInfo.Invoke(instance, null);

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
