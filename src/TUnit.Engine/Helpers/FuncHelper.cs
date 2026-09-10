using System.Diagnostics.CodeAnalysis;

namespace TUnit.Engine.Helpers;

/// <summary>
/// Helper for invoking Func delegates using reflection.
/// Used in reflection mode to execute data source factories dynamically.
/// </summary>
internal class FuncHelper
{
    public static bool TryInvokeFunc(object? func, out object? result)
    {
        if (func is Delegate method)
        {
            result = method.DynamicInvoke();
            return true;
        }

        result = null;
        return false;
    }

    public static object? InvokeFunc(object func)
    {
        return ((Delegate)func).DynamicInvoke();
    }
}
