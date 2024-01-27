using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace TUnit.Engine;

public class MethodInvoker
{
    public async Task InvokeMethod(object? @class, MethodInfo methodInfo, BindingFlags bindingFlags, object?[]? arguments)
    {
        try
        {
            var result = await Task.Run(() => methodInfo.Invoke(@class, bindingFlags, null, arguments, CultureInfo.InvariantCulture));

            if (result is ValueTask valueTask)
            { 
                await valueTask;
            }
            else if (result is Task task)
            {
                await task;
            }
        }
        catch (TargetInvocationException e)
        {
            if (e.InnerException is null)
            {
                throw;
            }
            
            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
        }
    }
}