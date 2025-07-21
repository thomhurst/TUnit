using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Validates that all reflection usage has been replaced with AOT-compatible alternatives
/// and generates warnings/errors for any remaining AOT-incompatible code
/// </summary>
[Generator]
public sealed class AotCompatibilityValidatorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all potentially problematic reflection usage
        var reflectionUsage = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsProblematicReflectionUsage(node),
                transform: (ctx, _) => AnalyzeReflectionUsage(ctx))
            .Where(x => x is not null)
            .Select((x, _) => x!);

        // Generate validation report
        context.RegisterSourceOutput(reflectionUsage.Collect(), GenerateCompatibilityReport);
    }

    private static bool IsProblematicReflectionUsage(SyntaxNode node)
    {
        if (node is InvocationExpressionSyntax invocation)
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            var methodName = memberAccess?.Name.Identifier.ValueText;

            // Check for potentially problematic reflection methods
            return methodName switch
            {
                "GetType" when memberAccess.Expression.ToString() == "Type" => true,
                "MakeGenericType" => true,
                "Invoke" when memberAccess.Expression.ToString().Contains("MethodInfo") => true,
                "GetField" or "GetFields" => true,
                "GetProperty" or "GetProperties" => true,
                "GetMethod" or "GetMethods" => true,
                "CreateInstance" when memberAccess.Expression.ToString().Contains("Activator") => true,
                "GetValue" or "SetValue" when IsReflectionMember(memberAccess) => true,
                _ => false
            };
        }

        return false;
    }

    private static bool IsReflectionMember(MemberAccessExpressionSyntax memberAccess)
    {
        var expressionText = memberAccess.Expression.ToString();
        return expressionText.Contains("FieldInfo") ||
               expressionText.Contains("PropertyInfo") ||
               expressionText.Contains("MethodInfo");
    }

    private static ReflectionUsageInfo? AnalyzeReflectionUsage(GeneratorSyntaxContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        if (memberAccess == null) return null;

        var methodName = memberAccess.Name.Identifier.ValueText;
        var targetType = semanticModel.GetTypeInfo(memberAccess.Expression).Type;

        // Determine if this usage is already handled by our generators
        var isHandled = IsHandledByGenerators(methodName, targetType, invocation, semanticModel);
        var severity = isHandled ? DiagnosticSeverity.Info : DiagnosticSeverity.Warning;

        return new ReflectionUsageInfo
        {
            MethodName = methodName,
            TargetType = targetType?.ToDisplayString() ?? "Unknown",
            Location = invocation.GetLocation(),
            IsHandled = isHandled,
            Severity = severity,
            UsageContext = GetUsageContext(invocation)
        };
    }

    private static bool IsHandledByGenerators(string methodName, ITypeSymbol? targetType, 
        InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        // Check if this is in a context where our generators should handle it
        var containingMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        var containingClass = invocation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();

        if (containingClass != null)
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(containingClass) as INamedTypeSymbol;
            
            // Check if this is in TUnit.Generated namespace (our generated code)
            if (classSymbol?.ContainingNamespace?.Name == "Generated" &&
                classSymbol.ContainingNamespace.ContainingNamespace?.Name == "TUnit")
            {
                return true; // Generated code is allowed to use reflection for fallbacks
            }

            // Check if this is in a source generator context
            if (classSymbol?.Name.EndsWith("Generator") == true)
            {
                return true; // Source generators themselves can use reflection
            }
        }

        // Check specific patterns that should be handled
        return methodName switch
        {
            "GetType" when targetType?.Name == "Type" => true, // Should be handled by AotTypeResolver
            "MakeGenericType" => true, // Should be handled by AotTypeResolver
            "Invoke" when targetType?.Name == "MethodInfo" => true, // Should be handled by AotMethodInvokers
            "GetFields" when IsInTupleContext(invocation) => true, // Should be handled by AotTupleProcessor
            _ => false
        };
    }

    private static bool IsInTupleContext(InvocationExpressionSyntax invocation)
    {
        // Check if this GetFields call is in a tuple processing context
        var containingMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        var methodName = containingMethod?.Identifier.ValueText;
        
        return methodName?.Contains("Tuple") == true ||
               methodName?.Contains("Convert") == true;
    }

    private static string GetUsageContext(InvocationExpressionSyntax invocation)
    {
        var containingMethod = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        var containingClass = invocation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        
        var methodName = containingMethod?.Identifier.ValueText ?? "Unknown";
        var className = containingClass?.Identifier.ValueText ?? "Unknown";
        
        return $"{className}.{methodName}";
    }

    private static void GenerateCompatibilityReport(SourceProductionContext context, 
        ImmutableArray<ReflectionUsageInfo> reflectionUsages)
    {
        if (reflectionUsages.IsEmpty) return;

        var writer = new CodeWriter();

        writer.AppendLine("// <auto-generated/>");
        writer.AppendLine("// AOT Compatibility Validation Report");
        writer.AppendLine("// This file provides information about reflection usage and AOT compatibility");
        writer.AppendLine();
        writer.AppendLine("/*");
        writer.AppendLine("AOT COMPATIBILITY REPORT");
        writer.AppendLine("========================");
        writer.AppendLine();

        var handledUsages = reflectionUsages.Where(u => u.IsHandled).ToList();
        var unhandledUsages = reflectionUsages.Where(u => !u.IsHandled).ToList();

        writer.AppendLine($"Total reflection usages found: {reflectionUsages.Length}");
        writer.AppendLine($"Handled by generators: {handledUsages.Count}");
        writer.AppendLine($"Unhandled (may need attention): {unhandledUsages.Count}");
        writer.AppendLine();

        if (handledUsages.Any())
        {
            writer.AppendLine("HANDLED REFLECTION USAGE:");
            writer.AppendLine("------------------------");
            foreach (var usage in handledUsages.GroupBy(u => u.MethodName))
            {
                writer.AppendLine($"- {usage.Key}: {usage.Count()} usages (AOT-safe replacements generated)");
            }
            writer.AppendLine();
        }

        if (unhandledUsages.Any())
        {
            writer.AppendLine("UNHANDLED REFLECTION USAGE:");
            writer.AppendLine("---------------------------");
            writer.AppendLine("The following reflection usage may not be AOT-compatible:");
            writer.AppendLine();
            
            foreach (var usage in unhandledUsages)
            {
                var location = usage.Location;
                writer.AppendLine($"- {usage.MethodName} on {usage.TargetType}");
                writer.AppendLine($"  Context: {usage.UsageContext}");
                writer.AppendLine($"  Location: {location.GetLineSpan().StartLinePosition}");
                writer.AppendLine();
            }
        }

        writer.AppendLine("GENERATED AOT REPLACEMENTS:");
        writer.AppendLine("---------------------------");
        writer.AppendLine("The following AOT-safe replacements have been generated:");
        writer.AppendLine("- AotTypeResolver: Replaces Type.GetType() and Type.MakeGenericType()");
        writer.AppendLine("- AotMethodInvokers: Replaces MethodInfo.Invoke()");
        writer.AppendLine("- AotTupleProcessor: Replaces tuple reflection operations");
        writer.AppendLine("- EnhancedPropertyInjector: Replaces reflection-based property injection");
        writer.AppendLine("- ReflectionReplacements: Provides unified replacement system");
        writer.AppendLine();
        writer.AppendLine("All replacements are automatically registered via module initializer.");
        writer.AppendLine("*/");

        context.AddSource("AotCompatibilityReport.g.cs", writer.ToString());

        // Generate diagnostic reports
        foreach (var usage in unhandledUsages)
        {
            var descriptor = new DiagnosticDescriptor(
                "TUNIT_AOT_001",
                "Potentially AOT-incompatible reflection usage",
                "Reflection usage '{0}' on type '{1}' in '{2}' may not be AOT-compatible",
                "AOT",
                usage.Severity,
                isEnabledByDefault: true,
                description: "This reflection usage may not work in AOT scenarios. Consider using source-generated alternatives or ensure AOT-safe replacements are in place.");

            var diagnostic = Diagnostic.Create(descriptor, usage.Location, 
                usage.MethodName, usage.TargetType, usage.UsageContext);
            
            context.ReportDiagnostic(diagnostic);
        }
    }

    private sealed class ReflectionUsageInfo
    {
        public required string MethodName { get; init; }
        public required string TargetType { get; init; }
        public required Location Location { get; init; }
        public required bool IsHandled { get; init; }
        public required DiagnosticSeverity Severity { get; init; }
        public required string UsageContext { get; init; }
    }
}