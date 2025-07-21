using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Helpers;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Generates AOT-compatible type resolution code to replace the reflection-based TypeResolver
/// </summary>
[Generator]
public sealed class AotTypeResolverGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all TypeReference usage and generic type instantiations
        var typeReferences = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsTypeReferenceUsage(node),
                transform: (ctx, _) => ExtractTypeReferenceInfo(ctx))
            .Where(x => x is not null)
            .Select((x, _) => x!);

        // Find all test classes and methods with generic parameters
        var genericTestInfo = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsTestClassOrMethod(node),
                transform: (ctx, _) => ExtractGenericTestInfo(ctx))
            .Where(x => x is not null)
            .Select((x, _) => x!);

        // Combine all type resolution requirements
        var allTypeInfo = typeReferences
            .Collect()
            .Combine(genericTestInfo.Collect());

        // Generate the type resolver
        context.RegisterSourceOutput(allTypeInfo, GenerateAotTypeResolver);
    }

    private static bool IsTypeReferenceUsage(SyntaxNode node)
    {
        // Look for TypeReference usage patterns
        if (node is InvocationExpressionSyntax invocation)
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            var identifierName = invocation.Expression as IdentifierNameSyntax;
            
            // Check for TypeResolver.Resolve() calls
            if (memberAccess?.Name.Identifier.ValueText == "Resolve" && 
                memberAccess.Expression.ToString().Contains("TypeResolver"))
            {
                return true;
            }
            
            // Check for Type.GetType() calls
            if (memberAccess?.Name.Identifier.ValueText == "GetType" &&
                memberAccess.Expression.ToString() == "Type")
            {
                return true;
            }
            
            // Check for MakeGenericType calls
            if (memberAccess?.Name.Identifier.ValueText == "MakeGenericType")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTestClassOrMethod(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { TypeParameterList: not null } ||
               node is MethodDeclarationSyntax { TypeParameterList: not null };
    }

    private static TypeReferenceInfo? ExtractTypeReferenceInfo(GeneratorSyntaxContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Analyze the invocation to determine what types are being resolved
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol method)
            return null;

        var containingType = method.ContainingType;
        var typeArguments = new List<ITypeSymbol>();
        
        // Extract type information from the method call
        if (method.Name == "Resolve" && containingType?.Name == "TypeResolver")
        {
            // Try to extract type information from TypeReference parameter
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var argument = invocation.ArgumentList.Arguments[0];
                var argType = semanticModel.GetTypeInfo(argument.Expression).Type;
                if (argType != null)
                {
                    typeArguments.Add(argType);
                }
            }
        }
        else if (method.Name == "GetType" && containingType?.Name == "Type")
        {
            // Extract type from Type.GetType(string) calls
            if (invocation.ArgumentList.Arguments.Count > 0 &&
                invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal)
            {
                var typeName = literal.Token.ValueText;
                if (!string.IsNullOrEmpty(typeName))
                {
                    var resolvedType = semanticModel.Compilation.GetTypeByMetadataName(typeName);
                    if (resolvedType != null)
                    {
                        typeArguments.Add(resolvedType);
                    }
                }
            }
        }
        else if (method.Name == "MakeGenericType")
        {
            // Extract generic type and its arguments
            var receiverType = semanticModel.GetTypeInfo(((MemberAccessExpressionSyntax)invocation.Expression).Expression).Type;
            if (receiverType != null)
            {
                typeArguments.Add(receiverType);
                
                // Extract type arguments passed to MakeGenericType
                foreach (var arg in invocation.ArgumentList.Arguments)
                {
                    var argTypeInfo = semanticModel.GetTypeInfo(arg.Expression);
                    if (argTypeInfo.Type != null)
                    {
                        typeArguments.Add(argTypeInfo.Type);
                    }
                }
            }
        }

        if (typeArguments.Count == 0)
            return null;

        return new TypeReferenceInfo
        {
            ReferencedTypes = typeArguments.ToImmutableArray(),
            Location = invocation.GetLocation(),
            Usage = method.Name switch
            {
                "Resolve" => TypeReferenceUsage.TypeResolverResolve,
                "GetType" => TypeReferenceUsage.TypeGetType,
                "MakeGenericType" => TypeReferenceUsage.MakeGenericType,
                _ => TypeReferenceUsage.Unknown
            }
        };
    }

    private static GenericTestInfo? ExtractGenericTestInfo(GeneratorSyntaxContext context)
    {
        var semanticModel = context.SemanticModel;
        
        if (context.Node is ClassDeclarationSyntax classDecl)
        {
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            if (classSymbol?.TypeParameters.Length > 0)
            {
                // Check if this is a test class
                if (HasTestMethods(classSymbol))
                {
                    return new GenericTestInfo
                    {
                        TypeSymbol = classSymbol,
                        TypeParameters = classSymbol.TypeParameters.ToImmutableArray(),
                        IsClass = true
                    };
                }
            }
        }
        else if (context.Node is MethodDeclarationSyntax methodDecl)
        {
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
            if (methodSymbol?.TypeParameters.Length > 0)
            {
                // Check if this is a test method
                if (HasTestAttribute(methodSymbol))
                {
                    return new GenericTestInfo
                    {
                        MethodSymbol = methodSymbol,
                        TypeParameters = methodSymbol.TypeParameters.ToImmutableArray(),
                        IsClass = false
                    };
                }
            }
        }

        return null;
    }

    private static void GenerateAotTypeResolver(SourceProductionContext context, 
        (ImmutableArray<TypeReferenceInfo> typeRefs, ImmutableArray<GenericTestInfo> genericTests) data)
    {
        var (typeReferences, genericTests) = data;
        
        if (typeReferences.IsEmpty && genericTests.IsEmpty)
            return;

        var writer = new CodeWriter();
        
        writer.AppendLine("// <auto-generated/>");
        writer.AppendLine("#pragma warning disable");
        writer.AppendLine("#nullable enable");
        writer.AppendLine();
        writer.AppendLine("using System;");
        writer.AppendLine("using System.Collections.Generic;");
        writer.AppendLine("using System.Collections.Concurrent;");
        writer.AppendLine();
        writer.AppendLine("namespace TUnit.Generated;");
        writer.AppendLine();
        
        GenerateAotTypeResolverClass(writer, typeReferences, genericTests);
        
        context.AddSource("AotTypeResolver.g.cs", writer.ToString());
    }

    private static void GenerateAotTypeResolverClass(CodeWriter writer, 
        ImmutableArray<TypeReferenceInfo> typeReferences, 
        ImmutableArray<GenericTestInfo> genericTests)
    {
        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// AOT-compatible type resolver that replaces reflection-based TypeResolver");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static class AotTypeResolver");
        writer.AppendLine("{");
        writer.Indent();

        // Generate type cache
        writer.AppendLine("private static readonly ConcurrentDictionary<string, Type> _typeCache = new();");
        writer.AppendLine("private static readonly ConcurrentDictionary<(Type, Type[]), Type> _genericTypeCache = new();");
        writer.AppendLine();

        // Collect all unique types referenced (only concrete types, no type parameters)
        var allReferencedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        
        foreach (var typeRef in typeReferences)
        {
            foreach (var type in typeRef.ReferencedTypes)
            {
                if (IsConcreteType(type))
                {
                    allReferencedTypes.Add(type);
                }
            }
        }
        
        foreach (var genericTest in genericTests)
        {
            if (genericTest.TypeSymbol != null && IsConcreteType(genericTest.TypeSymbol))
            {
                allReferencedTypes.Add(genericTest.TypeSymbol);
            }
        }

        // Generate static constructor to initialize type cache
        GenerateStaticConstructor(writer, allReferencedTypes);

        // Generate type resolution methods
        GenerateResolveTypeMethod(writer, allReferencedTypes);
        GenerateMakeGenericTypeMethod(writer, typeReferences, genericTests);
        GenerateGenericTypeFactories(writer, genericTests);

        writer.Unindent();
        writer.AppendLine("}");
    }

    private static void GenerateStaticConstructor(CodeWriter writer, HashSet<ITypeSymbol> allTypes)
    {
        writer.AppendLine("static AotTypeResolver()");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("// Populate type cache with all referenced types");
        foreach (var type in allTypes)
        {
            // Only generate code for concrete types
            if (IsConcreteType(type))
            {
                var fullyQualifiedName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var assemblyQualifiedName = GetAssemblyQualifiedName(type);
                
                writer.AppendLine($"_typeCache[\"{assemblyQualifiedName}\"] = typeof({fullyQualifiedName});");
            }
        }
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }

    private static void GenerateResolveTypeMethod(CodeWriter writer, HashSet<ITypeSymbol> allTypes)
    {
        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Resolves a type by its assembly-qualified name (AOT-safe replacement for Type.GetType)");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static Type? ResolveType(string assemblyQualifiedName)");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("if (string.IsNullOrEmpty(assemblyQualifiedName))");
        writer.AppendLine("    return null;");
        writer.AppendLine();
        writer.AppendLine("if (_typeCache.TryGetValue(assemblyQualifiedName, out var cachedType))");
        writer.AppendLine("    return cachedType;");
        writer.AppendLine();
        
        writer.AppendLine("// Fast path for known types");
        writer.AppendLine("return assemblyQualifiedName switch");
        writer.AppendLine("{");
        writer.Indent();
        
        foreach (var type in allTypes)
        {
            // Only generate code for concrete types
            if (IsConcreteType(type))
            {
                var fullyQualifiedName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var assemblyQualifiedName = GetAssemblyQualifiedName(type);
                writer.AppendLine($"\"{assemblyQualifiedName}\" => typeof({fullyQualifiedName}),");
            }
        }
        
        writer.AppendLine("_ => null");
        writer.Unindent();
        writer.AppendLine("};");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }

    private static void GenerateMakeGenericTypeMethod(CodeWriter writer, 
        ImmutableArray<TypeReferenceInfo> typeReferences, 
        ImmutableArray<GenericTestInfo> genericTests)
    {
        writer.AppendLine("/// <summary>");
        writer.AppendLine("/// Creates a generic type (AOT-safe replacement for Type.MakeGenericType)");
        writer.AppendLine("/// </summary>");
        writer.AppendLine("public static Type? MakeGenericType(Type genericTypeDefinition, params Type[] typeArguments)");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("if (genericTypeDefinition == null || typeArguments == null || typeArguments.Length == 0)");
        writer.AppendLine("    return null;");
        writer.AppendLine();
        writer.AppendLine("var cacheKey = (genericTypeDefinition, typeArguments);");
        writer.AppendLine("if (_genericTypeCache.TryGetValue(cacheKey, out var cachedGenericType))");
        writer.AppendLine("    return cachedGenericType;");
        writer.AppendLine();

        // Generate switch for known generic type combinations
        var genericTypeCombinations = ExtractGenericTypeCombinations(typeReferences, genericTests);
        
        if (genericTypeCombinations.Count > 0)
        {
            writer.AppendLine("// Handle known generic type combinations");
            writer.AppendLine($"if (genericTypeDefinition == typeof({genericTypeCombinations[0].GenericDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}) && typeArguments.Length == {genericTypeCombinations[0].TypeArguments.Length})");
            writer.AppendLine("{");
            writer.Indent();
            
            foreach (var combination in genericTypeCombinations)
            {
                GenerateGenericTypeCombination(writer, combination);
            }
            
            writer.Unindent();
            writer.AppendLine("}");
        }

        writer.AppendLine();
        writer.AppendLine("return null;");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }

    private static void GenerateGenericTypeFactories(CodeWriter writer, ImmutableArray<GenericTestInfo> genericTests)
    {
        var processedTypes = new HashSet<string>();
        
        foreach (var genericTest in genericTests)
        {
            if (genericTest.TypeSymbol != null)
            {
                var typeName = genericTest.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (processedTypes.Add(typeName))
                {
                    GenerateGenericTypeFactory(writer, genericTest.TypeSymbol);
                }
            }
        }
    }

    private static void GenerateGenericTypeFactory(CodeWriter writer, INamedTypeSymbol genericType)
    {
        // Skip if the type contains unresolved generic parameters
        if (ContainsUnresolvedGenericParameters(genericType))
        {
            return;
        }
        
        var safeName = GetSafeTypeName(genericType);
        var fullyQualifiedName = genericType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        writer.AppendLine($"/// <summary>");
        writer.AppendLine($"/// Factory for creating instances of {genericType.Name}");
        writer.AppendLine($"/// </summary>");
        writer.AppendLine($"public static Type Create{safeName}Type(params Type[] typeArguments)");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"if (typeArguments.Length != {genericType.TypeParameters.Length})");
        writer.AppendLine($"    throw new ArgumentException($\"Expected {genericType.TypeParameters.Length} type arguments, got {{typeArguments.Length}}\");");
        writer.AppendLine();
        
        // Generate specific combinations if we can infer them
        writer.AppendLine($"// Return constructed generic type");
        writer.AppendLine($"return typeof({fullyQualifiedName}).MakeGenericType(typeArguments);");
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }

    private static bool ContainsUnresolvedGenericParameters(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.TypeParameter)
        {
            return true;
        }
        
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                if (ContainsUnresolvedGenericParameters(typeArg))
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    private static void GenerateGenericTypeCombination(CodeWriter writer, GenericTypeCombination combination)
    {
        var conditions = new List<string>();
        
        // Only generate combinations for concrete types, not generic type parameters
        bool hasGenericParameters = false;
        for (int i = 0; i < combination.TypeArguments.Length; i++)
        {
            var typeArg = combination.TypeArguments[i];
            
            // Skip if this is a generic type parameter (like 'T')
            if (typeArg.TypeKind == TypeKind.TypeParameter)
            {
                hasGenericParameters = true;
                break;
            }
            
            var fullyQualifiedName = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            conditions.Add($"typeArguments[{i}] == typeof({fullyQualifiedName})");
        }
        
        // Only generate code for concrete type combinations
        if (hasGenericParameters || conditions.Count == 0)
        {
            return;
        }
        
        writer.AppendLine($"if ({string.Join(" && ", conditions)})");
        writer.AppendLine("{");
        writer.Indent();
        
        // Build the constructed type with concrete types only
        var typeArgStrings = combination.TypeArguments
            .Where(t => t.TypeKind != TypeKind.TypeParameter)
            .Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            
        if (typeArgStrings.Count() == combination.TypeArguments.Length)
        {
            var constructedType = $"{combination.GenericDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}<{string.Join(", ", typeArgStrings)}>";
            writer.AppendLine($"var result = typeof({constructedType});");
            writer.AppendLine("_genericTypeCache[cacheKey] = result;");
            writer.AppendLine("return result;");
        }
        
        writer.Unindent();
        writer.AppendLine("}");
    }

    private static List<GenericTypeCombination> ExtractGenericTypeCombinations(
        ImmutableArray<TypeReferenceInfo> typeReferences, 
        ImmutableArray<GenericTestInfo> genericTests)
    {
        var combinations = new List<GenericTypeCombination>();
        
        // Extract combinations from type references
        foreach (var typeRef in typeReferences)
        {
            if (typeRef.Usage == TypeReferenceUsage.MakeGenericType && typeRef.ReferencedTypes.Length >= 2)
            {
                var genericDef = typeRef.ReferencedTypes[0];
                var typeArgs = typeRef.ReferencedTypes.Skip(1).ToImmutableArray();
                
                combinations.Add(new GenericTypeCombination
                {
                    GenericDefinition = genericDef,
                    TypeArguments = typeArgs
                });
            }
        }
        
        return combinations.Distinct().ToList();
    }

    private static string GetAssemblyQualifiedName(ITypeSymbol type)
    {
        // Simplified assembly qualified name generation
        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var assemblyName = type.ContainingAssembly?.Name ?? "UnknownAssembly";
        return $"{typeName}, {assemblyName}";
    }

    private static string GetSafeTypeName(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "");
    }

    private static bool HasTestMethods(INamedTypeSymbol type)
    {
        return type.GetMembers()
            .OfType<IMethodSymbol>()
            .Any(HasTestAttribute);
    }

    private static bool HasTestAttribute(IMethodSymbol method)
    {
        return method.GetAttributes().Any(a =>
            a.AttributeClass?.Name == "TestAttribute" ||
            a.AttributeClass?.Name == "Test");
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

    private sealed class TypeReferenceInfo
    {
        public required ImmutableArray<ITypeSymbol> ReferencedTypes { get; init; }
        public required Location Location { get; init; }
        public required TypeReferenceUsage Usage { get; init; }
    }

    private sealed class GenericTestInfo
    {
        public INamedTypeSymbol? TypeSymbol { get; init; }
        public IMethodSymbol? MethodSymbol { get; init; }
        public required ImmutableArray<ITypeParameterSymbol> TypeParameters { get; init; }
        public required bool IsClass { get; init; }
    }

    private sealed class GenericTypeCombination
    {
        public required ITypeSymbol GenericDefinition { get; init; }
        public required ImmutableArray<ITypeSymbol> TypeArguments { get; init; }

        public override bool Equals(object? obj)
        {
            if (obj is not GenericTypeCombination other) return false;
            return SymbolEqualityComparer.Default.Equals(GenericDefinition, other.GenericDefinition) &&
                   System.Linq.Enumerable.SequenceEqual(TypeArguments, other.TypeArguments, SymbolEqualityComparer.Default);
        }

        public override int GetHashCode()
        {
            var hash = SymbolEqualityComparer.Default.GetHashCode(GenericDefinition);
            foreach (var typeArg in TypeArguments)
            {
                hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(typeArg);
            }
            return hash;
        }
    }

    private enum TypeReferenceUsage
    {
        Unknown,
        TypeResolverResolve,
        TypeGetType,
        MakeGenericType
    }
}