using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

/// <summary>
/// Generates strongly-typed delegates for test invocation, eliminating boxing/unboxing
/// </summary>
public class StronglyTypedDelegateGenerator
{
    private readonly StringBuilder _stringBuilder = new();

    public string GenerateTestDelegates(List<TestMethodMetadata> testMethods, DiagnosticContext? diagnosticContext = null)
    {
        _stringBuilder.Clear();

        _stringBuilder.AppendLine("using System;");
        _stringBuilder.AppendLine("using System.Threading.Tasks;");
        _stringBuilder.AppendLine("using TUnit.Core;");
        _stringBuilder.AppendLine();
        _stringBuilder.AppendLine("namespace TUnit.Generated;");
        _stringBuilder.AppendLine();
        _stringBuilder.AppendLine("/// <summary>");
        _stringBuilder.AppendLine("/// Strongly-typed delegates for test invocation without boxing");
        _stringBuilder.AppendLine("/// </summary>");
        _stringBuilder.AppendLine("internal static class StronglyTypedTestDelegates");
        _stringBuilder.AppendLine("{");

        var processedMethods = new HashSet<string>();

        foreach (var testMethod in testMethods)
        {
            var methodKey = GetMethodKey(testMethod);
            if (processedMethods.Contains(methodKey))
                continue;

            processedMethods.Add(methodKey);
            GenerateTestDelegate(testMethod, diagnosticContext);
        }

        _stringBuilder.AppendLine("}");
        return _stringBuilder.ToString();
    }

    private void GenerateTestDelegate(TestMethodMetadata testMethod, DiagnosticContext? diagnosticContext)
    {
        // Skip generic methods - they can't have strongly-typed delegates generated at compile time
        if (testMethod.MethodSymbol.IsGenericMethod)
        {
            return;
        }

        var className = testMethod.TypeSymbol.ToDisplayString();
        var methodName = testMethod.MethodSymbol.Name;
        var safeClassName = testMethod.TypeSymbol.Name.Replace(".", "_").Replace("<", "_").Replace(">", "_");
        var safeMethodName = methodName.Replace(".", "_");

        var parameters = testMethod.MethodSymbol.Parameters;
        var returnType = testMethod.MethodSymbol.ReturnType;

        // Determine if method is async
        var isAsync = IsAsyncMethod(returnType);
        var returnsValueTask = ReturnsValueTask(returnType);

        // Generate delegate type
        var delegateTypeName = $"{safeClassName}_{safeMethodName}_Delegate";

        // Build parameter list for delegate
        var parameterTypes = new List<string> { className }; // Instance type first
        parameterTypes.AddRange(parameters.Select(p => p.Type.ToDisplayString()));

        var returnTypeName = isAsync ? "Task" : "void";
        if (isAsync && !returnType.Name.StartsWith("Task") && !returnType.Name.StartsWith("ValueTask"))
        {
            // Handle Task<T> and ValueTask<T>
            if (returnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
            {
                returnTypeName = $"Task<{namedType.TypeArguments[0].ToDisplayString()}>";
            }
        }

        // Generate delegate type declaration
        _stringBuilder.AppendLine($"    /// <summary>");
        _stringBuilder.AppendLine($"    /// Strongly-typed delegate for {className}.{methodName}");
        _stringBuilder.AppendLine($"    /// </summary>");
        _stringBuilder.Append($"    public delegate {returnTypeName} {delegateTypeName}(");
        _stringBuilder.Append(string.Join(", ", parameterTypes.Select((type, index) =>
            index == 0 ? $"{type} instance" : $"{type} arg{index}")));
        _stringBuilder.AppendLine(");");
        _stringBuilder.AppendLine();

        // Generate static delegate instance
        var delegateName = $"{safeClassName}_{safeMethodName}";
        _stringBuilder.AppendLine($"    /// <summary>");
        _stringBuilder.AppendLine($"    /// Strongly-typed delegate instance for {className}.{methodName}");
        _stringBuilder.AppendLine($"    /// </summary>");
        _stringBuilder.Append($"    public static readonly {delegateTypeName} {delegateName} = ");

        if (parameters.Length == 0)
        {
            // No parameters
            if (isAsync)
            {
                if (returnsValueTask)
                {
                    _stringBuilder.AppendLine($"async instance => await instance.{methodName}().AsTask();");
                }
                else
                {
                    _stringBuilder.AppendLine($"async instance => await instance.{methodName}();");
                }
            }
            else
            {
                _stringBuilder.AppendLine($"instance => instance.{methodName}();");
            }
        }
        else
        {
            // With parameters
            var argList = string.Join(", ", parameters.Select((_, index) => $"arg{index + 1}"));

            if (isAsync)
            {
                if (returnsValueTask)
                {
                    _stringBuilder.AppendLine($"async (instance, {argList}) => await instance.{methodName}({argList}).AsTask();");
                }
                else
                {
                    _stringBuilder.AppendLine($"async (instance, {argList}) => await instance.{methodName}({argList});");
                }
            }
            else
            {
                _stringBuilder.AppendLine($"(instance, {argList}) => instance.{methodName}({argList});");
            }
        }

        _stringBuilder.AppendLine();

        // Generate instance factory delegate if needed
        GenerateInstanceFactoryDelegate(testMethod);

        // Register delegate with storage
        var parameterTypesList = string.Join(", ", parameters.Select(p => $"typeof({p.Type.ToDisplayString()})"));
        _stringBuilder.AppendLine($"    static {safeClassName}_{safeMethodName}()");
        _stringBuilder.AppendLine("    {");
        _stringBuilder.AppendLine($"        TestDelegateStorage.RegisterStronglyTypedDelegate(");
        _stringBuilder.AppendLine($"            \"{className}.{methodName}\",");
        _stringBuilder.AppendLine($"            new[] {{ {parameterTypesList} }},");
        _stringBuilder.AppendLine($"            {delegateName});");
        _stringBuilder.AppendLine("    }");
        _stringBuilder.AppendLine();
    }

    private void GenerateInstanceFactoryDelegate(TestMethodMetadata testMethod)
    {
        var className = testMethod.TypeSymbol.ToDisplayString();
        var safeClassName = testMethod.TypeSymbol.Name.Replace(".", "_").Replace("<", "_").Replace(">", "_");

        // Find public constructors
        var constructors = testMethod.TypeSymbol.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .ToList();

        var defaultConstructor = constructors.FirstOrDefault(c => c.Parameters.Length == 0);
        var parameterizedConstructor = constructors.FirstOrDefault(c => c.Parameters.Length > 0);

        if (defaultConstructor != null)
        {
            // Generate simple factory for parameterless constructor
            _stringBuilder.AppendLine($"    /// <summary>");
            _stringBuilder.AppendLine($"    /// Instance factory for {className} (parameterless constructor)");
            _stringBuilder.AppendLine($"    /// </summary>");
            _stringBuilder.AppendLine($"    public static readonly Func<{className}> {safeClassName}_Factory = () => new {className}();");
            _stringBuilder.AppendLine();
        }

        if (parameterizedConstructor != null)
        {
            // Generate typed factory for parameterized constructor
            var parameters = parameterizedConstructor.Parameters;
            var parameterTypes = parameters.Select(p => p.Type.ToDisplayString()).ToList();
            var factoryTypeName = $"Func<{string.Join(", ", parameterTypes)}, {className}>";

            _stringBuilder.AppendLine($"    /// <summary>");
            _stringBuilder.AppendLine($"    /// Instance factory for {className} (parameterized constructor)");
            _stringBuilder.AppendLine($"    /// </summary>");
            _stringBuilder.Append($"    public static readonly {factoryTypeName} {safeClassName}_ParameterizedFactory = ");

            var argList = string.Join(", ", parameters.Select((p, index) => $"arg{index}"));
            var paramList = string.Join(", ", parameters.Select((p, index) => $"{p.Type.ToDisplayString()} arg{index}"));

            _stringBuilder.AppendLine($"({paramList}) => new {className}({argList});");
            _stringBuilder.AppendLine();
        }
    }

    private bool IsAsyncMethod(ITypeSymbol returnType)
    {
        var typeName = returnType.Name;
        return typeName == "Task" || typeName == "ValueTask" ||
               typeName.StartsWith("Task`") || typeName.StartsWith("ValueTask`");
    }

    private bool ReturnsValueTask(ITypeSymbol returnType)
    {
        var typeName = returnType.Name;
        return typeName == "ValueTask" || typeName.StartsWith("ValueTask`");
    }

    private string GetMethodKey(TestMethodMetadata testMethod)
    {
        var className = testMethod.TypeSymbol.ToDisplayString();
        var methodName = testMethod.MethodSymbol.Name;
        var parameterTypes = string.Join(",", testMethod.MethodSymbol.Parameters.Select(p => p.Type.ToDisplayString()));
        return $"{className}.{methodName}({parameterTypes})";
    }
}
