using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core;

namespace TUnit.Engine;

internal class MethodInvoker
{
    public async Task<object?> InvokeMethod(object? @class, MethodInfo methodInfo, BindingFlags bindingFlags,
        object?[]? arguments, CancellationToken token)
    {
        try
        {
            if (methodInfo.GetCustomAttribute<TimeoutAttribute>() != null)
            {
                arguments = (arguments ?? Array.Empty<object?>()).Append(token).ToArray();
            }
            
            var result = methodInfo.Invoke(@class, bindingFlags, null, arguments, CultureInfo.InvariantCulture);

            if (result is ValueTask valueTask)
            { 
                await valueTask;
                
                if (valueTask.GetType().IsGenericType)
                {
                    return valueTask.GetType()
                        .GetProperty("Result", BindingFlags.Instance | BindingFlags.Public)
                        ?.GetValue(valueTask);
                }
                
                return null;
            }

            if (result is Task task)
            {
                await task;

                if (task.GetType().IsGenericType)
                {
                    return task.GetType()
                        .GetProperty("Result", BindingFlags.Instance | BindingFlags.Public)
                        ?.GetValue(task);
                }
                
                return null;
            }

            return result;
        }
        catch (TargetInvocationException e)
        {
            ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
            throw;
        }
    }
}