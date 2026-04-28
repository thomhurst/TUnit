using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Assertions.Should.SourceGenerator;

/// <summary>
/// Source generator that scans the current compilation and every referenced assembly for
/// public extension methods on <c>IAssertionSource&lt;T&gt;</c> whose return type derives
/// from <c>Assertion&lt;TReturn&gt;</c> and whose body is a "simple factory" — meaning the
/// non-CAE method parameters map 1-to-1 (by name and type) onto a public constructor of
/// the return type, after the leading <c>AssertionContext&lt;T&gt;</c> parameter. For each
/// match, emits a Should-flavored counterpart on <c>IShouldSource&lt;T&gt;</c>.
/// <para>
/// This unifies four assertion sources: classes with <c>[AssertionExtension]</c>, methods
/// with <c>[GenerateAssertion]</c>, types decorated with <c>[AssertionFrom&lt;T&gt;]</c>,
/// and any hand-written extension methods whose body is just <c>new T(ctx, args)</c>.
/// Methods that don't fit the factory template (context mapping, transformations, etc.)
/// are silently skipped — they couldn't be wrapped without inspecting their body anyway.
/// </para>
/// </summary>
[Generator]
public sealed class ShouldExtensionGenerator : IIncrementalGenerator
{
    private const string AssertionSourceFullName = "TUnit.Assertions.Core.IAssertionSource`1";
    private const string AssertionBaseFullName = "TUnit.Assertions.Core.Assertion`1";
    private const string ShouldExtensionsNamespace = "TUnit.Assertions.Should.Extensions";
    private const string ShouldNameAttributeFullName = "TUnit.Assertions.Should.Attributes.ShouldNameAttribute";
    private const string CallerArgumentExpressionAttributeName = "CallerArgumentExpressionAttribute";
    private const string RequiresUnreferencedCodeAttributeName = "RequiresUnreferencedCodeAttribute";

    /// <summary>
    /// Return-type names whose Should counterparts are hand-crafted instance methods on
    /// <c>ShouldCollectionSource&lt;T&gt;</c>. Skipped to prevent shadow / drift.
    /// </summary>
    private static readonly HashSet<string> InstanceMethodAssertions = new(StringComparer.Ordinal)
    {
        "CollectionIsInOrderAssertion",
        "CollectionIsInDescendingOrderAssertion",
        "CollectionAllAssertion",
        "CollectionAnyAssertion",
        "HasSingleItemAssertion",
        "HasSingleItemPredicateAssertion",
        "HasDistinctItemsAssertion",
    };

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
        var provider = context.CompilationProvider.Select((compilation, _) => CollectMethods(compilation));

        context.RegisterSourceOutput(provider, static (ctx, items) =>
        {
            if (items.IsDefaultOrEmpty)
            {
                return;
            }

            var emittedHints = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in items.GroupBy(m => m.ContainerName, StringComparer.Ordinal))
            {
                EmitContainer(ctx, group.Key, group.ToArray(), emittedHints);
            }
        });
    }

    private static ImmutableArray<MethodData> CollectMethods(Compilation compilation)
    {
        var assertionSource = compilation.GetTypeByMetadataName(AssertionSourceFullName);
        var assertionBase = compilation.GetTypeByMetadataName(AssertionBaseFullName);
        if (assertionSource is null || assertionBase is null)
        {
            return ImmutableArray<MethodData>.Empty;
        }

        var ctx = new CollectionContext(
            compilation,
            assertionSource,
            assertionBase,
            compilation.GetTypeByMetadataName(ShouldNameAttributeFullName),
            CollectAlreadyBakedShouldExtensionNames(compilation),
            ImmutableArray.CreateBuilder<MethodData>());

        WalkNamespace(compilation.Assembly.GlobalNamespace, ctx);
        foreach (var reference in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            WalkNamespace(reference.GlobalNamespace, ctx);
        }

        return ctx.Builder.ToImmutable();
    }

    private static HashSet<string> CollectAlreadyBakedShouldExtensionNames(Compilation compilation)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var reference in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (SymbolEqualityComparer.Default.Equals(reference, compilation.Assembly)) continue;
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
            CollectFromContainer(type, ctx);
        }
        foreach (var nested in ns.GetNamespaceMembers())
        {
            WalkNamespace(nested, ctx);
        }
    }

    private static void CollectFromContainer(INamedTypeSymbol type, CollectionContext ctx)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            CollectFromContainer(nested, ctx);
        }

        if (type.DeclaredAccessibility != Accessibility.Public
            || !type.IsStatic
            || type.IsGenericType)
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, ctx.Compilation.Assembly)
            && ctx.AlreadyBakedShouldExtensionNames.Contains($"Should{type.Name}"))
        {
            return;
        }

        foreach (var member in type.GetMembers())
        {
            if (member is IMethodSymbol method)
            {
                CollectFromMethod(method, type, ctx);
            }
        }
    }

    private static void CollectFromMethod(IMethodSymbol method, INamedTypeSymbol container, CollectionContext ctx)
    {
        if (method.DeclaredAccessibility != Accessibility.Public
            || !method.IsStatic
            || !method.IsExtensionMethod
            || method.Parameters.Length == 0)
        {
            return;
        }

        if (method.Parameters[0].Type is not INamedTypeSymbol firstParamType
            || !IsAssertionSourceInterface(firstParamType, ctx.AssertionSource))
        {
            return;
        }

        if (method.ReturnType is not INamedTypeSymbol returnType
            || !DerivesFromAssertion(returnType, ctx.AssertionBase, out var assertionTypeArg))
        {
            return;
        }

        if (InstanceMethodAssertions.Contains(returnType.OriginalDefinition.Name))
        {
            return;
        }

        // Method params after `this`: split into "ctor candidates" (non-CAE) and CAE.
        var paramData = ImmutableArray.CreateBuilder<ParameterData>();
        var ctorCandidates = new List<IParameterSymbol>();
        for (var i = 1; i < method.Parameters.Length; i++)
        {
            var p = method.Parameters[i];
            var caeTarget = TryGetCallerArgumentExpressionTarget(p);
            paramData.Add(new ParameterData(
                Name: p.Name,
                TypeName: p.Type.ToDisplayString(NoGlobalFormat),
                HasDefaultValue: p.HasExplicitDefaultValue,
                DefaultValueLiteral: p.HasExplicitDefaultValue ? FormatDefaultValue(p.ExplicitDefaultValue, p.Type) : null,
                CallerArgumentExpressionTarget: caeTarget));
            if (caeTarget is null) ctorCandidates.Add(p);
        }

        // Skip cross-type extensions where the source's TypeArg differs from the return-type's
        // assertion TypeArg (e.g. ImplicitConversionEqualityExtensions.IsEqualTo<TValue, TOther>).
        // These need a Context.Map call we can't synthesize without inspecting the body.
        if (!SymbolEqualityComparer.Default.Equals(firstParamType.TypeArguments[0], assertionTypeArg))
        {
            return;
        }

        // Find a public ctor on the return type whose param list (after the leading
        // AssertionContext<assertionTypeArg>) matches our ctor candidates by type.
        if (!HasMatchingConstructor(returnType, assertionTypeArg, ctorCandidates))
        {
            return;
        }

        var classGenericParams = ImmutableArray.CreateBuilder<GenericParamData>();
        foreach (var tp in method.TypeParameters)
        {
            classGenericParams.Add(GenericParamData.From(tp, NoGlobalFormat));
        }

        var rucMessage = TryGetRucMessage(method.GetAttributes())
                       ?? TryGetRucMessage(returnType.GetAttributes())
                       ?? TryGetRucMessageFromConstructors(returnType);

        var suppressedTrimWarnings = CollectSuppressedTrimWarnings(method.GetAttributes());

        var (overrideName, _) = TryGetShouldNameOverride(returnType, ctx.ShouldNameAttribute);

        ctx.Builder.Add(new MethodData(
            ContainerName: container.Name,
            MethodName: method.Name,
            MethodGenericParams: new EquatableArray<GenericParamData>(classGenericParams),
            SourceTypeArgDisplay: firstParamType.TypeArguments[0].ToDisplayString(NoGlobalFormat),
            AssertionTypeArgDisplay: assertionTypeArg.ToDisplayString(NoGlobalFormat),
            ReturnTypeFullName: returnType.ConstructedFrom.ToDisplayString(NameWithoutTypeArgsFormat),
            ReturnTypeGenericArgs: new EquatableArray<string>(returnType.TypeArguments.Select(a => a.ToDisplayString(NoGlobalFormat)).ToList()),
            Parameters: new EquatableArray<ParameterData>(paramData),
            ShouldNameOverride: overrideName,
            RequiresUnreferencedCodeMessage: rucMessage,
            SuppressedTrimWarnings: new EquatableArray<string>(suppressedTrimWarnings)));
    }

    private static List<string> CollectSuppressedTrimWarnings(ImmutableArray<AttributeData> attrs)
    {
        var result = new List<string>();
        foreach (var a in attrs)
        {
            if (a.AttributeClass?.Name != "UnconditionalSuppressMessageAttribute"
                || a.ConstructorArguments.Length < 2)
            {
                continue;
            }
            if (a.ConstructorArguments[0].Value is string category && category == "Trimming"
                && a.ConstructorArguments[1].Value is string code)
            {
                result.Add(code);
            }
        }
        return result;
    }

    /// <summary>
    /// Returns true when <paramref name="returnType"/> has a public ctor whose parameters,
    /// after a leading <c>AssertionContext&lt;assertionTypeArg&gt;</c>, match
    /// <paramref name="ctorCandidates"/> by type. This guards the "simple factory" template
    /// against extension methods that map context or otherwise transform before construction.
    /// </summary>
    private static bool HasMatchingConstructor(
        INamedTypeSymbol returnType,
        ITypeSymbol assertionTypeArg,
        List<IParameterSymbol> ctorCandidates)
    {
        foreach (var ctor in returnType.Constructors)
        {
            if (ctor.DeclaredAccessibility != Accessibility.Public
                || ctor.IsStatic
                || ctor.Parameters.Length != ctorCandidates.Count + 1)
            {
                continue;
            }

            var firstCtorParam = ctor.Parameters[0].Type as INamedTypeSymbol;
            if (firstCtorParam is null
                || firstCtorParam.Name != "AssertionContext"
                || firstCtorParam.ContainingNamespace?.ToDisplayString() != "TUnit.Assertions.Core"
                || firstCtorParam.TypeArguments.Length != 1
                || !SymbolEqualityComparer.Default.Equals(firstCtorParam.TypeArguments[0], assertionTypeArg))
            {
                continue;
            }

            var allMatch = true;
            for (var i = 0; i < ctorCandidates.Count; i++)
            {
                if (!SymbolEqualityComparer.Default.Equals(ctor.Parameters[i + 1].Type, ctorCandidates[i].Type))
                {
                    allMatch = false;
                    break;
                }
            }
            if (allMatch) return true;
        }
        return false;
    }

    private static (string? Name, string? Negated) TryGetShouldNameOverride(INamedTypeSymbol returnType, INamedTypeSymbol? shouldNameAttr)
    {
        if (shouldNameAttr is null) return (null, null);
        var attr = returnType.GetAttributes().FirstOrDefault(a =>
            SymbolEqualityComparer.Default.Equals(a.AttributeClass, shouldNameAttr));
        if (attr is null || attr.ConstructorArguments.Length == 0) return (null, null);
        var name = attr.ConstructorArguments[0].Value as string;
        string? negated = null;
        foreach (var na in attr.NamedArguments)
        {
            if (na.Key == "Negated") negated = na.Value.Value as string;
        }
        return (name, negated);
    }

    private static bool IsAssertionSourceInterface(INamedTypeSymbol type, INamedTypeSymbol assertionSource)
        => type.OriginalDefinition is { } def
           && SymbolEqualityComparer.Default.Equals(def, assertionSource)
           && type.TypeArguments.Length == 1;

    private static bool DerivesFromAssertion(INamedTypeSymbol type, INamedTypeSymbol assertionBase, out ITypeSymbol assertionTypeArg)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.OriginalDefinition is { } def
                && SymbolEqualityComparer.Default.Equals(def, assertionBase))
            {
                assertionTypeArg = current.TypeArguments[0];
                return true;
            }
        }
        assertionTypeArg = null!;
        return false;
    }

    private static string? TryGetCallerArgumentExpressionTarget(IParameterSymbol parameter)
    {
        foreach (var attr in parameter.GetAttributes())
        {
            if (attr.AttributeClass?.Name == CallerArgumentExpressionAttributeName
                && attr.ConstructorArguments.Length > 0
                && attr.ConstructorArguments[0].Value is string target)
            {
                return target;
            }
        }
        return null;
    }

    private static string? TryGetRucMessage(ImmutableArray<AttributeData> attrs)
    {
        foreach (var a in attrs)
        {
            if (a.AttributeClass?.Name == RequiresUnreferencedCodeAttributeName
                && a.ConstructorArguments.Length > 0)
            {
                return a.ConstructorArguments[0].Value as string;
            }
        }
        return null;
    }

    private static string? TryGetRucMessageFromConstructors(INamedTypeSymbol type)
    {
        foreach (var ctor in type.Constructors)
        {
            var msg = TryGetRucMessage(ctor.GetAttributes());
            if (msg is not null) return msg;
        }
        return null;
    }

    private static string FormatDefaultValue(object? defaultValue, ITypeSymbol type)
    {
        if (defaultValue is null)
        {
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

    private static void EmitContainer(SourceProductionContext ctx, string containerName, MethodData[] methods, HashSet<string> emittedHints)
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

        var className = "Should" + containerName;
        sb.AppendLine($"public static partial class {className}");
        sb.AppendLine("{");

        foreach (var m in methods)
        {
            EmitMethod(sb, m);
        }

        sb.AppendLine("}");

        var hint = className + ".g.cs";
        var suffix = 0;
        while (!emittedHints.Add(hint))
        {
            hint = $"{className}_{++suffix}.g.cs";
        }
        ctx.AddSource(hint, sb.ToString());
    }

    private static void EmitMethod(StringBuilder sb, MethodData m)
    {
        var positiveName = m.ShouldNameOverride ?? NameConjugator.Conjugate(m.MethodName).Name;

        var genericList = m.MethodGenericParams.Length > 0
            ? "<" + string.Join(", ", m.MethodGenericParams.Select(p =>
                p.DynamicallyAccessedMembersAttribute is null
                    ? p.Name
                    : $"{p.DynamicallyAccessedMembersAttribute} {p.Name}")) + ">"
            : string.Empty;

        var constraints = string.Join(" ", m.MethodGenericParams
            .Select(p => p.ConstraintClause)
            .Where(c => c is not null));

        var sourceType = $"global::TUnit.Assertions.Should.Core.IShouldSource<{m.SourceTypeArgDisplay}>";
        var returnType = $"global::TUnit.Assertions.Should.Core.ShouldAssertion<{m.AssertionTypeArgDisplay}>";

        sb.AppendLine();
        if (!string.IsNullOrEmpty(m.RequiresUnreferencedCodeMessage))
        {
            var escaped = m.RequiresUnreferencedCodeMessage!.Replace("\"", "\\\"");
            sb.AppendLine($"    [global::System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(\"{escaped}\")]");
        }
        foreach (var code in m.SuppressedTrimWarnings)
        {
            sb.AppendLine($"    [global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"{code}\", Justification = \"Forwarded from source method\")]");
        }

        sb.Append($"    public static {returnType} {positiveName}{genericList}(this {sourceType} source");
        foreach (var p in m.Parameters)
        {
            if (p.CallerArgumentExpressionTarget is not null) continue;
            sb.Append($", {p.TypeName} {p.Name}");
            if (p.HasDefaultValue)
            {
                sb.Append(" = ").Append(p.DefaultValueLiteral);
            }
        }
        foreach (var p in m.Parameters)
        {
            if (p.CallerArgumentExpressionTarget is null) continue;
            sb.Append($", [global::System.Runtime.CompilerServices.CallerArgumentExpression(\"{p.CallerArgumentExpressionTarget}\")] string? {p.Name} = null");
        }
        sb.Append(')');

        if (!string.IsNullOrEmpty(constraints))
        {
            sb.AppendLine();
            sb.Append("        ").Append(constraints);
        }

        sb.AppendLine();
        sb.AppendLine("    {");
        sb.AppendLine("        var innerContext = source.Context;");
        sb.AppendLine($"        innerContext.ExpressionBuilder.Append(\".{positiveName}(\");");

        var caeParams = m.Parameters.Where(p => p.CallerArgumentExpressionTarget is not null).ToArray();
        if (caeParams.Length == 1)
        {
            sb.AppendLine($"        innerContext.ExpressionBuilder.Append({caeParams[0].Name});");
        }
        else if (caeParams.Length > 1)
        {
            sb.AppendLine("        var __added = false;");
            foreach (var p in caeParams)
            {
                sb.AppendLine($"        if ({p.Name} is not null)");
                sb.AppendLine("        {");
                sb.AppendLine("            if (__added) innerContext.ExpressionBuilder.Append(\", \");");
                sb.AppendLine($"            innerContext.ExpressionBuilder.Append({p.Name});");
                sb.AppendLine("            __added = true;");
                sb.AppendLine("        }");
            }
        }
        sb.AppendLine("        innerContext.ExpressionBuilder.Append(\")\");");

        var ctorArgs = new List<string> { "innerContext" };
        ctorArgs.AddRange(m.Parameters.Where(p => p.CallerArgumentExpressionTarget is null).Select(p => p.Name));

        sb.AppendLine($"        var inner = new global::{m.ReturnTypeFullName}{FormatGenericArgs(m.ReturnTypeGenericArgs)}({string.Join(", ", ctorArgs)});");
        sb.AppendLine($"        return new global::TUnit.Assertions.Should.Core.ShouldAssertion<{m.AssertionTypeArgDisplay}>(innerContext, inner);");
        sb.AppendLine("    }");
    }

    private static string FormatGenericArgs(EquatableArray<string> args)
        => args.Length == 0 ? string.Empty : "<" + string.Join(", ", args) + ">";

    private sealed record CollectionContext(
        Compilation Compilation,
        INamedTypeSymbol AssertionSource,
        INamedTypeSymbol AssertionBase,
        INamedTypeSymbol? ShouldNameAttribute,
        HashSet<string> AlreadyBakedShouldExtensionNames,
        ImmutableArray<MethodData>.Builder Builder);

    private sealed record MethodData(
        string ContainerName,
        string MethodName,
        EquatableArray<GenericParamData> MethodGenericParams,
        string SourceTypeArgDisplay,
        string AssertionTypeArgDisplay,
        string ReturnTypeFullName,
        EquatableArray<string> ReturnTypeGenericArgs,
        EquatableArray<ParameterData> Parameters,
        string? ShouldNameOverride,
        string? RequiresUnreferencedCodeMessage,
        EquatableArray<string> SuppressedTrimWarnings);

    private sealed record ParameterData(
        string Name,
        string TypeName,
        bool HasDefaultValue,
        string? DefaultValueLiteral,
        string? CallerArgumentExpressionTarget);

    private sealed record GenericParamData(string Name, string? ConstraintClause, string? DynamicallyAccessedMembersAttribute)
    {
        public static GenericParamData From(ITypeParameterSymbol tp, SymbolDisplayFormat format)
        {
            var constraints = new List<string>();
            if (tp.HasReferenceTypeConstraint) constraints.Add("class");
            if (tp.HasValueTypeConstraint) constraints.Add("struct");
            if (tp.HasNotNullConstraint) constraints.Add("notnull");
            foreach (var ct in tp.ConstraintTypes)
            {
                constraints.Add(ct.ToDisplayString(format));
            }
            if (tp.HasConstructorConstraint) constraints.Add("new()");

            string? damAttr = null;
            foreach (var attr in tp.GetAttributes())
            {
                if (attr.AttributeClass?.Name != "DynamicallyAccessedMembersAttribute"
                    || attr.ConstructorArguments.Length == 0)
                {
                    continue;
                }
                var ctorArg = attr.ConstructorArguments[0];
                if (ctorArg.Type is INamedTypeSymbol enumType && ctorArg.Value is int intValue)
                {
                    damAttr = $"[global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(({enumType.ToDisplayString(format)}){intValue})]";
                }
                break;
            }

            return new GenericParamData(
                tp.Name,
                constraints.Count > 0 ? $"where {tp.Name} : {string.Join(", ", constraints)}" : null,
                damAttr);
        }
    }
}
