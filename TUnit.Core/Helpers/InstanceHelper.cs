using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core.Enums;
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

    public static async Task InitializePropertiesAsync(object instance, MethodMetadata methodInformation, TestBuilderContext testBuilderContext)
    {
        var classInformation = methodInformation.Class;
        
        foreach (var propertyInformation in classInformation.Properties)
        {
            foreach (var dataAttribute in propertyInformation.Attributes.OfType<IDataAttribute>())
            {
                var testBuilderContextAccessor = new TestBuilderContextAccessor(testBuilderContext);

                var metadata = new DataGeneratorMetadata
                {
                    Type = DataGeneratorType.Property,
                    TestInformation = methodInformation,
                    ClassInstanceArguments = [],
                    MembersToGenerate = [propertyInformation],
                    TestBuilderContext = testBuilderContextAccessor,
                    TestClassInstance = instance,
                    TestSessionId = string.Empty,
                };

                var value = await ReflectionValueCreator.CreatePropertyValueAsync(
                    classInformation,
                    testBuilderContextAccessor,
                    dataAttribute,
                    propertyInformation,
                    metadata).ConfigureAwait(false);

                if (value is not null)
                {
                    // Initialize the property value using DataSourceInitializer
                    // This handles nested data sources and IAsyncInitializer
                    await DataSourceInitializer.InitializeAsync(value, metadata, testBuilderContextAccessor).ConfigureAwait(false);
                }

                propertyInformation.ReflectionInfo.SetValue(instance, value);
            }
        }
    }

    private static object CreateObject(ClassMetadata classInformation, object?[]? args, IDictionary<string, object?>? testClassProperties, TestBuilderContext testBuilderContext)
    {
        try
        {
            if (classInformation.Attributes.OfType<ClassConstructorAttribute>().FirstOrDefault() is { } classConstructorAttribute)
            {
                var classConstructor = (IClassConstructor) Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!;

                return classConstructor.Create(classInformation.Type, new ClassConstructorMetadata
                {
                    TestBuilderContext = testBuilderContext,
                    TestSessionId = TestSessionContext.Current?.Id ?? string.Empty
                });
            }
            var type = classInformation.Type;

            var parameters = type.GetConstructors().First(x => !x.IsStatic).GetParameters();

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
}
