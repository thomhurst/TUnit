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

    private static IEnumerable<DynamicTest> Build(MethodInfo[] testMethods, Type[] allTypes)
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
                        foreach (var testDataAttribute in GetDataAttributes(testMethod))
                        {
                            foreach (var classInstanceArguments in GetArguments(type, testMethod, typeDataAttribute, DataGeneratorType.ClassParameters, null, null))
                            {
                                BuildTests(typeDataAttribute, type, classInstanceArguments, testMethod, testDataAttribute, testsBuilderDynamicTests, testAttribute);
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

    private static void BuildTests(IDataAttribute typeDataAttribute, Type type, object?[] classInstanceArguments, MethodInfo testMethod, IDataAttribute testDataAttribute,
        List<DynamicTest> testsBuilderDynamicTests, TestAttribute testAttribute)
    {
        try
        {
            var instance = CreateInstance(typeDataAttribute, type, classInstanceArguments, out var exception);

            foreach (var testArguments in GetArguments(type, testMethod, testDataAttribute, DataGeneratorType.TestParameters, instance, classInstanceArguments))
            {
                testsBuilderDynamicTests.Add(new UntypedDynamicTest(type, testMethod)
                {
                    TestMethodArguments = testArguments,
                    Attributes =
                    [
                        ..testMethod.GetCustomAttributes(),
                        ..type.GetCustomAttributes(),
                        ..type.Assembly.GetCustomAttributes()
                    ],
                    TestName = testMethod.Name,
                    TestClassArguments = classInstanceArguments,
                    TestFilePath = testAttribute.File,
                    TestLineNumber = testAttribute.Line,
                    Exception = exception,
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

    private static object? CreateInstance(IDataAttribute typeDataAttribute, Type type, object?[] classInstanceArguments, out Exception? exception)
    {
        exception = null;
        
        try
        {
            if (typeDataAttribute is ClassConstructorAttribute classConstructorAttribute)
            {
                return CreateByClassConstructor(type, classConstructorAttribute);
            }
        
            return Activator.CreateInstance(type, classInstanceArguments);
        }
        catch (TargetInvocationException targetInvocationException)
        {
            if (targetInvocationException.InnerException != null)
            {
                exception = targetInvocationException.InnerException;
                return null;
            }

            exception = targetInvocationException;
            return null;
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

    private static IEnumerable<object?[]> 
        GetArguments([DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type, MethodInfo method, IDataAttribute testDataAttribute, DataGeneratorType dataGeneratorType, object? instance, object?[]? classInstanceArguments)
    {
        if (testDataAttribute is IDataSourceGeneratorAttribute dataSourceGeneratorAttribute)
        {
            var parameters = dataGeneratorType == DataGeneratorType.TestParameters
                ? method.GetParameters()
                : type.GetConstructors().FirstOrDefault(x => !x.IsStatic)?.GetParameters() ?? [];
            
            var invoke = dataSourceGeneratorAttribute.GetType().GetMethod("GenerateDataSources")!.Invoke(testDataAttribute, [
                new DataGeneratorMetadata
                {
                    Type = dataGeneratorType,
                    TestInformation = SourceModelHelpers.BuildTestMethod(type, method, [], method.Name), // TODO
                    ClassInstanceArguments = classInstanceArguments,
                    MembersToGenerate = parameters.Select(x => new SourceGeneratedParameterInformation(x.ParameterType)
                    {
                        Name = x.Name!,
                        Attributes = x.GetCustomAttributes().ToArray(),
                    }).ToArray<SourceGeneratedMemberInformation>(),
                    TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),
                    TestClassInstance = instance,
                    TestSessionId = string.Empty,
                }
            ]) as IEnumerable;

            var funcEnumerable = invoke?.Cast<object>().ToArray() ?? []!;

            if (funcEnumerable.Length == 0)
            {
                yield return [];
                yield break;
            }
            
            foreach (var func in funcEnumerable)
            {
                var funcResult = func.GetType().GetMethod("Invoke")!.Invoke(func, []);
                
                if (TupleHelper.TryParseTupleToObjectArray(funcResult, out var objectArray2))
                {
                    yield return objectArray2;
                    yield break;
                }

                yield return funcResult as object?[] ?? [funcResult];
            }
        }
        else if (testDataAttribute is ArgumentsAttribute argumentsAttribute)
        {
            yield return argumentsAttribute.Values;
        }
        else if (testDataAttribute is InstanceMethodDataSourceAttribute instanceMethodDataSourceAttribute)
        {
            var methodDataSourceType = instanceMethodDataSourceAttribute.ClassProvidingDataSource 
                ?? instance?.GetType()
                ?? type;

            var result = methodDataSourceType.GetMethod(instanceMethodDataSourceAttribute.MethodNameProvidingDataSource)?.Invoke(instance, []);

            var enumerableResult = result is IEnumerable enumerable
                ? enumerable.Cast<object?>().ToArray()
                : [result];

            foreach (var methodResult in enumerableResult)
            {
                if(FuncHelper.TryInvokeFunc(methodResult, out var funcResult))
                {
                    if (TupleHelper.TryParseTupleToObjectArray(funcResult, out var funcObjectArray))
                    {
                        yield return funcObjectArray;
                        yield break;
                    }

                    yield return funcResult as object?[] ?? [funcResult];
                }
                
                if (TupleHelper.TryParseTupleToObjectArray(methodResult, out var objectArray))
                {
                    yield return objectArray;
                    yield break;
                }

                yield return
                [
                    methodResult
                ];
            }
        }
        else if (testDataAttribute is MethodDataSourceAttribute methodDataSourceAttribute)
        {
            var methodDataSourceType = methodDataSourceAttribute.ClassProvidingDataSource 
                ?? instance?.GetType()
                ?? type;
            
            var result = methodDataSourceType.GetMethod(methodDataSourceAttribute.MethodNameProvidingDataSource)?.Invoke(instance, []) ?? Array.Empty<object>();

            var enumerableResult = result is IEnumerable enumerable
                ? enumerable.Cast<object?>().ToArray()
                : [result];

            foreach (var methodResult in enumerableResult)
            {
                if (FuncHelper.TryInvokeFunc(methodResult, out var funcResult))
                {
                    if (TupleHelper.TryParseTupleToObjectArray(funcResult, out var funcObjectArray))
                    {
                        yield return funcObjectArray;
                        yield break;
                    }

                    yield return funcResult as object?[] ?? [funcResult];
                }
                
                if (TupleHelper.TryParseTupleToObjectArray(methodResult, out var objectArray))
                {
                    yield return objectArray;
                    yield break;
                }

                yield return
                [
                    methodResult
                ];
            }
        }
        else if (testDataAttribute is NoOpDataAttribute or ClassConstructorAttribute)
        {
            yield return [];
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

    private static Type[] GetDerivedTypes(Type[] allTypes, Type baseType)
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