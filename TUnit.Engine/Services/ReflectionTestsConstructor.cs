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
using TUnit.Engine.Extensions;
using TUnit.Engine.Helpers;
#pragma warning disable TUnitWIP0001

namespace TUnit.Engine.Services;

[UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2067:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The parameter of method does not have matching annotations.")]
[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
[UnconditionalSuppressMessage("Trimming", "IL2070:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The parameter of method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2055:Either the type on which the MakeGenericType is called can\'t be statically determined, or the type parameters to be used for generic arguments can\'t be statically determined.")]
[UnconditionalSuppressMessage("Trimming", "IL2060:MakeGenericMethod call does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
[UnconditionalSuppressMessage("Trimming", "IL2111:Reflection")]
internal class ReflectionTestsConstructor(IExtension extension,
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

                        foreach (var classInstanceArguments in GetArguments(type, testMethod, null, typeDataAttribute, DataGeneratorType.ClassParameters, () => [], testInformation, testBuilderContextAccessor))
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

    private void BuildTests(SourceGeneratedMethodInformation testInformation, Func<object?[]> classInstanceArguments, IDataAttribute typeDataAttribute, IDataAttribute testDataAttribute,
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

            foreach (var attribute in allAttributes)
            {
                CreateNestedDataGenerators(attribute, testInformation, testBuilderContextAccessor, []);
            }

            var repeatCount = allAttributes.OfType<RepeatAttribute>().FirstOrDefault()?.Times ?? 0;

            for (var index = 0; index < repeatCount + 1; index++)
            {
                var invokedClassInstanceArguments = classInstanceArguments();

                foreach (var testArguments in GetArguments(type, testMethod, null, testDataAttribute, DataGeneratorType.TestParameters, () => invokedClassInstanceArguments, testInformation,
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

    private void CreateNestedDataGenerators(object obj, SourceGeneratedMethodInformation methodInformation, TestBuilderContextAccessor testBuilderContextAccessor, HashSet<object> visited)
    {
        if (!visited.Add(obj))
        {
            return;
        }

        foreach (var property in CollectSettableProperties(obj, methodInformation, testBuilderContextAccessor))
        {
            if (property.GetValue(obj) is not {} propertyValue)
            {
                // Try to generate value using existing data generation logic
                propertyValue = CreatePropertyValue(obj.GetType(), property, methodInformation, testBuilderContextAccessor);

                if (propertyValue != null)
                {
                    property.SetValue(obj, propertyValue);
                }
            }

            if(propertyValue is IAsyncInitializer)
            {
                testBuilderContextAccessor.Current.Events.OnInitialize += async (_, _) =>
                {
                    await reflectionDataInitializer.Initialize(propertyValue, []);
                };
            }

            if (propertyValue is not null)
            {
                CreateNestedDataGenerators(propertyValue, methodInformation, testBuilderContextAccessor, visited);
            }        }
    }    private static IEnumerable<PropertyInfo> CollectSettableProperties(object obj, SourceGeneratedMethodInformation methodInformation, TestBuilderContextAccessor testBuilderContextAccessor)
    {
        var type = obj.GetType();

        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite &&
                   (p.GetCustomAttributes().OfType<IDataAttribute>().Any() ||
                    ShouldCreateNestedInstance(p.PropertyType)));
    }

    private static bool ShouldCreateNestedInstance(Type propertyType)
    {
        // Create nested instances for complex types that have default constructors
        // but skip basic types, strings, and value types
        return propertyType.IsClass &&
               propertyType != typeof(string) &&
               !propertyType.IsAbstract &&
               !propertyType.IsInterface &&
               propertyType.GetConstructor(Type.EmptyTypes) != null;
    }

    private object? CreatePropertyValue(Type type, PropertyInfo property, SourceGeneratedMethodInformation methodInformation, TestBuilderContextAccessor testBuilderContextAccessor)
    {
        var dataAttributes = GetDataAttributes(property);

        foreach (var dataAttribute in dataAttributes)
        {
            // Reuse the existing GetArguments logic for property data generation
            var argumentGenerators = GetArguments(
                type,
                null,
                property,
                dataAttribute,
                DataGeneratorType.Property,
                () => [],
                methodInformation,
                testBuilderContextAccessor
            );

            var firstArgumentGenerator = argumentGenerators.FirstOrDefault();
            if (firstArgumentGenerator != null)
            {
                var arguments = firstArgumentGenerator();
                return arguments?.FirstOrDefault();
            }
        }

        return null;
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

        // Create a mapping from type parameters to concrete types
        var typeParameterMap = new Dictionary<Type, Type>();

        // First pass: map type parameters that directly correspond to parameter types
        for (var i = 0; i < parameters.Length && i < argumentsTypes.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            var argumentType = argumentsTypes[i];

            if (argumentType != null)
            {
                MapTypeParameters(parameterType, argumentType, typeParameterMap);
            }
        }

        // Second pass: resolve any remaining unmapped type parameters
        List<Type> substituteTypes = [];
        foreach (var typeArgument in typeArguments)
        {
            if (typeParameterMap.TryGetValue(typeArgument, out var mappedType))
            {
                substituteTypes.Add(mappedType);
            }
            else
            {
                // If we can't map the type parameter, try to infer it from the parameter position
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

                    // Handle nullable types
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
            // Direct mapping: T -> int, T -> string, etc.
            if (!typeParameterMap.ContainsKey(parameterType))
            {
                typeParameterMap[parameterType] = argumentType;
            }
        }
        else if (parameterType.IsGenericType && argumentType.IsGenericType)
        {
            // Handle generic types: List<T> -> List<int>, Dictionary<T, U> -> Dictionary<string, int>, etc.
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
            // Handle nullable types: T? -> int, T? -> int?
            var underlyingParameterType = parameterType.GetGenericArguments()[0];
            var underlyingArgumentType = Nullable.GetUnderlyingType(argumentType) ?? argumentType;
            MapTypeParameters(underlyingParameterType, underlyingArgumentType, typeParameterMap);
        }    }

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

                arguments = [
                    ..arguments.Take(arguments.Length - 1),
                    typedArray
                ];
            }

            return;
        }

        if(parameters.Length == 0)
        {
            arguments = [];
            return;
        }

        if (arguments.Length < parameters.Length)
        {
            var missingParameters = parameters.Skip(arguments.Length).ToArray();

            if (missingParameters.All(x => x.IsOptional))
            {
                arguments = [
                    ..arguments,
                    ..missingParameters.Select(x => x.DefaultValue)
                ];
                return;
            }

            if (parameters.LastOrDefault()?.Type == typeof(CancellationToken)
                && arguments.LastOrDefault() is not CancellationToken)
            {
                // We'll add this later
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

                if(argumentsAfterParams.All(x => x is null || IsConvertibleTo(x, underlyingType)))
                {
                    var typedArray = Array.CreateInstance(underlyingType, argumentsAfterParams.Length);

                    for (var i = 0; i < argumentsAfterParams.Length; i++)
                    {
                        typedArray.SetValue(CastHelper.Cast(underlyingType, argumentsAfterParams[i]), i);
                    }

                    // We have a params argument, so we can just add the rest of the arguments
                    arguments = [
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
            var dataAttributes = GetDataAttributes(propertyInfo)[0];

            var args = GetArguments(type, null, propertyInfo, dataAttributes, DataGeneratorType.Property, () => classInstanceArguments, testInformation, testBuilderContextAccessor).ToArray();

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

            var invoke = dataSourceGeneratorAttribute.GetType().GetMethod("GenerateDataSources")!.Invoke(testDataAttribute, [
                CreateDataGeneratorMetadata(type, method, propertyInfo, testDataAttribute, dataGeneratorType, classInstanceArguments, testInformation, testBuilderContextAccessor, classInstanceArgumentsInvoked, needsInstance)
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
                        Name = x.Name!,
                        Attributes = x.GetCustomAttributes().ToArray(),
                        ReflectionInfo = x,
                    }).ToArray<SourceGeneratedMemberInformation>(),
                DataGeneratorType.ClassParameters => type.GetConstructors().FirstOrDefault(x => !x.IsStatic)?
                    .GetParameters()
                    .Select(x => new SourceGeneratedParameterInformation(x.ParameterType)
                    {
                        Name = x.Name!,
                        Attributes = x.GetCustomAttributes().ToArray(),
                        ReflectionInfo = x,
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
            TestClassInstance = needsInstance ? CreateInstance(testDataAttribute, type, classInstanceArgumentsInvoked ?? classInstanceArguments(), testInformation, testBuilderContextAccessor, out _) : null,
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
