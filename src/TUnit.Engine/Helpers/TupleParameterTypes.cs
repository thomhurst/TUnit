using System.Collections.Concurrent;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Helpers;

internal static class TupleParameterTypes
{
    private static readonly ConcurrentDictionary<Type, Type[]> ElementTypes = new();

    // Data rows containing mutable struct initializers must stay flattened until invocation.
    // Inference still compares each box against its logical tuple element type.
    internal static Type[]? GetFlattenedTypes(Type[] parameterTypes, object?[] arguments, bool includesInjectedTokens = false)
    {
        var dataIndex = -1;
        var tokenCount = 0;
        for (var i = 0; i < parameterTypes.Length; i++)
        {
            if (parameterTypes[i] == typeof(CancellationToken))
            {
                tokenCount++;
            }
            else if (dataIndex == -1)
            {
                dataIndex = i;
            }
            else
            {
                return null;
            }
        }

        if (dataIndex == -1 || !TupleHelper.IsTupleType(parameterTypes[dataIndex]))
        {
            return null;
        }

        var tupleType = parameterTypes[dataIndex];
        var argumentIndex = includesInjectedTokens ? dataIndex : 0;
        if (argumentIndex >= arguments.Length
            || arguments.Length == 1 + (includesInjectedTokens ? tokenCount : 0)
                && (arguments[argumentIndex] is null || TupleHelper.IsTupleType(arguments[argumentIndex])))
        {
            return null;
        }

        var elements = ElementTypes.GetOrAdd(tupleType, static type => GetElementTypes(type));
        return arguments.Length == elements.Length + (includesInjectedTokens ? tokenCount : 0)
            ? elements
            : null;
    }

    private static Type[] GetElementTypes(Type type)
    {
        var arguments = type.GetGenericArguments();
        if (arguments.Length != 8 || !TupleHelper.IsTupleType(arguments[7]))
        {
            return arguments;
        }

        var rest = GetElementTypes(arguments[7]);
        var result = new Type[7 + rest.Length];
        Array.Copy(arguments, result, 7);
        Array.Copy(rest, 0, result, 7, rest.Length);
        return result;
    }
}
