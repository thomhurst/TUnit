using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Generates AOT-compatible method invocation code to replace MethodInfo.Invoke calls
/// </summary>
[Generator]
public sealed class AotMethodInvocationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all method data source attributes and methods that need invocation
        var methodDataSources = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsMethodDataSourceUsage(node),
                transform: (ctx, _) => ExtractMethodDataSourceInfo(ctx))
            .Where(x => x is not null)
            .Select((x, _) => x!);

        // Find all MethodInfo.Invoke usage
        var methodInvocations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsMethodInfoInvocation(node),
                transform: (ctx, _) => ExtractMethodInvocationInfo(ctx))
            .Where(x => x is not null)
            .Select((x, _) => x!);

        // Combine all method invocation requirements
        var allMethodInfo = methodDataSources
            .Collect()
            .Combine(methodInvocations.Collect());

        // Generate the method invocation helpers
        context.RegisterSourceOutput(allMethodInfo, GenerateMethodInvokers);
    }

    private static bool IsMethodDataSourceUsage(SyntaxNode node)
    {
        // Look for MethodDataSourceAttribute usage
        if (node is AttributeSyntax attribute)
        {
            var name = attribute.Name.ToString();
            return name.Contains("MethodDataSource") || name.Contains("MethodDataSourceAttribute");
        }

        // Look for method declarations that could be data sources
        if (node is MethodDeclarationSyntax method)
        {
            // Check if method returns IEnumerable or similar
            var returnType = method.ReturnType?.ToString();
            if (returnType != null && (
                returnType.Contains("IEnumerable") ||
                returnType.Contains("IAsyncEnumerable") ||
                returnType.Contains("Task<IEnumerable") ||
                returnType.Contains("Task<IAsyncEnumerable")))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMethodInfoInvocation(SyntaxNode node)
    {
        if (node is InvocationExpressionSyntax invocation)
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            return memberAccess?.Name.Identifier.ValueText == "Invoke" &&
                   memberAccess.Expression.ToString().Contains("MethodInfo");
        }

        return false;
    }

    private static MethodDataSourceInfo? ExtractMethodDataSourceInfo(GeneratorSyntaxContext context)
    {
        var semanticModel = context.SemanticModel;

        if (context.Node is AttributeSyntax attribute)
        {
            return ExtractFromAttribute(attribute, semanticModel);
        }
        else if (context.Node is MethodDeclarationSyntax method)
        {
            return ExtractFromMethod(method, semanticModel);
        }

        return null;
    }

    private static MethodInvocationInfo? ExtractMethodInvocationInfo(GeneratorSyntaxContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return null;
        }

        // Get the MethodInfo being invoked
        var methodInfoExpression = memberAccess.Expression;
        var methodInfoType = semanticModel.GetTypeInfo(methodInfoExpression).Type;

        if (methodInfoType?.Name != "MethodInfo")
        {
            return null;
        }

        // Try to extract the method being invoked
        var targetMethod = ExtractTargetMethod(methodInfoExpression, semanticModel);
        if (targetMethod == null)
        {
            return null;
        }

        return new MethodInvocationInfo
        {
            TargetMethod = targetMethod,
            Location = invocation.GetLocation(),
            InvocationExpression = invocation
        };
    }

    private static MethodDataSourceInfo? ExtractFromAttribute(AttributeSyntax attribute, SemanticModel semanticModel)
    {
        var attributeSymbol = semanticModel.GetSymbolInfo(attribute).Symbol as IMethodSymbol;
        var attributeType = attributeSymbol?.ContainingType;

        if (attributeType?.Name != "MethodDataSourceAttribute")
        {
            return null;
        }

        // Extract method name from attribute arguments
        string? methodName = null;
        if (attribute.ArgumentList?.Arguments.Count > 0)
        {
            var firstArg = attribute.ArgumentList.Arguments[0];
            if (firstArg.Expression is LiteralExpressionSyntax literal)
            {
                methodName = literal.Token.ValueText;
            }
        }

        if (string.IsNullOrEmpty(methodName))
        {
            return null;
        }

        // Find the method in the containing type
        var containingClass = attribute.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (containingClass == null)
        {
            return null;
        }

        var classSymbol = semanticModel.GetDeclaredSymbol(containingClass) as INamedTypeSymbol;
        var targetMethod = classSymbol?.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault();

        if (targetMethod == null)
        {
            return null;
        }

        // Only include publicly accessible methods for AOT compatibility
        if (!IsAccessibleMethod(targetMethod))
        {
            return null;
        }

        return new MethodDataSourceInfo
        {
            TargetMethod = targetMethod,
            Location = attribute.GetLocation(),
            Usage = MethodUsage.DataSource
        };
    }

    private static MethodDataSourceInfo? ExtractFromMethod(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        if (semanticModel.GetDeclaredSymbol(method) is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        // Check if this method could be used as a data source
        var returnType = methodSymbol.ReturnType;
        if (!IsDataSourceReturnType(returnType))
        {
            return null;
        }

        // Only include publicly accessible methods for AOT compatibility
        if (!IsAccessibleMethod(methodSymbol))
        {
            return null;
        }

        return new MethodDataSourceInfo
        {
            TargetMethod = methodSymbol,
            Location = method.GetLocation(),
            Usage = MethodUsage.DataSource
        };
    }

    private static IMethodSymbol? ExtractTargetMethod(ExpressionSyntax methodInfoExpression, SemanticModel semanticModel)
    {
        // Try to extract method from common patterns:
        // - typeof(Class).GetMethod("MethodName")
        // - instance.GetType().GetMethod("MethodName")
        
        if (methodInfoExpression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetMethod" } memberAccess } invocation)
        {
            // Extract method name from GetMethod call
            if (invocation.ArgumentList.Arguments.Count > 0 &&
                invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal)
            {
                var methodName = literal.Token.ValueText;
                
                // Try to resolve the type
                ITypeSymbol? targetType = null;
                
                if (memberAccess.Expression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "typeof" } } typeofInvocation)
                {
                    // typeof(Class).GetMethod pattern
                    if (typeofInvocation.ArgumentList.Arguments.Count > 0)
                    {
                        targetType = semanticModel.GetTypeInfo(typeofInvocation.ArgumentList.Arguments[0].Expression).Type;
                    }
                }
                else
                {
                    // instance.GetType().GetMethod pattern
                    var getTypeInvocation = memberAccess.Expression as InvocationExpressionSyntax;
                    if (getTypeInvocation?.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetType" } getTypeMember)
                    {
                        targetType = semanticModel.GetTypeInfo(getTypeMember.Expression).Type;
                    }
                }
                
                if (targetType != null)
                {
                    var method = targetType.GetMembers(methodName)
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault();
                        
                    // Only return publicly accessible methods
                    if (method != null && IsAccessibleMethod(method))
                    {
                        return method;
                    }
                }
            }
        }

        return null;
    }

    private static bool IsDataSourceReturnType(ITypeSymbol returnType)
    {
        var typeName = returnType.ToDisplayString();
        return typeName.Contains("IEnumerable") ||
               typeName.Contains("IAsyncEnumerable") ||
               (returnType is INamedTypeSymbol namedType && 
                namedType.AllInterfaces.Any(i => i.Name.Contains("IEnumerable")));
    }

    private static void GenerateMethodInvokers(SourceProductionContext context,
        (ImmutableArray<MethodDataSourceInfo> dataSources, ImmutableArray<MethodInvocationInfo> invocations) data)
    {
        var (dataSources, invocations) = data;

        if (dataSources.IsEmpty && invocations.IsEmpty)
        {
            return;
        }

        var writer = new CodeWriter();

        writer.AppendLine("// <auto-generated/>");
        writer.AppendLine("#pragma warning disable");
        writer.AppendLine("#nullable enable");
        writer.AppendLine();
        writer.AppendLine("using System;");
        writer.AppendLine("using System.Threading.Tasks;");
        writer.AppendLine("using System.Collections.Generic;");
        writer.AppendLine("using System.Collections;");
        writer.AppendLine();
        writer.AppendLine("namespace TUnit.Generated;");
        writer.AppendLine();

        GenerateMethodInvokerClass(writer, dataSources, invocations);

        context.AddSource("AotMethodInvokers.g.cs", writer.ToString());
    }

    private static void GenerateMethodInvokerClass(CodeWriter writer,
        ImmutableArray<MethodDataSourceInfo> dataSources,
        ImmutableArray<MethodInvocationInfo> invocations)
    {
        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// AOT-compatible method invocation helpers to replace MethodInfo.Invoke");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static class AotMethodInvokers");
        writer.AppendLine("{");
        writer.Indent();

        // Generate registry
        GenerateMethodRegistry(writer, dataSources, invocations);

        // Generate invocation helper methods
        GenerateInvocationMethods(writer, dataSources, invocations);

        // Generate strongly-typed invokers for each method (avoid duplicates by invoker name)
        var allMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (var ds in dataSources)
        {
            if (!HasUnresolvedTypeParameters(ds.TargetMethod) && IsAccessibleMethod(ds.TargetMethod))
            {
                allMethods.Add(ds.TargetMethod);
            }
        }
        foreach (var inv in invocations)
        {
            if (!HasUnresolvedTypeParameters(inv.TargetMethod) && IsAccessibleMethod(inv.TargetMethod))
            {
                allMethods.Add(inv.TargetMethod);
            }
        }

        var processedInvokerNames = new HashSet<string>();
        foreach (var method in allMethods)
        {
            var invokerName = GetInvokerMethodName(method);
            if (processedInvokerNames.Add(invokerName))
            {
                GenerateStronglyTypedInvoker(writer, method);
            }
        }

        writer.Unindent();
        writer.AppendLine("}");
    }

    private static void GenerateMethodRegistry(CodeWriter writer,
        ImmutableArray<MethodDataSourceInfo> dataSources,
        ImmutableArray<MethodInvocationInfo> invocations)
    {
        writer.AppendLine("private static readonly Dictionary<string, Func<object?, object?[]?, Task<object?>>> _methodInvokers = new()");
        writer.AppendLine("{");
        writer.Indent();

        var processedMethods = new HashSet<string>();

        foreach (var ds in dataSources)
        {
            // Only include methods that will have implementations generated
            if (!HasUnresolvedTypeParameters(ds.TargetMethod) && IsAccessibleMethod(ds.TargetMethod))
            {
                var methodKey = GetMethodKey(ds.TargetMethod);
                if (processedMethods.Add(methodKey))
                {
                    var invokerName = GetInvokerMethodName(ds.TargetMethod);
                    writer.AppendLine($"[\"{methodKey}\"] = {invokerName},");
                }
            }
        }

        foreach (var inv in invocations)
        {
            // Only include methods that will have implementations generated
            if (!HasUnresolvedTypeParameters(inv.TargetMethod) && IsAccessibleMethod(inv.TargetMethod))
            {
                var methodKey = GetMethodKey(inv.TargetMethod);
                if (processedMethods.Add(methodKey))
                {
                    var invokerName = GetInvokerMethodName(inv.TargetMethod);
                    writer.AppendLine($"[\"{methodKey}\"] = {invokerName},");
                }
            }
        }

        writer.Unindent();
        writer.AppendLine("};");
        writer.AppendLine();
    }

    private static void GenerateInvocationMethods(CodeWriter writer,
        ImmutableArray<MethodDataSourceInfo> dataSources,
        ImmutableArray<MethodInvocationInfo> invocations)
    {
        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Invokes a method by key (AOT-safe replacement for MethodInfo.Invoke)");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static async Task<object?> InvokeMethodAsync(string methodKey, object? instance, params object?[]? parameters)");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine("if (_methodInvokers.TryGetValue(methodKey, out var invoker))");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("return await invoker(instance, parameters);");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
        writer.AppendLine("throw new global::System.InvalidOperationException($\"No invoker found for method key: {methodKey}\");");

        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();

        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Gets the method key for a given method signature");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static string GetMethodKey(string typeName, string methodName, int parameterCount = 0)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("return $\"{typeName}.{methodName}({parameterCount})\";");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }

    private static void GenerateStronglyTypedInvoker(CodeWriter writer, IMethodSymbol method)
    {
        // Note: Pre-filtered for accessibility and type parameters
        
        var invokerName = GetInvokerMethodName(method);
        var returnType = method.ReturnType;
        var isAsync = IsAsyncMethod(method);
        var isVoid = returnType.SpecialType == SpecialType.System_Void;

        writer.AppendLine("/// <summary>");
        writer.AppendLine($"/// Strongly-typed invoker for {method.ContainingType.Name}.{method.Name}");
        writer.AppendLine("/// </summary>");
        writer.AppendLine($"private static async Task<object?> {invokerName}(object? instance, object?[]? parameters)");
        writer.AppendLine("{");
        writer.Indent();

        // Generate parameter extraction
        var parameters = method.Parameters;
        if (parameters.Length > 0)
        {
            writer.AppendLine("// Extract and convert parameters");
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramType = param.Type.GloballyQualified();
                
                // Skip parameters with unresolved type parameters
                if (ContainsTypeParameters(param.Type))
                {
                    writer.AppendLine($"var param{i} = parameters?[{i}]; // Type parameter - using object");
                }
                else
                {
                    writer.AppendLine($"var param{i} = parameters != null && parameters.Length > {i} && parameters[{i}] != null");
                    writer.AppendLine($"    ? ({paramType})parameters[{i}]!");
                    writer.AppendLine($"    : default({paramType});");
                }
            }
            writer.AppendLine();
        }

        // Generate method invocation
        if (method.IsStatic)
        {
            writer.Append("var result = ");
            if (isAsync)
            {
                writer.Append("await ");
            }

            var containingType = method.ContainingType.GloballyQualified();
            writer.Append($"{containingType}.{method.Name}(");
            
            if (parameters.Length > 0)
            {
                writer.Append(string.Join(", ", parameters.Select((_, i) => $"param{i}")));
            }
            
            writer.AppendLine(");");
        }
        else
        {
            writer.AppendLine($"if (instance is not {method.ContainingType.GloballyQualified()} typedInstance)");
            writer.AppendLine($"    throw new global::System.InvalidOperationException($\"Expected instance of type {{typeof({method.ContainingType.GloballyQualified()}).FullName}}, got {{instance?.GetType().FullName ?? \"null\"}}\");");
            writer.AppendLine();

            writer.Append("var result = ");
            if (isAsync)
            {
                writer.Append("await ");
            }

            writer.Append($"typedInstance.{method.Name}(");
            
            if (parameters.Length > 0)
            {
                writer.Append(string.Join(", ", parameters.Select((_, i) => $"param{i}")));
            }
            
            writer.AppendLine(");");
        }

        // Handle return value
        if (isVoid)
        {
            writer.AppendLine("return null;");
        }
        else if (isAsync && returnType is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.Name: "Task" } namedReturnType)
        {
            if (namedReturnType.TypeArguments.Length > 0)
            {
                // Task<T> - return the result
                writer.AppendLine("return result;");
            }
            else
            {
                // Task (void) - return null
                writer.AppendLine("return null;");
            }
        }
        else
        {
            writer.AppendLine("return result;");
        }

        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }

    private static string GetMethodKey(IMethodSymbol method)
    {
        var typeName = method.ContainingType.GloballyQualified();
        var methodName = method.Name;
        var parameterCount = method.Parameters.Length;
        return $"{typeName}.{methodName}({parameterCount})";
    }

    private static string GetInvokerMethodName(IMethodSymbol method)
    {
        var typeName = GetSafeIdentifierName(method.ContainingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        var methodName = GetSafeIdentifierName(method.Name);
        
        return $"Invoke_{typeName}_{methodName}_{method.Parameters.Length}";
    }
    
    private static string GetSafeIdentifierName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "Unknown";
        }

        // Replace all invalid characters with underscores
        var result = name
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "_")
            .Replace("`", "_")
            .Replace("[", "_")
            .Replace("]", "_")
            .Replace("(", "_")
            .Replace(")", "_")
            .Replace("+", "_")
            .Replace("-", "_")
            .Replace("*", "_")
            .Replace("/", "_")
            .Replace("&", "_")
            .Replace("|", "_")
            .Replace("^", "_")
            .Replace("%", "_")
            .Replace("=", "_")
            .Replace("!", "_")
            .Replace("?", "_")
            .Replace(":", "_")
            .Replace(";", "_")
            .Replace("#", "_")
            .Replace("@", "_")
            .Replace("$", "_")
            .Replace("~", "_")
            .Replace("`", "_");
            
        // Remove consecutive underscores
        while (result.Contains("__"))
        {
            result = result.Replace("__", "_");
        }
        
        // Ensure it starts with a letter or underscore
        if (result.Length > 0 && !char.IsLetter(result[0]) && result[0] != '_')
        {
            result = "_" + result;
        }
        
        return result;
    }
    
    private static bool HasUnresolvedTypeParameters(IMethodSymbol method)
    {
        // Check method type parameters
        if (method.TypeParameters.Length > 0)
        {
            return true;
        }
        
        // Check containing type type parameters
        if (method.ContainingType.TypeParameters.Length > 0)
        {
            return true;
        }
        
        // Check parameter types for type parameters
        foreach (var param in method.Parameters)
        {
            if (ContainsTypeParameters(param.Type))
            {
                return true;
            }
        }
        
        // Check return type for type parameters
        if (ContainsTypeParameters(method.ReturnType))
        {
            return true;
        }
        
        return false;
    }
    
    private static bool ContainsTypeParameters(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.TypeParameter)
        {
            return true;
        }
        
        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                if (ContainsTypeParameters(typeArg))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private static bool IsAccessibleMethod(IMethodSymbol method)
    {
        // Method must be public for AOT compatibility
        if (method.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }
        
        // Containing type must also be accessible
        var containingType = method.ContainingType;
        while (containingType != null)
        {
            if (containingType.DeclaredAccessibility != Accessibility.Public &&
                containingType.DeclaredAccessibility != Accessibility.Internal)
            {
                return false;
            }
            containingType = containingType.ContainingType;
        }
        
        return true;
    }

    private static bool IsAsyncMethod(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        return returnType.Name == "Task" || 
               returnType.Name == "ValueTask" ||
               returnType is INamedTypeSymbol { ConstructedFrom.Name: "Task" } ||
               returnType is INamedTypeSymbol { ConstructedFrom.Name: "ValueTask" };
    }

    private sealed class MethodDataSourceInfo
    {
        public required IMethodSymbol TargetMethod { get; init; }
        public required Location Location { get; init; }
        public required MethodUsage Usage { get; init; }
    }

    private sealed class MethodInvocationInfo
    {
        public required IMethodSymbol TargetMethod { get; init; }
        public required Location Location { get; init; }
        public required InvocationExpressionSyntax InvocationExpression { get; init; }
    }

    private enum MethodUsage
    {
        DataSource,
        DirectInvocation
    }
}