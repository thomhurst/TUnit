using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Parameter)]
public class MatrixMethodAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TClass>(string methodName)
    : MatrixAttribute(GetMethodValue(methodName))
{
    private static object?[] GetMethodValue(string methodName)
    {
        var result = typeof(TClass)
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)?
            .Invoke(null, null);

        if (result is object?[] objectArray)
        {
            return objectArray;
        }

        if (result is IEnumerable enumerable)
        {
            return [..enumerable];
        }
        
        return [result];
    }
}