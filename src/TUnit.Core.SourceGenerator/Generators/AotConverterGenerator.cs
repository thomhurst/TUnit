using TUnit.SourceGeneration;
using System.Globalization;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.Extensions;
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
        var enabledProvider = GenerationEnabled.CreateProvider(context);

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
                        id: GeneratorDiagnosticIds.AotConverterFailed,
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
                ScanAttributesForTypes(parameter.GetAttributes(), methodTypes, orderedMethodTypes, methodSymbol.ContainingType, compilation, caches);
            }

            ScanAttributesForTypes(methodSymbol.GetAttributes(), methodTypes, orderedMethodTypes, methodSymbol.ContainingType, compilation, caches);

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

    private static void ScanAttributesForTypes(ImmutableArray<AttributeData> attributes, HashSet<ITypeSymbol> typesToScan, List<ITypeSymbol> orderedTypes, INamedTypeSymbol containingType, Compilation compilation, CompilationCaches caches)
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

            ScanMethodDataSourceReturnType(attribute, containingType, compilation, caches, typesToScan, orderedTypes);

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

    private static void ScanMethodDataSourceReturnType(AttributeData attribute, INamedTypeSymbol containingType,
        Compilation compilation, CompilationCaches caches, HashSet<ITypeSymbol> typesToScan, List<ITypeSymbol> orderedTypes)
    {
        // Include derived attributes such as InstanceMethodDataSourceAttribute.
        var sourceAttribute = attribute.AttributeClass;
        while (sourceAttribute is not null && !caches.IsMethodDataSourceAttribute(sourceAttribute))
        {
            sourceAttribute = sourceAttribute.BaseType;
        }

        if (sourceAttribute is null || attribute.ConstructorArguments.Length == 0)
        {
            return;
        }

        ITypeSymbol providerType = containingType;
        string? memberName = null;
        if (attribute.ConstructorArguments is [{ Value: ITypeSymbol explicitType }, { Value: string explicitName }, ..])
        {
            providerType = explicitType;
            memberName = explicitName;
        }
        else if (attribute.ConstructorArguments[0].Value is string name)
        {
            memberName = name;
            if (sourceAttribute is { IsGenericType: true, TypeArguments: [var genericProvider] })
            {
                providerType = genericProvider;
            }
        }

        if (memberName is null)
        {
            return;
        }

        // Reuse the factory resolver, including overload selection, inherited members, properties and fields.
        var member = TestMetadataGenerator.ResolveMethodDataSourceMember(providerType, memberName, attribute, compilation);
        var returnType = member switch
        {
            IMethodSymbol method => method.ReturnType,
            IPropertySymbol property => property.Type,
            IFieldSymbol field => field.Type,
            _ => null
        };
        if (returnType is not null)
        {
            AddType(returnType, typesToScan, orderedTypes);
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

    private static void CollectConversionsForType(ITypeSymbol type, List<ConversionEntry> conversions, Compilation compilation, HashSet<ITypeSymbol> visited, HashSet<ITypeSymbol> expandingSequences)
    {
        if (!visited.Add(type))
        {
            return;
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            CollectConversionsForType(arrayType.ElementType, conversions, compilation, visited, expandingSequences);
            return;
        }

        if (type is not INamedTypeSymbol namedType)
        {
            return;
        }

        // Inaccessible wrappers can still expose accessible sequence elements.
        if (ShouldIncludeType(namedType, compilation))
        {
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

            // Native AOT cannot rely on CastHelper finding Parse by reflection, so string arguments
            // for parsable types get a converter that calls Parse directly.
            // Primitives and decimal already convert identically in every mode through IConvertible.
            if (namedType.SpecialType is SpecialType.None or SpecialType.System_DateTime
                && namedType.IsParsableFromString() && IsAccessibleType(namedType, compilation)
                && !((CSharpCompilation)compilation).ClassifyConversion(compilation.GetSpecialType(SpecialType.System_String), namedType).Exists
                && FindPublicParse(namedType) is { } takesFormatProvider)
            {
                conversions.Add(ToConversionEntry(new ConversionInfo
                {
                    SourceType = compilation.GetSpecialType(SpecialType.System_String),
                    TargetType = namedType,
                    IsImplicit = false,
                    ParseFromString = true,
                    ParseTakesFormatProvider = takesFormatProvider,
                }));
            }
        }

        if (namedType.IsGenericType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                CollectConversionsForType(typeArg, conversions, compilation, visited, expandingSequences);
            }
        }

        // A sequence can expand indefinitely, e.g. Rows<T> : IEnumerable<Rows<T[]>>.
        // Track definitions on the current sequence path, while still scanning concrete
        // type arguments and operators above, and allowing sibling instantiations.
        if (!expandingSequences.Add(namedType.OriginalDefinition))
        {
            return;
        }

        try
        {
            // Concrete collections can hide their element type in a base class or interface.
            // Only sequence interfaces describe data rows; arbitrary interfaces are not data wrappers.
            foreach (var sequence in namedType.AllInterfaces)
            {
                if (sequence is { Arity: 1, Name: "IEnumerable" or "IAsyncEnumerable" } &&
                    sequence.ContainingNamespace is
                        { Name: "Generic", ContainingNamespace: { Name: "Collections", ContainingNamespace:
                            { Name: "System", ContainingNamespace.IsGlobalNamespace: true } } })
                {
                    CollectConversionsForType(sequence.TypeArguments[0], conversions, compilation, visited, expandingSequences);
                }
            }
        }
        finally
        {
            expandingSequences.Remove(namedType.OriginalDefinition);
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
            RoundsDecimalToIntegral = conversion.SourceType.SpecialType == SpecialType.System_Decimal &&
                conversion.TargetType.SpecialType is SpecialType.System_SByte or SpecialType.System_Byte or
                    SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or
                    SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64,
            ParseFromString = conversion.ParseFromString,
            ParseTakesFormatProvider = conversion.ParseTakesFormatProvider,
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
        if (type is IArrayTypeSymbol arrayType)
        {
            return IsAccessibleType(arrayType.ElementType, compilation);
        }

        if (type is IPointerTypeSymbol pointerType)
        {
            return IsAccessibleType(pointerType.PointedAtType, compilation);
        }

        // Converters live in a separate top-level class, so same-assembly membership alone
        // does not grant access to private/protected types or their containing types.
        if (!compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
        {
            return false;
        }

        return type is not INamedTypeSymbol namedType ||
               namedType.TypeArguments.All(argument => IsAccessibleType(argument, compilation));
    }

    private static ConversionInfo? GetConversionInfoFromSymbol(IMethodSymbol methodSymbol, Compilation compilation)
    {
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
        {
            return null;
        }

        // Generated code that calls an [Obsolete(..., error: true)] operator fails
        // with CS0619, which the generated file's #pragma warning disable cannot suppress.
        if (IsObsoleteAsError(methodSymbol))
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

        // Match the assignment/cast that will be emitted. An operator on each endpoint
        // can make the conversion ambiguous even when the test never needs that pair.
        var conversion = ((CSharpCompilation)compilation).ClassifyConversion(sourceType, targetType);
        // Decimal exposes operators that Roslyn classifies as built-in numeric
        // conversions, with no selected method. They still need AOT registrations.
        if (!conversion.Exists ||
            (isImplicit && !conversion.IsImplicit) ||
            (conversion.IsUserDefined
                ? !SymbolEqualityComparer.Default.Equals(conversion.MethodSymbol, methodSymbol)
                : !conversion.IsNumeric))
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

    private static bool IsObsoleteAsError(ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass is not { Name: "ObsoleteAttribute", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } })
            {
                continue;
            }

            var args = attribute.ConstructorArguments;
            return args.Length >= 2 && args[1].Value is true;
        }

        return false;
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
                            id: GeneratorDiagnosticIds.NullConversionType,
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
                        id: GeneratorDiagnosticIds.ConversionTypeCheckFailed,
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
                if (conversion.ParseFromString)
                {
                    var formatProvider = conversion.ParseTakesFormatProvider ? ", global::System.Globalization.CultureInfo.InvariantCulture" : "";
                    writer.AppendLine($"{targetTypeName} converted = {targetTypeName}.Parse(sourceTypedValue{formatProvider});");
                }
                else if (conversion.RoundsDecimalToIntegral)
                {
                    // Match CastHelper's primitive fallback while preserving registered-converter priority.
                    writer.AppendLine($"{targetTypeName} converted = ({targetTypeName})global::System.Convert.ChangeType(sourceTypedValue, typeof({targetTypeName}));");
                }
                else if (conversion.IsImplicit)
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
        public bool RoundsDecimalToIntegral { get; init; }
        public bool ParseFromString { get; init; }
        public bool ParseTakesFormatProvider { get; init; }
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
        private readonly ConcurrentDictionary<IAssemblySymbol, MethodSourceDefinitions> _methodSourceDefinitions = new(SymbolEqualityComparer.Default);

        private sealed record MethodSourceDefinitions(INamedTypeSymbol? NonGeneric, INamedTypeSymbol? Generic);

        public bool IsMethodDataSourceAttribute(INamedTypeSymbol attributeType)
        {
            // Resolve in the bound symbol's assembly: compilation-wide lookup can be ambiguous
            // when another reference defines the same metadata name (including extern aliases).
            var definitions = _methodSourceDefinitions.GetOrAdd(attributeType.ContainingAssembly, static assembly =>
                new MethodSourceDefinitions(
                    assembly.GetTypeByMetadataName("TUnit.Core.MethodDataSourceAttribute"),
                    assembly.GetTypeByMetadataName("TUnit.Core.MethodDataSourceAttribute`1")));
            return SymbolEqualityComparer.Default.Equals(attributeType.OriginalDefinition, definitions.NonGeneric) ||
                   SymbolEqualityComparer.Default.Equals(attributeType.OriginalDefinition, definitions.Generic);
        }

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
                    CollectConversionsForType(type, collected, compilation, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default), new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
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

            ScanAttributesForTypes(classSymbol.GetAttributesIncludingBaseTypes().ToImmutableArray(), typesToScan, orderedTypes, classSymbol, compilation, this);

            foreach (var type in classSymbol.GetSelfAndBaseTypes())
            {
                foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
                {
                    ScanAttributesForTypes(property.GetAttributes(), typesToScan, orderedTypes, classSymbol, compilation, this);
                }
            }

            foreach (var constructor in classSymbol.Constructors)
            {
                if (constructor.IsImplicitlyDeclared)
                {
                    continue;
                }

                foreach (var parameter in constructor.Parameters)
                {
                    AddType(parameter.Type, typesToScan, orderedTypes);
                    ScanAttributesForTypes(parameter.GetAttributes(), typesToScan, orderedTypes, classSymbol, compilation, this);
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
        public bool ParseFromString { get; init; }
        public bool ParseTakesFormatProvider { get; init; }
    }

    /// <summary>
    /// Returns whether the public static Parse overload to call takes an <see cref="IFormatProvider"/>, or null
    /// when there is none (e.g. Parse implemented explicitly). Older frameworks lack Guid.Parse(string, IFormatProvider).
    /// </summary>
    private static bool? FindPublicParse(INamedTypeSymbol type)
    {
        bool? found = null;
        foreach (var method in type.GetMembers("Parse").OfType<IMethodSymbol>())
        {
            if (method is not { IsStatic: true, DeclaredAccessibility: Accessibility.Public }
                || method.Parameters.Length == 0 || method.Parameters[0].Type.SpecialType != SpecialType.System_String
                || !SymbolEqualityComparer.Default.Equals(method.ReturnType, type))
            {
                continue;
            }

            if (method.Parameters is [_, { Type.Name: "IFormatProvider", Type.ContainingNamespace.Name: "System" }])
            {
                return true;
            }

            if (method.Parameters.Length == 1)
            {
                found = false;
            }
        }

        return found;
    }
}
