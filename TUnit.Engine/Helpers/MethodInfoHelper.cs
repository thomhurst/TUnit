using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using TUnit.Core;

namespace TUnit.Engine.Helpers;

[RequiresUnreferencedCode("Reflection")]
[RequiresDynamicCode("Reflection")]
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

        try
        {
            return methodInfo.Invoke(null, args.ToArray());
        }
        catch (TargetInvocationException targetInvocationException)
        {
            if (targetInvocationException.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(targetInvocationException.InnerException).Throw();
            }

            throw;
        }
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

        try
        {
            if (methodInfo.DeclaringType!.ContainsGenericParameters)
            {
                return instance.GetType()
                    .GetMembers()
                    .OfType<MethodInfo>()
                    .First(x => x.Name == methodInfo.Name
                        && x.GetParameters().Length == methodInfo.GetParameters().Length)
                    .Invoke(instance, args.ToArray());
            }

            return methodInfo.Invoke(instance, args.ToArray());
        }
        catch (TargetInvocationException targetInvocationException)
        {
            if (targetInvocationException.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(targetInvocationException.InnerException).Throw();
            }

            throw;
        }
    }
}
