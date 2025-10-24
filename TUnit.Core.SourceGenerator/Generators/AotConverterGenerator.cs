using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Generators;

[Generator]
public class AotConverterGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var allTypes = context.CompilationProvider
            .Select((compilation, _) =>
            {
                var conversionInfos = new List<ConversionInfo>();

                // Scan source code for conversion operators
                foreach (var tree in compilation.SyntaxTrees)
                {
                    var root = tree.GetRoot();
                    var semanticModel = compilation.GetSemanticModel(tree);

                    var conversionOperators = root.DescendantNodes()
                        .OfType<ConversionOperatorDeclarationSyntax>()
                        .ToList();

                    foreach (var operatorDecl in conversionOperators)
                    {
                        var conversionInfo = GetConversionInfo(operatorDecl, semanticModel);
                        if (conversionInfo != null)
                        {
                            conversionInfos.Add(conversionInfo);
                        }
                    }
                }

                // Scan referenced assemblies for conversion operators
                ScanReferencedAssemblies(compilation, conversionInfos);

                // Scan for closed generic types used in the source code and generate converters for them
                ScanClosedGenericTypesInSource(compilation, conversionInfos);

                return conversionInfos.ToImmutableArray();
            });

        context.RegisterSourceOutput(allTypes, GenerateConverters!);
    }

    private void ScanClosedGenericTypesInSource(Compilation compilation, List<ConversionInfo> conversionInfos)
    {
        // Find all closed generic types used in method parameters
        var typesInUse = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var tree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            // Find all method declarations
            var methods = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                if (methodSymbol == null)
                {
                    continue;
                }

                // Collect parameter types
                foreach (var parameter in methodSymbol.Parameters)
                {
                    CollectTypeAndRelatedTypes(parameter.Type, typesInUse);
                }
            }
        }

        // For each type in use, check if it has conversion operators
        foreach (var type in typesInUse)
        {
            if (type is not INamedTypeSymbol namedType)
            {
                continue;
            }

            // Skip non-public types
            if (namedType.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            // Get conversion operators for this type
            var conversionOperators = namedType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => (m.Name == "op_Implicit" || m.Name == "op_Explicit") &&
                           m.IsStatic &&
                           m.Parameters.Length == 1);

            foreach (var method in conversionOperators)
            {
                var conversionInfo = GetConversionInfoFromSymbol(method);
                if (conversionInfo != null)
                {
                    conversionInfos.Add(conversionInfo);
                }
            }
        }
    }

    private void CollectTypeAndRelatedTypes(ITypeSymbol type, HashSet<ITypeSymbol> types)
    {
        if (type == null || !types.Add(type))
        {
            return; // Already processed
        }

        // If this is a constructed generic type, collect it
        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            // Collect type arguments (e.g., for OneOf<TestEnum, TestEnum2>, collect TestEnum and TestEnum2)
            foreach (var typeArg in namedType.TypeArguments)
            {
                CollectTypeAndRelatedTypes(typeArg, types);
            }
        }

        // If this is an array, collect element type
        if (type is IArrayTypeSymbol arrayType)
        {
            CollectTypeAndRelatedTypes(arrayType.ElementType, types);
        }
    }

    private void ScanReferencedAssemblies(Compilation compilation, List<ConversionInfo> conversionInfos)
    {
        // Get all types from referenced assemblies
        var referencedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assemblySymbol)
            {
                // Skip System assemblies and other common assemblies that won't have test-relevant converters
                var assemblyName = assemblySymbol.Name;
                if (assemblyName.StartsWith("System.") ||
                    assemblyName.StartsWith("Microsoft.") ||
                    assemblyName == "mscorlib" ||
                    assemblyName == "netstandard")
                {
                    continue;
                }

                CollectTypesFromNamespace(assemblySymbol.GlobalNamespace, referencedTypes);
            }
        }

        // Find conversion operators in referenced types
        foreach (var type in referencedTypes)
        {
            // Only process public types
            if (type.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            // Get all members and filter for conversion operators
            var conversionOperators = type.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => (m.Name == "op_Implicit" || m.Name == "op_Explicit") &&
                           m.IsStatic &&
                           m.Parameters.Length == 1);

            foreach (var method in conversionOperators)
            {
                var conversionInfo = GetConversionInfoFromSymbol(method);
                if (conversionInfo != null)
                {
                    conversionInfos.Add(conversionInfo);
                }
            }
        }
    }

    private void CollectTypesFromNamespace(INamespaceSymbol namespaceSymbol, HashSet<INamedTypeSymbol> types)
    {
        foreach (var member in namespaceSymbol.GetMembers())
        {
            if (member is INamedTypeSymbol type)
            {
                types.Add(type);

                // Recursively collect nested types
                CollectNestedTypes(type, types);
            }
            else if (member is INamespaceSymbol childNamespace)
            {
                CollectTypesFromNamespace(childNamespace, types);
            }
        }
    }

    private void CollectNestedTypes(INamedTypeSymbol type, HashSet<INamedTypeSymbol> types)
    {
        foreach (var nestedType in type.GetTypeMembers())
        {
            types.Add(nestedType);
            CollectNestedTypes(nestedType, types);
        }
    }

    private ConversionInfo? GetConversionInfoFromSymbol(IMethodSymbol methodSymbol)
    {
        var containingType = methodSymbol.ContainingType;
        var sourceType = methodSymbol.Parameters[0].Type;
        var targetType = methodSymbol.ReturnType;
        var isImplicit = methodSymbol.Name == "op_Implicit";

        // Skip conversion operators with unbound generic type parameters
        // These cannot be properly represented in AOT converters at runtime
        if (sourceType.IsGenericDefinition() || targetType.IsGenericDefinition())
        {
            return null;
        }

        // Skip ref structs (Span<T>, ReadOnlySpan<T>, etc.) - they cannot be boxed to object
        if (sourceType.IsRefLikeType || targetType.IsRefLikeType)
        {
            return null;
        }

        // Skip pointer types and void - they cannot be used as object
        if (sourceType.TypeKind == TypeKind.Pointer || targetType.TypeKind == TypeKind.Pointer ||
            sourceType.SpecialType == SpecialType.System_Void || targetType.SpecialType == SpecialType.System_Void)
        {
            return null;
        }

        // Skip conversion operators where the containing type is not publicly accessible
        // The generated code won't be able to reference private/internal types
        if (containingType.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        // Also skip if the source or target type is not publicly accessible
        // (unless it's a built-in type)
        if (sourceType is INamedTypeSymbol { SpecialType: SpecialType.None } namedSourceType &&
            namedSourceType.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        if (targetType is INamedTypeSymbol { SpecialType: SpecialType.None } namedTargetType &&
            namedTargetType.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        return new ConversionInfo
        {
            ContainingType = containingType,
            SourceType = sourceType,
            TargetType = targetType,
            IsImplicit = isImplicit,
            MethodSymbol = methodSymbol
        };
    }

    private ConversionInfo? GetConversionInfo(ConversionOperatorDeclarationSyntax operatorDeclaration, SemanticModel semanticModel)
    {
        var isImplicit = operatorDeclaration.ImplicitOrExplicitKeyword.IsKind(SyntaxKind.ImplicitKeyword);
        var isExplicit = operatorDeclaration.ImplicitOrExplicitKeyword.IsKind(SyntaxKind.ExplicitKeyword);

        if (!isImplicit && !isExplicit)
        {
            return null;
        }

        var methodSymbol = semanticModel.GetDeclaredSymbol(operatorDeclaration) as IMethodSymbol;
        if (methodSymbol == null || !methodSymbol.IsStatic || methodSymbol.Parameters.Length != 1)
        {
            return null;
        }

        return GetConversionInfoFromSymbol(methodSymbol);
    }

    private void GenerateConverters(SourceProductionContext context, ImmutableArray<ConversionInfo> conversions)
    {
        var writer = new CodeWriter();
        writer.AppendLine("#nullable enable");

        if (conversions.IsEmpty)
        {
            writer.AppendLine();
            writer.AppendLine("// No conversion operators found");
            context.AddSource("AotConverters.g.cs", writer.ToString());
            return;
        }

        // Deduplicate conversions based on source and target types
        var seenConversions = new HashSet<(ITypeSymbol Source, ITypeSymbol Target)>(
            new TypePairEqualityComparer());
        var uniqueConversions = new List<ConversionInfo>();

        foreach (var conversion in conversions)
        {
            if (conversion == null)
            {
                continue;
            }

            var key = (conversion.SourceType, conversion.TargetType);
            if (seenConversions.Add(key))
            {
                uniqueConversions.Add(conversion);
            }
        }

        writer.AppendLine();
        writer.AppendLine("using System;");
        writer.AppendLine("using TUnit.Core.Converters;");
        writer.AppendLine();
        writer.AppendLine("namespace TUnit.Generated;");
        writer.AppendLine();

        var converterIndex = 0;
        var registrations = new List<string>();

        foreach (var conversion in uniqueConversions)
        {
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

            // For nullable value types, we need to use the underlying type in the pattern
            // because you can't use nullable types in patterns in older C# versions
            var sourceType = conversion.SourceType;
            var underlyingType = sourceType.IsValueType && sourceType is INamedTypeSymbol named && named.OriginalDefinition?.SpecialType == SpecialType.System_Nullable_T
                ? ((INamedTypeSymbol)sourceType).TypeArguments[0]
                : sourceType;

            var patternTypeName = underlyingType.GloballyQualified();

            writer.AppendLine($"if (value is {patternTypeName} typedValue)");
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
        
        writer.AppendLine("internal static class AotConverterRegistration");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
        writer.AppendLine("[global::System.Diagnostics.CodeAnalysis.SuppressMessage(\"Performance\", \"CA2255:The 'ModuleInitializer' attribute should not be used in libraries\",");
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

    private class TypePairEqualityComparer : IEqualityComparer<(ITypeSymbol Source, ITypeSymbol Target)>
    {
        public bool Equals((ITypeSymbol Source, ITypeSymbol Target) x, (ITypeSymbol Source, ITypeSymbol Target) y)
        {
            return SymbolEqualityComparer.Default.Equals(x.Source, y.Source) &&
                   SymbolEqualityComparer.Default.Equals(x.Target, y.Target);
        }

        public int GetHashCode((ITypeSymbol Source, ITypeSymbol Target) obj)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(obj.Source);
                hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(obj.Target);
                return hash;
            }
        }
    }
}