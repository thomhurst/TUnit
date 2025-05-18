using System.Diagnostics.CodeAnalysis;

namespace TUnit.Engine.Helpers;

[UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
internal class FuncHelper
{
    public static bool TryInvokeFunc(object? func, out object? result)
    {
        if (func?.GetType().GetMethod("Invoke") is {} method)
        {
            result = method.Invoke(func, []);
            return true;
        }

        result = null;
        return false;
    }
    
    public static object? InvokeFunc(object func)
    {
        return func.GetType().GetMethod("Invoke")!.Invoke(func, []);
    }
}