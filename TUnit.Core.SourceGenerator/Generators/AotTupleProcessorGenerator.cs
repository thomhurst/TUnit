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
/// Generates AOT-compatible tuple processing code to replace reflection-based tuple unwrapping
/// </summary>
[Generator]
public sealed class AotTupleProcessorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all tuple usages in the codebase
        var tupleUsages = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsTupleUsage(node),
                transform: (ctx, _) => ExtractTupleInfo(ctx))
            .Where(x => x is not null)
            .Select((x, _) => x!);

        // Find all methods that return tuple types and could be data sources
        var tupleDataSources = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsTupleDataSource(node),
                transform: (ctx, _) => ExtractTupleDataSourceInfo(ctx))
            .Where(x => x is not null)
            .Select((x, _) => x!);

        // Combine all tuple processing requirements
        var allTupleInfo = tupleUsages
            .Collect()
            .Combine(tupleDataSources.Collect());

        // Generate the tuple processing helpers
        context.RegisterSourceOutput(allTupleInfo, GenerateTupleProcessors);
    }

    private static bool IsTupleUsage(SyntaxNode node)
    {
        // Look for method calls that suggest tuple processing
        if (node is InvocationExpressionSyntax invocation)
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            var methodName = memberAccess?.Name.Identifier.ValueText;

            // Look for tuple-related method calls
            if (methodName is "ConvertTupleToArray" or "IsTupleType" or "GetFields" or "UnpackTuple")
            {
                return true;
            }

            // Look for GetFields calls that might be on tuple types
            if (methodName == "GetFields")
            {
                return true;
            }
        }

        // Look for tuple type syntax (ValueTuple, Tuple)
        if (node is TupleTypeSyntax)
        {
            return true;
        }
        else if (node is GenericNameSyntax generic)
        {
            var name = generic.Identifier.ValueText;
            if (name is "ValueTuple" or "Tuple")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTupleDataSource(SyntaxNode node)
    {
        if (node is MethodDeclarationSyntax method)
        {
            // Check if method returns a tuple type or IEnumerable of tuples
            var returnTypeName = method.ReturnType?.ToString();
            if (returnTypeName != null && (
                returnTypeName.Contains("ValueTuple") ||
                returnTypeName.Contains("Tuple") ||
                returnTypeName.Contains("IEnumerable<(") ||
                returnTypeName.Contains("IEnumerable<ValueTuple") ||
                returnTypeName.Contains("IEnumerable<Tuple")))
            {
                return true;
            }
        }

        return false;
    }

    private static TupleUsageInfo? ExtractTupleInfo(GeneratorSyntaxContext context)
    {
        var semanticModel = context.SemanticModel;

        if (context.Node is InvocationExpressionSyntax invocation)
        {
            return ExtractFromInvocation(invocation, semanticModel);
        }
        else if (context.Node is TupleTypeSyntax tupleType)
        {
            return ExtractFromTupleType(tupleType, semanticModel);
        }
        else if (context.Node is GenericNameSyntax genericName)
        {
            return ExtractFromGenericName(genericName, semanticModel);
        }

        return null;
    }

    private static TupleDataSourceInfo? ExtractTupleDataSourceInfo(GeneratorSyntaxContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        var methodSymbol = semanticModel.GetDeclaredSymbol(method) as IMethodSymbol;
        if (methodSymbol == null) return null;

        var returnType = methodSymbol.ReturnType;
        var tupleTypes = ExtractTupleTypesFromReturnType(returnType);

        if (tupleTypes.Count == 0) return null;

        return new TupleDataSourceInfo
        {
            Method = methodSymbol,
            TupleTypes = tupleTypes.ToImmutableArray(),
            Location = method.GetLocation()
        };
    }

    private static TupleUsageInfo? ExtractFromInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        var methodName = memberAccess?.Name.Identifier.ValueText;

        if (methodName == "GetFields" && memberAccess != null)
        {
            // Try to determine if this is called on a tuple type
            var targetType = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
            if (targetType != null && IsTupleType(targetType))
            {
                return new TupleUsageInfo
                {
                    TupleType = targetType,
                    Usage = TupleUsageType.FieldAccess,
                    Location = invocation.GetLocation()
                };
            }
        }

        return null;
    }

    private static TupleUsageInfo? ExtractFromTupleType(TupleTypeSyntax tupleType, SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(tupleType);
        if (typeInfo.Type != null)
        {
            return new TupleUsageInfo
            {
                TupleType = typeInfo.Type,
                Usage = TupleUsageType.TypeDeclaration,
                Location = tupleType.GetLocation()
            };
        }

        return null;
    }

    private static TupleUsageInfo? ExtractFromGenericName(GenericNameSyntax genericName, SemanticModel semanticModel)
    {
        var name = genericName.Identifier.ValueText;
        if (name is not ("ValueTuple" or "Tuple")) return null;

        var typeInfo = semanticModel.GetTypeInfo(genericName);
        if (typeInfo.Type != null && IsTupleType(typeInfo.Type))
        {
            return new TupleUsageInfo
            {
                TupleType = typeInfo.Type,
                Usage = TupleUsageType.TypeDeclaration,
                Location = genericName.GetLocation()
            };
        }

        return null;
    }

    private static List<ITypeSymbol> ExtractTupleTypesFromReturnType(ITypeSymbol returnType)
    {
        var tupleTypes = new List<ITypeSymbol>();

        if (IsTupleType(returnType))
        {
            tupleTypes.Add(returnType);
        }
        else if (returnType is INamedTypeSymbol namedType)
        {
            // Check if it's IEnumerable<TupleType>
            if (namedType.IsGenericType)
            {
                var typeArgs = namedType.TypeArguments;
                foreach (var typeArg in typeArgs)
                {
                    if (IsTupleType(typeArg))
                    {
                        tupleTypes.Add(typeArg);
                    }
                }
            }

            // Check implemented interfaces for IEnumerable<TupleType>
            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.Name == "IEnumerable" && iface.IsGenericType && iface.TypeArguments.Length > 0)
                {
                    var elementType = iface.TypeArguments[0];
                    if (IsTupleType(elementType))
                    {
                        tupleTypes.Add(elementType);
                    }
                }
            }
        }

        return tupleTypes;
    }

    private static bool IsTupleType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
        {
            return false;
        }

        var typeName = namedType.ConstructedFrom.Name;
        return typeName is "ValueTuple" or "Tuple";
    }

    private static void GenerateTupleProcessors(SourceProductionContext context,
        (ImmutableArray<TupleUsageInfo> usages, ImmutableArray<TupleDataSourceInfo> dataSources) data)
    {
        var (usages, dataSources) = data;

        if (usages.IsEmpty && dataSources.IsEmpty)
            return;

        var writer = new CodeWriter();

        writer.AppendLine("// <auto-generated/>");
        writer.AppendLine("#pragma warning disable");
        writer.AppendLine("#nullable enable");
        writer.AppendLine();
        writer.AppendLine("using System;");
        writer.AppendLine("using System.Collections.Generic;");
        writer.AppendLine();
        writer.AppendLine("namespace TUnit.Generated;");
        writer.AppendLine();

        GenerateTupleProcessorClass(writer, usages, dataSources);

        context.AddSource("AotTupleProcessors.g.cs", writer.ToString());
    }

    private static void GenerateTupleProcessorClass(CodeWriter writer,
        ImmutableArray<TupleUsageInfo> usages,
        ImmutableArray<TupleDataSourceInfo> dataSources)
    {
        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// AOT-compatible tuple processing helpers to replace reflection-based tuple operations");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static class AotTupleProcessor");
        writer.AppendLine("{");
        writer.Indent();

        // Collect all unique tuple types (only concrete types, no type parameters)
        var allTupleTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        
        foreach (var usage in usages)
        {
            if (IsConcreteType(usage.TupleType))
            {
                allTupleTypes.Add(usage.TupleType);
            }
        }
        
        foreach (var dataSource in dataSources)
        {
            foreach (var tupleType in dataSource.TupleTypes)
            {
                if (IsConcreteType(tupleType))
                {
                    allTupleTypes.Add(tupleType);
                }
            }
        }

        // Generate strongly-typed tuple processors for each tuple type
        var processedProcessorNames = new HashSet<string>();
        var processorNameMap = new Dictionary<string, string>();
        
        // Build the map of processor names first, ensuring unique names
        foreach (var tupleType in allTupleTypes)
        {
            if (IsConcreteType(tupleType))
            {
                var fullyQualifiedName = tupleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (!processorNameMap.ContainsKey(fullyQualifiedName))
                {
                    var processorName = GetUniqueTupleProcessorName(tupleType, new HashSet<string>(processorNameMap.Values));
                    processorNameMap[fullyQualifiedName] = processorName;
                }
            }
        }
        
        // Generate tuple type registry
        GenerateTupleRegistry(writer, allTupleTypes, processorNameMap);

        // Generate main helper methods
        GenerateMainHelperMethods(writer);

        // Generate processors using the mapped names
        foreach (var tupleType in allTupleTypes)
        {
            if (IsConcreteType(tupleType))
            {
                var fullyQualifiedName = tupleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var processorName = processorNameMap[fullyQualifiedName];
                if (processedProcessorNames.Add(processorName))
                {
                    GenerateTupleProcessor(writer, tupleType, processorName);
                }
            }
        }

        writer.Unindent();
        writer.AppendLine("}");
    }

    private static void GenerateTupleRegistry(CodeWriter writer, HashSet<ITypeSymbol> tupleTypes, Dictionary<string, string> processorNameMap)
    {
        writer.AppendLine("private static readonly Dictionary<Type, Func<object, object?[]?>> _tupleConverters = new()");
        writer.AppendLine("{");
        writer.Indent();

        var processedSignatures = new HashSet<string>();
        
        // Generate registry entries using the processor names from the map
        foreach (var tupleType in tupleTypes)
        {
            if (IsConcreteType(tupleType))
            {
                var fullyQualifiedName = tupleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                
                // Avoid duplicate entries by signature
                if (processedSignatures.Add(fullyQualifiedName))
                {
                    var processorName = processorNameMap[fullyQualifiedName];
                    writer.AppendLine($"[typeof({fullyQualifiedName})] = obj => {processorName}(({fullyQualifiedName})obj),");
                }
            }
        }

        writer.Unindent();
        writer.AppendLine("};");
        writer.AppendLine();

        writer.AppendLine("private static readonly HashSet<Type> _knownTupleTypes = new()");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var tupleType in tupleTypes)
        {
            if (IsConcreteType(tupleType))
            {
                var fullyQualifiedName = tupleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                writer.AppendLine($"typeof({fullyQualifiedName}),");
            }
        }

        writer.Unindent();
        writer.AppendLine("};");
        writer.AppendLine();
    }

    private static void GenerateMainHelperMethods(CodeWriter writer)
    {
        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Checks if a type is a known tuple type (AOT-safe replacement for IsTupleType)");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static bool IsTupleType(Type type)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("return _knownTupleTypes.Contains(type);");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();

        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Converts a tuple to an object array (AOT-safe replacement for ConvertTupleToArray)");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static object?[]? ConvertTupleToArray(object tuple)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("if (tuple == null) return null;");
        writer.AppendLine();
        writer.AppendLine("var tupleType = tuple.GetType();");
        writer.AppendLine("if (_tupleConverters.TryGetValue(tupleType, out var converter))");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("return converter(tuple);");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
        writer.AppendLine("throw new ArgumentException($\"Unsupported tuple type: {tupleType.FullName}\", nameof(tuple));");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();

        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Gets the tuple element count for a given tuple type");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static int GetTupleElementCount(Type tupleType)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("if (!IsTupleType(tupleType)) return 0;");
        writer.AppendLine();
        writer.AppendLine("// For known tuple types, return the generic argument count");
        writer.AppendLine("if (tupleType.IsGenericType)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("return tupleType.GetGenericArguments().Length;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
        writer.AppendLine("return 0;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }

    private static void GenerateTupleProcessor(CodeWriter writer, ITypeSymbol tupleType, string processorName)
    {
        if (tupleType is not INamedTypeSymbol namedTupleType || !namedTupleType.IsGenericType || !IsConcreteType(tupleType))
            return;

        var fullyQualifiedName = tupleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var elementTypes = namedTupleType.TypeArguments;
        
        // Only generate for concrete types (no type parameters)
        if (elementTypes.Any(t => t.TypeKind == TypeKind.TypeParameter))
        {
            return;
        }

        writer.AppendLine($"/// <summary>");
        writer.AppendLine($"/// Strongly-typed processor for {namedTupleType.Name} with {elementTypes.Length} elements");
        writer.AppendLine($"/// </summary>");
        writer.AppendLine($"private static object?[]? {processorName}({fullyQualifiedName} tuple)");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"return new object?[] {{ {GenerateTupleElementAccess(elementTypes.Length)} }};");

        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }

    private static bool IsConcreteType(ITypeSymbol type)
    {
        // Filter out type parameters and unresolved generic types
        if (type.TypeKind == TypeKind.TypeParameter)
        {
            return false;
        }
        
        // Check if this is a generic type with type parameters
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                if (typeArg.TypeKind == TypeKind.TypeParameter)
                {
                    return false;
                }
            }
        }
        
        // Must have a valid name and assembly
        if (string.IsNullOrEmpty(type.Name) || type.ContainingAssembly == null)
        {
            return false;
        }
        
        return true;
    }

    private static string GenerateTupleElementAccess(int elementCount)
    {
        var elements = new List<string>();
        
        // Handle ValueTuple's nested structure for more than 7 elements
        if (elementCount <= 7)
        {
            for (int i = 1; i <= elementCount; i++)
            {
                elements.Add($"tuple.Item{i}");
            }
        }
        else
        {
            // For 8+ elements, ValueTuple nests the 8th+ items in tuple.Rest
            for (int i = 1; i <= 7; i++)
            {
                elements.Add($"tuple.Item{i}");
            }
            
            // Access remaining elements through Rest property
            var restCount = elementCount - 7;
            for (int i = 1; i <= restCount; i++)
            {
                if (restCount == 1)
                {
                    elements.Add("tuple.Rest");
                }
                else
                {
                    elements.Add($"tuple.Rest.Item{i}");
                }
            }
        }
        
        return string.Join(", ", elements);
    }

    private static string GetUniqueTupleProcessorName(ITypeSymbol tupleType, HashSet<string> processedNames)
    {
        if (tupleType is not INamedTypeSymbol namedType || !namedType.IsGenericType)
            return "ProcessUnknownTuple";

        var baseName = namedType.ConstructedFrom.Name; // "ValueTuple" or "Tuple"
        var elementCount = namedType.TypeArguments.Length;
        
        // Create a base name
        var baseProcName = $"Process{baseName}{elementCount}";
        
        // If no conflicts, use the base name
        if (!processedNames.Contains(baseProcName))
        {
            return baseProcName;
        }
        
        // Generate a unique name by adding type hash
        var typeSignature = tupleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var hash = Math.Abs(typeSignature.GetHashCode()).ToString();
        var uniqueName = $"{baseProcName}_{hash}";
        
        // Ensure uniqueness
        int counter = 1;
        while (processedNames.Contains(uniqueName))
        {
            uniqueName = $"{baseProcName}_{hash}_{counter++}";
        }
        
        return uniqueName;
    }

    private sealed class TupleUsageInfo
    {
        public required ITypeSymbol TupleType { get; init; }
        public required TupleUsageType Usage { get; init; }
        public required Location Location { get; init; }
    }

    private sealed class TupleDataSourceInfo
    {
        public required IMethodSymbol Method { get; init; }
        public required ImmutableArray<ITypeSymbol> TupleTypes { get; init; }
        public required Location Location { get; init; }
    }

    private enum TupleUsageType
    {
        TypeDeclaration,
        FieldAccess,
        Conversion
    }
}