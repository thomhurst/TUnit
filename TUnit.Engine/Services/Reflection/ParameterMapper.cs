using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Helpers;
using Polyfills;

namespace TUnit.Engine.Services.Reflection;

[UnconditionalSuppressMessage("Trimming", "IL2067")]
[UnconditionalSuppressMessage("Trimming", "IL2072")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
internal static class ParameterMapper
{
    public static void MapImplicitParameters(ref object?[] arguments, TestParameter[] parameters)
    {
        if (arguments.Length == parameters.Length)
        {
            HandleExactMatch(ref arguments, parameters);
            return;
        }

        if (parameters.Length == 0)
        {
            arguments = [];
            return;
        }

        if (arguments.Length < parameters.Length)
        {
            HandleMissingParameters(ref arguments, parameters);
            return;
        }

        if (arguments.Length > parameters.Length)
        {
            HandleExcessParameters(ref arguments, parameters);
        }
    }

    private static void HandleExactMatch(ref object?[] arguments, TestParameter[] parameters)
    {
        if (parameters.Length > 0 && parameters.Last().IsParams &&
            arguments.Length > 0 && arguments.Last() is not IEnumerable)
        {
            WrapLastArgumentInParamsArray(ref arguments, parameters.Last());
        }
    }

    private static void HandleMissingParameters(ref object?[] arguments, TestParameter[] parameters)
    {
        var missingParameters = parameters.Skip(arguments.Length).ToArray();

        if (missingParameters.All(x => x.IsOptional))
        {
            arguments = [..arguments, ..missingParameters.Select(x => x.DefaultValue)];
            return;
        }

        if (parameters.LastOrDefault()?.Type == typeof(CancellationToken) &&
            arguments.LastOrDefault() is not CancellationToken)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Not enough arguments provided to fulfil the parameters. Expected {parameters.Length}, but got {arguments.Length}.");
    }

    private static void HandleExcessParameters(ref object?[] arguments, TestParameter[] parameters)
    {
        var lastParameter = parameters.Last();

        if (lastParameter.IsParams)
        {
            ConvertToParamsArray(ref arguments, parameters, lastParameter);
            return;
        }

        arguments = arguments.Take(parameters.Length).ToArray();
    }

    private static void WrapLastArgumentInParamsArray(
        ref object?[] arguments, 
        TestParameter lastParameter)
    {
        var underlyingType = GetUnderlyingParamsType(lastParameter);
        var typedArray = Array.CreateInstance(underlyingType, 1);
        var value = CastHelper.Cast(underlyingType, arguments.Last());
        typedArray.SetValue(value, 0);

        arguments = [..arguments.Take(arguments.Length - 1), typedArray];
    }

    private static void ConvertToParamsArray(
        ref object?[] arguments,
        TestParameter[] parameters,
        TestParameter lastParameter)
    {
        var underlyingType = GetUnderlyingParamsType(lastParameter);
        var argumentsBeforeParams = arguments.Take(parameters.Length - 1).ToArray();
        var argumentsAfterParams = arguments.Skip(argumentsBeforeParams.Length).ToArray();

        if (argumentsAfterParams.All(x => x is null || IsConvertibleTo(x, underlyingType)))
        {
            var typedArray = Array.CreateInstance(underlyingType, argumentsAfterParams.Length);

            for (var i = 0; i < argumentsAfterParams.Length; i++)
            {
                typedArray.SetValue(CastHelper.Cast(underlyingType, argumentsAfterParams[i]), i);
            }

            arguments = [..argumentsBeforeParams, typedArray];
        }
    }

    private static Type GetUnderlyingParamsType(TestParameter parameter)
    {
        return parameter.Type.GetElementType() 
               ?? parameter.Type.GenericTypeArguments.FirstOrDefault() 
               ?? throw new InvalidOperationException(
                   "Cannot determine the underlying type of the params argument. Use an array to fix this.");
    }

    private static bool IsConvertibleTo(object x, Type targetType)
    {
#if NETSTANDARD2_0
        if (targetType.IsAssignableFrom(x.GetType()))
#else
        if (x.GetType().IsAssignableTo(targetType))
#endif
        {
            return true;
        }

        if (CastHelper.GetConversionMethod(x.GetType(), targetType) is not null)
        {
            return true;
        }

        try
        {
            _ = Convert.ChangeType(x, targetType);
            return true;
        }
        catch
        {
            return false;
        }
    }
}