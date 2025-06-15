using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Services.Reflection;

[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("Trimming", "IL2070")]
[UnconditionalSuppressMessage("Trimming", "IL2072")]
[UnconditionalSuppressMessage("Trimming", "IL2111")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
internal static class InstanceCreator
{
    // Synchronous wrapper for backward compatibility
    public static object? CreateInstance(
        IDataAttribute typeDataAttribute,
        SourceGeneratedClassInformation classInformation,
        object?[] classInstanceArguments,
        SourceGeneratedMethodInformation testInformation,
        TestBuilderContextAccessor testBuilderContextAccessor,
        out Exception? exception)
    {
        return CreateInstance(typeDataAttribute, classInformation, classInstanceArguments, 
            testInformation, testBuilderContextAccessor, false, out exception);
    }
    
    // Synchronous wrapper for backward compatibility
    public static object? CreateInstance(
        IDataAttribute typeDataAttribute,
        SourceGeneratedClassInformation classInformation,
        object?[] classInstanceArguments,
        SourceGeneratedMethodInformation testInformation,
        TestBuilderContextAccessor testBuilderContextAccessor,
        bool skipPropertyInitialization,
        out Exception? exception)
    {
        var (instance, error) = CreateInstanceAsync(typeDataAttribute, classInformation, classInstanceArguments,
            testInformation, testBuilderContextAccessor, skipPropertyInitialization).GetAwaiter().GetResult();
        exception = error;
        return instance;
    }

    public static async Task<(object? Instance, Exception? Exception)> CreateInstanceAsync(
        IDataAttribute typeDataAttribute,
        SourceGeneratedClassInformation classInformation,
        object?[] classInstanceArguments,
        SourceGeneratedMethodInformation testInformation,
        TestBuilderContextAccessor testBuilderContextAccessor)
    {
        return await CreateInstanceAsync(typeDataAttribute, classInformation, classInstanceArguments, 
            testInformation, testBuilderContextAccessor, false);
    }

    public static async Task<(object? Instance, Exception? Exception)> CreateInstanceAsync(
        IDataAttribute typeDataAttribute,
        SourceGeneratedClassInformation classInformation,
        object?[] classInstanceArguments,
        SourceGeneratedMethodInformation testInformation,
        TestBuilderContextAccessor testBuilderContextAccessor,
        bool skipPropertyInitialization)
    {
        try
        {
            if (typeDataAttribute is ClassConstructorAttribute classConstructorAttribute)
            {
                return (CreateByClassConstructor(classInformation.Type, classConstructorAttribute), null);
            }

            var args = classInstanceArguments
                .Select((x, i) => CastHelper.Cast(testInformation.Class.Parameters[i].Type, x))
                .ToArray();

            var propertyArgs = skipPropertyInitialization
                ? new Dictionary<string, object?>()
                : await GetPropertyArgumentsAsync(classInformation, args, testInformation, testBuilderContextAccessor);

            var instance = InstanceHelper.CreateInstance(testInformation, args, propertyArgs, 
                testBuilderContextAccessor.Current);
            return (instance, null);
        }
        catch (Exception e)
        {
            return (null, e);
        }
    }

    private static object CreateByClassConstructor(Type type, ClassConstructorAttribute classConstructorAttribute)
    {
        var classConstructor = (IClassConstructor)Activator.CreateInstance(
            classConstructorAttribute.ClassConstructorType)!;

        var metadata = new ClassConstructorMetadata
        {
            TestBuilderContext = new TestBuilderContext(),
            TestSessionId = string.Empty,
        };

        var createMethod = typeof(IClassConstructor)
            .GetMethod(nameof(IClassConstructor.Create))!
            .MakeGenericMethod(type);

        return createMethod.Invoke(classConstructor, [metadata])!;
    }

    private static async Task<Dictionary<string, object?>> GetPropertyArgumentsAsync(
        SourceGeneratedClassInformation classInformation,
        object?[] args,
        SourceGeneratedMethodInformation testInformation,
        TestBuilderContextAccessor testBuilderContextAccessor)
    {
        var propertyArgs = new Dictionary<string, object?>();
        
        await foreach (var (propertyInformation, argsFunc) in GetPropertyArgumentsEnumerableAsync(
            classInformation, args, testInformation, testBuilderContextAccessor))
        {
            var propArgs = await argsFunc();
            propertyArgs[propertyInformation.Name] = propArgs.ElementAtOrDefault(0);
        }
        
        return propertyArgs;
    }

    public static async IAsyncEnumerable<(SourceGeneratedPropertyInformation PropertyInformation, Func<Task<object?[]>> Args)> 
        GetPropertyArgumentsEnumerableAsync(
            SourceGeneratedClassInformation type,
            object?[] classInstanceArguments,
            SourceGeneratedMethodInformation testInformation,
            TestBuilderContextAccessor testBuilderContextAccessor)
    {
        var properties = testInformation.Class.Properties;

        foreach (var propertyInformation in properties)
        {
            var dataAttribute = propertyInformation.Attributes
                .OfType<IDataAttribute>()
                .ElementAtOrDefault(0);

            if (dataAttribute is null)
            {
                continue;
            }

            var context = new DataGeneratorContext
            {
                TypeDataAttribute = dataAttribute,
                ClassInformation = type,
                Method = testInformation,
                PropertyInfo = propertyInformation,
                TestDataAttribute = dataAttribute,
                DataGeneratorType = DataGeneratorType.Property,
                ClassInstanceArguments = () => classInstanceArguments,
                TestInformation = testInformation,
                TestBuilderContextAccessor = testBuilderContextAccessor,
                ClassInstanceArgumentsInvoked = classInstanceArguments,
                NeedsInstance = false
            };

            await foreach (var func in DataGeneratorHandler.GetArgumentsFromDataAttributeAsync(dataAttribute, context))
            {
                yield return (propertyInformation, func);
                break; // Only take the first one
            }
        }
    }
}