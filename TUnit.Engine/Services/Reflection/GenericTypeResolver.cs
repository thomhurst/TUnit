using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Services.Reflection;

[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("Trimming", "IL2055")]
[UnconditionalSuppressMessage("Trimming", "IL2060")]
[UnconditionalSuppressMessage("Trimming", "IL2075")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
internal static class GenericTypeResolver
{
    public static TestMethod ResolveGenericMethod(
        TestMethod methodInfo,
        object?[] arguments)
    {
        if (!methodInfo.ReflectionInformation.IsGenericMethodDefinition)
        {
            return methodInfo;
        }

        var typeArguments = methodInfo.ReflectionInformation.GetGenericArguments();
        var parameters = methodInfo.Parameters;
        var argumentsTypes = arguments.Select(x => x?.GetType()).ToArray();

        var typeParameterMap = BuildTypeParameterMap(parameters, argumentsTypes);
        var substituteTypes = ResolveTypeArguments(typeArguments, parameters, argumentsTypes, typeParameterMap);

        return ReflectionToSourceModelHelpers.BuildTestMethod(
            methodInfo.Class,
            methodInfo.ReflectionInformation.MakeGenericMethod(substituteTypes.ToArray()),
            methodInfo.Name);
    }

    public static (TestClass ClassInfo, TestMethod MethodInfo) 
        ResolveGenericClass(
            TestClass classInformation,
            TestMethod testInformation,
            object?[] invokedClassInstanceArguments)
    {
        if (!classInformation.Type.ContainsGenericParameters)
        {
            return (classInformation, testInformation);
        }

        var classParametersTypes = testInformation.Class.Parameters.Select(p => p.Type).ToList();

        var substitutedTypes = classInformation.Type.GetGenericArguments()
            .Select(pc => classParametersTypes.FindIndex(pt => pt == pc))
            .Select(i => invokedClassInstanceArguments[i]!.GetType())
            .ToArray();

        var newClassInfo = ReflectionToSourceModelHelpers.GenerateClass(
            classInformation.Type.MakeGenericType(substitutedTypes));

        var newMethodInfo = ReflectionToSourceModelHelpers.BuildTestMethod(
            newClassInfo,
            newClassInfo.Type.GetMembers()
                .OfType<MethodInfo>()
                .First(x => x.Name == testInformation.Name &&
                           x.GetParameters().Length == testInformation.Parameters.Length),
            testInformation.Name);

        return (newClassInfo, newMethodInfo);
    }

    private static Dictionary<Type, Type> BuildTypeParameterMap(
        TestParameter[] parameters,
        Type?[] argumentsTypes)
    {
        var typeParameterMap = new Dictionary<Type, Type>();

        for (var i = 0; i < parameters.Length && i < argumentsTypes.Length; i++)
        {
            var parameterType = parameters[i].Type;
            var argumentType = argumentsTypes[i];

            if (argumentType != null)
            {
                MapTypeParameters(parameterType, argumentType, typeParameterMap);
            }
        }

        return typeParameterMap;
    }

    private static List<Type> ResolveTypeArguments(
        Type[] typeArguments,
        TestParameter[] parameters,
        Type?[] argumentsTypes,
        Dictionary<Type, Type> typeParameterMap)
    {
        var substituteTypes = new List<Type>();

        foreach (var typeArgument in typeArguments)
        {
            if (typeParameterMap.TryGetValue(typeArgument, out var mappedType))
            {
                substituteTypes.Add(mappedType);
            }
            else
            {
                var inferredType = InferTypeFromParameters(typeArgument, parameters, argumentsTypes);
                substituteTypes.Add(inferredType);
            }
        }

        return substituteTypes;
    }

    private static Type InferTypeFromParameters(
        Type typeArgument,
        TestParameter[] parameters,
        Type?[] argumentsTypes)
    {
        var parameterIndex = FindParameterIndex(typeArgument, parameters);

        if (parameterIndex >= 0 && parameterIndex < argumentsTypes.Length && 
            argumentsTypes[parameterIndex] != null)
        {
            var inferredType = argumentsTypes[parameterIndex]!;

            if (parameters[parameterIndex].Type.IsGenericType &&
                parameters[parameterIndex].Type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                inferredType = Nullable.GetUnderlyingType(inferredType) ?? inferredType;
            }

            return inferredType;
        }

        throw new InvalidOperationException(
            $"Cannot infer type for generic parameter '{typeArgument.Name}'. No matching argument found.");
    }

    private static int FindParameterIndex(Type typeArgument, TestParameter[] parameters)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].Type == typeArgument ||
                (parameters[i].Type.IsGenericType &&
                 parameters[i].Type.GetGenericArguments().Contains(typeArgument)))
            {
                return i;
            }
        }

        return -1;
    }

    private static void MapTypeParameters(
        Type parameterType,
        Type argumentType,
        Dictionary<Type, Type> typeParameterMap)
    {
        if (parameterType.IsGenericParameter)
        {
            if (!typeParameterMap.ContainsKey(parameterType))
            {
                typeParameterMap[parameterType] = argumentType;
            }
        }
        else if (parameterType.IsGenericType && argumentType.IsGenericType)
        {
            MapGenericTypeParameters(parameterType, argumentType, typeParameterMap);
        }
        else if (parameterType.IsGenericType && 
                 parameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            MapNullableTypeParameters(parameterType, argumentType, typeParameterMap);
        }
    }

    private static void MapGenericTypeParameters(
        Type parameterType,
        Type argumentType,
        Dictionary<Type, Type> typeParameterMap)
    {
        var parameterGenericDef = parameterType.GetGenericTypeDefinition();
        var argumentGenericDef = argumentType.GetGenericTypeDefinition();

        if (parameterGenericDef == argumentGenericDef)
        {
            var parameterTypeArgs = parameterType.GetGenericArguments();
            var argumentTypeArgs = argumentType.GetGenericArguments();

            for (var i = 0; i < Math.Min(parameterTypeArgs.Length, argumentTypeArgs.Length); i++)
            {
                MapTypeParameters(parameterTypeArgs[i], argumentTypeArgs[i], typeParameterMap);
            }
        }
    }

    private static void MapNullableTypeParameters(
        Type parameterType,
        Type argumentType,
        Dictionary<Type, Type> typeParameterMap)
    {
        var underlyingParameterType = parameterType.GetGenericArguments()[0];
        var underlyingArgumentType = Nullable.GetUnderlyingType(argumentType) ?? argumentType;
        MapTypeParameters(underlyingParameterType, underlyingArgumentType, typeParameterMap);
    }
}