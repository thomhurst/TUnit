using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Generators;

[Generator]
public class AotConverterGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all types with conversion operators
        var typesWithConversions = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => node is OperatorDeclarationSyntax,
                transform: (ctx, _) => GetConversionInfo(ctx))
            .Where(info => info != null)
            .Collect();

        context.RegisterSourceOutput(typesWithConversions, GenerateConverters!);
    }

    private ConversionInfo? GetConversionInfo(GeneratorSyntaxContext context)
    {
        var operatorDeclaration = (OperatorDeclarationSyntax)context.Node;
        
        // Get the conversion modifier (implicit or explicit)
        var isImplicit = operatorDeclaration.Modifiers.Any(m => m.ValueText == "implicit");
        var isExplicit = operatorDeclaration.Modifiers.Any(m => m.ValueText == "explicit");
        
        if (!isImplicit && !isExplicit)
        {
            return null;
        }

        var semanticModel = context.SemanticModel;
        var methodSymbol = semanticModel.GetDeclaredSymbol(operatorDeclaration) as IMethodSymbol;
        if (methodSymbol == null || !methodSymbol.IsStatic || methodSymbol.Parameters.Length != 1)
        {
            return null;
        }

        var containingType = methodSymbol.ContainingType;
        var sourceType = methodSymbol.Parameters[0].Type;
        var targetType = methodSymbol.ReturnType;

        return new ConversionInfo
        {
            ContainingType = containingType,
            SourceType = sourceType,
            TargetType = targetType,
            IsImplicit = isImplicit,
            MethodSymbol = methodSymbol
        };
    }

    private void GenerateConverters(SourceProductionContext context, ImmutableArray<ConversionInfo?> conversions)
    {
        if (conversions.IsEmpty)
        {
            return;
        }

        var writer = new CodeWriter();
        
        writer.AppendLine("#nullable enable");
        writer.AppendLine();
        writer.AppendLine("using System;");
        writer.AppendLine("using TUnit.Core.Converters;");
        writer.AppendLine();
        writer.AppendLine("namespace TUnit.Generated;");
        writer.AppendLine();
        
        // Generate converter classes
        var converterIndex = 0;
        var registrations = new List<string>();
        
        foreach (var conversion in conversions)
        {
            if (conversion == null)
                continue;

            var converterClassName = $"AotConverter_{converterIndex++}";
            var sourceTypeName = conversion.SourceType.GloballyQualified();
            var targetTypeName = conversion.TargetType.GloballyQualified();
            
            writer.AppendLine($"internal sealed class {converterClassName} : IAotConverter");
            writer.AppendLine("{");
            writer.Indent();
            
            writer.AppendLine($"public Type SourceType => typeof({sourceTypeName});");
            writer.AppendLine($"public Type TargetType => typeof({targetTypeName});");
            writer.AppendLine();
            
            writer.AppendLine("public object? Convert(object? value)");
            writer.AppendLine("{");
            writer.Indent();
            
            writer.AppendLine("if (value == null) return null;");
            writer.AppendLine($"if (value is {sourceTypeName} typedValue)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"return ({targetTypeName})typedValue;");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("return value; // Return original value if type doesn't match");
            
            writer.Unindent();
            writer.AppendLine("}");
            
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine();
            
            registrations.Add($"AotConverterRegistry.Register(new {converterClassName}());");
        }
        
        // Generate module initializer to register all converters
        writer.AppendLine("internal static class AotConverterRegistration");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]");
        writer.AppendLine("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Performance\", \"CA2255:The 'ModuleInitializer' attribute should not be used in libraries\",");
        writer.AppendLine("    Justification = \"Test framework needs to register AOT converters for conversion operators\")]");
        writer.AppendLine("public static void Initialize()");
        writer.AppendLine("{");
        writer.Indent();
        
        foreach (var registration in registrations)
        {
            writer.AppendLine(registration);
        }
        
        writer.Unindent();
        writer.AppendLine("}");
        
        writer.Unindent();
        writer.AppendLine("}");

        context.AddSource("AotConverters.g.cs", writer.ToString());
    }

    private class ConversionInfo
    {
        public required INamedTypeSymbol ContainingType { get; init; }
        public required ITypeSymbol SourceType { get; init; }
        public required ITypeSymbol TargetType { get; init; }
        public required bool IsImplicit { get; init; }
        public required IMethodSymbol MethodSymbol { get; init; }
    }
}