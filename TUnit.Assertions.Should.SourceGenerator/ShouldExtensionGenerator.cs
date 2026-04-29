using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
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
    private const string AssertionContextFullName = "TUnit.Assertions.Core.AssertionContext`1";
    private const string ShouldExtensionsNamespace = "TUnit.Assertions.Should.Extensions";
    private const string ShouldNameAttributeFullName = "TUnit.Assertions.Should.Attributes.ShouldNameAttribute";
    private const string CallerArgumentExpressionAttributeName = "CallerArgumentExpressionAttribute";
    private const string RequiresUnreferencedCodeAttributeName = "RequiresUnreferencedCodeAttribute";
    private const string UnconditionalSuppressMessageAttributeName = "UnconditionalSuppressMessageAttribute";
    private const string DynamicallyAccessedMembersAttributeName = "DynamicallyAccessedMembersAttribute";
    private const string ShouldGeneratePartialAttributeFullName = "TUnit.Assertions.Should.Attributes.ShouldGeneratePartialAttribute";

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
        var provider = context.CompilationProvider.Select((compilation, _) => Collect(compilation));

        context.RegisterSourceOutput(provider, static (ctx, payload) =>
        {
            var emittedHints = new HashSet<string>(StringComparer.Ordinal);

            // Wrappers first: they own the return types they cover, and their method names
            // win over extension methods at call sites anyway.
            foreach (var wrapper in payload.Wrappers)
            {
                EmitWrapperPartial(ctx, wrapper, emittedHints);
            }

            foreach (var group in payload.Methods.GroupBy(m => m.ContainerName, StringComparer.Ordinal))
            {
                EmitContainer(ctx, group.Key, group.ToArray(), emittedHints);
            }
        });
    }

    /// <summary>
    /// Caches the data extracted from each referenced assembly, keyed on the
    /// <see cref="MetadataReference"/> instance. Roslyn typically reuses the same
    /// <c>MetadataReference</c> across compilations as long as the underlying assembly
    /// hasn't been rebuilt, so cache hits eliminate the expensive cross-assembly walk on
    /// every keystroke. The cache stores raw walk results (no dedup applied) so that the
    /// dedup sets — built from the union of all references plus the current compilation —
    /// can be applied at merge time without invalidating cache entries.
    /// <para>
    /// <see cref="ConditionalWeakTable{TKey, TValue}"/> uses weak keys so entries become
    /// eligible for GC the moment Roslyn drops the underlying <c>MetadataReference</c> (e.g.
    /// when the dependency assembly is rebuilt). A <c>ConcurrentDictionary</c> would pin
    /// stale references for the lifetime of the IDE process and cause unbounded memory
    /// growth across long sessions with frequent rebuilds.
    /// </para>
    /// </summary>
    private static readonly ConditionalWeakTable<MetadataReference, ReferenceData> s_referenceCache = new();

    private sealed record ReferenceData(
        EquatableArray<MethodData> Methods,
        EquatableArray<WrapperData> Wrappers,
        EquatableArray<string> AlreadyBakedNames);

    private static GeneratorPayload Collect(Compilation compilation)
    {
        var assertionSource = compilation.GetTypeByMetadataName(AssertionSourceFullName);
        var assertionBase = compilation.GetTypeByMetadataName(AssertionBaseFullName);
        var assertionContext = compilation.GetTypeByMetadataName(AssertionContextFullName);
        var shouldNameAttr = compilation.GetTypeByMetadataName(ShouldNameAttributeFullName);
        var partialMarker = compilation.GetTypeByMetadataName(ShouldGeneratePartialAttributeFullName);

        if (assertionSource is null || assertionBase is null || assertionContext is null)
        {
            return new GeneratorPayload(
                new EquatableArray<MethodData>(Array.Empty<MethodData>()),
                new EquatableArray<WrapperData>(Array.Empty<WrapperData>()));
        }

        // Phase 1 — per-reference scan, cached by MetadataReference identity.
        // Skip references that don't transitively reference TUnit.Assertions: the BCL plus arbitrary
        // NuGet packages can't possibly contain extension methods on IAssertionSource<T>, and the
        // closure pre-filter is the single biggest perf win for the generator.
        var assertionsAssembly = assertionSource.ContainingAssembly;
        var refResults = new List<ReferenceData>();
        foreach (var refAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (!ReferencesAssertionsAssembly(refAssembly, assertionsAssembly))
            {
                continue;
            }
            refResults.Add(GetOrComputeReferenceData(
                compilation, refAssembly, assertionSource, assertionBase, assertionContext, shouldNameAttr, partialMarker));
        }

        // Phase 2 — union dedup sets across all references.
        var alreadyBaked = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in refResults)
        {
            foreach (var name in r.AlreadyBakedNames)
            {
                alreadyBaked.Add(name);
            }
        }

        // Phase 3 — walk the current compilation (cannot be cached: changes on every edit).
        var localMethods = ImmutableArray.CreateBuilder<MethodData>();
        var localCtx = new CollectionContext(
            compilation,
            assertionSource,
            assertionBase,
            assertionContext,
            shouldNameAttr,
            alreadyBaked,
            localMethods);
        WalkNamespace(compilation.Assembly.GlobalNamespace, localCtx);

        var localWrappers = new List<WrapperData>();
        if (partialMarker is not null)
        {
            WalkForWrappers(compilation.Assembly.GlobalNamespace, partialMarker, assertionBase, assertionContext, localWrappers, isCurrentAssembly: true);
        }

        // Phase 4 — merge and apply post-walk dedup. Wrapper instance methods and Should-flavored
        // extensions co-exist by design: the wrapper's [ShouldGeneratePartial] only emits methods
        // whose source overload exactly matches a public ctor on the inner assertion (the simple-
        // factory rule), so overloads with optional/default parameters land only on the extension
        // surface. Instance methods take overload-resolution precedence at call sites, so there's
        // no ambiguity. References whose ShouldNameExtensions counterpart is already baked are
        // dropped to prevent CS0121.
        var allMethods = ImmutableArray.CreateBuilder<MethodData>();
        foreach (var m in localMethods)
        {
            allMethods.Add(m);
        }
        foreach (var r in refResults)
        {
            foreach (var m in r.Methods)
            {
                if (alreadyBaked.Contains($"Should{m.ContainerName}"))
                {
                    continue;
                }
                allMethods.Add(m);
            }
        }

        return new GeneratorPayload(
            new EquatableArray<MethodData>(allMethods.ToArray()),
            new EquatableArray<WrapperData>(localWrappers.ToArray()));
    }

    /// <summary>
    /// Returns the cached <see cref="ReferenceData"/> for <paramref name="refAssembly"/>, or
    /// performs a one-shot scan and stores the result. The scan is dedup-free — the union dedup
    /// is applied at merge time, so a cache entry remains valid even when other references in
    /// the compilation change.
    /// </summary>
    private static ReferenceData GetOrComputeReferenceData(
        Compilation compilation,
        IAssemblySymbol refAssembly,
        INamedTypeSymbol assertionSource,
        INamedTypeSymbol assertionBase,
        INamedTypeSymbol assertionContext,
        INamedTypeSymbol? shouldNameAttr,
        INamedTypeSymbol? partialMarker)
    {
        var metadataRef = compilation.GetMetadataReference(refAssembly);
        if (metadataRef is null)
        {
            return ScanReference(refAssembly, compilation, assertionSource, assertionBase, assertionContext, shouldNameAttr, partialMarker);
        }

        if (s_referenceCache.TryGetValue(metadataRef, out var cached))
        {
            return cached;
        }

        var fresh = ScanReference(refAssembly, compilation, assertionSource, assertionBase, assertionContext, shouldNameAttr, partialMarker);

        // Concurrent races between two compilations seeing the same uncached MetadataReference
        // are harmless — both compute the same ReferenceData; first writer wins. Catching
        // ArgumentException is the documented way to handle the "already added" case on
        // ConditionalWeakTable.Add (no TryAdd overload exists in netstandard2.0).
        try
        {
            s_referenceCache.Add(metadataRef, fresh);
        }
        catch (ArgumentException)
        {
        }
        return fresh;
    }

    private static ReferenceData ScanReference(
        IAssemblySymbol refAssembly,
        Compilation compilation,
        INamedTypeSymbol assertionSource,
        INamedTypeSymbol assertionBase,
        INamedTypeSymbol assertionContext,
        INamedTypeSymbol? shouldNameAttr,
        INamedTypeSymbol? partialMarker)
    {
        var methods = ImmutableArray.CreateBuilder<MethodData>();
        var ctx = new CollectionContext(
            compilation,
            assertionSource,
            assertionBase,
            assertionContext,
            shouldNameAttr,
            new HashSet<string>(StringComparer.Ordinal), // no per-reference dedup; applied at merge
            methods);
        WalkNamespace(refAssembly.GlobalNamespace, ctx);

        var wrappers = new List<WrapperData>();
        if (partialMarker is not null)
        {
            WalkForWrappers(refAssembly.GlobalNamespace, partialMarker, assertionBase, assertionContext, wrappers, isCurrentAssembly: false);
        }

        var bakedNames = new List<string>();
        var bakedNs = LookupNamespace(refAssembly.GlobalNamespace, ShouldExtensionsNamespace);
        if (bakedNs is not null)
        {
            foreach (var t in bakedNs.GetTypeMembers())
            {
                bakedNames.Add(t.Name);
            }
        }

        return new ReferenceData(
            new EquatableArray<MethodData>(methods.ToArray()),
            new EquatableArray<WrapperData>(wrappers.ToArray()),
            new EquatableArray<string>(bakedNames.ToArray()));
    }

    private static void WalkForWrappers(
        INamespaceSymbol ns,
        INamedTypeSymbol marker,
        INamedTypeSymbol assertionBase,
        INamedTypeSymbol assertionContext,
        List<WrapperData> builder,
        bool isCurrentAssembly)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            CollectWrapper(type, marker, assertionBase, assertionContext, builder, isCurrentAssembly);
        }
        foreach (var nested in ns.GetNamespaceMembers())
        {
            WalkForWrappers(nested, marker, assertionBase, assertionContext, builder, isCurrentAssembly);
        }
    }

    private static void CollectWrapper(
        INamedTypeSymbol type,
        INamedTypeSymbol marker,
        INamedTypeSymbol assertionBase,
        INamedTypeSymbol assertionContext,
        List<WrapperData> builder,
        bool isCurrentAssembly)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            CollectWrapper(nested, marker, assertionBase, assertionContext, builder, isCurrentAssembly);
        }

        // Read the wrapped type from [ShouldGeneratePartial(typeof(...))]. The attribute's
        // single ctor argument names the wrapped definition explicitly, so the wrapper class
        // is free to construct its own AssertionContext rather than piggybacking on the
        // wrapped type's constructor. For 1-arity generics the open form is supplied
        // (typeof(Foo<>)) and we substitute the wrapper class's type parameter to close it.
        INamedTypeSymbol? wrappedType = null;
        ITypeSymbol? wrappedAssertionTypeArg = null;
        foreach (var attr in type.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, marker)) continue;
            if (attr.ConstructorArguments.Length != 1) continue;
            if (attr.ConstructorArguments[0].Value is not INamedTypeSymbol declared) continue;

            var closed = CloseWrappedType(declared, type);
            if (closed is null) continue;
            if (!DerivesFromAssertion(closed, assertionBase, out var typeArg)) continue;

            wrappedType = closed;
            wrappedAssertionTypeArg = typeArg;
            break;
        }

        if (wrappedType is null || wrappedAssertionTypeArg is null)
        {
            return;
        }

        // Wrappers from referenced assemblies are still collected — their return-type keys
        // feed the dedup set so the main extension-method scan skips already-baked extensions.
        // The IsCurrentAssembly flag on WrapperData controls whether the emission step actually
        // generates partial methods (only true for wrappers in this compilation).
        var methods = ImmutableArray.CreateBuilder<WrapperMethodData>();
        foreach (var sourceMember in EnumerateInstanceMethods(wrappedType))
        {
            if (TryDescribeWrapperMethod(sourceMember, wrappedAssertionTypeArg, assertionBase, assertionContext, out var data))
            {
                methods.Add(data);
            }
        }

        if (methods.Count == 0 && isCurrentAssembly)
        {
            return;
        }

        builder.Add(new WrapperData(
            ContainingNamespace: type.ContainingNamespace?.ToDisplayString(NoGlobalFormat) ?? string.Empty,
            ClassName: type.Name,
            ClassGenericParams: new EquatableArray<GenericParamData>(type.TypeParameters.Select(tp => GenericParamData.From(tp, NoGlobalFormat)).ToList()),
            ClassGenericSuffix: type.IsGenericType ? "<" + string.Join(", ", type.TypeParameters.Select(tp => tp.Name)) + ">" : string.Empty,
            AssertionTypeArgDisplay: wrappedAssertionTypeArg.ToDisplayString(NoGlobalFormat),
            Methods: new EquatableArray<WrapperMethodData>(methods),
            IsCurrentAssembly: isCurrentAssembly));
    }

    /// <summary>
    /// Closes <paramref name="declared"/> against <paramref name="wrapper"/>'s type parameters.
    /// Already-closed types pass through unchanged. Open generics whose arity matches the wrapper
    /// are constructed by substituting the wrapper's type parameters in declaration order — this
    /// covers the typical <c>typeof(Foo&lt;&gt;)</c> on a 1-arity wrapper case. Anything else
    /// returns null so the caller skips emission.
    /// </summary>
    private static INamedTypeSymbol? CloseWrappedType(INamedTypeSymbol declared, INamedTypeSymbol wrapper)
    {
        if (!declared.IsUnboundGenericType && !declared.IsGenericType)
        {
            return declared;
        }
        if (!declared.IsUnboundGenericType)
        {
            return declared; // already closed
        }
        if (declared.TypeParameters.Length != wrapper.TypeParameters.Length)
        {
            return null;
        }
        return declared.OriginalDefinition.Construct(wrapper.TypeParameters.Cast<ITypeSymbol>().ToArray());
    }

    private static IEnumerable<IMethodSymbol> EnumerateInstanceMethods(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IMethodSymbol m
                    && m.MethodKind == MethodKind.Ordinary
                    && !m.IsStatic
                    && m.DeclaredAccessibility == Accessibility.Public)
                {
                    yield return m;
                }
            }
        }
    }

    private static bool TryDescribeWrapperMethod(
        IMethodSymbol method,
        ITypeSymbol assertionTypeArg,
        INamedTypeSymbol assertionBase,
        INamedTypeSymbol assertionContext,
        out WrapperMethodData data)
    {
        data = null!;

        // Skip methods with method-level generic parameters for v1 — emitting them requires
        // propagating type-arg references that appear in the return type's generic arguments
        // (e.g. IsAssignableTo<TTarget> returns IsAssignableToAssertion<TTarget, TValue>) and
        // the inference works less reliably without explicit declaration site info.
        if (method.TypeParameters.Length > 0)
        {
            return false;
        }

        if (method.ReturnType is not INamedTypeSymbol returnType
            || !DerivesFromAssertion(returnType, assertionBase, out var returnedAssertionArg))
        {
            return false;
        }

        // Wrapper instance methods only make sense when the underlying assertion's value type
        // matches the wrapper's wrapped type — anything else would require a context Map.
        if (!SymbolEqualityComparer.Default.Equals(returnedAssertionArg, assertionTypeArg))
        {
            return false;
        }

        var paramData = ImmutableArray.CreateBuilder<ParameterData>();
        var ctorCandidates = new List<IParameterSymbol>();
        foreach (var p in method.Parameters)
        {
            var caeTarget = TryGetCallerArgumentExpressionTarget(p);
            paramData.Add(new ParameterData(
                Name: p.Name,
                TypeName: p.Type.ToDisplayString(NoGlobalFormat),
                HasDefaultValue: p.HasExplicitDefaultValue,
                DefaultValueLiteral: p.HasExplicitDefaultValue ? FormatDefaultValue(p.ExplicitDefaultValue, p.Type) : null,
                CallerArgumentExpressionTarget: caeTarget));
            if (caeTarget is null) ctorCandidates.Add(p);
        }

        if (!HasMatchingConstructor(returnType, assertionTypeArg, assertionContext, ctorCandidates))
        {
            return false;
        }

        data = new WrapperMethodData(
            SourceMethodName: method.Name,
            Parameters: new EquatableArray<ParameterData>(paramData),
            ReturnTypeFullName: returnType.ConstructedFrom.ToDisplayString(NameWithoutTypeArgsFormat),
            ReturnTypeGenericArgs: new EquatableArray<string>(returnType.TypeArguments.Select(a => a.ToDisplayString(NoGlobalFormat)).ToList()),
            RequiresUnreferencedCodeMessage: TryGetRucMessage(method.GetAttributes())
                                          ?? TryGetRucMessage(returnType.GetAttributes())
                                          ?? TryGetRucMessageFromConstructors(returnType));
        return true;
    }

    private static bool ReferencesAssertionsAssembly(IAssemblySymbol reference, IAssemblySymbol assertionsAssembly)
    {
        if (SymbolEqualityComparer.Default.Equals(reference, assertionsAssembly))
        {
            return true;
        }

        foreach (var module in reference.Modules)
        {
            foreach (var refed in module.ReferencedAssemblySymbols)
            {
                if (SymbolEqualityComparer.Default.Equals(refed, assertionsAssembly))
                {
                    return true;
                }
            }
        }
        return false;
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
        if (!HasMatchingConstructor(returnType, assertionTypeArg, ctx.AssertionContext, ctorCandidates))
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

        var forwardedAttributes = CollectForwardedAttributes(method.GetAttributes());

        var overrideName = TryGetShouldNameOverride(returnType, ctx.ShouldNameAttribute);

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
            SuppressedTrimWarnings: new EquatableArray<string>(suppressedTrimWarnings),
            ForwardedAttributes: new EquatableArray<string>(forwardedAttributes)));
    }

    /// <summary>
    /// Captures <see cref="System.ObsoleteAttribute"/> and
    /// <see cref="System.ComponentModel.EditorBrowsableAttribute"/> on the source extension method
    /// so they propagate to the Should-flavored counterpart. Without forwarding, deprecating an
    /// underlying assertion (<c>IsAll</c>) would leave the Should counterpart (<c>BeAll</c>)
    /// undeprecated — users would see the warning on one entry but not the other.
    /// </summary>
    private static List<string> CollectForwardedAttributes(ImmutableArray<AttributeData> attrs)
    {
        var result = new List<string>();
        foreach (var a in attrs)
        {
            var ns = a.AttributeClass?.ContainingNamespace?.ToDisplayString();
            if (a.AttributeClass?.Name == "ObsoleteAttribute" && ns == "System")
            {
                result.Add(FormatObsolete(a));
            }
            else if (a.AttributeClass?.Name == "EditorBrowsableAttribute" && ns == "System.ComponentModel")
            {
                result.Add(FormatEditorBrowsable(a));
            }
        }
        return result;
    }

    private static string FormatObsolete(AttributeData attr)
        => TUnit.SourceGen.Shared.AttributeForwardingFormatters.FormatObsolete(attr, globalQualifier: "global::");

    private static string FormatEditorBrowsable(AttributeData attr)
        => TUnit.SourceGen.Shared.AttributeForwardingFormatters.FormatEditorBrowsable(attr, globalQualifier: "global::");

    private static List<string> CollectSuppressedTrimWarnings(ImmutableArray<AttributeData> attrs)
    {
        var result = new List<string>();
        foreach (var a in attrs)
        {
            if (a.AttributeClass?.Name != UnconditionalSuppressMessageAttributeName
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
        INamedTypeSymbol assertionContextSymbol,
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
                || !SymbolEqualityComparer.Default.Equals(firstCtorParam.OriginalDefinition, assertionContextSymbol)
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

    private static string? TryGetShouldNameOverride(INamedTypeSymbol returnType, INamedTypeSymbol? shouldNameAttr)
    {
        if (shouldNameAttr is null) return null;
        foreach (var attr in returnType.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, shouldNameAttr)
                && attr.ConstructorArguments.Length > 0)
            {
                return attr.ConstructorArguments[0].Value as string;
            }
        }
        return null;
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

        // Numeric literals need their C# type suffix or they'd default-bind to int/double:
        // a `float` parameter with default 1.5f would otherwise emit `= 1.5` (a double literal)
        // and fail to compile. Cast through invariant culture so locales using comma decimal
        // separators don't produce malformed literals like `1,5F`.
        return defaultValue switch
        {
            string s => "\"" + s.Replace("\"", "\\\"") + "\"",
            bool b => b ? "true" : "false",
            char c => $"'{c}'",
            float f => System.FormattableString.Invariant($"{f}F"),
            double d => System.FormattableString.Invariant($"{d}D"),
            decimal m => System.FormattableString.Invariant($"{m}M"),
            long l => System.FormattableString.Invariant($"{l}L"),
            ulong ul => System.FormattableString.Invariant($"{ul}UL"),
            uint u => System.FormattableString.Invariant($"{u}U"),
            _ => System.Convert.ToString(defaultValue, System.Globalization.CultureInfo.InvariantCulture) ?? "default",
        };
    }

    private static void EmitWrapperPartial(SourceProductionContext ctx, WrapperData wrapper, HashSet<string> emittedHints)
    {
        if (!wrapper.IsCurrentAssembly || wrapper.Methods.Length == 0)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using TUnit.Assertions.Core;");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(wrapper.ContainingNamespace))
        {
            sb.AppendLine($"namespace {wrapper.ContainingNamespace};");
            sb.AppendLine();
        }

        var classGenericList = wrapper.ClassGenericParams.Length > 0
            ? "<" + string.Join(", ", wrapper.ClassGenericParams.Select(p => p.Name)) + ">"
            : string.Empty;

        sb.AppendLine($"partial class {wrapper.ClassName}{classGenericList}");
        sb.AppendLine("{");

        foreach (var m in wrapper.Methods)
        {
            EmitWrapperMethod(sb, wrapper, m);
        }

        sb.AppendLine("}");

        var hint = $"{wrapper.ClassName}.Generated.g.cs";
        var suffix = 0;
        while (!emittedHints.Add(hint))
        {
            hint = $"{wrapper.ClassName}_{++suffix}.Generated.g.cs";
        }
        ctx.AddSource(hint, sb.ToString());
    }

    private static void EmitWrapperMethod(StringBuilder sb, WrapperData wrapper, WrapperMethodData m)
    {
        var positiveName = NameConjugator.Conjugate(m.SourceMethodName);
        var returnType = $"global::TUnit.Assertions.Should.Core.ShouldAssertion<{wrapper.AssertionTypeArgDisplay}>";

        sb.AppendLine();
        if (!string.IsNullOrEmpty(m.RequiresUnreferencedCodeMessage))
        {
            var escaped = m.RequiresUnreferencedCodeMessage!.Replace("\"", "\\\"");
            sb.AppendLine($"    [global::System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(\"{escaped}\")]");
        }

        sb.Append($"    public {returnType} {positiveName}(");
        var first = true;
        foreach (var p in m.Parameters)
        {
            if (p.CallerArgumentExpressionTarget is not null) continue;
            if (!first) sb.Append(", ");
            sb.Append($"{p.TypeName} {p.Name}");
            if (p.HasDefaultValue)
            {
                sb.Append(" = ").Append(p.DefaultValueLiteral);
            }
            first = false;
        }
        foreach (var p in m.Parameters)
        {
            if (p.CallerArgumentExpressionTarget is null) continue;
            if (!first) sb.Append(", ");
            sb.Append($"[global::System.Runtime.CompilerServices.CallerArgumentExpression(\"{p.CallerArgumentExpressionTarget}\")] string? {p.Name} = null");
            first = false;
        }
        sb.AppendLine(")");
        sb.AppendLine("    {");
        sb.AppendLine($"        Context.ExpressionBuilder.Append(\".{positiveName}(\");");

        var caeParams = m.Parameters.Where(p => p.CallerArgumentExpressionTarget is not null).ToArray();
        if (caeParams.Length == 1)
        {
            sb.AppendLine($"        Context.ExpressionBuilder.Append({caeParams[0].Name});");
        }
        else if (caeParams.Length > 1)
        {
            sb.AppendLine("        var __added = false;");
            foreach (var p in caeParams)
            {
                sb.AppendLine($"        if ({p.Name} is not null)");
                sb.AppendLine("        {");
                sb.AppendLine("            if (__added) Context.ExpressionBuilder.Append(\", \");");
                sb.AppendLine($"            Context.ExpressionBuilder.Append({p.Name});");
                sb.AppendLine("            __added = true;");
                sb.AppendLine("        }");
            }
        }
        sb.AppendLine("        Context.ExpressionBuilder.Append(\")\");");

        var ctorArgs = new List<string> { "Context" };
        ctorArgs.AddRange(m.Parameters.Where(p => p.CallerArgumentExpressionTarget is null).Select(p => p.Name));

        sb.AppendLine($"        var inner = new global::{m.ReturnTypeFullName}{FormatGenericArgs(m.ReturnTypeGenericArgs)}({string.Join(", ", ctorArgs)});");
        sb.AppendLine($"        return new global::TUnit.Assertions.Should.Core.ShouldAssertion<{wrapper.AssertionTypeArgDisplay}>(Context, inner);");
        sb.AppendLine("    }");
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
        var positiveName = m.ShouldNameOverride ?? NameConjugator.Conjugate(m.MethodName);

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
        foreach (var attr in m.ForwardedAttributes)
        {
            sb.AppendLine($"    {attr}");
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

    private sealed record GeneratorPayload(
        EquatableArray<MethodData> Methods,
        EquatableArray<WrapperData> Wrappers);

    private sealed record WrapperData(
        string ContainingNamespace,
        string ClassName,
        EquatableArray<GenericParamData> ClassGenericParams,
        string ClassGenericSuffix,
        string AssertionTypeArgDisplay,
        EquatableArray<WrapperMethodData> Methods,
        bool IsCurrentAssembly);

    private sealed record WrapperMethodData(
        string SourceMethodName,
        EquatableArray<ParameterData> Parameters,
        string ReturnTypeFullName,
        EquatableArray<string> ReturnTypeGenericArgs,
        string? RequiresUnreferencedCodeMessage);

    /// <summary>
    /// Mutable bag of pre-resolved Roslyn symbols and the in-flight <see cref="MethodData"/>
    /// builder, threaded through the namespace walk. Not a record — it doesn't flow through
    /// the incremental pipeline as a cache key, and embeds a mutable builder.
    /// </summary>
    private sealed class CollectionContext
    {
        public CollectionContext(
            Compilation compilation,
            INamedTypeSymbol assertionSource,
            INamedTypeSymbol assertionBase,
            INamedTypeSymbol assertionContext,
            INamedTypeSymbol? shouldNameAttribute,
            HashSet<string> alreadyBakedShouldExtensionNames,
            ImmutableArray<MethodData>.Builder builder)
        {
            Compilation = compilation;
            AssertionSource = assertionSource;
            AssertionBase = assertionBase;
            AssertionContext = assertionContext;
            ShouldNameAttribute = shouldNameAttribute;
            AlreadyBakedShouldExtensionNames = alreadyBakedShouldExtensionNames;
            Builder = builder;
        }

        public Compilation Compilation { get; }
        public INamedTypeSymbol AssertionSource { get; }
        public INamedTypeSymbol AssertionBase { get; }
        public INamedTypeSymbol AssertionContext { get; }
        public INamedTypeSymbol? ShouldNameAttribute { get; }
        public HashSet<string> AlreadyBakedShouldExtensionNames { get; }
        public ImmutableArray<MethodData>.Builder Builder { get; }
    }

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
        EquatableArray<string> SuppressedTrimWarnings,
        EquatableArray<string> ForwardedAttributes);

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
                if (attr.AttributeClass?.Name != DynamicallyAccessedMembersAttributeName
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
