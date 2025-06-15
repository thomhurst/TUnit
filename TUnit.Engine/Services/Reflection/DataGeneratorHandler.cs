using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Extensions;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Engine.Helpers;
using Polyfills;

namespace TUnit.Engine.Services.Reflection;

[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("Trimming", "IL2072")]
[UnconditionalSuppressMessage("Trimming", "IL2070")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
internal static class DataGeneratorHandler
{
    public static async Task<IDataAttribute> PrepareDataGeneratorInstanceAsync(
        IDataAttribute dataAttribute,
        SourceGeneratedMethodInformation testInformation,
        TestBuilderContextAccessor testBuilderContextAccessor)
    {
        if (dataAttribute is not IAsyncDataSourceGeneratorAttribute ||
            DotNetAssemblyHelper.IsInDotNetCoreLibrary(dataAttribute.GetType()))
        {
            return dataAttribute;
        }

        var instance = (IDataAttribute)Activator.CreateInstance(dataAttribute.GetType())!;
        
        // Use centralized DataSourceInitializer
        var metadata = new DataGeneratorMetadata
        {
            Type = DataGeneratorType.Property,
            TestInformation = testInformation,
            ClassInstanceArguments = [],
            MembersToGenerate = [],
            TestBuilderContext = testBuilderContextAccessor,
            TestClassInstance = null,
            TestSessionId = string.Empty,
        };
        
        await DataSourceInitializer.InitializeAsync(instance, metadata, testBuilderContextAccessor);

        return instance;
    }



    public static async IAsyncEnumerable<Func<Task<object?[]>>> GetArgumentsFromDataAttributeAsync(
        IDataAttribute dataAttribute,
        DataGeneratorContext context)
    {
        switch (dataAttribute)
        {
            case IAsyncDataSourceGeneratorAttribute async:
                await foreach (var item in GetArgumentsFromAsyncGeneratorAsync(async, context))
                    yield return item;
                break;
            case ArgumentsAttribute args:
                foreach (var item in GetArgumentsFromArgumentsAttribute(args))
                    yield return async () => 
                    {
                        var result = item();
                        await InitializeDataGeneratorResultsAsync(result, context);
                        return result;
                    };
                break;
            case InstanceMethodDataSourceAttribute instance:
                await foreach (var item in GetArgumentsFromInstanceMethodAsync(instance, context))
                    yield return item;
                break;
            case MethodDataSourceAttribute method:
                await foreach (var item in GetArgumentsFromMethodAsync(method, context))
                    yield return item;
                break;
            case NoOpDataAttribute or ClassConstructorAttribute:
                yield return () => Task.FromResult(Array.Empty<object?>());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dataAttribute), dataAttribute, null);
        }
    }
    
    private static async IAsyncEnumerable<Func<Task<object?[]>>> GetArgumentsFromAsyncGeneratorAsync(
        IAsyncDataSourceGeneratorAttribute generator,
        DataGeneratorContext context)
    {
        var generatorToUse = await PrepareGeneratorIfNeededAsync(generator, context);
        var metadata = await context.CreateMetadataAsync();
        var asyncEnumerable = generatorToUse.GenerateAsync(metadata);

        await foreach (var func in asyncEnumerable)
        {
            yield return async () =>
            {
                var funcResult = await func();
                var result = ProcessDataGeneratorResult(funcResult);
                
                // Initialize all objects returned by the data generator
                await InitializeDataGeneratorResultsAsync(result, context);
                
                return result;
            };
        }
    }



    private static IEnumerable<Func<object?[]>> GetArgumentsFromArgumentsAttribute(ArgumentsAttribute args)
    {
        yield return () => args.Values;
    }

    private static async IAsyncEnumerable<Func<Task<object?[]>>> GetArgumentsFromInstanceMethodAsync(
        InstanceMethodDataSourceAttribute attribute,
        DataGeneratorContext context)
    {
        var (instance, error) = await InstanceCreator.CreateInstanceAsync(
            context.TypeDataAttribute,
            context.ClassInformation,
            context.ClassInstanceArgumentsInvoked ?? context.ClassInstanceArguments(),
            context.TestInformation,
            context.TestBuilderContextAccessor);
            
        if (error != null)
        {
            throw error;
        }

        var methodDataSourceType = attribute.ClassProvidingDataSource ?? context.ClassInformation.Type;
        var result = InvokeMethod(methodDataSourceType, attribute.MethodNameProvidingDataSource,
            attribute.Arguments, instance);

        // Handle async instance methods
        if (TryGetAsyncTask(result, out var asyncTask))
        {
            var unwrappedResult = await asyncTask;
            await foreach (var item in ProcessMethodResultsAsync(unwrappedResult, context))
                yield return item;
        }
        else
        {
            await foreach (var item in ProcessMethodResultsAsync(result, context))
                yield return item;
        }
    }
    
    private static async IAsyncEnumerable<Func<Task<object?[]>>> GetArgumentsFromMethodAsync(
        MethodDataSourceAttribute attribute,
        DataGeneratorContext context)
    {
        var methodDataSourceType = attribute.ClassProvidingDataSource ?? context.ClassInformation.Type;
        
        await foreach (var item in ProcessStaticMethodResultsAsync(methodDataSourceType, attribute, context))
            yield return item;
    }



    private static async Task<IAsyncDataSourceGeneratorAttribute> PrepareGeneratorIfNeededAsync(
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
        
        // Use centralized DataSourceInitializer
        var metadata = new DataGeneratorMetadata
        {
            Type = DataGeneratorType.Property,
            TestInformation = context.TestInformation,
            ClassInstanceArguments = context.ClassInstanceArgumentsInvoked ?? [],
            MembersToGenerate = [],
            TestBuilderContext = context.TestBuilderContextAccessor,
            TestClassInstance = null,
            TestSessionId = string.Empty,
        };
        
        await DataSourceInitializer.InitializeAsync(instance, metadata, context.TestBuilderContextAccessor);
        return instance;
    }

    private static object?[] ProcessDataGeneratorResult(object? funcResult)
    {
        // Note: Object initialization should be done at execution time, not discovery time
        // InitializeResultAsync(funcResult);

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

    private static async Task InitializeDataGeneratorResultsAsync(object?[]? results, DataGeneratorContext context)
    {
        if (results is null)
        {
            return;
        }

        // Create metadata for initialization
        var metadata = new DataGeneratorMetadata
        {
            Type = context.DataGeneratorType,
            TestInformation = context.TestInformation,
            ClassInstanceArguments = context.ClassInstanceArgumentsInvoked ?? [],
            MembersToGenerate = [],
            TestBuilderContext = context.TestBuilderContextAccessor,
            TestClassInstance = null,
            TestSessionId = string.Empty,
        };

        // Initialize each result object
        foreach (var obj in results.Where(o => o is not null))
        {
            await DataSourceInitializer.InitializeAsync(obj!, metadata, context.TestBuilderContextAccessor);
        }
    }

    
    #pragma warning disable CS1998 // Async method lacks 'await' operators
    private static async IAsyncEnumerable<Func<Task<object?[]>>> ProcessMethodResultsAsync(
        object? result,
        DataGeneratorContext context)
    {
    #pragma warning restore CS1998
        var enumerableResult = result is not string and IEnumerable enumerable
            ? enumerable.Cast<object?>().ToArray()
            : [result];

        foreach (var methodResult in enumerableResult)
        {
            yield return async () => 
            {
                var result = ProcessMethodResult(methodResult, context);
                await InitializeDataGeneratorResultsAsync(result, context);
                return result;
            };
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

    
    private static async IAsyncEnumerable<Func<Task<object?[]>>> ProcessStaticMethodResultsAsync(
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
            var unwrappedResult = await asyncTask;
            
            // Now process the unwrapped result normally
            await foreach (var item in ProcessMethodResultsAsync(unwrappedResult, context))
            {
                yield return item;
            }
        }
        else
        {
            // Regular synchronous method result
            await foreach (var item in ProcessMethodResultsAsync(result, context))
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
            #pragma warning disable IL2075
            var asTaskMethod = type.GetMethod("AsTask");
            #pragma warning restore IL2075
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

    public async Task<DataGeneratorMetadata> CreateMetadataAsync()
    {
        object? testClassInstance = null;
        
        if (NeedsInstance)
        {
            var (instance, error) = await InstanceCreator.CreateInstanceAsync(
                TestDataAttribute, ClassInformation,
                ClassInstanceArgumentsInvoked ?? ClassInstanceArguments(),
                TestInformation, TestBuilderContextAccessor, true);
                
            if (error != null)
            {
                throw error;
            }
            
            testClassInstance = instance;
        }
        
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
            TestClassInstance = testClassInstance,
            TestSessionId = string.Empty,
        };
    }
}
