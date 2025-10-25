using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Generators;

[Generator]
public class AotConverterGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var allTypes = context.CompilationProvider
            .Select((compilation, ct) =>
            {
                try
                {
                    var conversionInfos = new List<ConversionInfo>();
                    ScanTestParameters(compilation, conversionInfos, ct);
                    return conversionInfos.ToImmutableArray();
                }
                catch (NullReferenceException ex)
                {
                    var stackTrace = ex.StackTrace ?? "No stack trace";
                    throw new InvalidOperationException($"NullReferenceException in ScanTestParameters: {ex.Message}\nStack: {stackTrace}", ex);
                }
            });

        context.RegisterSourceOutput(allTypes, (spc, source) =>
        {
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
                        messageFormat: "Generator failed with exception: {0}",
                        category: "TUnit.Generator",
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        description: e.ToString()),
                    Location.None));
            }
        });
    }

    private void ScanTestParameters(Compilation compilation, List<ConversionInfo> conversionInfos, CancellationToken cancellationToken)
    {
        var typesToScan = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var tree in compilation.SyntaxTrees)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var semanticModel = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            var methods = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                if (methodSymbol == null)
                {
                    continue;
                }

                if (!IsTestMethod(methodSymbol))
                {
                    continue;
                }

                foreach (var parameter in methodSymbol.Parameters)
                {
                    typesToScan.Add(parameter.Type);
                    ScanAttributesForTypes(parameter.GetAttributes(), typesToScan);
                }

                ScanAttributesForTypes(methodSymbol.GetAttributes(), typesToScan);
            }

            var classes = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classes)
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
                if (classSymbol == null)
                {
                    continue;
                }

                if (!IsTestClass(classSymbol))
                {
                    continue;
                }

                ScanAttributesForTypes(classSymbol.GetAttributes(), typesToScan);

                foreach (var constructor in classSymbol.Constructors)
                {
                    if (constructor.IsImplicitlyDeclared)
                    {
                        continue;
                    }

                    foreach (var parameter in constructor.Parameters)
                    {
                        typesToScan.Add(parameter.Type);
                        ScanAttributesForTypes(parameter.GetAttributes(), typesToScan);
                    }
                }
            }
        }

        foreach (var type in typesToScan)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CollectConversionsForType(type, conversionInfos, compilation);
        }
    }

    private bool IsTestMethod(IMethodSymbol method)
    {
        return method.GetAttributes().Any(attr =>
        {
            var attrClass = attr.AttributeClass;
            if (attrClass == null)
            {
                return false;
            }

            var baseType = attrClass;
            while (baseType != null)
            {
                if (baseType.ToDisplayString() == WellKnownFullyQualifiedClassNames.BaseTestAttribute.WithoutGlobalPrefix)
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }

            return false;
        });
    }

    private bool IsTestClass(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Any(IsTestMethod);
    }

    private void ScanAttributesForTypes(ImmutableArray<AttributeData> attributes, HashSet<ITypeSymbol> typesToScan)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass == null)
            {
                continue;
            }

            if (!IsDataSourceAttribute(attribute.AttributeClass))
            {
                continue;
            }

            if (attribute.AttributeClass.IsGenericType)
            {
                foreach (var typeArg in attribute.AttributeClass.TypeArguments)
                {
                    typesToScan.Add(typeArg);
                }
            }

            foreach (var arg in attribute.ConstructorArguments)
            {
                ScanTypedConstantForTypes(arg, typesToScan);
            }

            foreach (var namedArg in attribute.NamedArguments)
            {
                ScanTypedConstantForTypes(namedArg.Value, typesToScan);
            }
        }
    }

    private bool IsDataSourceAttribute(INamedTypeSymbol attributeClass)
    {
        var currentType = attributeClass;
        while (currentType != null)
        {
            var fullName = currentType.ToDisplayString();
            if (fullName == WellKnownFullyQualifiedClassNames.AsyncDataSourceGeneratorAttribute.WithoutGlobalPrefix ||
                fullName == WellKnownFullyQualifiedClassNames.AsyncUntypedDataSourceGeneratorAttribute.WithoutGlobalPrefix ||
                fullName == WellKnownFullyQualifiedClassNames.ArgumentsAttribute.WithoutGlobalPrefix)
            {
                return true;
            }

            if (currentType.AllInterfaces.Any(i =>
                i.ToDisplayString() == WellKnownFullyQualifiedClassNames.IDataSourceAttribute.WithoutGlobalPrefix))
            {
                return true;
            }

            currentType = currentType.BaseType;
        }

        return false;
    }

    private void ScanTypedConstantForTypes(TypedConstant constant, HashSet<ITypeSymbol> typesToScan)
    {
        if (constant.IsNull)
        {
            return;
        }

        if (constant is { Kind: TypedConstantKind.Type, Value: ITypeSymbol typeValue })
        {
            typesToScan.Add(typeValue);
        }

        else if (constant is { Kind: TypedConstantKind.Array, IsNull: false })
        {
            foreach (var element in constant.Values)
            {
                ScanTypedConstantForTypes(element, typesToScan);
            }
        }
        else if (constant.Kind != TypedConstantKind.Array && constant is { Value: not null, Type: not null })
        {
            typesToScan.Add(constant.Type);
        }
    }

    private void CollectConversionsForType(ITypeSymbol type, List<ConversionInfo> conversionInfos, Compilation compilation)
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
                conversionInfos.Add(conversionInfo);
            }
        }

        if (namedType.IsGenericType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                CollectConversionsForType(typeArg, conversionInfos, compilation);
            }
        }
    }

    private bool ShouldIncludeType(INamedTypeSymbol type, Compilation compilation)
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

    private bool IsAccessibleType(ITypeSymbol type, Compilation compilation)
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

    private ConversionInfo? GetConversionInfoFromSymbol(IMethodSymbol methodSymbol, Compilation compilation)
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
            ContainingType = containingType,
            SourceType = sourceType,
            TargetType = targetType,
            IsImplicit = isImplicit,
            MethodSymbol = methodSymbol
        };
    }

    private bool TypeContainsGenericTypeParameters(ITypeSymbol type)
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
            try
            {
                if (conversion.SourceType == null || conversion.TargetType == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "TUNITGEN002",
                            title: "Null type in conversion",
                            messageFormat: "Skipping converter generation: SourceType={0}, TargetType={1}",
                            category: "TUnit.Generator",
                            defaultSeverity: DiagnosticSeverity.Warning,
                            isEnabledByDefault: true),
                        Location.None,
                        conversion.SourceType?.ToDisplayString() ?? "null",
                        conversion.TargetType?.ToDisplayString() ?? "null"));
                    continue;
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "TUNITGEN003",
                        title: "Error checking conversion types",
                        messageFormat: "Error during null check: {0}",
                        category: "TUnit.Generator",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    Location.None,
                    ex.ToString()));
                continue;
            }

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

            // Use Zen's more robust approach for handling nullable types and type checks
            var sourceType = conversion.SourceType;
            var targetType = conversion.TargetType;

            ITypeSymbol typeForTargetPattern = targetType;
            if (targetType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments.Length: > 0 } nullableTargetType)
            {
                typeForTargetPattern = nullableTargetType.TypeArguments[0];
            }
            var targetPatternTypeName = typeForTargetPattern.GloballyQualified();

            writer.AppendLine($"if (value is {targetPatternTypeName} targetTypedValue)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("return targetTypedValue;");
            writer.Unindent();
            writer.AppendLine("}");

            // 2. If types are different, generate the fallback check for the source type.
            //    This handles cases that require an implicit conversion.
            if (!SymbolEqualityComparer.Default.Equals(sourceType, targetType))
            {
                // For pattern matching, we must unwrap nullable types (C# language requirement - CS8116)
                ITypeSymbol typeForSourcePattern = sourceType;
                if (sourceType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments.Length: > 0 } nullableSourceType)
                {
                    typeForSourcePattern = nullableSourceType.TypeArguments[0];
                }

                var sourcePatternTypeName = typeForSourcePattern.GloballyQualified();

                writer.AppendLine();
                writer.AppendLine($"if (value is {sourcePatternTypeName} sourceTypedValue)");
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
