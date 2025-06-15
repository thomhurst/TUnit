using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using Polyfills;

namespace TUnit.Engine.Services.Reflection;

[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("Trimming", "IL2072")]
[UnconditionalSuppressMessage("Trimming", "IL2070")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
internal static class DataGeneratorHandler
{
    public static IDataAttribute PrepareDataGeneratorInstance(
        IDataAttribute dataAttribute,
        SourceGeneratedMethodInformation testInformation,
        TestBuilderContextAccessor testBuilderContextAccessor)
    {
        if (dataAttribute is not IDataSourceGeneratorAttribute ||
            DotNetAssemblyHelper.IsInDotNetCoreLibrary(dataAttribute.GetType()))
        {
            return dataAttribute;
        }

        var instance = (IDataAttribute)Activator.CreateInstance(dataAttribute.GetType())!;
        InitializeNestedDataGenerators(instance, testInformation, testBuilderContextAccessor);

        if (instance is IAsyncInitializer asyncInitializer)
        {
            AsyncToSyncHelper.RunSync(() => asyncInitializer.InitializeAsync());
        }

        return instance;
    }

    public static void InitializeNestedDataGenerators(
        object? obj,
        SourceGeneratedMethodInformation methodInformation,
        TestBuilderContextAccessor testBuilderContextAccessor)
    {
        var visited = new HashSet<object>();
        InitializeNestedDataGeneratorsInternal(obj, methodInformation, testBuilderContextAccessor, visited);
    }

    private static void InitializeNestedDataGeneratorsInternal(
        object? obj,
        SourceGeneratedMethodInformation methodInformation,
        TestBuilderContextAccessor testBuilderContextAccessor,
        HashSet<object> visited)
    {
        if (obj is null || !visited.Add(obj))
        {
            return;
        }

        var classInformation = ReflectionToSourceModelHelpers.GenerateClass(obj.GetType());

        foreach (var property in classInformation.Properties.Where(p => p.HasAttribute<IDataAttribute>()))
        {
            var generator = property.Attributes.OfType<IDataAttribute>().First();

            InitializeNestedDataGeneratorsInternal(generator, methodInformation, testBuilderContextAccessor, visited);

            var dataGeneratorMetadata = new DataGeneratorMetadata
            {
                Type = DataGeneratorType.Property,
                TestInformation = methodInformation,
                ClassInstanceArguments = [],
                MembersToGenerate = [property],
                TestBuilderContext = testBuilderContextAccessor,
                TestClassInstance = null,
                TestSessionId = string.Empty,
            };

            var value = ReflectionValueCreator.CreatePropertyValue(
                classInformation,
                testBuilderContextAccessor,
                generator,
                property,
                dataGeneratorMetadata);

            if (value is not null)
            {
                property.ReflectionInfo.SetValue(obj, value);
                AsyncToSyncHelper.RunSync(() => ObjectInitializer.InitializeAsync(value));
            }
        }
    }

    public static IEnumerable<Func<object?[]>> GetArgumentsFromDataAttribute(
        IDataAttribute dataAttribute,
        DataGeneratorContext context)
    {
        return dataAttribute switch
        {
            IDataSourceGeneratorAttribute sync => GetArgumentsFromSyncGenerator(sync, context),
            IAsyncDataSourceGeneratorAttribute async => GetArgumentsFromAsyncGenerator(async, context),
            ArgumentsAttribute args => GetArgumentsFromArgumentsAttribute(args),
            InstanceMethodDataSourceAttribute instance => GetArgumentsFromInstanceMethod(instance, context),
            MethodDataSourceAttribute method => GetArgumentsFromMethod(method, context),
            NoOpDataAttribute or ClassConstructorAttribute => [() => []],
            _ => throw new ArgumentOutOfRangeException(nameof(dataAttribute), dataAttribute, null)
        };
    }

    private static IEnumerable<Func<object?[]>> GetArgumentsFromSyncGenerator(
        IDataSourceGeneratorAttribute generator,
        DataGeneratorContext context)
    {
        var generatorToUse = PrepareGeneratorIfNeeded(generator, context);
        var metadata = context.CreateMetadata();
        var funcEnumerable = generatorToUse.Generate(metadata);

        foreach (var func in funcEnumerable)
        {
            yield return () => ProcessDataGeneratorResult(FuncHelper.InvokeFunc(func));
        }
    }

    private static IEnumerable<Func<object?[]>> GetArgumentsFromAsyncGenerator(
        IAsyncDataSourceGeneratorAttribute generator,
        DataGeneratorContext context)
    {
        var generatorToUse = PrepareGeneratorIfNeeded(generator, context);
        var metadata = context.CreateMetadata();
        var asyncEnumerable = generatorToUse.GenerateAsync(metadata);
        var enumerator = asyncEnumerable.GetAsyncEnumerator();

        try
        {
            foreach (var func in AsyncToSyncHelper.EnumerateSync(asyncEnumerable))
            {
                yield return () =>
                {
                    var funcResult = AsyncToSyncHelper.RunSync(func);
                    return ProcessDataGeneratorResult(funcResult);
                };
            }
        }
        finally
        {
            // EnumerateSync handles disposal internally
        }
    }

    private static IEnumerable<Func<object?[]>> GetArgumentsFromArgumentsAttribute(ArgumentsAttribute args)
    {
        yield return () => args.Values;
    }

    private static IEnumerable<Func<object?[]>> GetArgumentsFromInstanceMethod(
        InstanceMethodDataSourceAttribute attribute,
        DataGeneratorContext context)
    {
        var instance = InstanceCreator.CreateInstance(
            context.TypeDataAttribute,
            context.ClassInformation,
            context.ClassInstanceArgumentsInvoked ?? context.ClassInstanceArguments(),
            context.TestInformation,
            context.TestBuilderContextAccessor,
            out _);

        var methodDataSourceType = attribute.ClassProvidingDataSource ?? context.ClassInformation.Type;
        var result = InvokeMethod(methodDataSourceType, attribute.MethodNameProvidingDataSource,
            attribute.Arguments, instance);

        // Handle async instance methods
        if (TryGetAsyncTask(result, out var asyncTask))
        {
            // Since we're in test discovery which is synchronous, we need to block
            var unwrappedResult = AsyncToSyncHelper.RunSync(() => asyncTask);
            return ProcessMethodResults(unwrappedResult, context);
        }

        return ProcessMethodResults(result, context);
    }

    private static IEnumerable<Func<object?[]>> GetArgumentsFromMethod(
        MethodDataSourceAttribute attribute,
        DataGeneratorContext context)
    {
        var methodDataSourceType = attribute.ClassProvidingDataSource ?? context.ClassInformation.Type;

        // We need to handle async methods here, but since this method returns IEnumerable<Func<object?[]>>,
        // we'll wrap the async call in a synchronous context that can be executed later
        return ProcessMethodResultsAsync(methodDataSourceType, attribute, context);
    }

    private static IDataSourceGeneratorAttribute PrepareGeneratorIfNeeded(
        IDataSourceGeneratorAttribute generator,
        DataGeneratorContext context)
    {
        // Don't prepare generators from core libraries or if not for properties
        if (context.DataGeneratorType != DataGeneratorType.Property ||
            DotNetAssemblyHelper.IsInDotNetCoreLibrary(generator.GetType()))
        {
            return generator;
        }

        // For property generators, we need a fresh instance that's properly initialized
        var instance = (IDataSourceGeneratorAttribute)Activator.CreateInstance(generator.GetType())!;
        InitializeNestedDataGenerators(instance, context.TestInformation, context.TestBuilderContextAccessor);
        return instance;
    }

    private static IAsyncDataSourceGeneratorAttribute PrepareGeneratorIfNeeded(
        IAsyncDataSourceGeneratorAttribute generator,
        DataGeneratorContext context)
    {
        // Don't prepare generators from core libraries or if not for properties
        if (context.DataGeneratorType != DataGeneratorType.Property ||
            DotNetAssemblyHelper.IsInDotNetCoreLibrary(generator.GetType()))
        {
            return generator;
        }

        // For property generators, we need a fresh instance that's properly initialized
        var instance = (IAsyncDataSourceGeneratorAttribute)Activator.CreateInstance(generator.GetType())!;
        InitializeNestedDataGenerators(instance, context.TestInformation, context.TestBuilderContextAccessor);
        return instance;
    }

    private static object?[] ProcessDataGeneratorResult(object? funcResult)
    {
        InitializeResult(funcResult);

        if (TupleHelper.TryParseTupleToObjectArray(funcResult, out var objectArray))
        {
            return objectArray;
        }

        if (funcResult is object?[] arr)
        {
            return arr;
        }

        return [funcResult];
    }

    private static void InitializeResult(object? funcResult)
    {
        if (funcResult is object?[] objectArray)
        {
            foreach (var obj in objectArray.Where(o => o is not null))
            {
                AsyncToSyncHelper.RunSync(() => ObjectInitializer.InitializeAsync(obj));
            }
        }
        else if (funcResult is not null)
        {
            AsyncToSyncHelper.RunSync(() => ObjectInitializer.InitializeAsync(funcResult));
        }
    }

    private static IEnumerable<Func<object?[]>> ProcessMethodResults(
        object? result,
        DataGeneratorContext context)
    {
        var enumerableResult = result is not string and IEnumerable enumerable
            ? enumerable.Cast<object?>().ToArray()
            : [result];

        foreach (var methodResult in enumerableResult)
        {
            yield return () => ProcessMethodResult(methodResult, context);
        }
    }

    private static object?[] ProcessMethodResult(object? methodResult, DataGeneratorContext context)
    {
        var parameterType = context.Method.Parameters.ElementAtOrDefault(0)?.Type;

        if (IsAssignableTo(methodResult?.GetType(), parameterType))
        {
            return [methodResult];
        }

        if (FuncHelper.TryInvokeFunc(methodResult, out var funcResult))
        {
            if (IsAssignableTo(funcResult?.GetType(), parameterType) ||
                context.DataGeneratorType == DataGeneratorType.Property)
            {
                return [funcResult];
            }

            if (TupleHelper.TryParseTupleToObjectArray(funcResult, out var funcObjectArray))
            {
                return funcObjectArray;
            }

            return funcResult as object?[] ?? [funcResult];
        }

        if (context.DataGeneratorType == DataGeneratorType.Property)
        {
            return [methodResult];
        }

        if (TupleHelper.TryParseTupleToObjectArray(methodResult, out var objectArray))
        {
            return objectArray;
        }

        return [methodResult];
    }

    private static object? InvokeMethod(Type type, string methodName, object?[] arguments, object? instance)
    {
        return type.GetMethod(
                name: methodName,
                types: arguments.Select(x => x?.GetType() ?? typeof(object)).ToArray())
            ?.Invoke(instance, arguments);
    }

    private static IEnumerable<Func<object?[]>> ProcessMethodResultsAsync(
        Type methodDataSourceType,
        MethodDataSourceAttribute attribute,
        DataGeneratorContext context)
    {
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.Static | BindingFlags.FlattenHierarchy;

        var method = methodDataSourceType.GetMethod(
            name: attribute.MethodNameProvidingDataSource,
            bindingAttr: bindingFlags,
            binder: null,
            types: attribute.Arguments.Select(x => x?.GetType() ?? typeof(object)).ToArray(),
            modifiers: null)!;

        var result = method.Invoke(null, attribute.Arguments);

        // If the method returns a Task or ValueTask, we need to handle it
        if (TryGetAsyncTask(result, out var asyncTask))
        {
            // For async methods, we need to await the result before processing
            // Note: This follows the same pattern as async data generators - test discovery is synchronous
            var unwrappedResult = AsyncToSyncHelper.RunSync(() => asyncTask);

            // Now process the unwrapped result normally
            foreach (var item in ProcessMethodResults(unwrappedResult, context))
            {
                yield return item;
            }
        }
        else
        {
            // Regular synchronous method result
            foreach (var item in ProcessMethodResults(result, context))
            {
                yield return item;
            }
        }
    }

    private static bool IsAsyncResult(object? result)
    {
        if (result is null)
            return false;

        var type = result.GetType();
        return typeof(Task).IsAssignableFrom(type) || type.Name.StartsWith("ValueTask");
    }

    private static bool TryGetAsyncTask(object? result, out Task<object?> task)
    {
        task = null!;

        if (result is null)
            return false;

        var type = result.GetType();

        // Handle Task<T>
        if (result is Task taskResult)
        {
            if (type.IsGenericType)
            {
                // Create a Task<object?> that wraps the original task
                task = CreateObjectTask(taskResult, type);
                return true;
            }
            // Non-generic Task, wrap it as Task<object?>
            task = taskResult.ContinueWith(_ => (object?)null);
            return true;
        }

        // Handle ValueTask<T>
        if (type.Name.StartsWith("ValueTask"))
        {
            // Convert ValueTask to Task first
            var asTaskMethod = type.GetMethod("AsTask");
            var convertedTask = (Task)asTaskMethod!.Invoke(result, null)!;

            if (type.IsGenericType)
            {
                task = CreateObjectTask(convertedTask, convertedTask.GetType());
                return true;
            }
            task = convertedTask.ContinueWith(_ => (object?)null);
            return true;
        }

        return false;
    }

    private static Task<object?> CreateObjectTask(Task task, Type taskType)
    {
        // Use ContinueWith to extract the result and cast to object?
        return task.ContinueWith(t =>
        {
            if (t.IsFaulted)
                throw t.Exception!.InnerException!;

            if (t.IsCanceled)
                throw new TaskCanceledException();

            var resultProperty = taskType.GetProperty("Result");
            return resultProperty?.GetValue(t);
        });
    }

    private static object? UnwrapAsyncResult(object? result)
    {
        return AsyncToSyncHelper.UnwrapTaskResult(result);
    }

    private static bool IsAssignableTo(Type? source, Type? target)
    {
        if (source is null || target is null)
        {
            return false;
        }
#if NETSTANDARD2_0
        return target.IsAssignableFrom(source);
#else
        return source.IsAssignableTo(target);
#endif
    }
}

internal class DataGeneratorContext
{
    public required IDataAttribute TypeDataAttribute { get; init; }
    public required SourceGeneratedClassInformation ClassInformation { get; init; }
    public required SourceGeneratedMethodInformation Method { get; init; }
    public required SourceGeneratedPropertyInformation? PropertyInfo { get; init; }
    public required IDataAttribute TestDataAttribute { get; init; }
    public required DataGeneratorType DataGeneratorType { get; init; }
    public required Func<object?[]> ClassInstanceArguments { get; init; }
    public required SourceGeneratedMethodInformation TestInformation { get; init; }
    public required TestBuilderContextAccessor TestBuilderContextAccessor { get; init; }
    public required object?[]? ClassInstanceArgumentsInvoked { get; init; }
    public required bool NeedsInstance { get; init; }

    public DataGeneratorMetadata CreateMetadata()
    {
        return new DataGeneratorMetadata
        {
            Type = DataGeneratorType,
            TestInformation = TestInformation,
            ClassInstanceArguments = ClassInstanceArgumentsInvoked,
            MembersToGenerate = DataGeneratorType switch
            {
                DataGeneratorType.TestParameters => Method.Parameters.ToArray<SourceGeneratedMemberInformation>(),
                DataGeneratorType.ClassParameters => ClassInformation.Parameters.ToArray<SourceGeneratedMemberInformation>(),
                DataGeneratorType.Property => PropertyInfo is null ? [] : [PropertyInfo],
                _ => throw new ArgumentOutOfRangeException(nameof(DataGeneratorType), DataGeneratorType, null)
            },
            TestBuilderContext = TestBuilderContextAccessor,
            TestClassInstance = NeedsInstance
                ? InstanceCreator.CreateInstance(TestDataAttribute, ClassInformation,
                    ClassInstanceArgumentsInvoked ?? ClassInstanceArguments(),
                    TestInformation, TestBuilderContextAccessor, true, out _)
                : null,
            TestSessionId = string.Empty,
        };
    }
}
