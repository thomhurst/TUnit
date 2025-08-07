using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.Helpers;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public class MethodDataSourceAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
T>(string methodNameProvidingDataSource)
    : MethodDataSourceAttribute(typeof(T), methodNameProvidingDataSource);

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public class MethodDataSourceAttribute : Attribute, IDataSourceAttribute
{
    private const BindingFlags BindingFlags = System.Reflection.BindingFlags.Public
        | System.Reflection.BindingFlags.NonPublic
        | System.Reflection.BindingFlags.Static
        | System.Reflection.BindingFlags.Instance
        | System.Reflection.BindingFlags.FlattenHierarchy;

    // Cache for compiled method delegates to avoid repeated reflection
    private static readonly ConcurrentDictionary<MethodCacheKey, Func<object?, object?[], object?>> MethodDelegateCache = new();
    
    // Struct key for efficient dictionary lookups
    private readonly struct MethodCacheKey : IEquatable<MethodCacheKey>
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public readonly Type Type;
        public readonly string MethodName;
        public readonly Type[] ArgumentTypes;
        private readonly int _hashCode;

        public MethodCacheKey(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type, 
            string methodName, 
            Type[] argumentTypes)
        {
            Type = type;
            MethodName = methodName;
            ArgumentTypes = argumentTypes;
            
            // Pre-compute hash code for performance
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + type.GetHashCode();
                hash = hash * 31 + methodName.GetHashCode();
                foreach (var argType in argumentTypes)
                {
                    hash = hash * 31 + (argType?.GetHashCode() ?? 0);
                }
                _hashCode = hash;
            }
        }

        public bool Equals(MethodCacheKey other)
        {
            return Type == other.Type && 
                   MethodName == other.MethodName && 
                   ArgumentTypes.SequenceEqual(other.ArgumentTypes);
        }

        public override bool Equals(object? obj) => obj is MethodCacheKey key && Equals(key);
        public override int GetHashCode() => _hashCode;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
    public Type? ClassProvidingDataSource { get; }
    public string MethodNameProvidingDataSource { get; }

    public Func<DataGeneratorMetadata, IAsyncEnumerable<Func<Task<object?[]?>>>>? Factory { get; set; }

    public object?[] Arguments { get; set; } = [];

    public MethodDataSourceAttribute(string methodNameProvidingDataSource)
    {
        if (methodNameProvidingDataSource is null or { Length: < 1 })
        {
            throw new ArgumentException("No method name was provided");
        }

        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }

    public MethodDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type classProvidingDataSource,
        string methodNameProvidingDataSource)
    {
        if (methodNameProvidingDataSource is null or { Length: < 1 })
        {
            throw new ArgumentException("No method name was provided");
        }

        ClassProvidingDataSource = classProvidingDataSource ?? throw new ArgumentNullException(nameof(classProvidingDataSource), "No class type was provided");
        MethodNameProvidingDataSource = methodNameProvidingDataSource;
    }

    [UnconditionalSuppressMessage("AOT", "IL2072:UnrecognizedReflectionPattern", Justification = "Data source methods use dynamic patterns")]
    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern", Justification = "Data source methods use dynamic patterns")]
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        if (Factory != null)
        {
            await foreach (var func in Factory(dataGeneratorMetadata))
            {
                yield return func;
            }

            yield break;
        }

        if (dataGeneratorMetadata.MembersToGenerate.Length == 0)
        {
            throw new InvalidOperationException($"No members to generate were provided for {dataGeneratorMetadata.Type}");
        }

        var item1 = dataGeneratorMetadata.MembersToGenerate[0];

        var targetType = ClassProvidingDataSource
            ?? (item1 as PropertyMetadata)?.ClassMetadata.Type
            ?? TestClassTypeHelper.GetTestClassType(dataGeneratorMetadata);

        // If we have a test class instance and no explicit class was provided,
        // use the instance's actual type (which will be the constructed generic type)
        if (ClassProvidingDataSource == null && dataGeneratorMetadata.TestClassInstance != null)
        {
            targetType = dataGeneratorMetadata.TestClassInstance.GetType();
        }
        
        if (targetType == null)
        {
            throw new InvalidOperationException($"Could not determine target type for method '{MethodNameProvidingDataSource}'. This may occur during static property initialization without a test context.");
        }

        // Create cache key for delegate lookup
        var argumentTypes = Arguments.Select(a => a?.GetType() ?? typeof(object)).ToArray();
        var cacheKey = new MethodCacheKey(targetType, MethodNameProvidingDataSource, argumentTypes);
        
        // Get or create cached delegate
        var methodDelegate = MethodDelegateCache.GetOrAdd(cacheKey, key => 
        {
            var methodInfo = key.Type.GetMethods(BindingFlags).SingleOrDefault(x => x.Name == key.MethodName
                    && x.GetParameters().Select(p => p.ParameterType).SequenceEqual(key.ArgumentTypes))
                ?? key.Type.GetMethod(key.MethodName, BindingFlags);
                
            if (methodInfo is null)
            {
                throw new InvalidOperationException($"Method '{key.MethodName}' not found in class '{key.Type.Name}' with the specified arguments.");
            }
            
            // Compile method to delegate for faster invocation
            return CompileMethodDelegate(methodInfo);
        });

        // Determine if it's an instance method (cached in delegate)
        object? instance = null;
        var methodInfo = targetType.GetMethods(BindingFlags).SingleOrDefault(x => x.Name == MethodNameProvidingDataSource
                && x.GetParameters().Select(p => p.ParameterType).SequenceEqual(Arguments.Select(a => a?.GetType())))
            ?? targetType.GetMethod(MethodNameProvidingDataSource, BindingFlags);
            
        if (methodInfo != null && !methodInfo.IsStatic)
        {
            instance = dataGeneratorMetadata.TestClassInstance ?? Activator.CreateInstance(targetType);
        }

        var methodResult = methodDelegate(instance, Arguments);

        // Handle different return types
        if (methodResult == null)
        {
            yield break;
        }

        // If it's IAsyncEnumerable, handle it specially
        if (IsAsyncEnumerable(methodResult.GetType()))
        {
            await foreach (var item in ConvertToAsyncEnumerable(methodResult))
            {
                yield return async () =>
                {
                    return await Task.FromResult<object?[]?>(item.ToObjectArray());
                };
            }
        }
        // If it's Task<IEnumerable>
        else if (methodResult is Task task)
        {
            await task.ConfigureAwait(false);
            var taskResult = GetTaskResult(task);

            if (taskResult is System.Collections.IEnumerable enumerable and not string && !DataSourceHelpers.IsTuple(taskResult))
            {
                foreach (var item in enumerable)
                {
                    yield return async () =>
                    {
                        return await Task.FromResult<object?[]?>(item.ToObjectArray());
                    };
                }
            }
            else
            {
                yield return async () =>
                {
                    return await Task.FromResult<object?[]?>(taskResult.ToObjectArray());
                };
            }
        }
        // Regular IEnumerable - but check if it's a tuple first
        // Tuples implement IEnumerable but should be treated as single values
        else if (methodResult is System.Collections.IEnumerable enumerable and not string && !DataSourceHelpers.IsTuple(methodResult))
        {
            foreach (var item in enumerable)
            {
                yield return () => Task.FromResult<object?[]?>(item.ToObjectArray());
            }
        }
        else
        {
            yield return async () =>
            {
                return await Task.FromResult<object?[]?>(methodResult.ToObjectArray());
            };
        }
    }

    private static bool IsAsyncEnumerable([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        return type.GetInterfaces()
            .Any(i => i.IsGenericType &&
                     i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
    }

    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern", Justification = "Data source methods may use dynamic patterns")]
    private static async IAsyncEnumerable<object?> ConvertToAsyncEnumerable(object asyncEnumerable)
    {
        var type = asyncEnumerable.GetType();
        var enumeratorMethod = type.GetMethod("GetAsyncEnumerator");
        var enumerator = enumeratorMethod!.Invoke(asyncEnumerable, [CancellationToken.None]);

        var moveNextMethod = enumerator!.GetType().GetMethod("MoveNextAsync");
        var currentProperty = enumerator.GetType().GetProperty("Current");

        while (true)
        {
            var moveNextTask = (ValueTask<bool>)moveNextMethod!.Invoke(enumerator, null)!;
            if (!await moveNextTask.ConfigureAwait(false))
            {
                break;
            }

            yield return currentProperty!.GetValue(enumerator);
        }

        // Dispose the enumerator
        var disposeMethod = enumerator.GetType().GetMethod("DisposeAsync");
        if (disposeMethod != null)
        {
            var disposeTask = (ValueTask)disposeMethod.Invoke(enumerator, null)!;
            await disposeTask.ConfigureAwait(false);
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern", Justification = "Task result property access")]
    private static object? GetTaskResult(Task task)
    {
        var taskType = task.GetType();

        if (taskType.IsGenericType)
        {
            var resultProperty = taskType.GetProperty("Result");
            return resultProperty?.GetValue(task);
        }

        return null;
    }
    
    // Compile method to delegate for fast invocation without reflection
    private static Func<object?, object?[], object?> CompileMethodDelegate(MethodInfo methodInfo)
    {
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var argumentsParam = Expression.Parameter(typeof(object[]), "arguments");
        
        var parameters = methodInfo.GetParameters();
        var parameterExpressions = new Expression[parameters.Length];
        
        for (int i = 0; i < parameters.Length; i++)
        {
            var arrayAccess = Expression.ArrayIndex(argumentsParam, Expression.Constant(i));
            parameterExpressions[i] = Expression.Convert(arrayAccess, parameters[i].ParameterType);
        }
        
        Expression methodCall;
        if (methodInfo.IsStatic)
        {
            methodCall = Expression.Call(methodInfo, parameterExpressions);
        }
        else
        {
            var typedInstance = Expression.Convert(instanceParam, methodInfo.DeclaringType!);
            methodCall = Expression.Call(typedInstance, methodInfo, parameterExpressions);
        }
        
        // Handle void methods
        if (methodInfo.ReturnType == typeof(void))
        {
            var block = Expression.Block(methodCall, Expression.Constant(null, typeof(object)));
            return Expression.Lambda<Func<object?, object?[], object?>>(block, instanceParam, argumentsParam).Compile();
        }
        
        // Convert return value to object
        var returnValue = methodInfo.ReturnType.IsValueType 
            ? Expression.Convert(methodCall, typeof(object))
            : (Expression)methodCall;
            
        return Expression.Lambda<Func<object?, object?[], object?>>(returnValue, instanceParam, argumentsParam).Compile();
    }
}
