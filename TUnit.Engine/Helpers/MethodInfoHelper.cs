using System.Diagnostics;
using System.Reflection;

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
}