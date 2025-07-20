using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TUnit.Core;

/// <summary>
/// Internal data source attribute for method-based data generation
/// Used when we need to invoke a method at runtime to get data
/// </summary>
[UnconditionalSuppressMessage("AOT", "IL2072:UnrecognizedReflectionPattern", Justification = "Only used in reflection mode")]
[UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern", Justification = "Only used in reflection mode")]
internal sealed class MethodBasedDataSourceAttribute : Attribute, IDataSourceAttribute
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    private readonly Type _targetType;
    private readonly string _methodName;
    private readonly object?[]? _arguments;
    private readonly bool _isInstance;
    
    [UnconditionalSuppressMessage("AOT", "IL2069:UnrecognizedReflectionPattern", Justification = "Only used in reflection mode")]
    public MethodBasedDataSourceAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type targetType, string methodName, object?[]? arguments = null, bool isInstance = false)
    {
        _targetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        _methodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        _arguments = arguments;
        _isInstance = isInstance;
    }
    
    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
        bindingFlags |= _isInstance ? BindingFlags.Instance : BindingFlags.Static;
        
        var method = _targetType.GetMethod(_methodName, bindingFlags);
        if (method == null)
        {
            throw new InvalidOperationException($"Method '{_methodName}' not found on type '{_targetType.FullName}'");
        }
        
        object? instance = null;
        if (_isInstance)
        {
            instance = Activator.CreateInstance(_targetType);
        }
        
        var result = method.Invoke(instance, _arguments);
        
        // Handle different return types
        if (result == null)
        {
            yield break;
        }
        
        // If it's IAsyncEnumerable, handle it specially
        if (IsAsyncEnumerable(result.GetType()))
        {
            await foreach (var item in ConvertToAsyncEnumerable(result))
            {
                yield return () => Task.FromResult<object?[]?>(ConvertToObjectArray(item));
            }
        }
        // If it's Task<IEnumerable>
        else if (result is Task task)
        {
            await task.ConfigureAwait(false);
            var taskResult = GetTaskResult(task);
            
            if (taskResult is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    yield return () => Task.FromResult<object?[]?>(ConvertToObjectArray(item));
                }
            }
            else
            {
                yield return () => Task.FromResult<object?[]?>(ConvertToObjectArray(taskResult));
            }
        }
        // Regular IEnumerable
        else if (result is IEnumerable enumerable && !(result is string))
        {
            foreach (var item in enumerable)
            {
                yield return () => Task.FromResult<object?[]?>(ConvertToObjectArray(item));
            }
        }
        else
        {
            yield return () => Task.FromResult<object?[]?>(ConvertToObjectArray(result));
        }
    }
    
    [UnconditionalSuppressMessage("AOT", "IL2070:UnrecognizedReflectionPattern", Justification = "Only used in reflection mode")]
    private static bool IsAsyncEnumerable([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        return type.GetInterfaces()
            .Any(i => i.IsGenericType && 
                     i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
    }
    
    [UnconditionalSuppressMessage("AOT", "IL2075:UnrecognizedReflectionPattern", Justification = "Only used in reflection mode")]
    private static async IAsyncEnumerable<object?> ConvertToAsyncEnumerable(object asyncEnumerable)
    {
        var type = asyncEnumerable.GetType();
        var enumeratorMethod = type.GetMethod("GetAsyncEnumerator");
        var enumerator = enumeratorMethod!.Invoke(asyncEnumerable, new object?[] { default(CancellationToken) });
        
        var moveNextMethod = enumerator!.GetType().GetMethod("MoveNextAsync");
        var currentProperty = enumerator.GetType().GetProperty("Current");
        
        while (true)
        {
            var moveNextTask = (ValueTask<bool>)moveNextMethod!.Invoke(enumerator, null)!;
            if (!await moveNextTask.ConfigureAwait(false))
                break;
                
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
    
    private static object?[] ConvertToObjectArray(object? item)
    {
        if (item == null)
            return new object?[] { null };
            
        if (item is object?[] objArray)
            return objArray;
            
        if (item.GetType().IsArray)
        {
            var array = (Array)item;
            var result = new object?[array.Length];
            array.CopyTo(result, 0);
            return result;
        }
        
        return new[] { item };
    }
}