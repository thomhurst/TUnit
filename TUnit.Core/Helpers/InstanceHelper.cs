using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Exceptions;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

[RequiresDynamicCode("Calls System.Type.MakeGenericType(params Type[])")]
[RequiresUnreferencedCode("Reflection")]
internal static class InstanceHelper
{
    public static object CreateInstance(MethodMetadata methodInformation, object?[]? args, IDictionary<string, object?>? testClassProperties, TestBuilderContext testBuilderContext)
    {
        var classInformation = methodInformation.Class;
        var instance = CreateObject(classInformation, args, testClassProperties, testBuilderContext);

        // Properties with data attributes are handled separately after instance creation
        // to support async initialization

        return instance;
    }

    private static object CreateObject(ClassMetadata classInformation, object?[]? args, IDictionary<string, object?>? testClassProperties, TestBuilderContext testBuilderContext)
    {
        try
        {
            if (classInformation.Attributes.Select(a => a.Instance).OfType<ClassConstructorAttribute>().FirstOrDefault() is { } classConstructorAttribute)
            {
                var classConstructor = (IClassConstructor) Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!;

                return classConstructor.Create(classInformation.Type, new ClassConstructorMetadata
                {
                    TestBuilderContext = testBuilderContext,
                    TestSessionId = TestSessionContext.Current?.Id ?? string.Empty
                });
            }
            var type = classInformation.Type;

            // Find the best matching constructor
            var constructors = type.GetConstructors().Where(x => !x.IsStatic).ToArray();
            var constructor = FindBestMatchingConstructor(constructors);

            var parameters = constructor.GetParameters();

            var castedArgs = args?.Select((a, index) =>
            {
                var parameterType = parameters.ElementAtOrDefault(index)?.ParameterType;

                if (parameterType is null)
                {
                    return a;
                }

                return CastHelper.Cast(parameterType, a);
            }).ToArray();

            if (type.ContainsGenericParameters)
            {
                var substitutedTypes = type.GetGenericArguments()
                    .Select(pc => parameters.Select(p => p.ParameterType).ToList().FindIndex(pt => pt == pc))
                    .Select(i => castedArgs![i]!.GetType())
                    .ToArray();

                type = type.MakeGenericType(substitutedTypes);
            }

            var instance = Activator.CreateInstance(type, castedArgs)!;

            foreach (var (propertyName, value) in testClassProperties ?? new Dictionary<string, object?>())
            {
                type.GetProperty(propertyName)!.SetValue(instance, value);
            }

            return instance;
        }
        catch (TargetInvocationException targetInvocationException)
        {
            ExceptionDispatchInfo.Capture(targetInvocationException.InnerException ?? targetInvocationException).Throw();
            throw;
        }
        catch (MissingMethodException e)
        {
            throw new TUnitException("Cannot create instance of type " + classInformation.Type.FullName, e);
        }
    }

    private static ConstructorInfo FindBestMatchingConstructor(ConstructorInfo[] constructors)
    {
        return constructors.First();
    }
}
