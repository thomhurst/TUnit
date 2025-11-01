using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
    private static readonly DiagnosticDescriptor MethodMustBeStaticRule = new DiagnosticDescriptor(
        id: "TUNITGEN001",
        title: "Method must be static",
        messageFormat: "Method '{0}' decorated with [GenerateAssertion] must be static",
        category: "TUnit.Assertions.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Methods decorated with [GenerateAssertion] must be static to be used in generated assertions.");

    private static readonly DiagnosticDescriptor MethodMustHaveParametersRule = new DiagnosticDescriptor(
        id: "TUNITGEN002",
        title: "Method must have at least one parameter",
        messageFormat: "Method '{0}' decorated with [GenerateAssertion] must have at least one parameter (the value to assert)",
        category: "TUnit.Assertions.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Methods decorated with [GenerateAssertion] must have at least one parameter representing the value being asserted.");

    private static readonly DiagnosticDescriptor UnsupportedReturnTypeRule = new DiagnosticDescriptor(
        id: "TUNITGEN003",
        title: "Unsupported return type",
        messageFormat: "Method '{0}' decorated with [GenerateAssertion] has unsupported return type '{1}'. Supported types are: bool, AssertionResult, Task<bool>, Task<AssertionResult>",
        category: "TUnit.Assertions.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Methods decorated with [GenerateAssertion] must return bool, AssertionResult, Task<bool>, or Task<AssertionResult>.");

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all methods decorated with [GenerateAssertion]
        var assertionMethodsOrDiagnostics = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Assertions.Attributes.GenerateAssertionAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: (ctx, ct) => GetAssertionMethodData(ctx, ct));

        // Split into methods and diagnostics
        var methods = assertionMethodsOrDiagnostics
            .Where(x => x.Data != null)
            .Select((x, _) => x.Data!);

        var diagnostics = assertionMethodsOrDiagnostics
            .Where(x => x.Diagnostic != null)
            .Select((x, _) => x.Diagnostic!);

        // Report diagnostics
        context.RegisterSourceOutput(diagnostics, static (context, diagnostic) =>
        {
            context.ReportDiagnostic(diagnostic);
        });

        // Generate assertion classes and extension methods
        context.RegisterSourceOutput(methods.Collect(), static (context, methods) =>
        {
            GenerateAssertions(context, methods);
        });
    }

    private static (AssertionMethodData? Data, Diagnostic? Diagnostic) GetAssertionMethodData(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return (null, null);
        }

        var location = context.TargetNode.GetLocation();

        // Validate method is static
        if (!methodSymbol.IsStatic)
        {
            var diagnostic = Diagnostic.Create(
                MethodMustBeStaticRule,
                location,
                methodSymbol.Name);
            return (null, diagnostic);
        }

        // Validate method has at least one parameter
        if (methodSymbol.Parameters.Length == 0)
        {
            var diagnostic = Diagnostic.Create(
                MethodMustHaveParametersRule,
                location,
                methodSymbol.Name);
            return (null, diagnostic);
        }

        // Get return type info
        var returnTypeInfo = AnalyzeReturnType(methodSymbol.ReturnType);
        if (returnTypeInfo == null)
        {
            var diagnostic = Diagnostic.Create(
                UnsupportedReturnTypeRule,
                location,
                methodSymbol.Name,
                methodSymbol.ReturnType.ToDisplayString());
            return (null, diagnostic);
        }

        // First parameter is the target type (what becomes IAssertionSource<T>)
        var targetType = methodSymbol.Parameters[0].Type;
        var additionalParameters = methodSymbol.Parameters.Skip(1).ToImmutableArray();

        // Check if it's an extension method
        var isExtensionMethod = methodSymbol.IsExtensionMethod ||
                               (methodSymbol.Parameters.Length > 0 && methodSymbol.Parameters[0].IsThis);

        // Extract custom expectation message and inlining preference if provided
        string? customExpectation = null;
        bool inlineMethodBody = false;
        var attribute = context.Attributes.FirstOrDefault();
        if (attribute != null)
        {
            foreach (var namedArg in attribute.NamedArguments)
            {
                if (namedArg.Key == "ExpectationMessage" && namedArg.Value.Value is string expectation)
                {
                    customExpectation = expectation;
                }
                else if (namedArg.Key == "InlineMethodBody" && namedArg.Value.Value is bool inline)
                {
                    inlineMethodBody = inline;
                }
            }
        }

        // Extract attributes that should be copied to generated code
        // Split into two categories:
        // 1. Suppression attributes (for CheckAsync method - to suppress warnings in generated code)
        // 2. Diagnostic attributes (for extension method - to warn users)
        var suppressionAttributesForCheckAsync = new List<string>();
        var diagnosticAttributesForExtensionMethod = new List<string>();

        foreach (var attr in methodSymbol.GetAttributes())
        {
            var attributeClass = attr.AttributeClass;
            if (attributeClass == null || attributeClass.ContainingNamespace?.ToDisplayString() != "System.Diagnostics.CodeAnalysis")
                continue;

            // Handle UnconditionalSuppressMessage - goes to CheckAsync
            if (attributeClass.Name == "UnconditionalSuppressMessageAttribute" && attr.ConstructorArguments.Length >= 2)
            {
                var category = attr.ConstructorArguments[0].Value?.ToString();
                var checkId = attr.ConstructorArguments[1].Value?.ToString();

                var justification = "";
                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "Justification" && namedArg.Value.Value is string j)
                    {
                        justification = $", Justification = \"{j}\"";
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(checkId))
                {
                    suppressionAttributesForCheckAsync.Add($"[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"{category}\", \"{checkId}\"{justification})]");
                }
            }
            // Handle RequiresUnreferencedCode - goes to extension method AND add suppression to CheckAsync
            else if (attributeClass.Name == "RequiresUnreferencedCodeAttribute" && attr.ConstructorArguments.Length >= 1)
            {
                var message = attr.ConstructorArguments[0].Value?.ToString();

                var urlPart = "";
                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "Url" && namedArg.Value.Value is string url)
                    {
                        urlPart = $", Url = \"{url}\"";
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(message))
                {
                    // Add to extension method so users see the warning
                    diagnosticAttributesForExtensionMethod.Add($"[System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(\"{message}\"{urlPart})]");
                    // Add suppression to CheckAsync to avoid IL2046 (can't override without matching attributes)
                    suppressionAttributesForCheckAsync.Add($"[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2026\", Justification = \"Caller is already warned via RequiresUnreferencedCode on extension method\")]");
                }
            }
            // Handle RequiresDynamicCode - goes to extension method AND add suppression to CheckAsync
            else if (attributeClass.Name == "RequiresDynamicCodeAttribute" && attr.ConstructorArguments.Length >= 1)
            {
                var message = attr.ConstructorArguments[0].Value?.ToString();

                var urlPart = "";
                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "Url" && namedArg.Value.Value is string url)
                    {
                        urlPart = $", Url = \"{url}\"";
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(message))
                {
                    // Add to extension method so users see the warning
                    diagnosticAttributesForExtensionMethod.Add($"[System.Diagnostics.CodeAnalysis.RequiresDynamicCode(\"{message}\"{urlPart})]");
                    // Add suppression to CheckAsync
                    suppressionAttributesForCheckAsync.Add($"[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"AOT\", \"IL3050\", Justification = \"Caller is already warned via RequiresDynamicCode on extension method\")]");
                }
            }
        }

        // Check if the containing type is file-scoped and extract method body if inlining is requested
        var isFileScoped = IsFileScopedClass(methodSymbol.ContainingType);
        string? methodBody = null;
        SyntaxNode? expressionNode = null;

        if (inlineMethodBody && context.TargetNode is MethodDeclarationSyntax methodSyntax)
        {
            if (methodSyntax.ExpressionBody != null)
            {
                expressionNode = methodSyntax.ExpressionBody.Expression;
                methodBody = expressionNode.ToFullString().Trim();
            }
            else if (methodSyntax.Body != null)
            {
                var statements = methodSyntax.Body.Statements;
                if (statements.Count == 1 && statements[0] is ReturnStatementSyntax returnStatement && returnStatement.Expression != null)
                {
                    expressionNode = returnStatement.Expression;
                    methodBody = expressionNode.ToFullString().Trim();
                }
            }

            // Fully qualify the expression if we have it
            if (expressionNode != null && methodBody != null)
            {
                methodBody = FullyQualifyExpression(methodBody, context.SemanticModel, expressionNode) ?? methodBody;
            }
        }

        var data = new AssertionMethodData(
            methodSymbol,
            targetType,
            additionalParameters,
            returnTypeInfo.Value,
            isExtensionMethod,
            customExpectation,
            isFileScoped,
            methodBody,
            suppressionAttributesForCheckAsync.ToImmutableArray(),
            diagnosticAttributesForExtensionMethod.ToImmutableArray()
        );

        return (data, null);
    }

    /// <summary>
    /// Checks if a type is file-scoped (has 'file' accessibility)
    /// File-scoped types have specific metadata that we can check.
    /// </summary>
    private static bool IsFileScopedClass(INamedTypeSymbol typeSymbol)
    {
        // In Roslyn, file-scoped types are marked with a special compiler-generated attribute
        // We can check for this by looking at the type's attributes
        // The safest approach: check if the type is internal and has no public/protected members
        // except the methods marked with [GenerateAssertion]

        // For now, we use a heuristic: if the class is not public and not a known extension class pattern,
        // we treat it as file-scoped for inlining purposes
        if (typeSymbol.DeclaredAccessibility == Accessibility.Public)
        {
            return false;
        }

        // Check if there are file-local modifiers (Roslyn 4.4+)
        // File-scoped types have IsFileLocal property set to true
        // We'll use reflection to check if this property exists and is true
        var isFileLocalProperty = typeSymbol.GetType().GetProperty("IsFileLocal");
        if (isFileLocalProperty != null)
        {
            var isFileLocal = isFileLocalProperty.GetValue(typeSymbol);
            if (isFileLocal is bool fileLocal && fileLocal)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Fully qualifies type names in an expression to make it safe for inlining across namespaces.
    /// Uses the semantic model to resolve all type references.
    /// </summary>
    private static string? FullyQualifyExpression(string expression, SemanticModel semanticModel, SyntaxNode expressionNode)
    {
        try
        {
            var rewriter = new TypeQualifyingRewriter(semanticModel);
            var rewrittenNode = rewriter.Visit(expressionNode);
            return rewrittenNode?.ToFullString().Trim();
        }
        catch
        {
            // If rewriting fails, return the original expression
            return expression;
        }
    }

    /// <summary>
    /// Syntax rewriter that fully qualifies all type references in an expression.
    /// </summary>
    private class TypeQualifyingRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _semanticModel;

        public TypeQualifyingRewriter(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            var symbolInfo = _semanticModel.GetSymbolInfo(node);
            var symbol = symbolInfo.Symbol;

            // Fully qualify type references, static members, etc.
            if (symbol is INamedTypeSymbol typeSymbol)
            {
                var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return SyntaxFactory.ParseExpression(fullName).WithTriviaFrom(node);
            }
            else if (symbol is IFieldSymbol fieldSymbol && fieldSymbol.IsStatic)
            {
                var typeName = fieldSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return SyntaxFactory.ParseExpression($"{typeName}.{fieldSymbol.Name}").WithTriviaFrom(node);
            }
            else if (symbol is IPropertySymbol propertySymbol && propertySymbol.IsStatic)
            {
                var typeName = propertySymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return SyntaxFactory.ParseExpression($"{typeName}.{propertySymbol.Name}").WithTriviaFrom(node);
            }

            return base.VisitIdentifierName(node);
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            // Check if the left side is a type reference that needs qualification
            var symbolInfo = _semanticModel.GetSymbolInfo(node.Expression);
            var symbol = symbolInfo.Symbol;

            if (symbol is INamedTypeSymbol typeSymbol)
            {
                // Fully qualify the type on the left side
                var fullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var newExpression = SyntaxFactory.ParseExpression(fullName);
                return node.WithExpression(newExpression);
            }

            return base.VisitMemberAccessExpression(node);
        }
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

            // Generate extension methods class
            // For file-scoped types, we can't use partial classes, so create a standalone public class
            // For non-file-scoped types, we use partial classes to combine with the source definition
            var isFileScopedType = methodGroup.Any(m => m.IsFileScoped);

            var extensionClassName = isFileScopedType
                ? $"{containingType.Name}Extensions"  // Standalone class for file-scoped types
                : containingType.Name;                 // Partial class for public types

            var partialModifier = isFileScopedType ? "" : "partial ";
            sourceBuilder.AppendLine($"public static {partialModifier}class {extensionClassName}");
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

        // Collect generic constraints from the method
        var genericConstraints = CollectGenericConstraints(data.Method);

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

        // Apply generic constraints if present
        if (genericConstraints.Count > 0)
        {
            foreach (var constraint in genericConstraints)
            {
                sb.AppendLine($"    {constraint}");
            }
        }

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

        // Add suppression attributes to CheckAsync method when method body is inlined
        if (!string.IsNullOrEmpty(data.MethodBody) && data.SuppressionAttributesForCheckAsync.Length > 0)
        {
            foreach (var suppressionAttr in data.SuppressionAttributesForCheckAsync)
            {
                sb.AppendLine($"    {suppressionAttr}");
            }
        }

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
        // If we have a method body available (with fully qualified types), inline it
        // Otherwise, fall back to calling the method (backward compatibility)
        var shouldInline = !string.IsNullOrEmpty(data.MethodBody);
        var methodCall = shouldInline
            ? BuildInlinedExpression(data)
            : BuildMethodCallExpression(data);

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
                sb.AppendLine("            : AssertionResult.Failed($\"found {value}\"));");
                break;

            case ReturnTypeKind.TaskAssertionResult:
                sb.AppendLine($"        return await {methodCall};");
                break;
        }
    }

    /// <summary>
    /// Builds an inlined expression by replacing parameter references in the method body.
    /// For example: "value == true" or "collection.Contains(value)"
    /// </summary>
    private static string BuildInlinedExpression(AssertionMethodData data)
    {
        if (string.IsNullOrEmpty(data.MethodBody))
        {
            // Fallback to method call if body is not available
            return BuildMethodCallExpression(data);
        }

        var inlinedBody = data.MethodBody;

        // Replace first parameter name with "value" (already named value in our context)
        var firstParamName = data.Method.Parameters[0].Name;
        if (firstParamName != "value")
        {
            // Use word boundary replacement to avoid partial matches
            inlinedBody = Regex.Replace(
                inlinedBody,
                $@"\b{Regex.Escape(firstParamName)}\b",
                "value");
        }

        // Replace additional parameter names with their field references (_paramName)
        foreach (var param in data.AdditionalParameters)
        {
            var paramName = param.Name;
            inlinedBody = Regex.Replace(
                inlinedBody,
                $@"\b{Regex.Escape(paramName)}\b",
                $"_{paramName}");
        }

        // Add null-forgiving operator for reference types if not already present
        // This is safe because we've already checked for null above
        var isNullable = data.TargetType.IsReferenceType || data.TargetType.NullableAnnotation == NullableAnnotation.Annotated;
        if (isNullable && !string.IsNullOrEmpty(inlinedBody) && !inlinedBody.StartsWith("value!"))
        {
            // Replace null-conditional operators with null-forgiving + regular operators
            // value?.Member becomes value!.Member (safe because we already null-checked)
            inlinedBody = inlinedBody.Replace("value?.", "value!.");
            inlinedBody = inlinedBody.Replace("value?[", "value![");

            // Replace regular member access with null-forgiving operator
            // But only if we haven't already added it via the null-conditional replacement
            if (!inlinedBody.Contains("value!"))
            {
                inlinedBody = inlinedBody.Replace("value.", "value!.");
                inlinedBody = inlinedBody.Replace("value[", "value![");
            }

            // Handle cases like "value == something" - we need to be careful here
            if (!inlinedBody.Contains("value!"))
            {
                // If value is used directly without member access, add ! when first used
                inlinedBody = Regex.Replace(
                    inlinedBody,
                    @"\bvalue\b(?![!\.\?])",
                    "value!",
                    RegexOptions.None,
                    TimeSpan.FromSeconds(1));
            }
        }

        return inlinedBody ?? string.Empty;
    }

    private static string BuildMethodCallExpression(AssertionMethodData data)
    {
        var containingType = data.Method.ContainingType.ToDisplayString();
        var methodName = data.Method.Name;

        // Build type arguments if the method is generic
        var typeArguments = "";
        if (data.Method.IsGenericMethod && data.Method.TypeParameters.Length > 0)
        {
            var typeParams = string.Join(", ", data.Method.TypeParameters.Select(tp => tp.Name));
            typeArguments = $"<{typeParams}>";
        }

        if (data.IsExtensionMethod)
        {
            // Extension method syntax: value!.MethodName<T1, T2>(params)
            // Use null-forgiving operator since we've already checked for null above
            var paramList = string.Join(", ", data.AdditionalParameters.Select(p => $"_{p.Name}"));
            return $"value!.{methodName}{typeArguments}({paramList})";
        }
        else
        {
            // Static method syntax: ContainingType.MethodName<T1, T2>(value, params)
            var allParams = new List<string> { "value" };
            allParams.AddRange(data.AdditionalParameters.Select(p => $"_{p.Name}"));
            var paramList = string.Join(", ", allParams);
            return $"{containingType}.{methodName}{typeArguments}({paramList})";
        }
    }

    private static void GenerateExtensionMethod(StringBuilder sb, AssertionMethodData data)
    {
        var className = GenerateClassName(data);
        var targetTypeName = data.TargetType.ToDisplayString();
        var methodName = data.Method.Name;
        var genericParams = GetGenericTypeParameters(data.TargetType, data.Method);
        var genericDeclaration = genericParams.Length > 0 ? $"<{string.Join(", ", genericParams)}>" : "";

        // Collect generic constraints from the method
        var genericConstraints = CollectGenericConstraints(data.Method);

        // XML documentation
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Generated extension method for {methodName}");
        sb.AppendLine("    /// </summary>");

        // Add suppression for generic types to avoid trimming warnings
        if (genericParams.Length > 0)
        {
            sb.AppendLine($"    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2091\", Justification = \"Generic type parameter is only used for property access, not instantiation\")]");
        }

        // Add diagnostic attributes (RequiresUnreferencedCode, RequiresDynamicCode) to extension method
        if (data.DiagnosticAttributesForExtensionMethod.Length > 0)
        {
            foreach (var diagnosticAttr in data.DiagnosticAttributesForExtensionMethod)
            {
                sb.AppendLine($"    {diagnosticAttr}");
            }
        }

        // Method signature
        sb.Append($"    public static {className}{genericDeclaration} {methodName}{genericDeclaration}(");
        sb.Append($"this IAssertionSource<{targetTypeName}> source");

        // Additional parameters
        foreach (var param in data.AdditionalParameters)
        {
            var paramsModifier = param.IsParams ? "params " : "";
            sb.Append($", {paramsModifier}{param.Type.ToDisplayString()} {param.Name}");
        }

        // CallerArgumentExpression parameters (skip for params since params must be last)
        for (int i = 0; i < data.AdditionalParameters.Length; i++)
        {
            var param = data.AdditionalParameters[i];
            if (!param.IsParams)
            {
                sb.Append($", [CallerArgumentExpression(nameof({param.Name}))] string? {param.Name}Expression = null");
            }
        }

        sb.AppendLine(")");

        // Apply generic constraints if present
        if (genericConstraints.Count > 0)
        {
            foreach (var constraint in genericConstraints)
            {
                sb.AppendLine($"    {constraint}");
            }
        }

        sb.AppendLine("    {");

        // Build expression string
        if (data.AdditionalParameters.Length > 0)
        {
            // For params parameters, use parameter name directly (no Expression suffix since we didn't generate it)
            var exprList = string.Join(", ", data.AdditionalParameters.Select(p =>
                p.IsParams ? $"{{{p.Name}}}" : $"{{{p.Name}Expression}}"));
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

    /// <summary>
    /// Collects generic constraints from method type parameters.
    /// Returns a list of constraint strings in the format "where T : constraint1, constraint2"
    /// </summary>
    private static List<string> CollectGenericConstraints(IMethodSymbol method)
    {
        var constraints = new List<string>();

        if (!method.IsGenericMethod || method.TypeParameters.Length == 0)
        {
            return constraints;
        }

        foreach (var typeParameter in method.TypeParameters)
        {
            var typeConstraints = new List<string>();

            if (typeParameter.HasReferenceTypeConstraint)
            {
                typeConstraints.Add("class");
            }
            if (typeParameter.HasValueTypeConstraint)
            {
                typeConstraints.Add("struct");
            }
            if (typeParameter.HasNotNullConstraint)
            {
                typeConstraints.Add("notnull");
            }
            foreach (var constraintType in typeParameter.ConstraintTypes)
            {
                typeConstraints.Add(constraintType.ToDisplayString());
            }
            if (typeParameter.HasConstructorConstraint)
            {
                typeConstraints.Add("new()");
            }

            if (typeConstraints.Count > 0)
            {
                constraints.Add($"where {typeParameter.Name} : {string.Join(", ", typeConstraints)}");
            }
        }

        return constraints;
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
        string? CustomExpectation,
        bool IsFileScoped,
        string? MethodBody,
        ImmutableArray<string> SuppressionAttributesForCheckAsync,
        ImmutableArray<string> DiagnosticAttributesForExtensionMethod
    );
}
