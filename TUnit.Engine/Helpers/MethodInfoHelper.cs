using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Helpers;

internal static class MethodInfoHelper
{
    public static object? InvokeStaticHook(this MethodInfo methodInfo, object context, CancellationToken cancellationToken)
    {
        List<object?> args = [];

        foreach (var parameterInfo in methodInfo.GetParameters())
        {
            if (parameterInfo.ParameterType == typeof(CancellationToken))
            {
                args.Add(cancellationToken);
            }

            if (parameterInfo.ParameterType == context?.GetType())
            {
                args.Add(context);
            }
        }

        return methodInfo.Invoke(null, args.ToArray());
    }
    
    public static object? InvokeInstanceHook(this MethodInfo methodInfo, object instance, TestContext context, CancellationToken cancellationToken)
    {
        List<object?> args = [];

        foreach (var parameterInfo in methodInfo.GetParameters())
        {
            if (parameterInfo.ParameterType == typeof(CancellationToken))
            {
                args.Add(cancellationToken);
            }

            if (parameterInfo.ParameterType == typeof(TestContext))
            {
                args.Add(context);
            }
        }

        return methodInfo.Invoke(instance, args.ToArray());
    }
}