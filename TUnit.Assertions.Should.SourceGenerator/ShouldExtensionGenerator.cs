using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.Should.SourceGenerator;

/// <summary>
/// Source generator that scans the current compilation and every referenced assembly
/// for classes decorated with <c>TUnit.Assertions.Attributes.AssertionExtensionAttribute</c>
/// and emits Should-flavored extension methods on <c>IShouldSource&lt;T&gt;</c> that
/// construct the original assertion class and wrap it in <c>ShouldAssertion&lt;T&gt;</c>.
/// </summary>
[Generator]
public sealed class ShouldExtensionGenerator : IIncrementalGenerator
{
    private const string AssertionExtensionAttributeFullName = "TUnit.Assertions.Attributes.AssertionExtensionAttribute";
    private const string ShouldNameAttributeFullName = "TUnit.Assertions.Should.Attributes.ShouldNameAttribute";
    private const string AssertionBaseFullName = "TUnit.Assertions.Core.Assertion`1";
    private const string AssertionContextFullName = "TUnit.Assertions.Core.AssertionContext`1";
    private const string ShouldExtensionsNamespace = "TUnit.Assertions.Should.Extensions";

    private static readonly SymbolDisplayFormat NoGlobalFormat =
        SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
            .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly SymbolDisplayFormat NameWithoutTypeArgsFormat =
        SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
            .WithGenericsOptions(SymbolDisplayGenericsOptions.None);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assertionsProvider = context.CompilationProvider
            .Select((compilation, _) => CollectAssertions(compilation));

        context.RegisterSourceOutput(assertionsProvider, static (ctx, items) =>
        {
            if (items.IsDefaultOrEmpty)
            {
                return;
            }

            var emittedFiles = new HashSet<string>(StringComparer.Ordinal);
            var seenTypes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var item in items)
            {
                if (!seenTypes.Add(item.ClassFullName))
                {
                    continue;
                }
                EmitClass(ctx, item, emittedFiles);
            }
        });
    }

    private static ImmutableArray<AssertionData> CollectAssertions(Compilation compilation)
    {
        var attributeSymbol = compilation.GetTypeByMetadataName(AssertionExtensionAttributeFullName);
        var assertionBaseSymbol = compilation.GetTypeByMetadataName(AssertionBaseFullName);
        var assertionContextSymbol = compilation.GetTypeByMetadataName(AssertionContextFullName);

        if (attributeSymbol is null || assertionBaseSymbol is null || assertionContextSymbol is null)
        {
            return ImmutableArray<AssertionData>.Empty;
        }

        var ctx = new CollectionContext(
            compilation,
            compilation.Assembly,
            attributeSymbol,
            compilation.GetTypeByMetadataName(ShouldNameAttributeFullName),
            assertionBaseSymbol,
            assertionContextSymbol,
            CollectAlreadyBakedShouldExtensionNames(compilation),
            ImmutableArray.CreateBuilder<AssertionData>());

        WalkNamespace(ctx.CurrentAssembly.GlobalNamespace, ctx);
        foreach (var reference in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            WalkNamespace(reference.GlobalNamespace, ctx);
        }

        return ctx.Builder.ToImmutable();
    }

    /// <summary>
    /// Build a one-shot set of <c>Should{Name}Extensions</c> classes that already exist in
    /// referenced (non-current) assemblies — typically those baked into TUnit.Assertions.Should.dll.
    /// Avoids per-type metadata lookups during the walk.
    /// </summary>
    private static HashSet<string> CollectAlreadyBakedShouldExtensionNames(Compilation compilation)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var currentAssembly = compilation.Assembly;

        foreach (var reference in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (SymbolEqualityComparer.Default.Equals(reference, currentAssembly))
            {
                continue;
            }

            var ns = LookupNamespace(reference.GlobalNamespace, ShouldExtensionsNamespace);
            if (ns is null) continue;

            foreach (var type in ns.GetTypeMembers())
            {
                result.Add(type.Name);
            }
        }

        return result;
    }

    private static INamespaceSymbol? LookupNamespace(INamespaceSymbol root, string dottedName)
    {
        var current = root;
        foreach (var segment in dottedName.Split('.'))
        {
            current = current.GetNamespaceMembers().FirstOrDefault(n => n.Name == segment);
            if (current is null) return null;
        }
        return current;
    }

    private static void WalkNamespace(INamespaceSymbol ns, CollectionContext ctx)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            CollectFromType(type, ctx);
        }

        foreach (var nested in ns.GetNamespaceMembers())
        {
            WalkNamespace(nested, ctx);
        }
    }

    private static void CollectFromType(INamedTypeSymbol type, CollectionContext ctx)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            CollectFromType(nested, ctx);
        }

        if (type.DeclaredAccessibility != Accessibility.Public)
        {
            return;
        }

        // Skip referenced types whose Should{ClassName}Extensions has already been baked in
        // another reference (typically TUnit.Assertions.Should.dll); re-emitting would cause
        // CS0121 ambiguities. Types declared in the current compilation are always emitted
        // so users get Should-flavored extensions for assertion classes they author.
        if (!SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, ctx.CurrentAssembly)
            && ctx.AlreadyBakedShouldExtensionNames.Contains($"Should{type.Name}Extensions"))
        {
            return;
        }

        var typeAttributes = type.GetAttributes();

        var attribute = typeAttributes.FirstOrDefault(a =>
            SymbolEqualityComparer.Default.Equals(a.AttributeClass, ctx.AssertionExtensionAttribute));
        if (attribute is null || attribute.ConstructorArguments.Length == 0)
        {
            return;
        }

        var methodName = attribute.ConstructorArguments[0].Value as string;
        if (string.IsNullOrEmpty(methodName))
        {
            return;
        }

        string? negatedMethodName = null;
        var overloadPriority = 0;
        foreach (var named in attribute.NamedArguments)
        {
            switch (named.Key)
            {
                case "NegatedMethodName":
                    negatedMethodName = named.Value.Value as string;
                    break;
                case "OverloadResolutionPriority" when named.Value.Value is int p:
                    overloadPriority = p;
                    break;
            }
        }

        var assertionBaseType = FindAssertionBase(type, ctx.AssertionBase);
        if (assertionBaseType is null)
        {
            return;
        }

        string? shouldNameOverride = null;
        string? shouldNegatedOverride = null;
        if (ctx.ShouldNameAttribute is not null)
        {
            var shouldAttr = typeAttributes.FirstOrDefault(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, ctx.ShouldNameAttribute));
            if (shouldAttr is { ConstructorArguments.Length: > 0 })
            {
                shouldNameOverride = shouldAttr.ConstructorArguments[0].Value as string;
                foreach (var named in shouldAttr.NamedArguments)
                {
                    if (named.Key == "Negated")
                    {
                        shouldNegatedOverride = named.Value.Value as string;
                    }
                }
            }
        }

        var ctorData = ImmutableArray.CreateBuilder<ConstructorData>();
        foreach (var ctor in type.Constructors)
        {
            if (ctor.DeclaredAccessibility != Accessibility.Public
                || ctor.IsStatic
                || ctor.Parameters.Length == 0)
            {
                continue;
            }

            var firstParam = ctor.Parameters[0].Type as INamedTypeSymbol;
            if (firstParam is null
                || !SymbolEqualityComparer.Default.Equals(firstParam.OriginalDefinition, ctx.AssertionContext))
            {
                continue;
            }

            var paramData = ImmutableArray.CreateBuilder<ParameterData>(ctor.Parameters.Length - 1);
            for (var i = 1; i < ctor.Parameters.Length; i++)
            {
                var p = ctor.Parameters[i];
                paramData.Add(new ParameterData(
                    p.Name,
                    p.Type.ToDisplayString(NoGlobalFormat),
                    p.HasExplicitDefaultValue,
                    p.HasExplicitDefaultValue ? FormatDefaultValue(p.ExplicitDefaultValue, p.Type) : null));
            }

            ctorData.Add(new ConstructorData(paramData.ToImmutable(), TryGetRucMessage(ctor.GetAttributes())));
        }

        if (ctorData.Count == 0)
        {
            return;
        }

        var rucMessage = TryGetRucMessage(typeAttributes);

        var typeParam = assertionBaseType.TypeArguments[0];
        var typeParamDisplay = typeParam.ToDisplayString(NoGlobalFormat);
        var typeParamIsTypeParameter = typeParam is ITypeParameterSymbol;
        // No method-level covariance: every extension targets IShouldSource<typeParam> directly.
        // Source-type narrowing happens at the Should() entry overloads (mirroring Assert.That),
        // which avoids inference failures on element-typed assertions like BeInOrder where there's
        // no parameter to bind TItem from.
        var needsMethodLevelCovariance = false;

        var classGenericParams = ImmutableArray.CreateBuilder<GenericParamData>();
        if (type.IsGenericType)
        {
            foreach (var tp in type.TypeParameters)
            {
                classGenericParams.Add(GenericParamData.From(tp));
            }
        }

        ctx.Builder.Add(new AssertionData(
            ClassName: type.Name,
            ClassFullName: type.ToDisplayString(NameWithoutTypeArgsFormat),
            IsClassGeneric: type.IsGenericType,
            ClassGenericParams: classGenericParams.ToImmutable(),
            MethodName: methodName!,
            NegatedMethodName: negatedMethodName,
            OverloadResolutionPriority: overloadPriority,
            ShouldNameOverride: shouldNameOverride,
            ShouldNegatedOverride: shouldNegatedOverride,
            TypeParamDisplay: typeParamDisplay,
            TypeParamIsTypeParameter: typeParamIsTypeParameter,
            NeedsMethodLevelCovariance: needsMethodLevelCovariance,
            TypeParamConstraintName: CovarianceHelper.GetConstraintTypeName(typeParamDisplay, typeParam),
            Constructors: ctorData.ToImmutable(),
            RequiresUnreferencedCodeMessage: rucMessage));
    }

    private static INamedTypeSymbol? FindAssertionBase(INamedTypeSymbol type, INamedTypeSymbol assertionBaseSymbol)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, assertionBaseSymbol))
            {
                return current;
            }
        }
        return null;
    }

    private static string FormatDefaultValue(object? defaultValue, ITypeSymbol type)
    {
        if (defaultValue is null)
        {
            // For non-nullable reference types declared with a null default, emit "default!" so the
            // nullability annotation is suppressed; the original assertion class accepts null at runtime.
            return type.IsReferenceType && type.NullableAnnotation != NullableAnnotation.Annotated
                ? "default!"
                : "default";
        }

        if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol enumType)
        {
            foreach (var member in enumType.GetMembers())
            {
                if (member is IFieldSymbol { HasConstantValue: true } field
                    && field.ConstantValue is not null
                    && field.ConstantValue.Equals(defaultValue))
                {
                    return $"{enumType.ToDisplayString(NoGlobalFormat)}.{field.Name}";
                }
            }
            return $"({enumType.ToDisplayString(NoGlobalFormat)})({defaultValue})";
        }

        return defaultValue switch
        {
            string s => "\"" + s.Replace("\"", "\\\"") + "\"",
            bool b => b ? "true" : "false",
            char c => $"'{c}'",
            _ => defaultValue.ToString() ?? "default",
        };
    }

    private static void EmitClass(SourceProductionContext ctx, AssertionData data, HashSet<string> emittedFiles)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using TUnit.Assertions.Core;");
        sb.AppendLine("using TUnit.Assertions.Should.Core;");
        sb.AppendLine();
        sb.AppendLine("namespace TUnit.Assertions.Should.Extensions;");
        sb.AppendLine();

        // Class name needs to be unique even when several assertion classes share a simple name
        // across namespaces. Suffix with the FullName hash if needed for uniqueness.
        var className = "Should" + data.ClassName + "Extensions";
        sb.AppendLine($"public static partial class {className}");
        sb.AppendLine("{");

        var positiveName = data.ShouldNameOverride
                          ?? NameConjugator.Conjugate(data.MethodName).Name;

        var negatedName = data.ShouldNegatedOverride
                          ?? (data.NegatedMethodName is not null
                              ? NameConjugator.Conjugate(data.NegatedMethodName).Name
                              : null);

        foreach (var ctor in data.Constructors)
        {
            EmitMethod(sb, data, ctor, positiveName);
            if (negatedName is not null)
            {
                EmitMethod(sb, data, ctor, negatedName);
            }
        }

        sb.AppendLine("}");

        // Avoid duplicate hint name collisions across compilations / nested types.
        var hint = className;
        var suffix = 0;
        while (!emittedFiles.Add(hint + ".g.cs"))
        {
            hint = className + "_" + (++suffix);
        }
        ctx.AddSource(hint + ".g.cs", sb.ToString());
    }

    private static void EmitMethod(StringBuilder sb, AssertionData data, ConstructorData ctor, string methodName)
    {
        var classGenericList = data.ClassGenericParams.Select(p => p.Name).ToList();
        var allMethodGenerics = new List<string>(classGenericList);
        var constraints = new List<string>();
        foreach (var p in data.ClassGenericParams)
        {
            if (p.ConstraintClause is not null)
            {
                constraints.Add(p.ConstraintClause);
            }
        }

        string sourceType;
        string contextExpr;
        var assertionTypeParamForCtor = data.TypeParamDisplay;

        if (data.NeedsMethodLevelCovariance)
        {
            var covariantParam = CovarianceHelper.GetCovariantTypeParamName(classGenericList);
            allMethodGenerics.Add(covariantParam);
            sourceType = $"global::TUnit.Assertions.Should.Core.IShouldSource<{covariantParam}>";
            constraints.Add($"where {covariantParam} : {data.TypeParamConstraintName}");
            // Lambda body upcasts TActual to typeParam via the constraint; the signature shift handles variance.
            contextExpr = $"source.Context.Map<{data.TypeParamDisplay}>(static x => x)";
        }
        else
        {
            sourceType = $"global::TUnit.Assertions.Should.Core.IShouldSource<{data.TypeParamDisplay}>";
            contextExpr = "source.Context";
        }

        var classGenericSuffix = data.IsClassGeneric
            ? "<" + string.Join(", ", classGenericList) + ">"
            : string.Empty;
        var methodGenericSuffix = allMethodGenerics.Count > 0
            ? "<" + string.Join(", ", allMethodGenerics) + ">"
            : string.Empty;
        var returnType = $"global::TUnit.Assertions.Should.Core.ShouldAssertion<{assertionTypeParamForCtor}>";

        sb.AppendLine();
        var rucMessage = ctor.RequiresUnreferencedCodeMessage ?? data.RequiresUnreferencedCodeMessage;
        if (!string.IsNullOrEmpty(rucMessage))
        {
            var escaped = rucMessage!.Replace("\"", "\\\"");
            sb.AppendLine($"    [global::System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(\"{escaped}\")]");
        }

        if (data.OverloadResolutionPriority > 0)
        {
            sb.AppendLine($"    [global::System.Runtime.CompilerServices.OverloadResolutionPriority({data.OverloadResolutionPriority})]");
        }

        sb.Append($"    public static {returnType} {methodName}{methodGenericSuffix}(this {sourceType} source");
        foreach (var p in ctor.Parameters)
        {
            sb.Append($", {p.TypeName} {p.Name}");
            if (p.HasDefaultValue)
            {
                sb.Append(" = ").Append(p.DefaultValueLiteral);
            }
        }
        foreach (var p in ctor.Parameters)
        {
            sb.Append($", [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"{p.Name}\")] string? {p.Name}Expression = null");
        }
        sb.Append(')');

        if (constraints.Count > 0)
        {
            sb.AppendLine();
            sb.Append("        ").Append(string.Join(" ", constraints));
        }

        sb.AppendLine();
        sb.AppendLine("    {");

        sb.AppendLine($"        var innerContext = {contextExpr};");

        sb.AppendLine($"        innerContext.ExpressionBuilder.Append(\".{methodName}(\");");
        if (ctor.Parameters.Length == 1)
        {
            sb.AppendLine($"        innerContext.ExpressionBuilder.Append({ctor.Parameters[0].Name}Expression);");
        }
        else if (ctor.Parameters.Length > 1)
        {
            sb.AppendLine("        var __added = false;");
            foreach (var p in ctor.Parameters)
            {
                sb.AppendLine($"        if ({p.Name}Expression is not null)");
                sb.AppendLine("        {");
                sb.AppendLine("            if (__added) innerContext.ExpressionBuilder.Append(\", \");");
                sb.AppendLine($"            innerContext.ExpressionBuilder.Append({p.Name}Expression);");
                sb.AppendLine("            __added = true;");
                sb.AppendLine("        }");
            }
        }
        sb.AppendLine("        innerContext.ExpressionBuilder.Append(\")\");");

        var innerCtorArgs = new List<string> { "innerContext" };
        innerCtorArgs.AddRange(ctor.Parameters.Select(p => p.Name));

        var innerTypeName = "global::" + data.ClassFullName + classGenericSuffix;

        sb.AppendLine($"        var inner = new {innerTypeName}({string.Join(", ", innerCtorArgs)});");
        sb.AppendLine($"        return new global::TUnit.Assertions.Should.Core.ShouldAssertion<{assertionTypeParamForCtor}>(innerContext, inner);");
        sb.AppendLine("    }");
    }

    private static string? TryGetRucMessage(ImmutableArray<AttributeData> attributes)
    {
        foreach (var a in attributes)
        {
            if (a.AttributeClass?.Name == "RequiresUnreferencedCodeAttribute"
                && a.ConstructorArguments.Length > 0)
            {
                return a.ConstructorArguments[0].Value as string;
            }
        }
        return null;
    }

    /// <summary>
    /// Bag of per-compilation state passed to walk/collect helpers. Avoids threading 7+ args
    /// through the recursive namespace walk.
    /// </summary>
    private sealed record CollectionContext(
        Compilation Compilation,
        IAssemblySymbol CurrentAssembly,
        INamedTypeSymbol AssertionExtensionAttribute,
        INamedTypeSymbol? ShouldNameAttribute,
        INamedTypeSymbol AssertionBase,
        INamedTypeSymbol AssertionContext,
        HashSet<string> AlreadyBakedShouldExtensionNames,
        ImmutableArray<AssertionData>.Builder Builder);

    private sealed record AssertionData(
        string ClassName,
        string ClassFullName,
        bool IsClassGeneric,
        EquatableArray<GenericParamData> ClassGenericParams,
        string MethodName,
        string? NegatedMethodName,
        int OverloadResolutionPriority,
        string? ShouldNameOverride,
        string? ShouldNegatedOverride,
        string TypeParamDisplay,
        bool TypeParamIsTypeParameter,
        bool NeedsMethodLevelCovariance,
        string TypeParamConstraintName,
        EquatableArray<ConstructorData> Constructors,
        string? RequiresUnreferencedCodeMessage);

    private sealed record ConstructorData(EquatableArray<ParameterData> Parameters, string? RequiresUnreferencedCodeMessage);

    private sealed record ParameterData(
        string Name,
        string TypeName,
        bool HasDefaultValue,
        string? DefaultValueLiteral) : IEquatable<ParameterData>;

    private sealed record GenericParamData(string Name, string? ConstraintClause) : IEquatable<GenericParamData>
    {
        public static GenericParamData From(ITypeParameterSymbol tp)
        {
            var constraints = new List<string>();
            if (tp.HasReferenceTypeConstraint) constraints.Add("class");
            if (tp.HasValueTypeConstraint) constraints.Add("struct");
            if (tp.HasNotNullConstraint) constraints.Add("notnull");
            foreach (var ct in tp.ConstraintTypes)
            {
                constraints.Add(ct.ToDisplayString(NoGlobalFormat));
            }
            if (tp.HasConstructorConstraint) constraints.Add("new()");
            return new GenericParamData(tp.Name, constraints.Count > 0 ? $"where {tp.Name} : {string.Join(", ", constraints)}" : null);
        }
    }
}
