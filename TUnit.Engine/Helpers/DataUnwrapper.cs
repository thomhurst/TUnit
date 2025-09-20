using System;
using System.Linq;
using TUnit.Core;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Helpers;

internal class DataUnwrapper
{
    public static object?[] Unwrap(object?[] values)
    {
        if(values.Length == 1 && DataSourceHelpers.IsTuple(values[0]))
        {
            return values[0].ToObjectArray();
        }

        return values;
    }
    
    public static object?[] UnwrapWithTypes(object?[] values, ParameterMetadata[]? expectedParameters)
    {
        // If no parameter information, fall back to default behavior
        if (expectedParameters == null || expectedParameters.Length == 0)
        {
            return Unwrap(values);
        }
        
        // Special case: If we have a single value that's a tuple, and a single parameter that expects a tuple,
        // don't unwrap it
        if (values.Length == 1 && 
            expectedParameters.Length == 1 && 
            DataSourceHelpers.IsTuple(values[0]) &&
            IsTupleType(expectedParameters[0].Type))
        {
            return values;
        }
        
        // Otherwise use the default unwrapping
        if(values.Length == 1 && DataSourceHelpers.IsTuple(values[0]))
        {
            var paramTypes = expectedParameters.Select(p => p.Type).ToArray();
            return values[0].ToObjectArrayWithTypes(paramTypes);
        }

        return values;
    }
    
    private static bool IsTupleType(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var genericType = type.GetGenericTypeDefinition();
        return genericType == typeof(ValueTuple<>) ||
            genericType == typeof(ValueTuple<,>) ||
            genericType == typeof(ValueTuple<,,>) ||
            genericType == typeof(ValueTuple<,,,>) ||
            genericType == typeof(ValueTuple<,,,,>) ||
            genericType == typeof(ValueTuple<,,,,,>) ||
            genericType == typeof(ValueTuple<,,,,,,>) ||
            genericType == typeof(ValueTuple<,,,,,,,>) ||
            genericType == typeof(Tuple<>) ||
            genericType == typeof(Tuple<,>) ||
            genericType == typeof(Tuple<,,>) ||
            genericType == typeof(Tuple<,,,>) ||
            genericType == typeof(Tuple<,,,,>) ||
            genericType == typeof(Tuple<,,,,,>) ||
            genericType == typeof(Tuple<,,,,,,>) ||
            genericType == typeof(Tuple<,,,,,,,>);
    }
}
