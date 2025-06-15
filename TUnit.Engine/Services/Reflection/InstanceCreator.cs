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

    public static object? CreateInstance(
        IDataAttribute typeDataAttribute,
        SourceGeneratedClassInformation classInformation,
        object?[] classInstanceArguments,
        SourceGeneratedMethodInformation testInformation,
        TestBuilderContextAccessor testBuilderContextAccessor,
        bool skipPropertyInitialization,
        out Exception? exception)
    {
        exception = null;

        try
        {
            if (typeDataAttribute is ClassConstructorAttribute classConstructorAttribute)
            {
                return CreateByClassConstructor(classInformation.Type, classConstructorAttribute);
            }

            var args = classInstanceArguments
                .Select((x, i) => CastHelper.Cast(testInformation.Class.Parameters[i].Type, x))
                .ToArray();

            var propertyArgs = skipPropertyInitialization
                ? new Dictionary<string, object?>()
                : GetPropertyArguments(classInformation, args, testInformation, testBuilderContextAccessor);

            return InstanceHelper.CreateInstance(testInformation, args, propertyArgs, 
                testBuilderContextAccessor.Current);
        }
        catch (Exception e)
        {
            exception = e;
            return null;
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

    private static Dictionary<string, object?> GetPropertyArguments(
        SourceGeneratedClassInformation classInformation,
        object?[] args,
        SourceGeneratedMethodInformation testInformation,
        TestBuilderContextAccessor testBuilderContextAccessor)
    {
        return GetPropertyArgumentsEnumerable(classInformation, args, testInformation, testBuilderContextAccessor)
            .ToDictionary(p => p.PropertyInformation.Name, p => p.Args().ElementAtOrDefault(0));
    }

    public static IEnumerable<(SourceGeneratedPropertyInformation PropertyInformation, Func<object?[]> Args)> 
        GetPropertyArgumentsEnumerable(
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

            var args = DataGeneratorHandler.GetArgumentsFromDataAttribute(dataAttribute, context).ToArray();

            yield return (propertyInformation, args[0]);
        }
    }
}