using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Assertions.SourceGenerator.Generators;

/// <summary>
/// Source generator that creates assertion infrastructure from methods decorated with [GenerateAssertion].
/// Generates:
/// - Assertion&lt;T&gt; classes containing the assertion logic
/// - Extension methods on IAssertionSource&lt;T&gt; that construct the assertions
/// </summary>
[Generator]
public sealed class MethodAssertionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all methods decorated with [GenerateAssertion]
        var assertionMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Assertions.Attributes.GenerateAssertionAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, ct) => GetAssertionMethodData(ctx, ct))
            .Where(static x => x != null)
            .Select(static (x, _) => x!);

        // Generate assertion classes and extension methods
        context.RegisterSourceOutput(assertionMethods.Collect(), static (context, methods) =>
        {
            GenerateAssertions(context, methods);
        });
    }

    private static AssertionMethodData? GetAssertionMethodData(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        // Validate method is static
        if (!methodSymbol.IsStatic)
        {
            // TODO: Report diagnostic - method must be static
            return null;
        }

        // Validate method has at least one parameter
        if (methodSymbol.Parameters.Length == 0)
        {
            // TODO: Report diagnostic - method must have at least one parameter
            return null;
        }

        // Get return type info
        var returnTypeInfo = AnalyzeReturnType(methodSymbol.ReturnType);
        if (returnTypeInfo == null)
        {
            // TODO: Report diagnostic - unsupported return type
            return null;
        }

        // First parameter is the target type (what becomes IAssertionSource<T>)
        var targetType = methodSymbol.Parameters[0].Type;
        var additionalParameters = methodSymbol.Parameters.Skip(1).ToImmutableArray();

        // Check if it's an extension method
        var isExtensionMethod = methodSymbol.IsExtensionMethod ||
                               (methodSymbol.Parameters.Length > 0 && methodSymbol.Parameters[0].IsThis);

        // Extract custom expectation message if provided
        string? customExpectation = null;
        var attribute = context.Attributes.FirstOrDefault();
        if (attribute != null)
        {
            foreach (var namedArg in attribute.NamedArguments)
            {
                if (namedArg.Key == "ExpectationMessage" && namedArg.Value.Value is string expectation)
                {
                    customExpectation = expectation;
                    break;
                }
            }
        }

        return new AssertionMethodData(
            methodSymbol,
            targetType,
            additionalParameters,
            returnTypeInfo.Value,
            isExtensionMethod,
            customExpectation
        );
    }

    private static ReturnTypeInfo? AnalyzeReturnType(ITypeSymbol returnType)
    {
        // Handle Task<T>
        if (returnType is INamedTypeSymbol namedType)
        {
            if (namedType.Name == "Task" &&
                namedType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks")
            {
                if (namedType.TypeArguments.Length == 1)
                {
                    var innerType = namedType.TypeArguments[0];

                    // Task<bool>
                    if (innerType.SpecialType == SpecialType.System_Boolean)
                    {
                        return new ReturnTypeInfo(ReturnTypeKind.TaskBool, innerType);
                    }

                    // Task<AssertionResult>
                    if (innerType.Name == "AssertionResult" &&
                        innerType.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions.Core")
                    {
                        return new ReturnTypeInfo(ReturnTypeKind.TaskAssertionResult, innerType);
                    }
                }
            }

            // AssertionResult
            if (namedType.Name == "AssertionResult" &&
                namedType.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions.Core")
            {
                return new ReturnTypeInfo(ReturnTypeKind.AssertionResult, namedType);
            }
        }

        // bool
        if (returnType.SpecialType == SpecialType.System_Boolean)
        {
            return new ReturnTypeInfo(ReturnTypeKind.Bool, returnType);
        }

        return null;
    }

    private static void GenerateAssertions(
        SourceProductionContext context,
        ImmutableArray<AssertionMethodData> methods)
    {
        if (methods.IsEmpty)
        {
            return;
        }

        // Group by containing class to generate one file per class
        foreach (var methodGroup in methods.GroupBy(m => m.Method.ContainingType, SymbolEqualityComparer.Default))
        {
            var containingType = methodGroup.Key as INamedTypeSymbol;
            if (containingType == null)
            {
                continue;
            }

            var sourceBuilder = new StringBuilder();
            // Always generate extension methods in TUnit.Assertions.Extensions namespace
            // so they're available via implicit usings in consuming projects
            var namespaceName = "TUnit.Assertions.Extensions";

            // Get the original namespace where the helper methods are defined
            var originalNamespace = containingType.ContainingNamespace?.ToDisplayString();

            // File header
            sourceBuilder.AppendLine("#nullable enable");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
            sourceBuilder.AppendLine("using System.Threading.Tasks;");
            sourceBuilder.AppendLine("using TUnit.Assertions.Core;");

            // Add using for the original namespace to access helper methods
            if (!string.IsNullOrEmpty(originalNamespace) && originalNamespace != namespaceName)
            {
                sourceBuilder.AppendLine($"using {originalNamespace};");
            }

            sourceBuilder.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sourceBuilder.AppendLine($"namespace {namespaceName};");
                sourceBuilder.AppendLine();
            }

            // Generate assertion classes
            foreach (var methodData in methodGroup)
            {
                GenerateAssertionClass(sourceBuilder, methodData);
                sourceBuilder.AppendLine();
            }

            // Generate extension methods in a partial class
            var extensionClassName = containingType.Name;
            sourceBuilder.AppendLine($"public static partial class {extensionClassName}");
            sourceBuilder.AppendLine("{");

            foreach (var methodData in methodGroup)
            {
                GenerateExtensionMethod(sourceBuilder, methodData);
                sourceBuilder.AppendLine();
            }

            sourceBuilder.AppendLine("}");

            // Add source to compilation
            var fileName = $"{containingType.Name}.GeneratedAssertions.g.cs";
            context.AddSource(fileName, sourceBuilder.ToString());
        }
    }

    private static void GenerateAssertionClass(StringBuilder sb, AssertionMethodData data)
    {
        var className = GenerateClassName(data);
        var targetTypeName = data.TargetType.ToDisplayString();
        var genericParams = GetGenericTypeParameters(data.TargetType, data.Method);
        var genericDeclaration = genericParams.Length > 0 ? $"<{string.Join(", ", genericParams)}>" : "";
        var isNullable = data.TargetType.IsReferenceType || data.TargetType.NullableAnnotation == NullableAnnotation.Annotated;

        // Class declaration
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Generated assertion for {data.Method.Name}");
        sb.AppendLine($"/// </summary>");

        // Add suppression for generic types to avoid trimming warnings
        if (genericParams.Length > 0)
        {
            sb.AppendLine($"[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2091\", Justification = \"Generic type parameter is only used for property access, not instantiation\")]");
        }

        sb.AppendLine($"public sealed class {className}{genericDeclaration} : Assertion<{targetTypeName}>");
        sb.AppendLine("{");

        // Private fields for additional parameters
        foreach (var param in data.AdditionalParameters)
        {
            sb.AppendLine($"    private readonly {param.Type.ToDisplayString()} _{param.Name};");
        }

        if (data.AdditionalParameters.Length > 0)
        {
            sb.AppendLine();
        }

        // Constructor
        sb.Append($"    public {className}(AssertionContext<{targetTypeName}> context");
        foreach (var param in data.AdditionalParameters)
        {
            sb.Append($", {param.Type.ToDisplayString()} {param.Name}");
        }
        sb.AppendLine(")");
        sb.AppendLine("        : base(context)");
        sb.AppendLine("    {");
        foreach (var param in data.AdditionalParameters)
        {
            sb.AppendLine($"        _{param.Name} = {param.Name};");
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        // CheckAsync method - only async if we need await
        var needsAsync = data.ReturnTypeInfo.Kind == ReturnTypeKind.TaskBool ||
                        data.ReturnTypeInfo.Kind == ReturnTypeKind.TaskAssertionResult;
        var asyncKeyword = needsAsync ? "async " : "";

        sb.AppendLine($"    protected override {asyncKeyword}Task<AssertionResult> CheckAsync(EvaluationMetadata<{targetTypeName}> metadata)");
        sb.AppendLine("    {");
        sb.AppendLine("        var value = metadata.Value;");
        sb.AppendLine("        var exception = metadata.Exception;");
        sb.AppendLine();
        sb.AppendLine("        if (exception != null)");
        sb.AppendLine("        {");
        sb.AppendLine($"            return {(needsAsync ? "" : "Task.FromResult(")}AssertionResult.Failed($\"threw {{exception.GetType().FullName}}\"){(needsAsync ? "" : ")")};");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Null check for reference types
        if (isNullable)
        {
            sb.AppendLine("        if (value is null)");
            sb.AppendLine("        {");
            sb.AppendLine($"            return {(needsAsync ? "" : "Task.FromResult(")}AssertionResult.Failed(\"Actual value is null\"){(needsAsync ? "" : ")")};");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // Call the user's method based on return type
        GenerateMethodCall(sb, data);

        sb.AppendLine("    }");
        sb.AppendLine();

        // GetExpectation method
        sb.AppendLine("    protected override string GetExpectation()");
        sb.AppendLine("    {");

        if (!string.IsNullOrEmpty(data.CustomExpectation))
        {
            // Use custom expectation message
            // Replace parameter placeholders like {param} with {_param} (field references)
            var expectation = data.CustomExpectation;
            if (data.AdditionalParameters.Length > 0)
            {
                // Replace each parameter placeholder {paramName} with {_paramName}
                foreach (var param in data.AdditionalParameters)
                {
                    var paramName = param.Name;
                    if (!string.IsNullOrEmpty(paramName))
                    {
                        expectation = expectation!.Replace($"{{{paramName}}}", $"{{_{paramName}}}");
                    }
                }
                // Use interpolated string for parameter substitution
                sb.AppendLine($"        return $\"{expectation}\";");
            }
            else
            {
                // No parameters, just return the literal string
                sb.AppendLine($"        return \"{expectation}\";");
            }
        }
        else
        {
            // Use default expectation message
            if (data.AdditionalParameters.Length > 0)
            {
                var paramList = string.Join(", ", data.AdditionalParameters.Select(p => $"{{_{p.Name}}}"));
                sb.AppendLine($"        return $\"to satisfy {data.Method.Name}({paramList})\";");
            }
            else
            {
                sb.AppendLine($"        return \"to satisfy {data.Method.Name}\";");
            }
        }

        sb.AppendLine("    }");

        sb.AppendLine("}");
    }

    private static void GenerateMethodCall(StringBuilder sb, AssertionMethodData data)
    {
        var methodCall = BuildMethodCallExpression(data);

        switch (data.ReturnTypeInfo.Kind)
        {
            case ReturnTypeKind.Bool:
                sb.AppendLine($"        var result = {methodCall};");
                sb.AppendLine("        return Task.FromResult(result");
                sb.AppendLine("            ? AssertionResult.Passed");
                sb.AppendLine("            : AssertionResult.Failed($\"found {value}\"));");
                break;

            case ReturnTypeKind.AssertionResult:
                sb.AppendLine($"        return Task.FromResult({methodCall});");
                break;

            case ReturnTypeKind.TaskBool:
                sb.AppendLine($"        var result = await {methodCall};");
                sb.AppendLine("        return result");
                sb.AppendLine("            ? AssertionResult.Passed");
                sb.AppendLine("            : AssertionResult.Failed($\"found {value}\");");
                break;

            case ReturnTypeKind.TaskAssertionResult:
                sb.AppendLine($"        return await {methodCall};");
                break;
        }
    }

    private static string BuildMethodCallExpression(AssertionMethodData data)
    {
        var containingType = data.Method.ContainingType.ToDisplayString();
        var methodName = data.Method.Name;

        if (data.IsExtensionMethod)
        {
            // Extension method syntax: value.MethodName(params)
            var paramList = string.Join(", ", data.AdditionalParameters.Select(p => $"_{p.Name}"));
            return $"value.{methodName}({paramList})";
        }
        else
        {
            // Static method syntax: ContainingType.MethodName(value, params)
            var allParams = new List<string> { "value" };
            allParams.AddRange(data.AdditionalParameters.Select(p => $"_{p.Name}"));
            var paramList = string.Join(", ", allParams);
            return $"{containingType}.{methodName}({paramList})";
        }
    }

    private static void GenerateExtensionMethod(StringBuilder sb, AssertionMethodData data)
    {
        var className = GenerateClassName(data);
        var targetTypeName = data.TargetType.ToDisplayString();
        var methodName = data.Method.Name;
        var genericParams = GetGenericTypeParameters(data.TargetType, data.Method);
        var genericDeclaration = genericParams.Length > 0 ? $"<{string.Join(", ", genericParams)}>" : "";

        // XML documentation
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Generated extension method for {methodName}");
        sb.AppendLine("    /// </summary>");

        // Add suppression for generic types to avoid trimming warnings
        if (genericParams.Length > 0)
        {
            sb.AppendLine($"    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2091\", Justification = \"Generic type parameter is only used for property access, not instantiation\")]");
        }

        // Method signature
        sb.Append($"    public static {className}{genericDeclaration} {methodName}{genericDeclaration}(");
        sb.Append($"this IAssertionSource<{targetTypeName}> source");

        // Additional parameters
        foreach (var param in data.AdditionalParameters)
        {
            sb.Append($", {param.Type.ToDisplayString()} {param.Name}");
        }

        // CallerArgumentExpression parameters
        for (int i = 0; i < data.AdditionalParameters.Length; i++)
        {
            var param = data.AdditionalParameters[i];
            sb.Append($", [CallerArgumentExpression(nameof({param.Name}))] string? {param.Name}Expression = null");
        }

        sb.AppendLine(")");
        sb.AppendLine("    {");

        // Build expression string
        if (data.AdditionalParameters.Length > 0)
        {
            var exprList = string.Join(", ", data.AdditionalParameters.Select(p => $"{{{p.Name}Expression}}"));
            sb.AppendLine($"        source.Context.ExpressionBuilder.Append($\".{methodName}({exprList})\");");
        }
        else
        {
            sb.AppendLine($"        source.Context.ExpressionBuilder.Append(\".{methodName}()\");");
        }

        // Construct and return assertion
        sb.Append($"        return new {className}{genericDeclaration}(source.Context");
        foreach (var param in data.AdditionalParameters)
        {
            sb.Append($", {param.Name}");
        }
        sb.AppendLine(");");

        sb.AppendLine("    }");
    }

    private static string GenerateClassName(AssertionMethodData data)
    {
        var methodName = data.Method.Name;
        var targetTypeName = GetSimpleTypeName(data.TargetType);

        if (data.AdditionalParameters.Length == 0)
        {
            return $"{targetTypeName}_{methodName}_Assertion";
        }

        // Include parameter types to distinguish overloads
        var paramTypes = string.Join("_", data.AdditionalParameters.Select(p => GetSimpleTypeName(p.Type)));
        return $"{targetTypeName}_{methodName}_{paramTypes}_Assertion";
    }

    private static string[] GetGenericTypeParameters(ITypeSymbol type, IMethodSymbol method)
    {
        // For extension methods, if the method has generic parameters, those define ALL the type parameters
        // (including any used in the target type like Lazy<T> or T[])
        if (method != null && method.IsGenericMethod)
        {
            return method.TypeParameters.Select(t => t.Name).ToArray();
        }

        // If the method is not generic, check if the type itself has unbound generic parameters
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            return namedType.TypeArguments
                .OfType<ITypeParameterSymbol>()
                .Select(t => t.Name)
                .ToArray();
        }

        return Array.Empty<string>();
    }

    private static string GetSimpleTypeName(ITypeSymbol type)
    {
        // Handle special types first
        var simpleName = type.SpecialType switch
        {
            SpecialType.System_Boolean => "Bool",
            SpecialType.System_Char => "Char",
            SpecialType.System_String => "String",
            SpecialType.System_Int32 => "Int",
            SpecialType.System_Int64 => "Long",
            SpecialType.System_Double => "Double",
            SpecialType.System_Single => "Float",
            SpecialType.System_Decimal => "Decimal",
            _ => type.Name
        };

        // Handle generic types: Lazy<T> becomes LazyT
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeParams = string.Join("", namedType.TypeArguments.Select(t =>
            {
                // For generic type parameters like T, just use the name
                if (t is ITypeParameterSymbol)
                {
                    return t.Name;
                }
                // For concrete types, get their simple name recursively
                return GetSimpleTypeName(t);
            }));
            return $"{simpleName}{typeParams}";
        }

        return simpleName;
    }

    private enum ReturnTypeKind
    {
        Bool,
        AssertionResult,
        TaskBool,
        TaskAssertionResult
    }

    private readonly record struct ReturnTypeInfo(ReturnTypeKind Kind, ITypeSymbol Type);

    private record AssertionMethodData(
        IMethodSymbol Method,
        ITypeSymbol TargetType,
        ImmutableArray<IParameterSymbol> AdditionalParameters,
        ReturnTypeInfo ReturnTypeInfo,
        bool IsExtensionMethod,
        string? CustomExpectation
    );
}
