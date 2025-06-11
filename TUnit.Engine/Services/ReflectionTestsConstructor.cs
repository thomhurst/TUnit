using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions;
using Polyfills;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Engine.Helpers;
#pragma warning disable TUnitWIP0001

namespace TUnit.Engine.Services;

[UnconditionalSuppressMessage("Trimming",
    "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming",
    "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming",
    "IL2067:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.")]
[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
[UnconditionalSuppressMessage("Trimming",
    "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
[UnconditionalSuppressMessage("Trimming",
    "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming",
    "IL2055:Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined.")]
[UnconditionalSuppressMessage("Trimming",
    "IL2060:MakeGenericMethod call does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2111:Reflection")]
internal class ReflectionTestsConstructor(
    IExtension extension,
    DependencyCollector dependencyCollector,
    ContextManager contextManager,
    ReflectionDataInitializer reflectionDataInitializer,
    IServiceProvider serviceProvider) : BaseTestsConstructor(extension, dependencyCollector, contextManager, serviceProvider)
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

        return DiscoverTestsInternal(allTypes)
            .Concat(DiscoverDynamicTests(allTypes))
            .SelectMany(ConstructTests)
            .ToArray();
    }

    private IEnumerable<DynamicTest> DiscoverTestsInternal(HashSet<Type> allTypes)
    {
        foreach (var type in allTypes.Where(x => x is { IsClass: true, IsAbstract: false }))
        {
            var testMethods = type.GetMethods()
                .Where(x => !x.IsAbstract && IsTest(x))
                .ToArray();

            foreach (var dynamicTest in Build(type, testMethods))
            {
                yield return dynamicTest;
            }
        }
    }

    private IEnumerable<DynamicTest> DiscoverDynamicTests(HashSet<Type> allTypes)
    {
        foreach (var type in allTypes)
        {
            foreach (var methodInfo in type.GetMethods())
            {
                if (methodInfo.GetCustomAttributes<DynamicTestBuilderAttribute>().FirstOrDefault() is not { } dynamicTestBuilderAttribute)
                {
                    continue;
                }

                var context = new DynamicTestBuilderContext(dynamicTestBuilderAttribute.File, dynamicTestBuilderAttribute.Line);

                var instance = Activator.CreateInstance(type)!;

                methodInfo.Invoke(instance, [context]);

                foreach (var contextTest in context.Tests)
                {
                    yield return contextTest;
                }
            }
        }
    }

    private IEnumerable<DynamicTest> Build(Type type, MethodInfo[] testMethods)
    {
        var testsBuilderDynamicTests = new List<DynamicTest>();

        foreach (var testMethod in testMethods)
        {
            var testAttribute = testMethod.GetCustomAttribute<TestAttribute>()!;

            try
            {
                foreach (var typeDataAttribute in GetDataAttributes(type))
                {
                    var testInformation = ReflectionToSourceModelHelpers.BuildTestMethod(type, testMethod, testMethod.Name);

                    foreach (var testDataAttribute in GetDataAttributes(testMethod))
                    {
                        var testBuilderContextAccessor = new TestBuilderContextAccessor(new TestBuilderContext());

                        foreach (var classInstanceArguments in GetArguments(type, testMethod, null, typeDataAttribute, DataGeneratorType.ClassParameters, () => [],
                                     testInformation, testBuilderContextAccessor))
                        {
                            BuildTests(testInformation, classInstanceArguments, typeDataAttribute, testDataAttribute, testsBuilderDynamicTests, testBuilderContextAccessor);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                testsBuilderDynamicTests.Add(new UntypedFailedDynamicTest(testMethod)
                {
                    MethodName = testMethod.Name,
                    TestFilePath = testAttribute.File,
                    TestLineNumber = testAttribute.Line,
                    Exception = e,
                    TestClassType = type,
                });
            }
        }
        return testsBuilderDynamicTests;
    }

    private void BuildTests(SourceGeneratedMethodInformation testInformation, Func<object?[]> classInstanceArguments, IDataAttribute typeDataAttribute,
        IDataAttribute testDataAttribute,
        List<DynamicTest> testsBuilderDynamicTests, TestBuilderContextAccessor testBuilderContextAccessor)
    {
        var testMethod = testInformation.ReflectionInformation;

        var type = testInformation.Class.Type;

        var testAttribute = testInformation.Attributes.OfType<BaseTestAttribute>().First();

        try
        {
            Attribute[] allAttributes =
            [
                ..testInformation.Attributes,
                ..testInformation.Class.Attributes,
                ..testInformation.Class.Assembly.Attributes
            ];

            foreach (var attribute in allAttributes.Where(x => x is IDataSourceGeneratorAttribute)
                         .Where(a => !DotNetAssemblyHelper.IsInDotNetCoreLibrary(a.GetType())))
            {
                CreateNestedDataGenerators(attribute, testInformation, testBuilderContextAccessor, []);
            }

            var repeatCount = allAttributes.OfType<RepeatAttribute>().FirstOrDefault()?.Times ?? 0;

            for (var index = 0; index < repeatCount + 1; index++)
            {
                var invokedClassInstanceArguments = classInstanceArguments();

                foreach (var testArguments in GetArguments(type, testMethod, null, testDataAttribute, DataGeneratorType.TestParameters, () => invokedClassInstanceArguments,
                             testInformation,
                             testBuilderContextAccessor))
                {
                    var propertyArgs = GetPropertyArgs(type, invokedClassInstanceArguments, testInformation, testBuilderContextAccessor)
                        .ToDictionary(p => p.PropertyInfo, p => p.Args().ElementAtOrDefault(0));

                    if (typeDataAttribute is not ClassConstructorAttribute)
                    {
                        MapImplicitParameters(ref invokedClassInstanceArguments, testInformation.Class.Parameters);
                    }

                    var testMethodArguments = testArguments();

                    MapImplicitParameters(ref testMethodArguments, testInformation.Parameters);

                    if (type.ContainsGenericParameters)
                    {
                        var classParametersTypes = testInformation.Class.Parameters.Select(p => p.Type).ToList();

                        var substitutedTypes = type.GetGenericArguments()
                            .Select(pc => classParametersTypes.FindIndex(pt => pt == pc))
                            .Select(i => invokedClassInstanceArguments[i]!.GetType())
                            .ToArray();

                        type = type.MakeGenericType(substitutedTypes);

                        testMethod = type.GetMembers()
                            .OfType<MethodInfo>()
                            .First(x => x.Name == testMethod.Name
                                && x.GetParameters().Length == testMethod.GetParameters().Length);
                    }

                    if (testMethod.ContainsGenericParameters)
                    {
                        testMethod = GetRuntimeMethod(testMethod, testMethodArguments);
                    }

                    testsBuilderDynamicTests.Add(new UntypedDynamicTest(type, testMethod)
                    {
                        TestBuilderContext = testBuilderContextAccessor.Current,
                        TestMethodArguments = testMethodArguments,
                        Attributes = allAttributes,
                        TestName = testMethod.Name,
                        TestClassArguments = invokedClassInstanceArguments ??= classInstanceArguments(),
                        TestFilePath = testAttribute.File,
                        TestLineNumber = testAttribute.Line,
                        Properties = propertyArgs.ToDictionary(x => x.Key.Name, x => x.Value),
                    });

                    testBuilderContextAccessor.Current.Events.OnInitialize += async (_, context) =>
                    {
                        await reflectionDataInitializer.Initialize(context);
                    };

                    foreach (var (_, value) in propertyArgs.Where(x => x.Value is IAsyncInitializer && x.Key.GetMethod is { IsStatic: true }))
                    {
                        reflectionDataInitializer.RegisterForInitialize(value);
                    }

                    testBuilderContextAccessor.Current = new TestBuilderContext();

                    invokedClassInstanceArguments = classInstanceArguments();
                }
            }
        }
        catch (Exception e)
        {
            testsBuilderDynamicTests.Add(new UntypedFailedDynamicTest(testMethod)
            {
                MethodName = testMethod.Name,
                TestFilePath = testAttribute.File,
                TestLineNumber = testAttribute.Line,
                Exception = e,
                TestClassType = type,
            });

            testBuilderContextAccessor.Current = new TestBuilderContext();
        }
    }

    private static void CreateNestedDataGenerators(object obj, SourceGeneratedMethodInformation methodInformation, TestBuilderContextAccessor testBuilderContextAccessor,
        HashSet<object> visited)
    {
        if (!visited.Add(obj))
        {
            return;
        }

        var dependencyChain = new NestedDependency
        {
            Instance = obj,
            Property = null,
            Parent = null,
            Type = obj.GetType(),
            TestBuilderContextAccessor = testBuilderContextAccessor,
            DataGeneratorMetadata = new DataGeneratorMetadata
            {
                Type = DataGeneratorType.Property,
                TestInformation = methodInformation,
                ClassInstanceArguments = [],
                MembersToGenerate = [],
                TestBuilderContext = testBuilderContextAccessor,
                TestClassInstance = null,
                TestSessionId = string.Empty,
            },
        };
        dependencyChain.Children.AddRange(obj.GetType()
            .GetProperties()
            .Where(x => x.HasAttribute<IDataAttribute>())
            .Select(x => CreateDependencyChain(x, dependencyChain, methodInformation, testBuilderContextAccessor))
            .ToList());

        foreach (var nestedDependency in GetMostNestedDependencies(dependencyChain))
        {
            NestedDependency? lastProcessedDependency = null;
            var dependency = nestedDependency;
            while (dependency != null)
            {
                dependency.CreateInstance();

                lastProcessedDependency?.AddToParent();
                lastProcessedDependency = dependency;

                dependency = dependency.Parent;
            }
        }
    }

    private static IEnumerable<NestedDependency> GetMostNestedDependencies(NestedDependency dependency)
    {
        if (dependency.Children.Count == 0)
        {
            yield return dependency;
            yield break;
        }

        foreach (var nestedDependency in dependency.Children.SelectMany(GetMostNestedDependencies))
        {
            yield return nestedDependency;
        }
    }

    private static NestedDependency CreateDependencyChain(PropertyInfo property, NestedDependency parent, SourceGeneratedMethodInformation methodInformation,
        TestBuilderContextAccessor testBuilderContextAccessor)
    {
        var nestedDependency = new NestedDependency
        {
            Property = property,
            Parent = parent,
            Type = property.PropertyType,
            TestBuilderContextAccessor = testBuilderContextAccessor,
            DataGeneratorMetadata = new DataGeneratorMetadata
            {
                Type = DataGeneratorType.Property,
                TestInformation = methodInformation,
                ClassInstanceArguments = [],
                MembersToGenerate = [],
                TestBuilderContext = testBuilderContextAccessor,
                TestClassInstance = null,
                TestSessionId = string.Empty,
            }
        };

        nestedDependency.Children.AddRange(property.PropertyType
            .GetProperties()
            .Where(x => x.HasAttribute<IDataAttribute>())
            .Select(x => CreateDependencyChain(x, nestedDependency, methodInformation, testBuilderContextAccessor))
            .ToList());

        return nestedDependency;
    }

    private static MethodInfo GetRuntimeMethod(MethodInfo methodInfo, object?[] arguments)
    {
        if (!methodInfo.IsGenericMethodDefinition)
        {
            return methodInfo;
        }

        var typeArguments = methodInfo.GetGenericArguments();
        var parameters = methodInfo.GetParameters();
        var argumentsTypes = arguments.Select(x => x?.GetType()).ToArray();

        var typeParameterMap = new Dictionary<Type, Type>();

        for (var i = 0; i < parameters.Length && i < argumentsTypes.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            var argumentType = argumentsTypes[i];

            if (argumentType != null)
            {
                MapTypeParameters(parameterType, argumentType, typeParameterMap);
            }
        }

        List<Type> substituteTypes = [];
        foreach (var typeArgument in typeArguments)
        {
            if (typeParameterMap.TryGetValue(typeArgument, out var mappedType))
            {
                substituteTypes.Add(mappedType);
            }
            else
            {
                var parameterIndex = -1;
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType == typeArgument ||
                        (parameters[i].ParameterType.IsGenericType &&
                            parameters[i].ParameterType.GetGenericArguments().Contains(typeArgument)))
                    {
                        parameterIndex = i;
                        break;
                    }
                }

                if (parameterIndex >= 0 && parameterIndex < argumentsTypes.Length && argumentsTypes[parameterIndex] != null)
                {
                    var inferredType = argumentsTypes[parameterIndex]!;

                    if (parameters[parameterIndex].ParameterType.IsGenericType &&
                        parameters[parameterIndex].ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        inferredType = Nullable.GetUnderlyingType(inferredType) ?? inferredType;
                    }

                    substituteTypes.Add(inferredType);
                }
                else
                {
                    throw new InvalidOperationException($"Cannot infer type for generic parameter '{typeArgument.Name}'. No matching argument found.");
                }
            }
        }

        return methodInfo.MakeGenericMethod(substituteTypes.ToArray());
    }

    private static void MapTypeParameters(Type parameterType, Type argumentType, Dictionary<Type, Type> typeParameterMap)
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
        else if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingParameterType = parameterType.GetGenericArguments()[0];
            var underlyingArgumentType = Nullable.GetUnderlyingType(argumentType) ?? argumentType;
            MapTypeParameters(underlyingParameterType, underlyingArgumentType, typeParameterMap);
        }
    }

    private static void MapImplicitParameters(ref object?[] arguments, SourceGeneratedParameterInformation[] parameters)
    {
        if (arguments.Length == parameters.Length)
        {
            if (parameters.Length > 0 && parameters.Last().IsParams
                && arguments.Length > 0 && arguments.Last() is not IEnumerable)
            {
                var lastParameter = parameters.Last();

                var underlyingType = lastParameter.Type.GetElementType()
                    ?? lastParameter.Type.GenericTypeArguments.FirstOrDefault()
                    ?? throw new InvalidOperationException("Cannot determine the underlying type of the params argument. Use an array to fix this.");

                var typedArray = Array.CreateInstance(underlyingType, 1);

                var value = CastHelper.Cast(underlyingType, arguments.Last());
                typedArray.SetValue(value, 0);

                arguments =
                [
                    ..arguments.Take(arguments.Length - 1),
                    typedArray
                ];
            }

            return;
        }

        if (parameters.Length == 0)
        {
            arguments = [];
            return;
        }

        if (arguments.Length < parameters.Length)
        {
            var missingParameters = parameters.Skip(arguments.Length).ToArray();

            if (missingParameters.All(x => x.IsOptional))
            {
                arguments =
                [
                    ..arguments,
                    ..missingParameters.Select(x => x.DefaultValue)
                ];
                return;
            }

            if (parameters.LastOrDefault()?.Type == typeof(CancellationToken)
                && arguments.LastOrDefault() is not CancellationToken)
            {
                return;
            }

            throw new InvalidOperationException($"Not enough arguments provided to fulfil the parameters. Expected {parameters.Length}, but got {arguments.Length}.");
        }

        if (arguments.Length > parameters.Length)
        {
            var lastParameter = parameters.Last();

            if (lastParameter.IsParams)
            {
                var underlyingType = lastParameter.Type.GetElementType()
                    ?? lastParameter.Type.GenericTypeArguments.FirstOrDefault()
                    ?? throw new InvalidOperationException("Cannot determine the underlying type of the params argument. Use an array to fix this.");

                var argumentsBeforeParams = arguments.Take(parameters.Length - 1).ToArray();
                var argumentsAfterParams = arguments.Skip(argumentsBeforeParams.Length).ToArray();

                if (argumentsAfterParams.All(x => x is null || IsConvertibleTo(x, underlyingType)))
                {
                    var typedArray = Array.CreateInstance(underlyingType, argumentsAfterParams.Length);

                    for (var i = 0; i < argumentsAfterParams.Length; i++)
                    {
                        typedArray.SetValue(CastHelper.Cast(underlyingType, argumentsAfterParams[i]), i);
                    }

                    arguments =
                    [
                        ..argumentsBeforeParams,
                        typedArray
                    ];
                    return;
                }

            }

            arguments = arguments.Take(parameters.Length).ToArray();
        }
    }

    private static bool IsConvertibleTo(object x, Type underlyingType)
    {
        if (x.GetType().IsAssignableTo(underlyingType))
        {
            return true;
        }

        if (CastHelper.GetConversionMethod(x.GetType(), underlyingType) is not null)
        {
            return true;
        }

        try
        {
            _ = Convert.ChangeType(x, underlyingType);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<(PropertyInfo PropertyInfo, Func<object?[]> Args)> GetPropertyArgs(Type type, object?[] classInstanceArguments,
        SourceGeneratedMethodInformation testInformation, TestBuilderContextAccessor testBuilderContextAccessor)
    {
        var properties = type.GetProperties()
            .Where(p => p.GetCustomAttributes().OfType<IDataAttribute>().Any())
            .ToArray();

        foreach (var propertyInfo in properties)
        {
            var dataAttribute = GetDataAttributes(propertyInfo).ElementAtOrDefault(0);

            if (dataAttribute is null)
            {
                continue;
            }

            var args = GetArguments(type, null, propertyInfo, dataAttribute, DataGeneratorType.Property, () => classInstanceArguments, testInformation,
                testBuilderContextAccessor).ToArray();

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

            var propertyArgs = GetPropertyArgs(type, args, testInformation, testBuilderContextAccessor)
                .ToDictionary(p => p.PropertyInfo.Name, p => p.Args().ElementAtOrDefault(0));

            return InstanceHelper.CreateInstance(testInformation.Class, args, propertyArgs, testBuilderContextAccessor.Current);
        }
        catch (Exception e)
        {
            exception = e;
            return null;
        }
    }

    private static object CreateByClassConstructor(Type type, ClassConstructorAttribute classConstructorAttribute)
    {
        var classConstructor = (IClassConstructor) Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!;

        var metadata = new ClassConstructorMetadata
        {
            TestBuilderContext = new TestBuilderContext(), TestSessionId = string.Empty,
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
        var classInstanceArgumentsInvoked = dataGeneratorType != DataGeneratorType.ClassParameters ? classInstanceArguments() : null;

        if (testDataAttribute is IDataSourceGeneratorAttribute dataSourceGeneratorAttribute)
        {
            var memberAttributes = dataGeneratorType switch
            {
                DataGeneratorType.TestParameters => method!.GetParameters().SelectMany(x => x.GetCustomAttributes()),
                DataGeneratorType.Property => [],
                _ => type.GetConstructors().FirstOrDefault(x => !x.IsStatic)?.GetParameters().SelectMany(x => x.GetCustomAttributes()) ?? []
            };
            var needsInstance = memberAttributes.Any(x => x is IAccessesInstanceData);
            CreateNestedDataGenerators(testDataAttribute, testInformation, testBuilderContextAccessor, []);

            var invoke = dataSourceGeneratorAttribute.GetType().GetMethod("GenerateDataSources")!.Invoke(testDataAttribute, [
                CreateDataGeneratorMetadata(type, method, propertyInfo, testDataAttribute, dataGeneratorType, classInstanceArguments, testInformation, testBuilderContextAccessor,
                    classInstanceArgumentsInvoked, needsInstance)
            ]) as IEnumerable;

            var funcEnumerable = invoke?.Cast<object>() ?? [];

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
            var instance = CreateInstance(testDataAttribute, type, classInstanceArgumentsInvoked ?? classInstanceArguments(), testInformation, testBuilderContextAccessor, out _);

            var methodDataSourceType = instanceMethodDataSourceAttribute.ClassProvidingDataSource ?? type;

            var result = methodDataSourceType.GetMethod(
                    name: instanceMethodDataSourceAttribute.MethodNameProvidingDataSource,
                    types: instanceMethodDataSourceAttribute.Arguments.Select(x => x?.GetType() ?? typeof(object)).ToArray())
                ?.Invoke(instance, instanceMethodDataSourceAttribute.Arguments);

            var enumerableResult = result is not string and IEnumerable enumerable
                ? enumerable.Cast<object?>().ToArray()
                : [result];

            foreach (var methodResult in enumerableResult)
            {
                yield return () =>
                {
                    var parameterType = method?.GetParameters().ElementAtOrDefault(0)?.ParameterType;

                    if (methodResult?.GetType().IsAssignableTo(parameterType) is true)
                    {
                        return [methodResult];
                    }

                    if (FuncHelper.TryInvokeFunc(methodResult, out var funcResult))
                    {
                        if (funcResult?.GetType().IsAssignableTo(parameterType) is true)
                        {
                            return [funcResult];
                        }

                        if (dataGeneratorType == DataGeneratorType.Property)
                        {
                            return [funcResult];
                        }

                        if (TupleHelper.TryParseTupleToObjectArray(funcResult, out var funcObjectArray))
                        {
                            return funcObjectArray;
                        }

                        return funcResult as object?[] ?? [funcResult];
                    }

                    if (methodResult?.GetType().IsAssignableTo(parameterType) is true)
                    {
                        return [funcResult];
                    }

                    if (dataGeneratorType == DataGeneratorType.Property)
                    {
                        return [funcResult];
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

            var result = InvokeMethodDataSource(methodDataSourceType, methodDataSourceAttribute);

            var enumerableResult = result is not string and IEnumerable enumerable
                ? enumerable.Cast<object?>().ToArray()
                : [result];

            foreach (var methodResult in enumerableResult)
            {
                yield return () =>
                {
                    var parameterType = method?.GetParameters().ElementAtOrDefault(0)?.ParameterType;

                    if (methodResult?.GetType().IsAssignableTo(parameterType) is true)
                    {
                        return [methodResult];
                    }

                    if (FuncHelper.TryInvokeFunc(methodResult, out var funcResult))
                    {
                        if (funcResult?.GetType().IsAssignableTo(parameterType) is true)
                        {
                            return [funcResult];
                        }

                        if (dataGeneratorType == DataGeneratorType.Property)
                        {
                            return [funcResult];
                        }

                        if (TupleHelper.TryParseTupleToObjectArray(funcResult, out var funcObjectArray))
                        {
                            return funcObjectArray;
                        }

                        return funcResult as object?[] ?? [funcResult];
                    }

                    if (methodResult?.GetType().IsAssignableTo(parameterType) is true)
                    {
                        return [funcResult];
                    }

                    if (dataGeneratorType == DataGeneratorType.Property)
                    {
                        return [funcResult];
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

    private static DataGeneratorMetadata CreateDataGeneratorMetadata(Type type,
        MethodInfo? method,
        PropertyInfo? propertyInfo,
        IDataAttribute testDataAttribute,
        DataGeneratorType dataGeneratorType,
        Func<object?[]> classInstanceArguments,
        SourceGeneratedMethodInformation testInformation,
        TestBuilderContextAccessor testBuilderContextAccessor,
        object?[]? classInstanceArgumentsInvoked,
        bool needsInstance) =>
        new()
        {
            Type = dataGeneratorType,
            TestInformation = testInformation,
            ClassInstanceArguments = classInstanceArgumentsInvoked,
            MembersToGenerate = dataGeneratorType switch
            {
                DataGeneratorType.TestParameters => method!.GetParameters()
                    .Select(x => new SourceGeneratedParameterInformation(x.ParameterType)
                    {
                        Name = x.Name!, Attributes = x.GetCustomAttributes().ToArray(), ReflectionInfo = x,
                    }).ToArray<SourceGeneratedMemberInformation>(),
                DataGeneratorType.ClassParameters => type.GetConstructors().FirstOrDefault(x => !x.IsStatic)?
                    .GetParameters()
                    .Select(x => new SourceGeneratedParameterInformation(x.ParameterType)
                    {
                        Name = x.Name!, Attributes = x.GetCustomAttributes().ToArray(), ReflectionInfo = x,
                    }).ToArray<SourceGeneratedMemberInformation>() ?? [],
                DataGeneratorType.Property =>
                [
                    new SourceGeneratedPropertyInformation
                    {
                        Name = propertyInfo!.Name,
                        Attributes = propertyInfo.GetCustomAttributes().ToArray(),
                        Type = propertyInfo.PropertyType,
                        IsStatic = propertyInfo.GetMethod?.IsStatic ?? false,
                        Getter = propertyInfo.GetValue
                    }
                ],
                _ => throw new ArgumentOutOfRangeException(nameof(dataGeneratorType), dataGeneratorType, null)
            },
            TestBuilderContext = testBuilderContextAccessor,
            TestClassInstance =
                needsInstance
                    ? CreateInstance(testDataAttribute, type, classInstanceArgumentsInvoked ?? classInstanceArguments(), testInformation, testBuilderContextAccessor, out _)
                    : null,
            TestSessionId = string.Empty,
        };

    private static object InvokeMethodDataSource(Type methodDataSourceType, MethodDataSourceAttribute methodDataSourceAttribute)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        return methodDataSourceType.GetMethod(
                name: methodDataSourceAttribute.MethodNameProvidingDataSource,
                bindingAttr: bindingFlags,
                binder: null,
                types: methodDataSourceAttribute.Arguments.Select(x => x?.GetType() ?? typeof(object)).ToArray(),
                modifiers: null
            )!
            .Invoke(null, methodDataSourceAttribute.Arguments) ?? Array.Empty<object>();
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

    private static IEnumerable<IDataAttribute> GetDataAttributes(PropertyInfo property)
    {
        return property.GetCustomAttributes().OfType<IDataAttribute>();
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

    public class NestedDependency
    {
        private object? _instance;
        public required DataGeneratorMetadata DataGeneratorMetadata { get; init; }
        public List<NestedDependency> Children { get; } = [];
        public required NestedDependency? Parent { get; set; }
        public required PropertyInfo? Property { get; init; }
        public required Type Type { get; init; }
        public required TestBuilderContextAccessor TestBuilderContextAccessor { get; init; }

        public IDataAttribute? DataAttribute => field ??= Property?.GetCustomAttributes().OfType<IDataAttribute>().FirstOrDefault();

        public object? Instance
        {
            get => _instance ??= CreateInstance();
            init => _instance = value;
        }

        public void AddToParent()
        {
            if (Parent is null)
            {
                throw new InvalidOperationException("Cannot add to parent when there is no parent.");
            }

            Property?.SetValue(Parent?.Instance, Instance);
        }

        public object? CreateInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            if (DataAttribute is null || Property is null)
            {
                return null;
            }

            _instance ??= DataAttribute switch
            {
                ArgumentsAttribute argumentsAttribute => argumentsAttribute.Values.ElementAtOrDefault(0),
                ClassConstructorAttribute classConstructorAttribute => ((IClassConstructor) Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!).Create(
                    Property.PropertyType, new ClassConstructorMetadata
                    {
                        TestBuilderContext = DataGeneratorMetadata.TestBuilderContext.Current, TestSessionId = DataGeneratorMetadata.TestSessionId
                    }),
                IDataSourceGeneratorAttribute dataSourceGeneratorAttribute => dataSourceGeneratorAttribute.GenerateDataSourcesInternal(DataGeneratorMetadata with
                    {
                        MembersToGenerate = [ ReflectionToSourceModelHelpers.GenerateProperty(Property) ]
                    })
                    .ElementAtOrDefault(0)
                    ?.Invoke()
                    ?.ElementAtOrDefault(0),
                InstanceMethodDataSourceAttribute instanceMethodDataSourceAttribute => Parent?.Instance?.GetType()
                    .GetMethod(instanceMethodDataSourceAttribute.MethodNameProvidingDataSource, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)!
                    .Invoke(Parent?.Instance, instanceMethodDataSourceAttribute.Arguments),
                MethodDataSourceAttribute methodDataSourceAttribute => (methodDataSourceAttribute.ClassProvidingDataSource ?? Parent?.Type) !.GetMethod(
                    methodDataSourceAttribute.MethodNameProvidingDataSource, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy) !.Invoke(null,
                    methodDataSourceAttribute.Arguments),
                NoOpDataAttribute => null,
                _ => throw new ArgumentOutOfRangeException(nameof(DataAttribute))
            };

            TestBuilderContextAccessor.Current.Events.OnInitialize += async (_, _) =>
            {
                await ObjectInitializer.InitializeAsync(_instance);

                var parent = Parent;

                while (parent?.Instance != null)
                {
                    await ObjectInitializer.InitializeAsync(parent);
                    parent = Parent?.Parent;
                }
            };
            TestBuilderContextAccessor.Current.Events.OnInitialize.Order = int.MinValue;

            return _instance;
        }

        public override string ToString()
        {
            if (Property is null)
            {
                return $"{Type.Name}";
            }

            return $"{Parent}.{Property.Name}";
        }
    }
}
