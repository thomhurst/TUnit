using System.Globalization;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators;

[Generator]
public class AotConverterGenerator : IIncrementalGenerator
{
    public static string ParseAotConverter = "ParseCompilationMetadata";

    // TestAttribute is sealed and BaseTestAttribute's constructor is internal to TUnit.Core. Subclasses of
    // DynamicTestBuilderAttribute are not matched, consistent with DynamicTestsGenerator, which also matches
    // this exact name, so such methods are never source-generated tests and need no converters.
    private const string TestAttributeMetadataName = "TUnit.Core.TestAttribute";
    private const string DynamicTestBuilderAttributeMetadataName = "TUnit.Core.DynamicTestBuilderAttribute";

    // Per-compilation caches. Many tests share parameter types (and BCL types such as string), and
    // every test in a class shares the class-level scan, so these are computed once per compilation
    // instead of once per test method.
    private static readonly ConditionalWeakTable<Compilation, CompilationCaches> Caches = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enabledProvider = context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.EnableTUnitSourceGeneration", out var value);
                return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
            });

        var testMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                TestAttributeMetadataName,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, ct) => GetTestMethodConversions(ctx, ct))
            .Collect();

        var dynamicTestBuilders = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                DynamicTestBuilderAttributeMetadataName,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, ct) => GetTestMethodConversions(ctx, ct))
            .Collect();

        var allTypes = testMethods
            .Combine(dynamicTestBuilders)
            .Select(static (pair, _) => MergeConversions(pair.Left, pair.Right))
            .Combine(enabledProvider)
            .WithTrackingName(ParseAotConverter);

        context.RegisterSourceOutput(allTypes, (spc, data) =>
        {
            var (source, isEnabled) = data;
            if (!isEnabled)
            {
                return;
            }
            try
            {
                GenerateConverters(spc, source);
            }
            catch (Exception e)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "TUNITGEN001",
                        title: "TUnit.AotConverterGenerator Failed",
                        messageFormat: "AotConverterGenerator failed: {0}: {1}",
                        category: "TUnit.Generator",
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        description: e.ToString()),
                    Location.None,
                    e.GetType().Name,
                    e.Message));
            }
        });
    }

    private static TestMethodConversions? GetTestMethodConversions(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        try
        {
            var compilation = context.SemanticModel.Compilation;
            var caches = Caches.GetValue(compilation, static c => new CompilationCaches(c));

            // Types used by the test method's parameters and data source attributes.
            var methodTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            var orderedMethodTypes = new List<ITypeSymbol>();
            foreach (var parameter in methodSymbol.Parameters)
            {
                AddType(parameter.Type, methodTypes, orderedMethodTypes);
                ScanAttributesForTypes(parameter.GetAttributes(), methodTypes, orderedMethodTypes);
            }

            ScanAttributesForTypes(methodSymbol.GetAttributes(), methodTypes, orderedMethodTypes);

            var classConversions = methodSymbol.ContainingType is { } containingType
                ? caches.GetClassConversions(containingType, cancellationToken)
                : null;

            return new TestMethodConversions(
                classConversions?.TreeIndex ?? -1,
                classConversions?.Position ?? -1,
                classConversions?.Conversions ?? EquatableArray<ConversionEntry>.Empty,
                caches.GetTreeIndex(context.TargetNode.SyntaxTree),
                context.TargetNode.SpanStart,
                caches.GetConversions(orderedMethodTypes, cancellationToken));
        }
        catch (NullReferenceException ex)
        {
            var stackTrace = ex.StackTrace ?? "No stack trace";
            throw new InvalidOperationException($"NullReferenceException in ScanTestParameters for {methodSymbol.ToDisplayString()}: {ex.Message}\nStack: {stackTrace}", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Error in AotConverterGenerator.ScanTestParameters for {methodSymbol.ToDisplayString()}: {ex.GetType().Name}: {ex.Message}", ex);
        }
    }

    private static EquatableArray<ConversionMetadata> MergeConversions(
        ImmutableArray<TestMethodConversions?> testMethods,
        ImmutableArray<TestMethodConversions?> dynamicTestBuilders)
    {
        // Rebuild the declaration order of a full syntax walk (a test class is scanned at its first
        // declaration, a test method at its own position) so converter numbering stays stable.
        var entries = new List<(int TreeIndex, int Position, EquatableArray<ConversionEntry> Conversions)>();

        foreach (var method in testMethods.Concat(dynamicTestBuilders))
        {
            if (method is null)
            {
                continue;
            }

            if (method.ClassTreeIndex >= 0)
            {
                entries.Add((method.ClassTreeIndex, method.ClassPosition, method.ClassConversions));
            }

            entries.Add((method.MethodTreeIndex, method.MethodPosition, method.MethodConversions));
        }

        // Stable sort (OrderBy) keeps duplicates of the same class entry together.
        var seenConversions = new HashSet<string>();
        var uniqueConversions = new List<ConversionMetadata>();

        foreach (var entry in entries.OrderBy(static e => e.TreeIndex).ThenBy(static e => e.Position))
        {
            foreach (var conversion in entry.Conversions)
            {
                // Deduplicate conversions based on source and target types
                if (seenConversions.Add(conversion.Key))
                {
                    uniqueConversions.Add(conversion.Metadata);
                }
            }
        }

        return uniqueConversions.ToEquatableArray();
    }

    private static void AddType(ITypeSymbol type, HashSet<ITypeSymbol> typesToScan, List<ITypeSymbol> orderedTypes)
    {
        if (typesToScan.Add(type))
        {
            orderedTypes.Add(type);
        }
    }

    private static void ScanAttributesForTypes(ImmutableArray<AttributeData> attributes, HashSet<ITypeSymbol> typesToScan, List<ITypeSymbol> orderedTypes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass == null)
            {
                continue;
            }

            if (!DataSourceAttributeHelper.IsDataSourceAttribute(attribute.AttributeClass))
            {
                continue;
            }

            if (attribute.AttributeClass.IsGenericType)
            {
                foreach (var typeArg in attribute.AttributeClass.TypeArguments)
                {
                    AddType(typeArg, typesToScan, orderedTypes);
                }
            }

            foreach (var arg in attribute.ConstructorArguments)
            {
                ScanTypedConstantForTypes(arg, typesToScan, orderedTypes);
            }

            foreach (var namedArg in attribute.NamedArguments)
            {
                ScanTypedConstantForTypes(namedArg.Value, typesToScan, orderedTypes);
            }
        }
    }

    private static void ScanTypedConstantForTypes(TypedConstant constant, HashSet<ITypeSymbol> typesToScan, List<ITypeSymbol> orderedTypes)
    {
        if (constant.IsNull)
        {
            return;
        }

        if (constant is { Kind: TypedConstantKind.Type, Value: ITypeSymbol typeValue })
        {
            AddType(typeValue, typesToScan, orderedTypes);
        }

        else if (constant is { Kind: TypedConstantKind.Array, IsNull: false })
        {
            foreach (var element in constant.Values)
            {
                ScanTypedConstantForTypes(element, typesToScan, orderedTypes);
            }
        }
        else if (constant.Kind != TypedConstantKind.Array && constant is { Value: not null, Type: not null })
        {
            AddType(constant.Type, typesToScan, orderedTypes);
        }
    }

    private static void CollectConversionsForType(ITypeSymbol type, List<ConversionEntry> conversions, Compilation compilation)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return;
        }

        if (!ShouldIncludeType(namedType, compilation))
        {
            return;
        }

        var conversionOperators = namedType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => (m.Name == "op_Implicit" || m.Name == "op_Explicit") &&
                        m is { IsStatic: true, Parameters.Length: 1 });

        foreach (var method in conversionOperators)
        {
            var conversionInfo = GetConversionInfoFromSymbol(method, compilation);
            if (conversionInfo != null)
            {
                conversions.Add(ToConversionEntry(conversionInfo));
            }
        }

        if (namedType.IsGenericType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                CollectConversionsForType(typeArg, conversions, compilation);
            }
        }
    }

    private static ConversionEntry ToConversionEntry(ConversionInfo conversion)
    {
        // FullyQualifiedFormat omits nullable reference annotations, so the key matches the
        // SymbolEqualityComparer.Default identity of the (source, target) type pair.
        var key = conversion.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                  + " -> "
                  + conversion.TargetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return new ConversionEntry(key, new ConversionMetadata
        {
            SourceType = ToTypeMetadata(conversion.SourceType),
            TargetType = ToTypeMetadata(conversion.TargetType),
            TypesAreDifferent = !SymbolEqualityComparer.Default.Equals(conversion.SourceType, conversion.TargetType),
            IsImplicit = conversion.IsImplicit,
        });
    }

    private static bool ShouldIncludeType(INamedTypeSymbol type, Compilation compilation)
    {
        var typeAssembly = type.ContainingAssembly;
        var currentAssembly = compilation.Assembly;

        if (currentAssembly == null)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(typeAssembly, currentAssembly))
        {
            return true;
        }

        if (type.DeclaredAccessibility == Accessibility.Public)
        {
            return true;
        }

        if (type.DeclaredAccessibility == Accessibility.Internal)
        {
            if (typeAssembly != null && typeAssembly.GivesAccessTo(currentAssembly))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAccessibleType(ITypeSymbol type, Compilation compilation)
    {
        if (type == null || compilation == null)
        {
            return false;
        }

        if (type.SpecialType != SpecialType.None)
        {
            return true;
        }

        if (type.TypeKind == TypeKind.TypeParameter)
        {
            return true;
        }

        if (type is INamedTypeSymbol namedType)
        {
            var typeAssembly = namedType.ContainingAssembly;
            var currentAssembly = compilation.Assembly;

            if (currentAssembly != null && SymbolEqualityComparer.Default.Equals(typeAssembly, currentAssembly))
            {
                return true;
            }

            if (namedType.DeclaredAccessibility == Accessibility.Public)
            {
                return true;
            }

            if (namedType.DeclaredAccessibility == Accessibility.Internal)
            {
                if (currentAssembly == null)
                {
                    return false;
                }

                if (typeAssembly != null && typeAssembly.GivesAccessTo(currentAssembly))
                {
                    return true;
                }

                return false;
            }

            if (namedType.IsGenericType)
            {
                foreach (var typeArg in namedType.TypeArguments)
                {
                    if (!IsAccessibleType(typeArg, compilation))
                    {
                        return false;
                    }
                }
            }

            if (namedType.ContainingType != null)
            {
                return IsAccessibleType(namedType.ContainingType, compilation);
            }

            return false;
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return IsAccessibleType(arrayType.ElementType, compilation);
        }

        if (type is IPointerTypeSymbol pointerType)
        {
            return IsAccessibleType(pointerType.PointedAtType, compilation);
        }

        return false;
    }

    private static ConversionInfo? GetConversionInfoFromSymbol(IMethodSymbol methodSymbol, Compilation compilation)
    {
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
        {
            return null;
        }

        var sourceType = methodSymbol.Parameters[0].Type;
        var targetType = methodSymbol.ReturnType;
        var isImplicit = methodSymbol.Name == "op_Implicit";

        if (sourceType.IsGenericDefinition() || targetType.IsGenericDefinition())
        {
            return null;
        }

        if (TypeContainsGenericTypeParameters(sourceType) || TypeContainsGenericTypeParameters(targetType))
        {
            return null;
        }

        if (sourceType.IsRefLikeType || targetType.IsRefLikeType)
        {
            return null;
        }

        if (sourceType.TypeKind == TypeKind.Pointer || targetType.TypeKind == TypeKind.Pointer ||
            sourceType.SpecialType == SpecialType.System_Void || targetType.SpecialType == SpecialType.System_Void)
        {
            return null;
        }

        if (!IsAccessibleType(containingType, compilation))
        {
            return null;
        }

        if (!IsAccessibleType(sourceType, compilation) || !IsAccessibleType(targetType, compilation))
        {
            return null;
        }

        return new ConversionInfo
        {
            SourceType = sourceType,
            TargetType = targetType,
            IsImplicit = isImplicit,
        };
    }

    private static bool TypeContainsGenericTypeParameters(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.TypeParameter)
        {
            return true;
        }

        if (type is INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var typeArgument in namedTypeSymbol.TypeArguments)
            {
                if (TypeContainsGenericTypeParameters(typeArgument))
                {
                    return true;
                }
            }
        }

        if (type is IArrayTypeSymbol arrayTypeSymbol)
        {
            return TypeContainsGenericTypeParameters(arrayTypeSymbol.ElementType);
        }

        if (type is IPointerTypeSymbol pointerTypeSymbol)
        {
            return TypeContainsGenericTypeParameters(pointerTypeSymbol.PointedAtType);
        }

        return false;
    }

    private static void GenerateConverters(SourceProductionContext context, EquatableArray<ConversionMetadata> conversions)
    {
        var writer = new CodeWriter();
        writer.AppendLine("#nullable enable");

        if (conversions.Length == 0)
        {
            writer.AppendLine();
            writer.AppendLine("// No conversion operators found");
            context.AddSource("AotConverters.g.cs", writer.ToString());
            return;
        }

        writer.AppendLine();
        writer.AppendLine("using System;");
        writer.AppendLine("using TUnit.Core.Converters;");
        writer.AppendLine();
        writer.AppendLine("namespace TUnit.Generated");
        writer.AppendLine("{");
        writer.Indent();

        var converterIndex = 0;
        var converterClassNames = new List<string>();

        foreach (var conversion in conversions)
        {
            try
            {
                if (conversion.SourceType == null || conversion.TargetType == null)
                {
                    var sourceDisplay = conversion.SourceType?.DisplayString ?? "null";
                    var targetDisplay = conversion.TargetType?.DisplayString ?? "null";
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "TUNITGEN002",
                            title: "Null type in conversion",
                            messageFormat: "Skipping converter generation: SourceType={0}, TargetType={1}. Check test data sources that use implicit conversions between these types.",
                            category: "TUnit.Generator",
                            defaultSeverity: DiagnosticSeverity.Warning,
                            isEnabledByDefault: true),
                        Location.None,
                        sourceDisplay,
                        targetDisplay));
                    continue;
                }
            }
            catch (Exception ex)
            {
                var sourceDisplay = conversion.SourceType?.DisplayString ?? "unknown";
                var targetDisplay = conversion.TargetType?.DisplayString ?? "unknown";
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "TUNITGEN003",
                        title: "Error checking conversion types",
                        messageFormat: "Error checking conversion types (SourceType={0}, TargetType={1}): {2}",
                        category: "TUnit.Generator",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    Location.None,
                    sourceDisplay,
                    targetDisplay,
                    ex.ToString()));
                continue;
            }

            var converterClassName = $"AotConverter_{(converterIndex++).ToString(CultureInfo.InvariantCulture)}";
            var sourceTypeName = conversion.SourceType.GloballyQualified;
            var targetTypeName = conversion.TargetType.GloballyQualified;

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

            // Use Zen's more robust approach for handling nullable types and type checks
            var sourceType = conversion.SourceType;
            var targetType = conversion.TargetType;

            writer.AppendLine($"if (value is {targetType.PatternTypeName} targetTypedValue)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("return targetTypedValue;");
            writer.Unindent();
            writer.AppendLine("}");

            // 2. If types are different, generate the fallback check for the source type.
            //    This handles cases that require an implicit conversion.
            if (conversion.TypesAreDifferent)
            {
                writer.AppendLine();
                writer.AppendLine($"if (value is {sourceType.PatternTypeName} sourceTypedValue)");
                writer.AppendLine("{");
                writer.Indent();
                // For explicit conversions, we need to use an explicit cast
                // For implicit conversions, variable assignment works fine
                if (conversion.IsImplicit)
                {
                    writer.AppendLine($"{targetTypeName} converted = sourceTypedValue;");
                }
                else
                {
                    writer.AppendLine($"{targetTypeName} converted = ({targetTypeName})sourceTypedValue;");
                }
                writer.AppendLine("return converted;");
                writer.Unindent();
                writer.AppendLine("}");
            }

            writer.AppendLine("return value; // Return original value if type doesn't match");

            writer.Unindent();
            writer.AppendLine("}");

            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine();

            converterClassNames.Add(converterClassName);
        }

        // Contribute static field initializers to the shared TUnit_ConverterRegistration partial
        // (declared by InfrastructureGenerator). The compiler merges all contributions into one
        // .cctor, triggered once via RunClassConstructor from the single module initializer —
        // avoiding a separate [ModuleInitializer] method here.
        writer.AppendLine("internal static partial class TUnit_ConverterRegistration");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var converterClassName in converterClassNames)
        {
            writer.AppendLine($"static readonly int _r_{converterClassName} = global::TUnit.Core.Converters.AotConverterRegistry.Register(new {converterClassName}());");
        }

        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("}");

        context.AddSource("AotConverters.g.cs", writer.ToString());
    }

    private static TypeMetadata ToTypeMetadata(ITypeSymbol type)
    {
        var globallyQualified = type.GloballyQualified();

        // For pattern matching, we must unwrap nullable types (C# language requirement - CS8116)
        string patternTypeName = globallyQualified;
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments.Length: > 0 } nullableSourceType)
        {
            patternTypeName = nullableSourceType.TypeArguments[0].GloballyQualified();
        }
        return new TypeMetadata(globallyQualified, type.ToDisplayString(), patternTypeName);
    }

    public record TypeMetadata(string GloballyQualified, string DisplayString, string PatternTypeName);

    public record ConversionMetadata
    {
        public required TypeMetadata SourceType { get; init; }
        public required TypeMetadata TargetType { get; init; }
        public required bool TypesAreDifferent { get; init; }
        public required bool IsImplicit { get; init; }
    }

    /// <summary>
    /// The conversions needed by one test method, positioned so <see cref="MergeConversions"/> can restore
    /// the order of a full syntax walk. The class entry is absent (tree index -1) when the containing type
    /// has no class declaration (for example a record), which the full walk never scanned either.
    /// </summary>
    internal sealed record TestMethodConversions(
        int ClassTreeIndex,
        int ClassPosition,
        EquatableArray<ConversionEntry> ClassConversions,
        int MethodTreeIndex,
        int MethodPosition,
        EquatableArray<ConversionEntry> MethodConversions);

    /// <param name="Key">Identity of the (source, target) type pair used for deduplication.</param>
    /// <param name="Metadata">The conversion to generate.</param>
    internal sealed record ConversionEntry(string Key, ConversionMetadata Metadata);

    private sealed record ClassConversions(int TreeIndex, int Position, EquatableArray<ConversionEntry> Conversions);

    private sealed class CompilationCaches(Compilation compilation)
    {
        private readonly ConcurrentDictionary<ITypeSymbol, ConversionEntry[]> _typeConversions = new(SymbolEqualityComparer.IncludeNullability);
        private readonly ConcurrentDictionary<INamedTypeSymbol, ClassConversions?> _classConversions = new(SymbolEqualityComparer.Default);
        private readonly object _treeIndicesLock = new();
        private Dictionary<SyntaxTree, int>? _treeIndices;

        public int GetTreeIndex(SyntaxTree tree)
        {
            var treeIndices = _treeIndices;
            if (treeIndices is null)
            {
                lock (_treeIndicesLock)
                {
                    treeIndices = _treeIndices;
                    if (treeIndices is null)
                    {
                        treeIndices = new Dictionary<SyntaxTree, int>();
                        var index = 0;
                        foreach (var syntaxTree in compilation.SyntaxTrees)
                        {
                            treeIndices[syntaxTree] = index++;
                        }

                        _treeIndices = treeIndices;
                    }
                }
            }

            return treeIndices.TryGetValue(tree, out var treeIndex) ? treeIndex : int.MaxValue;
        }

        public EquatableArray<ConversionEntry> GetConversions(List<ITypeSymbol> types, CancellationToken cancellationToken)
        {
            List<ConversionEntry>? conversions = null;

            foreach (var type in types)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_typeConversions.TryGetValue(type, out var typeConversions))
                {
                    var collected = new List<ConversionEntry>();
                    CollectConversionsForType(type, collected, compilation);
                    typeConversions = _typeConversions.GetOrAdd(type, collected.ToArray());
                }

                if (typeConversions.Length > 0)
                {
                    conversions ??= [];
                    conversions.AddRange(typeConversions);
                }
            }

            return conversions is null ? EquatableArray<ConversionEntry>.Empty : conversions.ToEquatableArray();
        }

        public ClassConversions? GetClassConversions(INamedTypeSymbol classSymbol, CancellationToken cancellationToken)
        {
            if (_classConversions.TryGetValue(classSymbol, out var classConversions))
            {
                return classConversions;
            }

            return _classConversions.GetOrAdd(classSymbol, CreateClassConversions(classSymbol, cancellationToken));
        }

        private ClassConversions? CreateClassConversions(INamedTypeSymbol classSymbol, CancellationToken cancellationToken)
        {
            // A test class is scanned where its first class declaration appears.
            ClassDeclarationSyntax? classDeclaration = null;
            foreach (var reference in classSymbol.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax(cancellationToken) is ClassDeclarationSyntax declaration)
                {
                    classDeclaration = declaration;
                    break;
                }
            }

            if (classDeclaration is null)
            {
                return null;
            }

            var typesToScan = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            var orderedTypes = new List<ITypeSymbol>();

            ScanAttributesForTypes(classSymbol.GetAttributes(), typesToScan, orderedTypes);

            foreach (var constructor in classSymbol.Constructors)
            {
                if (constructor.IsImplicitlyDeclared)
                {
                    continue;
                }

                foreach (var parameter in constructor.Parameters)
                {
                    AddType(parameter.Type, typesToScan, orderedTypes);
                    ScanAttributesForTypes(parameter.GetAttributes(), typesToScan, orderedTypes);
                }
            }

            return new ClassConversions(
                GetTreeIndex(classDeclaration.SyntaxTree),
                classDeclaration.SpanStart,
                GetConversions(orderedTypes, cancellationToken));
        }
    }

    private class ConversionInfo
    {
        public required ITypeSymbol SourceType { get; init; }
        public required ITypeSymbol TargetType { get; init; }
        public required bool IsImplicit { get; init; }
    }
}
