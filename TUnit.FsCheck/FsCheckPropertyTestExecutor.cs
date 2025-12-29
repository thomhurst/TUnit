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
    private static MethodInfo GetMethodInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        Type classType,
        string methodName,
        ParameterMetadata[] parameters)
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
        try
        {
            Check.Method(config, methodInfo, classInstance);
        }
        catch (Exception ex)
        {
            throw new PropertyFailedException(FormatCounterexample(methodInfo, ex));
        }
    }

    private static string FormatCounterexample(MethodInfo methodInfo, Exception ex)
    {
        var parameters = methodInfo.GetParameters();
        var args = parameters
            .Select((p, i) => p.Name ?? $"arg{i}")
            .ToArray();

        var methodName = methodInfo.Name;

        var sb = new StringBuilder();
        sb.AppendLine($"Property '{methodName}' failed with counterexample:");

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
            var name = args[i];
            var value = shrunkValues?[i];
            if (value != null)
            {
                sb.AppendLine($"  {name} = {value}");
            }
        }

        // Append the FsCheck message for full details
        if (innerEx != null && !string.IsNullOrEmpty(innerEx.Message))
        {
            sb.AppendLine();
            sb.AppendLine("FsCheck output:");
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
}
#pragma warning restore IL2046
#pragma warning restore IL3051
#pragma warning restore IL2072
