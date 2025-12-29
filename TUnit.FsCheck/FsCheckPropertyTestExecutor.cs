using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using FsCheck;
using FsCheck.Fluent;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.FsCheck;

/// <summary>
/// A test executor that runs FsCheck property-based tests.
/// </summary>
#pragma warning disable IL2046 // RequiresUnreferencedCode attribute mismatch
#pragma warning disable IL3051 // RequiresDynamicCode attribute mismatch
#pragma warning disable IL2072 // DynamicallyAccessedMembers warning
public class FsCheckPropertyTestExecutor : ITestExecutor
{
    private readonly FsCheckPropertyAttribute _propertyAttribute;

    public FsCheckPropertyTestExecutor(FsCheckPropertyAttribute propertyAttribute)
    {
        _propertyAttribute = propertyAttribute;
    }

    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        var testDetails = context.Metadata.TestDetails;
        var classInstance = testDetails.ClassInstance;
        var classType = testDetails.ClassType;
        var methodName = testDetails.MethodName;

        // Get MethodInfo via reflection from the class type
        var methodInfo = GetMethodInfo(classType, methodName, testDetails.MethodMetadata.Parameters);

        var config = CreateConfig();

        RunPropertyCheck(methodInfo, classInstance, config);

        return default;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "FsCheck requires reflection")]
    private static MethodInfo GetMethodInfo(Type classType, string methodName, ParameterMetadata[] parameters)
    {
        // Try to find the method by name and parameter count
        var methods = classType
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => m.Name == methodName && m.GetParameters().Length == parameters.Length)
            .ToArray();

        if (methods.Length == 0)
        {
            throw new InvalidOperationException($"Could not find method '{methodName}' on type '{classType.FullName}'");
        }

        if (methods.Length == 1)
        {
            return methods[0];
        }

        // Multiple overloads - try to match by parameter types
        foreach (var method in methods)
        {
            var methodParams = method.GetParameters();
            var match = true;
            for (var i = 0; i < methodParams.Length; i++)
            {
                if (methodParams[i].ParameterType != parameters[i].Type)
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return method;
            }
        }

        // Just return the first one if no exact match
        return methods[0];
    }

    private Config CreateConfig()
    {
        var config = Config.QuickThrowOnFailure
            .WithMaxTest(_propertyAttribute.MaxTest)
            .WithMaxRejected(_propertyAttribute.MaxFail)
            .WithStartSize(_propertyAttribute.StartSize)
            .WithEndSize(_propertyAttribute.EndSize);

        if (!string.IsNullOrEmpty(_propertyAttribute.Replay))
        {
            var parts = _propertyAttribute.Replay!.Split(',');
            if (parts.Length >= 1 && ulong.TryParse(parts[0].Trim(), out var seed1))
            {
                var seed2 = parts.Length >= 2 && ulong.TryParse(parts[1].Trim(), out var s2) ? s2 : 0UL;
                config = config.WithReplay(seed1, seed2);
            }
        }

        if (_propertyAttribute.Arbitrary != null && _propertyAttribute.Arbitrary.Length > 0)
        {
            config = config.WithArbitrary(_propertyAttribute.Arbitrary);
        }

        return config;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "FsCheck requires reflection")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "FsCheck requires dynamic code")]
    private static void RunPropertyCheck(MethodInfo methodInfo, object classInstance, Config config)
    {
        var parameters = methodInfo.GetParameters();
        var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
        var parameterNames = parameters.Select(p => p.Name ?? "arg").ToArray();

        // Create the property based on parameter count
        switch (parameterTypes.Length)
        {
            case 0:
                RunPropertyWithNoParams(methodInfo, classInstance, config);
                break;
            case 1:
                RunPropertyWithParams1(methodInfo, classInstance, config, parameterTypes[0], parameterNames[0]);
                break;
            case 2:
                RunPropertyWithParams2(methodInfo, classInstance, config, parameterTypes[0], parameterTypes[1],
                    parameterNames);
                break;
            case 3:
                RunPropertyWithParams3(methodInfo, classInstance, config, parameterTypes[0], parameterTypes[1],
                    parameterTypes[2], parameterNames);
                break;
            default:
                throw new NotSupportedException(
                    $"FsCheck property tests with {parameterTypes.Length} parameters are not supported. Maximum is 3.");
        }
    }

    private static void RunPropertyWithNoParams(MethodInfo methodInfo, object classInstance, Config config)
    {
        object? result;
        try
        {
            result = methodInfo.Invoke(classInstance, null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // Unreachable
        }

        HandleVoidResult(result);
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "FsCheck requires dynamic code")]
    private static void RunPropertyWithParams1(MethodInfo methodInfo, object classInstance, Config config, Type type1,
        string paramName)
    {
        var genericMethod = typeof(FsCheckPropertyTestExecutor)
            .GetMethod(nameof(RunPropertyGeneric1), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type1);

        try
        {
            genericMethod.Invoke(null, [methodInfo, classInstance, config, paramName]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "FsCheck requires dynamic code")]
    private static void RunPropertyWithParams2(MethodInfo methodInfo, object classInstance, Config config, Type type1,
        Type type2, string[] paramNames)
    {
        var genericMethod = typeof(FsCheckPropertyTestExecutor)
            .GetMethod(nameof(RunPropertyGeneric2), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type1, type2);

        try
        {
            genericMethod.Invoke(null, [methodInfo, classInstance, config, paramNames]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "FsCheck requires dynamic code")]
    private static void RunPropertyWithParams3(MethodInfo methodInfo, object classInstance, Config config, Type type1,
        Type type2, Type type3, string[] paramNames)
    {
        var genericMethod = typeof(FsCheckPropertyTestExecutor)
            .GetMethod(nameof(RunPropertyGeneric3), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type1, type2, type3);

        try
        {
            genericMethod.Invoke(null, [methodInfo, classInstance, config, paramNames]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    private static void RunPropertyGeneric1<T>(MethodInfo methodInfo, object classInstance, Config config,
        string paramName)
    {
        T? failingValue = default;
        var hasFailed = false;

        try
        {
            Prop.ForAll<T>(arg =>
            {
                var result = methodInfo.Invoke(classInstance, [arg]);
                var passed = HandleResult(result);
                if (!passed && !hasFailed)
                {
                    failingValue = arg;
                    hasFailed = true;
                }

                return passed;
            }).Check(config);
        }
        catch (Exception ex)
        {
            throw new PropertyFailedException(
                FormatCounterexample(methodInfo.Name, [(paramName, failingValue)], ex));
        }
    }

    private static void RunPropertyGeneric2<T1, T2>(MethodInfo methodInfo, object classInstance, Config config,
        string[] paramNames)
    {
        T1? failingValue1 = default;
        T2? failingValue2 = default;
        var hasFailed = false;

        try
        {
            Prop.ForAll<T1, T2>((arg1, arg2) =>
            {
                var result = methodInfo.Invoke(classInstance, [arg1, arg2]);
                var passed = HandleResult(result);
                if (!passed && !hasFailed)
                {
                    failingValue1 = arg1;
                    failingValue2 = arg2;
                    hasFailed = true;
                }

                return passed;
            }).Check(config);
        }
        catch (Exception ex)
        {
            throw new PropertyFailedException(
                FormatCounterexample(methodInfo.Name, [(paramNames[0], failingValue1), (paramNames[1], failingValue2)],
                    ex));
        }
    }

    private static void RunPropertyGeneric3<T1, T2, T3>(MethodInfo methodInfo, object classInstance, Config config,
        string[] paramNames)
    {
        T1? failingValue1 = default;
        T2? failingValue2 = default;
        T3? failingValue3 = default;
        var hasFailed = false;

        try
        {
            Prop.ForAll<T1, T2, T3>((arg1, arg2, arg3) =>
            {
                var result = methodInfo.Invoke(classInstance, [arg1, arg2, arg3]);
                var passed = HandleResult(result);
                if (!passed && !hasFailed)
                {
                    failingValue1 = arg1;
                    failingValue2 = arg2;
                    failingValue3 = arg3;
                    hasFailed = true;
                }

                return passed;
            }).Check(config);
        }
        catch (Exception ex)
        {
            throw new PropertyFailedException(
                FormatCounterexample(methodInfo.Name,
                    [(paramNames[0], failingValue1), (paramNames[1], failingValue2), (paramNames[2], failingValue3)],
                    ex));
        }
    }

    private static string FormatCounterexample(string methodName, (string name, object? value)[] args, Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Property '{methodName}' failed with counterexample:");
        sb.AppendLine();

        // Unwrap TargetInvocationException to get to the actual FsCheck exception
        var innerEx = ex;
        while (innerEx is TargetInvocationException { InnerException: not null } tie)
        {
            innerEx = tie.InnerException;
        }

        // Try to extract shrunk values from FsCheck message
        var shrunkValues = TryParseShrunkValues(innerEx?.Message);

        // Display args, using shrunk values if available
        for (int i = 0; i < args.Length; i++)
        {
            var (name, value) = args[i];
            var displayValue = (shrunkValues != null && i < shrunkValues.Length)
                ? shrunkValues[i]
                : FormatValue(value);
            sb.AppendLine($"  {name} = {displayValue}");
        }

        // Append the FsCheck message for full details
        if (innerEx != null && !string.IsNullOrEmpty(innerEx.Message))
        {
            sb.AppendLine();
            sb.AppendLine("FsCheck output:");
            sb.AppendLine();
            // Indent each line of the FsCheck message
            foreach (var line in innerEx.Message.Split('\n'))
            {
                sb.Append("  ");
                sb.AppendLine(line.TrimEnd('\r'));
            }
        }

        return sb.ToString();
    }

    private static string[]? TryParseShrunkValues(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return null;

        // Look for "Shrunk:" followed by values on the next line
        var shrunkIndex = message!.IndexOf("Shrunk:", StringComparison.Ordinal);
        if (shrunkIndex < 0)
            return null;

        var afterShrunk = message[(shrunkIndex + 7)..].TrimStart();

        // Take only the first line
        var newlineIndex = afterShrunk.IndexOfAny(['\r', '\n']);
        var shrunkLine = newlineIndex >= 0 ? afterShrunk[..newlineIndex] : afterShrunk;

        if (shrunkLine.StartsWith('('))
        {
            return ParseTupleValues(shrunkLine);
        }
        else
        {
            // Single value (no brackets)
            return [shrunkLine.Trim()];
        }
    }

    private static string[]? ParseTupleValues(string tupleString)
    {
        if (!tupleString.StartsWith('('))
            return null;

        var values = new List<string>();
        var current = new StringBuilder();
        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = 1; i < tupleString.Length; i++)
        {
            var c = tupleString[i];

            if (escaped)
            {
                current.Append(c);
                escaped = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                current.Append(c);
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                current.Append(c);
                continue;
            }

            if (inString)
            {
                current.Append(c);
                continue;
            }

            switch (c)
            {
                case '(':
                    depth++;
                    current.Append(c);
                    break;
                case ')':
                    if (depth == 0)
                    {
                        if (current.Length > 0)
                            values.Add(current.ToString().Trim());
                        return values.ToArray();
                    }

                    depth--;
                    current.Append(c);
                    break;
                case ',' when depth == 0:
                    values.Add(current.ToString().Trim());
                    current.Clear();
                    break;
                default:
                    current.Append(c);
                    break;
            }
        }

        return values.ToArray();
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is string s)
        {
            return $"\"{s}\"";
        }

        if (value is char c)
        {
            return $"'{c}'";
        }

        if (value is Array arr)
        {
            var elements = new List<string>();
            foreach (var item in arr)
            {
                elements.Add(FormatValue(item));
                if (elements.Count > 10)
                {
                    elements.Add($"... ({arr.Length} total)");
                    break;
                }
            }
            return $"[{string.Join(", ", elements)}]";
        }

        return value.ToString() ?? "null";
    }

    private static void HandleVoidResult(object? result)
    {
        switch (result)
        {
            case Task task:
                task.GetAwaiter().GetResult();
                break;
            case ValueTask valueTask:
                valueTask.GetAwaiter().GetResult();
                break;
        }
    }

    private static bool HandleResult(object? result)
    {
        switch (result)
        {
            case Task<bool> taskBool:
                return taskBool.GetAwaiter().GetResult();
            case ValueTask<bool> valueTaskBool:
                return valueTaskBool.GetAwaiter().GetResult();
            case Task task:
                task.GetAwaiter().GetResult();
                return true;
            case ValueTask valueTask:
                valueTask.GetAwaiter().GetResult();
                return true;
            case bool boolResult:
                return boolResult;
            default:
                // Method returned void or non-boolean - assume success if no exception
                return true;
        }
    }
}
#pragma warning restore IL2046
#pragma warning restore IL3051
#pragma warning restore IL2072
