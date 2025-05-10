using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions;
using Polyfills;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Services;

[SuppressMessage("Trimming", "IL2026")]
[SuppressMessage("Trimming", "IL2070")]
[SuppressMessage("Trimming", "IL2067")]
[SuppressMessage("Trimming", "IL2071")]
[SuppressMessage("Trimming", "IL2072")]
[SuppressMessage("Trimming", "IL2075")]
[SuppressMessage("AOT", "IL3050")]
internal class ReflectionTestsConstructor(IExtension extension, 
    DependencyCollector dependencyCollector, 
    IServiceProvider serviceProvider) : BaseTestsConstructor(extension, dependencyCollector, serviceProvider)
{
    protected override DiscoveredTest[] DiscoverTests()
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new InvalidOperationException("Reflection tests are not supported with AOT or trimming enabled.");
        }
#endif
        var allTypes = ReflectionScanner.GetTypes();
        
        var testMethods = allTypes
            .SelectMany(x => x.GetMethods())
            .Where(IsTest)
            .Where(x => !x.IsAbstract)
            .ToArray();
        
        return Build(testMethods, allTypes)
            .SelectMany(ConstructTests)
            .ToArray();
    }

    private static IEnumerable<DynamicTest> Build(MethodInfo[] testMethods, IReadOnlyCollection<Type> allTypes)
    {
        var testsBuilderDynamicTests = new List<DynamicTest>();
        
        foreach (var testMethod in testMethods)
        {
            var testAttribute = testMethod.GetCustomAttribute<TestAttribute>()!;

            var types = GetDerivedTypes(allTypes, testMethod.DeclaringType!);

            foreach (var type in types)
            {
                try
                {
                    foreach (var typeDataAttribute in GetDataAttributes(type))
                    {
                        var testInformation = SourceModelHelpers.BuildTestMethod(type, testMethod, [], testMethod.Name);
                            
                        foreach (var testDataAttribute in GetDataAttributes(testMethod))
                        {
                            var testBuilderContextAccessor = new TestBuilderContextAccessor(new TestBuilderContext());
                            
                            foreach (var classInstanceArguments in GetArguments(type, testMethod, null, typeDataAttribute, DataGeneratorType.ClassParameters, () => [], testInformation, testBuilderContextAccessor))
                            {
                                BuildTests(type, classInstanceArguments, testMethod, testDataAttribute, testsBuilderDynamicTests, testAttribute, testBuilderContextAccessor);

                                testBuilderContextAccessor.Current = new TestBuilderContext();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    testsBuilderDynamicTests.Add(new UntypedFailedDynamicTest
                    {
                        MethodName = testMethod.Name,
                        TestFilePath = testAttribute.File,
                        TestLineNumber = testAttribute.Line,
                        Exception = e,
                        TestClassType = type,
                    });
                }
            }
        }
        return testsBuilderDynamicTests;
    }

    private static void BuildTests(Type type, Func<object?[]> classInstanceArguments, MethodInfo testMethod, IDataAttribute testDataAttribute,
        List<DynamicTest> testsBuilderDynamicTests, TestAttribute testAttribute, TestBuilderContextAccessor testBuilderContextAccessor)
    {
        try
        {
            var testInformation = SourceModelHelpers.BuildTestMethod(type, testMethod, [], testMethod.Name);
            
            foreach (var testArguments in GetArguments(type, testMethod, null, testDataAttribute, DataGeneratorType.TestParameters, classInstanceArguments, testInformation, testBuilderContextAccessor))
            {
                var propertyArgs = GetPropertyArgs(type, classInstanceArguments, testInformation, testBuilderContextAccessor)
                    .ToDictionary(p => p.PropertyInfo.Name, p => p.Args().ElementAtOrDefault(0));

                testsBuilderDynamicTests.Add(new UntypedDynamicTest(type, testMethod)
                {
                    TestBuilderContext = testBuilderContextAccessor.Current,
                    TestMethodArguments = testArguments(),
                    Attributes =
                    [
                        ..testMethod.GetCustomAttributes(),
                        ..type.GetCustomAttributes(),
                        ..type.Assembly.GetCustomAttributes()
                    ],
                    TestName = testMethod.Name,
                    TestClassArguments = classInstanceArguments(),
                    TestFilePath = testAttribute.File,
                    TestLineNumber = testAttribute.Line,
                    Properties = propertyArgs
                });
            }
        }
        catch (Exception e)
        {
            testsBuilderDynamicTests.Add(new UntypedFailedDynamicTest
            {
                MethodName = testMethod.Name,
                TestFilePath = testAttribute.File,
                TestLineNumber = testAttribute.Line,
                Exception = e,
                TestClassType = type,
            });
        }
    }

    private static IEnumerable<(PropertyInfo PropertyInfo, Func<object?[]> Args)> GetPropertyArgs(Type type, Func<object?[]> classInstanceArguments,
        SourceGeneratedMethodInformation testInformation, TestBuilderContextAccessor testBuilderContextAccessor)
    {
        var properties = type.GetProperties()
            .Where(p => p.GetCustomAttributes().OfType<IDataAttribute>().Any())
            .ToArray();

        foreach (var propertyInfo in properties)
        {
            var dataAttributes = GetDataAttributes(propertyInfo)[0];
            var args = GetArguments(type, null, propertyInfo, dataAttributes, DataGeneratorType.Property, classInstanceArguments, testInformation, testBuilderContextAccessor).ToArray();
            yield return (propertyInfo, args[0]);
        }
    }

    private static object? CreateInstance(IDataAttribute typeDataAttribute, Type type, object?[] classInstanceArguments, SourceGeneratedMethodInformation testInformation,
        TestBuilderContextAccessor testBuilderContextAccessor,
        out Exception? exception)
    {
        exception = null;
        
        try
        {
            if (typeDataAttribute is ClassConstructorAttribute classConstructorAttribute)
            {
                return CreateByClassConstructor(type, classConstructorAttribute);
            }

            var args = classInstanceArguments.Select((x, i) => CastHelper.Cast(testInformation.Class.Parameters[i].Type, x)).ToArray();
            
            var propertyArgs = GetPropertyArgs(type, () => args, testInformation, testBuilderContextAccessor)
                .ToDictionary(p => p.PropertyInfo.Name, p => p.Args().ElementAtOrDefault(0));

            return InstanceHelper.CreateInstance(type, args, propertyArgs);
        }
        catch (Exception e)
        {
            exception = e;
            return null;
        }
    }

    private static object CreateByClassConstructor(Type type, ClassConstructorAttribute classConstructorAttribute)
    {
        var classConstructor = (IClassConstructor)Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!;

        var metadata = new ClassConstructorMetadata
        {
            TestBuilderContext = new TestBuilderContext(),
            TestSessionId = string.Empty,
        };
        
        var createMethod = typeof(IClassConstructor).GetMethod(nameof(IClassConstructor.Create))!.MakeGenericMethod(type);

        var instance = createMethod.Invoke(classConstructor, [metadata]);
        
        return instance!;
    }

    private static IEnumerable<Func<object?[]>> GetArguments([DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type, MethodInfo? method, PropertyInfo? propertyInfo, IDataAttribute testDataAttribute, DataGeneratorType dataGeneratorType, Func<object?[]> classInstanceArguments,
        SourceGeneratedMethodInformation testInformation,
        TestBuilderContextAccessor testBuilderContextAccessor)
    {
        if (testDataAttribute is IDataSourceGeneratorAttribute dataSourceGeneratorAttribute)
        {
            var memberAttributes = dataGeneratorType switch
            {
                DataGeneratorType.TestParameters => method!.GetParameters().SelectMany(x => x.GetCustomAttributes()),
                DataGeneratorType.Property => [],
                _ => type.GetConstructors().FirstOrDefault(x => !x.IsStatic)?.GetParameters().SelectMany(x => x.GetCustomAttributes()) ?? []
            };
            
            var needsInstance = memberAttributes.Any(x => x is IAccessesInstanceData);

            var invoke = dataSourceGeneratorAttribute.GetType().GetMethod("GenerateDataSources")!.Invoke(testDataAttribute, [
                new DataGeneratorMetadata
                {
                    Type = dataGeneratorType,
                    TestInformation = testInformation,
                    ClassInstanceArguments = needsInstance ? classInstanceArguments() : null,
                    MembersToGenerate = dataGeneratorType switch
                    {
                        DataGeneratorType.TestParameters => method!.GetParameters()
                            .Select(x => new SourceGeneratedParameterInformation(x.ParameterType)
                            {
                                Name = x.Name!,
                                Attributes = x.GetCustomAttributes().ToArray(),
                            }).ToArray<SourceGeneratedMemberInformation>(),
                        DataGeneratorType.ClassParameters => type.GetConstructors().FirstOrDefault(x => !x.IsStatic)?
                            .GetParameters()
                            .Select(x => new SourceGeneratedParameterInformation(x.ParameterType)
                            {
                                Name = x.Name!,
                                Attributes = x.GetCustomAttributes().ToArray(),
                            }).ToArray<SourceGeneratedMemberInformation>() ?? [],
                        DataGeneratorType.Property =>
                        [
                            new SourceGeneratedPropertyInformation
                            {
                                Name = propertyInfo!.Name,
                                Attributes = propertyInfo.GetCustomAttributes().ToArray(),
                                Type = propertyInfo.PropertyType,
                                IsStatic = propertyInfo.GetMethod?.IsStatic ?? false
                            }
                        ],
                        _ => throw new ArgumentOutOfRangeException(nameof(dataGeneratorType), dataGeneratorType, null)
                    },
                    TestBuilderContext = new TestBuilderContextAccessor(testBuilderContextAccessor.Current),
                    TestClassInstance = needsInstance ? CreateInstance(testDataAttribute, type, classInstanceArguments(), testInformation, testBuilderContextAccessor, out _) : null,
                    TestSessionId = string.Empty,
                }
            ]) as IEnumerable;

            var funcEnumerable = invoke?.Cast<object>().ToArray() ?? []!;
            
            if (funcEnumerable.Length == 0)
            {
                yield return () => [];
                yield break;
            }
            
            foreach (var func in funcEnumerable)
            {
                yield return () =>
                {
                    var funcResult = FuncHelper.InvokeFunc(func);

                    if (TupleHelper.TryParseTupleToObjectArray(funcResult, out var objectArray2))
                    {
                        return objectArray2;
                    }

                    if (funcResult is object?[] objectArray)
                    {
                        return objectArray;
                    }

                    return [funcResult];
                };
            }
        }
        else if (testDataAttribute is ArgumentsAttribute argumentsAttribute)
        {
            yield return () => argumentsAttribute.Values;
        }
        else if (testDataAttribute is InstanceMethodDataSourceAttribute instanceMethodDataSourceAttribute)
        {
            var instance = CreateInstance(testDataAttribute, type, classInstanceArguments(), testInformation, testBuilderContextAccessor, out var exception);
            
            var methodDataSourceType = instanceMethodDataSourceAttribute.ClassProvidingDataSource ?? type;

            var result = methodDataSourceType.GetMethod(instanceMethodDataSourceAttribute.MethodNameProvidingDataSource)?.Invoke(instance, []);

            var enumerableResult = result is IEnumerable enumerable
                ? enumerable.Cast<object?>().ToArray()
                : [result];

            foreach (var methodResult in enumerableResult)
            {
                yield return () =>
                {
                    if (FuncHelper.TryInvokeFunc(methodResult, out var funcResult))
                    {
                        if (TupleHelper.TryParseTupleToObjectArray(funcResult, out var funcObjectArray))
                        {
                            return funcObjectArray;
                        }

                        return funcResult as object?[] ?? [funcResult];
                    }

                    if (TupleHelper.TryParseTupleToObjectArray(methodResult, out var objectArray))
                    {
                        return objectArray;
                    }

                    return [methodResult];
                };
            }
        }
        else if (testDataAttribute is MethodDataSourceAttribute methodDataSourceAttribute)
        {
            var methodDataSourceType = methodDataSourceAttribute.ClassProvidingDataSource ?? type;
            
            var result = methodDataSourceType.GetMethod(methodDataSourceAttribute.MethodNameProvidingDataSource)?.Invoke(null, []) ?? Array.Empty<object>();

            var enumerableResult = result is IEnumerable enumerable
                ? enumerable.Cast<object?>().ToArray()
                : [result];

            foreach (var methodResult in enumerableResult)
            {
                yield return () =>
                {
                    if (FuncHelper.TryInvokeFunc(methodResult, out var funcResult))
                    {
                        if (TupleHelper.TryParseTupleToObjectArray(funcResult, out var funcObjectArray))
                        {
                            return funcObjectArray;
                        }

                        return funcResult as object?[] ?? [funcResult];
                    }

                    if (TupleHelper.TryParseTupleToObjectArray(methodResult, out var objectArray))
                    {
                        return objectArray;
                    }

                    return [methodResult];
                };
            }
        }
        else if (testDataAttribute is NoOpDataAttribute or ClassConstructorAttribute)
        {
            yield return () => [];
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(testDataAttribute));
        }
    }

    private static IDataAttribute[] GetDataAttributes(MemberInfo memberInfo)
    {
        var dataAttributes = memberInfo.GetCustomAttributes()
            .OfType<IDataAttribute>()
            .ToArray();

        if (dataAttributes.Length == 0)
        {
            return NoOpDataAttribute.Array;
        }
        
        return dataAttributes;
    }

    private static Type[] GetDerivedTypes(IEnumerable<Type> allTypes, Type baseType)
    {
        return allTypes
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(baseType))
            .ToArray();
    }
    
    private bool IsTest(MethodInfo arg)
    {
        try
        {
            return arg.GetCustomAttributes()
                .OfType<TestAttribute>()
                .Any();
        }
        catch
        {
            return false;
        }
    }
}