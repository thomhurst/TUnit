using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    private const string AssertFullName = "TUnit.Assertions.Assert";
    private const string CallerArgumentExpressionAttributeFullName = "System.Runtime.CompilerServices.CallerArgumentExpressionAttribute";
    private const string ShouldExtensionsNamespace = "TUnit.Assertions.Should.Extensions";
    private const string ShouldNameAttributeFullName = "TUnit.Assertions.Should.Attributes.ShouldNameAttribute";
    private const string CallerArgumentExpressionAttributeName = "CallerArgumentExpressionAttribute";
    private const string RequiresUnreferencedCodeAttributeName = "RequiresUnreferencedCodeAttribute";
    private const string UnconditionalSuppressMessageAttributeName = "UnconditionalSuppressMessageAttribute";
    private const string DynamicallyAccessedMembersAttributeName = "DynamicallyAccessedMembersAttribute";
    private const string ShouldGeneratePartialAttributeFullName = "TUnit.Assertions.Should.Attributes.ShouldGeneratePartialAttribute";
    private const string ShouldExtensionsTypeFullName = "TUnit.Assertions.Should.ShouldExtensions";

    private static readonly SymbolDisplayFormat NoGlobalFormat =
        SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
            .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly SymbolDisplayFormat NameWithoutTypeArgsFormat =
        SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
            .WithGenericsOptions(SymbolDisplayGenericsOptions.None);

    public const string LocalContainersStep = "ShouldLocalContainers";
    public const string LocalWrappersStep = "ShouldLocalWrappers";
    public const string LocalDeclarationsStep = "ShouldLocalDeclarations";
    public const string CompilationDataStep = "ShouldCompilationData";
    public const string PayloadStep = "ShouldPayload";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // The current compilation is scanned through syntax providers so an edit only re-examines
        // candidate declarations instead of walking every type in the assembly:
        //  - extension-method containers: top-level classes declaring a method with a `this` parameter
        //    or a C# 14 extension block
        //  - wrappers: classes carrying [ShouldGeneratePartial]
        // The syntax steps yield only metadata names. What a candidate contributes also depends on
        // other types (a container's return types, a wrapper's wrapped assertion), which may be
        // declared in other files, so the names are resolved against every compilation in
        // LocalDeclarationsStep. That step only visits the candidates, and its result compares by
        // value, so the outputs stay cached when nothing relevant changed.
        // Referenced assemblies are still read from the CompilationProvider; that cost is absorbed
        // by the per-MetadataReference ConditionalWeakTable caches below.
        var localContainers = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsExtensionContainerCandidate(node),
                transform: static (ctx, ct) => GetDeclaredTypeMetadataName(ctx, ct))
            .Where(static x => x is not null)
            .Select(static (x, _) => x!)
            .WithTrackingName(LocalContainersStep);

        var localWrappers = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ShouldGeneratePartialAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => ctx.TargetSymbol is INamedTypeSymbol type ? GetMetadataName(type) : null)
            .Where(static x => x is not null)
            .Select(static (x, _) => x!)
            .WithTrackingName(LocalWrappersStep);

        var localDeclarations = localContainers.Collect()
            .Combine(localWrappers.Collect())
            .Combine(context.CompilationProvider)
            .Select(static (data, _) => ResolveLocalDeclarations(data.Right, data.Left.Left, data.Left.Right))
            .WithTrackingName(LocalDeclarationsStep);

        var compilationData = context.CompilationProvider
            .Select(static (compilation, _) => CollectCompilationData(compilation))
            .WithTrackingName(CompilationDataStep);

        var provider = compilationData
            .Combine(localDeclarations)
            .Select(static (data, _) => Merge(data.Left, data.Right))
            .WithTrackingName(PayloadStep);

        context.RegisterSourceOutput(provider, static (ctx, payload) =>
        {
            var emittedHints = new HashSet<string>(StringComparer.Ordinal);

            if (payload.Entries.Length > 0)
            {
                EmitShouldEntries(ctx, payload.Entries.ToArray(), emittedHints);
            }

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

    // IMPORTANT: keep ReferenceData compilation-independent. Do not cache live ISymbol
    // instances here; symbols are tied to the compilation that produced them and become stale
    // when Roslyn creates the next compilation.
    private sealed record ReferenceData(
        EquatableArray<MethodData> Methods,
        EquatableArray<WrapperData> Wrappers,
        EquatableArray<ShouldEntryData> Entries,
        EquatableArray<string> AlreadyBakedNames,
        EquatableArray<string> BakedShouldEntryKeys);

    /// <summary>
    /// Caches the <see cref="ReferencesAssertionsAssembly"/> pre-filter per referenced assembly,
    /// so the references-of-references loop runs once per <see cref="MetadataReference"/> rather
    /// than once per reference on every compilation. Keyed and weakly held like
    /// <see cref="s_referenceCache"/>.
    /// </summary>
    private static readonly ConditionalWeakTable<MetadataReference, PrefilterResult> s_prefilterCache = new();

    private sealed class PrefilterResult
    {
        public PrefilterResult(AssemblyIdentity assertionsIdentity, bool referencesAssertions)
        {
            AssertionsIdentity = assertionsIdentity;
            ReferencesAssertions = referencesAssertions;
        }

        public AssemblyIdentity AssertionsIdentity { get; }
        public bool ReferencesAssertions { get; }
    }

    /// <summary>
    /// Everything the generator needs from the compilation as a whole: the merged data of every
    /// referenced assembly plus the Should entry points. Computed on every compilation, but only
    /// from cheap lookups and cached per-reference results, and it compares by value so the
    /// downstream steps stay cached when nothing relevant changed.
    /// </summary>
    private static CompilationData CollectCompilationData(Compilation compilation)
    {
        var assertionSource = compilation.GetTypeByMetadataName(AssertionSourceFullName);
        var assertionBase = compilation.GetTypeByMetadataName(AssertionBaseFullName);
        var assertionContext = compilation.GetTypeByMetadataName(AssertionContextFullName);
        var shouldNameAttr = compilation.GetTypeByMetadataName(ShouldNameAttributeFullName);
        var partialMarker = compilation.GetTypeByMetadataName(ShouldGeneratePartialAttributeFullName);
        var assertType = compilation.GetTypeByMetadataName(AssertFullName);

        if (assertionSource is null || assertionBase is null || assertionContext is null)
        {
            return CompilationData.Disabled;
        }

        // Phase 1 — per-reference scan, cached by MetadataReference identity.
        // Skip references that don't transitively reference TUnit.Assertions: the BCL plus arbitrary
        // NuGet packages can't possibly contain extension methods on IAssertionSource<T>, and the
        // closure pre-filter is the single biggest perf win for the generator.
        var assertionsAssembly = assertionSource.ContainingAssembly;
        var refResults = new List<ReferenceData>();
        foreach (var refAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            var metadataRef = compilation.GetMetadataReference(refAssembly);
            if (!ReferencesAssertionsAssemblyCached(metadataRef, refAssembly, assertionsAssembly))
            {
                continue;
            }
            refResults.Add(GetOrComputeReferenceData(
                compilation, metadataRef, refAssembly, assertionSource, assertionBase, assertionContext, shouldNameAttr, partialMarker, assertType));
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

        // Phase 3 — the parts of the current compilation that are single-type lookups. Extension
        // containers and wrappers are collected by the syntax providers instead.
        var localEntries = ImmutableArray.CreateBuilder<ShouldEntryData>();
        if (assertType is not null && SymbolEqualityComparer.Default.Equals(assertType.ContainingAssembly, compilation.Assembly))
        {
            CollectShouldEntries(assertType, assertionSource, assertionBase, assertionContext, localEntries);
        }

        var currentShouldEntryKeys = new HashSet<string>(StringComparer.Ordinal);
        CollectExistingShouldEntryKeys(compilation.Assembly, currentShouldEntryKeys);

        var bakedReferencedShouldEntryKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in refResults)
        {
            foreach (var bakedKey in r.BakedShouldEntryKeys)
            {
                bakedReferencedShouldEntryKeys.Add(bakedKey);
            }
        }

        // Phase 4 — apply post-walk dedup to the referenced data. Wrapper instance methods and
        // Should-flavored extensions co-exist by design: the wrapper's [ShouldGeneratePartial] only
        // emits methods whose source overload exactly matches a public ctor on the inner assertion
        // (the simple-factory rule), so overloads with optional/default parameters land only on the
        // extension surface. Instance methods take overload-resolution precedence at call sites, so
        // there's no ambiguity. References whose ShouldNameExtensions counterpart is already baked
        // are dropped to prevent CS0121.
        var referenceMethods = ImmutableArray.CreateBuilder<MethodData>();
        foreach (var r in refResults)
        {
            foreach (var m in r.Methods)
            {
                if (alreadyBaked.Contains($"Should{m.ContainerName}"))
                {
                    continue;
                }
                referenceMethods.Add(m);
            }
        }

        var allEntries = ImmutableArray.CreateBuilder<ShouldEntryData>();
        foreach (var entry in localEntries)
        {
            if (!currentShouldEntryKeys.Contains(entry.SignatureKey))
            {
                allEntries.Add(entry);
            }
        }
        foreach (var r in refResults)
        {
            foreach (var entry in r.Entries)
            {
                var signatureKey = entry.SignatureKey;
                if (!currentShouldEntryKeys.Contains(signatureKey)
                    && !bakedReferencedShouldEntryKeys.Contains(signatureKey))
                {
                    allEntries.Add(entry);
                }
            }
        }

        return new CompilationData(
            IsEnabled: true,
            new EquatableArray<MethodData>(referenceMethods.ToArray()),
            new EquatableArray<ShouldEntryData>(DeduplicateEntries(allEntries.ToArray())));
    }

    private static GeneratorPayload Merge(CompilationData compilationData, LocalDeclarations localDeclarations)
    {
        if (!compilationData.IsEnabled)
        {
            return GeneratorPayload.Empty;
        }

        // Current-compilation methods come first, as the namespace walk used to produce them.
        var allMethods = new List<MethodData>(localDeclarations.Methods);
        allMethods.AddRange(compilationData.ReferenceMethods);

        return new GeneratorPayload(
            new EquatableArray<MethodData>(DeduplicateMethods(allMethods.ToArray())),
            localDeclarations.Wrappers,
            compilationData.Entries);
    }

    /// <summary>
    /// Raw kind of a C# 14 <c>extension(T x) { ... }</c> block. The generator is built against a
    /// Roslyn that predates the syntax, so the kind is looked up by name in the host compiler's
    /// <see cref="SyntaxKind"/>; it stays null on compilers that can't parse extension blocks.
    /// </summary>
    private static readonly int? s_extensionBlockRawKind =
        Enum.TryParse<SyntaxKind>("ExtensionBlockDeclaration", out var kind) ? (int)kind : null;

    /// <summary>
    /// Syntactic gate for <see cref="GetDeclaredTypeMetadataName"/>: a top-level class declaring a method
    /// whose first parameter carries <c>this</c>, or a C# 14 extension block. Extension methods can
    /// only live in top-level non-generic static classes, so every class the namespace walk could
    /// collect from passes. Extension-block members surface on the containing class as extension
    /// methods, which the namespace walk collected, so blocks must pass the gate too.
    /// </summary>
    private static bool IsExtensionContainerCandidate(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDeclaration
            || classDeclaration.Parent is not (BaseNamespaceDeclarationSyntax or CompilationUnitSyntax))
        {
            return false;
        }

        foreach (var member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax { ParameterList.Parameters: { Count: > 0 } parameters }
                && parameters[0].Modifiers.Any(SyntaxKind.ThisKeyword))
            {
                return true;
            }

            if (member.RawKind == s_extensionBlockRawKind)
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetDeclaredTypeMetadataName(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is INamedTypeSymbol type
            ? GetMetadataName(type)
            : null;
    }

    /// <summary>
    /// Builds the name that <see cref="IAssemblySymbol.GetTypeByMetadataName"/> resolves: namespaces
    /// joined by '.', containing types by '+', arity suffixes included.
    /// </summary>
    private static string GetMetadataName(INamedTypeSymbol type)
    {
        var builder = new StringBuilder(type.MetadataName);
        for (var containing = type.ContainingType; containing is not null; containing = containing.ContainingType)
        {
            builder.Insert(0, '+').Insert(0, containing.MetadataName);
        }
        for (var ns = type.ContainingNamespace; ns is { IsGlobalNamespace: false }; ns = ns.ContainingNamespace)
        {
            builder.Insert(0, '.').Insert(0, ns.MetadataName);
        }
        return builder.ToString();
    }

    /// <summary>
    /// Resolves the candidates found by the syntax providers against the current compilation.
    /// A partial type is reported once per declaration, so repeated names are skipped and the
    /// first occurrence keeps its position. Equal metadata names are treated as the same type:
    /// two distinct types cannot share one within an assembly (CS0101), and
    /// <see cref="IAssemblySymbol.GetTypeByMetadataName"/> resolves the name to that type either way.
    /// </summary>
    private static LocalDeclarations ResolveLocalDeclarations(
        Compilation compilation,
        ImmutableArray<string> containerNames,
        ImmutableArray<string> wrapperNames)
    {
        if (containerNames.IsEmpty && wrapperNames.IsEmpty)
        {
            return LocalDeclarations.Empty;
        }

        var assertionSource = compilation.GetTypeByMetadataName(AssertionSourceFullName);
        var assertionBase = compilation.GetTypeByMetadataName(AssertionBaseFullName);
        var assertionContext = compilation.GetTypeByMetadataName(AssertionContextFullName);
        if (assertionSource is null || assertionBase is null || assertionContext is null)
        {
            return LocalDeclarations.Empty;
        }

        var assembly = compilation.Assembly;

        var methods = ImmutableArray.CreateBuilder<MethodData>();
        var ctx = new CollectionContext(
            compilation,
            assertionSource,
            assertionBase,
            assertionContext,
            compilation.GetTypeByMetadataName(ShouldNameAttributeFullName),
            new HashSet<string>(StringComparer.Ordinal), // baked-name dedup never applies to the current assembly
            methods);
        var seenContainers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in containerNames)
        {
            if (seenContainers.Add(name) && assembly.GetTypeByMetadataName(name) is { } type)
            {
                CollectFromContainer(type, ctx);
            }
        }

        var wrappers = new List<WrapperData>(wrapperNames.Length);
        var seenWrappers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in wrapperNames)
        {
            if (seenWrappers.Add(name)
                && assembly.GetTypeByMetadataName(name) is { } type
                && FindAttributeClass(type, ShouldGeneratePartialAttributeFullName) is { } marker
                && DescribeWrapper(type, marker, assertionBase, assertionContext, isCurrentAssembly: true) is { } wrapper)
            {
                wrappers.Add(wrapper);
            }
        }

        return new LocalDeclarations(
            new EquatableArray<MethodData>(methods.ToArray()),
            new EquatableArray<WrapperData>(wrappers.ToArray()));
    }

    private static INamedTypeSymbol? FindAttributeClass(INamedTypeSymbol type, string attributeFullName)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass is { } attributeClass
                && attributeClass.ToDisplayString(NameWithoutTypeArgsFormat) == attributeFullName)
            {
                return attributeClass;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the cached <see cref="ReferenceData"/> for <paramref name="refAssembly"/>, or
    /// performs a one-shot scan and stores the result. The scan is dedup-free — the union dedup
    /// is applied at merge time, so a cache entry remains valid even when other references in
    /// the compilation change.
    /// </summary>
    private static ReferenceData GetOrComputeReferenceData(
        Compilation compilation,
        MetadataReference? metadataRef,
        IAssemblySymbol refAssembly,
        INamedTypeSymbol assertionSource,
        INamedTypeSymbol assertionBase,
        INamedTypeSymbol assertionContext,
        INamedTypeSymbol? shouldNameAttr,
        INamedTypeSymbol? partialMarker,
        INamedTypeSymbol? assertType)
    {
        if (metadataRef is null)
        {
            return ScanReference(refAssembly, compilation, assertionSource, assertionBase, assertionContext, shouldNameAttr, partialMarker, assertType);
        }

        if (s_referenceCache.TryGetValue(metadataRef, out var cached))
        {
            return cached;
        }

        var fresh = ScanReference(refAssembly, compilation, assertionSource, assertionBase, assertionContext, shouldNameAttr, partialMarker, assertType);

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
        INamedTypeSymbol? partialMarker,
        INamedTypeSymbol? assertType)
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

        var entries = ImmutableArray.CreateBuilder<ShouldEntryData>();
        if (assertType is not null && SymbolEqualityComparer.Default.Equals(assertType.ContainingAssembly, refAssembly))
        {
            CollectShouldEntries(assertType, assertionSource, assertionBase, assertionContext, entries);
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

        var bakedShouldEntryKeys = new HashSet<string>(StringComparer.Ordinal);
        CollectExistingShouldEntryKeys(refAssembly, bakedShouldEntryKeys);

        return new ReferenceData(
            new EquatableArray<MethodData>(DeduplicateMethods(methods.ToArray())),
            new EquatableArray<WrapperData>(wrappers.ToArray()),
            new EquatableArray<ShouldEntryData>(entries.ToArray()),
            new EquatableArray<string>(bakedNames.ToArray()),
            new EquatableArray<string>(bakedShouldEntryKeys.ToArray()));
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

        if (DescribeWrapper(type, marker, assertionBase, assertionContext, isCurrentAssembly) is { } wrapper)
        {
            builder.Add(wrapper);
        }
    }

    private static WrapperData? DescribeWrapper(
        INamedTypeSymbol type,
        INamedTypeSymbol marker,
        INamedTypeSymbol assertionBase,
        INamedTypeSymbol assertionContext,
        bool isCurrentAssembly)
    {
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
            return null;
        }

        // A partial declaration emitted into a generated file can never join a file-local type,
        // so a wrapper that is (or is nested in) a `file` type can't be augmented. Emitting would
        // declare an unrelated type whose members reference a missing Context and break the build.
        if (isCurrentAssembly && IsFileLocalOrNestedInFileLocal(type))
        {
            return null;
        }

        // Wrappers from referenced assemblies are still collected — their return-type keys
        // feed the dedup set so the main extension-method scan skips already-baked extensions.
        // The IsCurrentAssembly flag on WrapperData controls whether the emission step actually
        // generates partial methods (only true for wrappers in this compilation).
        var methods = ImmutableArray.CreateBuilder<WrapperMethodData>();
        var existingWrapperMethodKeys = isCurrentAssembly
            ? CollectExistingWrapperMethodKeys(type)
            : null;
        foreach (var sourceMember in EnumerateInstanceMethods(wrappedType))
        {
            if (TryDescribeWrapperMethod(sourceMember, wrappedAssertionTypeArg, assertionBase, assertionContext, out var data))
            {
                if (existingWrapperMethodKeys is null || existingWrapperMethodKeys.Add(GetWrapperMethodKey(data)))
                {
                    methods.Add(data);
                }
            }
        }

        if (methods.Count == 0 && isCurrentAssembly)
        {
            return null;
        }

        return new WrapperData(
            ContainingNamespace: type.ContainingNamespace?.ToDisplayString(NoGlobalFormat) ?? string.Empty,
            ContainingTypeDeclarations: GetContainingTypeDeclarations(type),
            ClassName: EscapeIdentifier(type.Name),
            ClassGenericParams: new EquatableArray<GenericParamData>(type.TypeParameters.Select(tp => GenericParamData.From(tp, NoGlobalFormat)).ToList()),
            ClassGenericSuffix: type.IsGenericType ? "<" + string.Join(", ", type.TypeParameters.Select(tp => EscapeIdentifier(tp.Name))) + ">" : string.Empty,
            AssertionTypeArgDisplay: wrappedAssertionTypeArg.ToDisplayString(NoGlobalFormat),
            Methods: new EquatableArray<WrapperMethodData>(methods),
            IsCurrentAssembly: isCurrentAssembly);
    }

    /// <summary>
    /// Partial declarations of the types enclosing a nested wrapper, outermost first, so the
    /// emitted partial lands inside them rather than at namespace scope.
    /// </summary>
    private static EquatableArray<string> GetContainingTypeDeclarations(INamedTypeSymbol type)
    {
        if (type.ContainingType is null)
        {
            return new EquatableArray<string>(Array.Empty<string>());
        }

        var declarations = new List<string>();
        for (var containing = type.ContainingType; containing is not null; containing = containing.ContainingType)
        {
            var keyword = containing switch
            {
                { IsRecord: true, TypeKind: TypeKind.Struct } => "record struct",
                { IsRecord: true } => "record",
                { TypeKind: TypeKind.Struct } => "struct",
                { TypeKind: TypeKind.Interface } => "interface",
                _ => "class",
            };
            var typeParameters = containing.TypeParameters.Length > 0
                ? "<" + string.Join(", ", containing.TypeParameters.Select(tp => EscapeIdentifier(tp.Name))) + ">"
                : string.Empty;
            declarations.Add($"partial {keyword} {EscapeIdentifier(containing.Name)}{typeParameters}");
        }
        declarations.Reverse();
        return new EquatableArray<string>(declarations.ToArray());
    }

    private static bool IsFileLocalOrNestedInFileLocal(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.IsFileLocal)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Prefixes reserved keywords with '@' so names such as <c>@event</c> stay valid when
    /// written back into generated declarations. <see cref="ISymbol.Name"/> drops the escape.
    /// </summary>
    private static string EscapeIdentifier(string name)
        => SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

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
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var current = type;
             current is not null && current.SpecialType != SpecialType.System_Object;
             current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IMethodSymbol m
                    && m.MethodKind == MethodKind.Ordinary
                    && !m.IsStatic
                    && m.DeclaredAccessibility == Accessibility.Public
                    && seen.Add(GetMethodSignatureKey(m)))
                {
                    yield return m;
                }
            }
        }
    }

    private static string GetMethodSignatureKey(IMethodSymbol method)
    {
        var sb = new StringBuilder(method.Name);
        sb.Append('`').Append(method.Arity);
        foreach (var parameter in method.Parameters)
        {
            sb.Append('|')
                .Append(parameter.RefKind)
                .Append(':')
                .Append(parameter.Type.ToDisplayString(NoGlobalFormat));
        }
        return sb.ToString();
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

        var matchedCtor = TryFindMatchingConstructor(returnType, assertionTypeArg, assertionContext, ctorCandidates);
        if (matchedCtor is null)
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
                                          ?? TryGetRucMessage(matchedCtor.GetAttributes()));
        return true;
    }

    private static HashSet<string> CollectExistingWrapperMethodKeys(INamedTypeSymbol type)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
        {
            if (method.MethodKind != MethodKind.Ordinary || method.IsStatic)
            {
                continue;
            }

            keys.Add(GetWrapperMethodKey(method));
        }

        return keys;
    }

    private static string GetWrapperMethodKey(WrapperMethodData method)
    {
        // Type-parameter count is always 0 here: TryDescribeWrapperMethod rejects methods with
        // method-level type parameters, so WrapperMethodData never carries them. Keep the literal
        // in lockstep with the IMethodSymbol overload's "method.TypeParameters.Length" segment.
        var sb = new StringBuilder(NameConjugator.Conjugate(method.SourceMethodName))
            .Append('|')
            .Append('0');

        foreach (var parameter in method.Parameters)
        {
            sb.Append('|').Append(parameter.TypeName);
        }

        return sb.ToString();
    }

    private static string GetWrapperMethodKey(IMethodSymbol method)
    {
        var sb = new StringBuilder(method.Name)
            .Append('|')
            .Append(method.TypeParameters.Length);

        foreach (var parameter in method.Parameters)
        {
            sb.Append('|').Append(parameter.Type.ToDisplayString(NoGlobalFormat));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Pre-filter: returns true only when the reference (or one of its direct module references)
    /// is <see cref="TUnit.Assertions"/> itself. The check is one-level deep, NOT transitive — a
    /// transitive reference is treated as "doesn't contain Should-relevant types" and skipped.
    /// In practice this is safe because <c>IAssertionSource&lt;T&gt;</c> lives in TUnit.Assertions,
    /// so any assembly declaring extension methods on it must have a direct reference. Skipping
    /// transitively-only-referenced assemblies is the single biggest perf win for the generator
    /// (otherwise it'd walk the entire BCL).
    /// </summary>
    private static bool ReferencesAssertionsAssemblyCached(
        MetadataReference? metadataRef,
        IAssemblySymbol reference,
        IAssemblySymbol assertionsAssembly)
    {
        if (metadataRef is null)
        {
            return ReferencesAssertionsAssembly(reference, assertionsAssembly);
        }

        // A reference's own metadata (and so its reference list) is fixed for a given
        // MetadataReference, so the answer only changes if TUnit.Assertions itself does.
        if (s_prefilterCache.TryGetValue(metadataRef, out var cached)
            && cached.AssertionsIdentity.Equals(assertionsAssembly.Identity))
        {
            return cached.ReferencesAssertions;
        }

        var result = ReferencesAssertionsAssembly(reference, assertionsAssembly);
        s_prefilterCache.Remove(metadataRef);
        try
        {
            s_prefilterCache.Add(metadataRef, new PrefilterResult(assertionsAssembly.Identity, result));
        }
        catch (ArgumentException)
        {
            // Another compilation cached it concurrently; either result is equivalent.
        }
        return result;
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
            || !type.IsStatic)
        {
            return;
        }

        // Generic static containers are not supported; [AssertionExtension] methods live on
        // non-generic static classes so the generated Should wrapper has a concrete owner.
        if (type.IsGenericType)
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
        var matchedCtor = TryFindMatchingConstructor(returnType, assertionTypeArg, ctx.AssertionContext, ctorCandidates);
        if (matchedCtor is null)
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
                       ?? TryGetRucMessage(matchedCtor.GetAttributes());

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
    /// Returns the public ctor on <paramref name="returnType"/> whose parameters, after a
    /// leading <c>AssertionContext&lt;assertionTypeArg&gt;</c>, match
    /// <paramref name="ctorCandidates"/> by type — or null if none match. Guards the
    /// "simple factory" template against extension methods that map context or otherwise
    /// transform before construction.
    /// </summary>
    private static IMethodSymbol? TryFindMatchingConstructor(
        INamedTypeSymbol returnType,
        ITypeSymbol assertionTypeArg,
        INamedTypeSymbol assertionContextSymbol,
        List<IParameterSymbol> ctorCandidates)
    {
        var minimumParameterCount = ctorCandidates.Count + 1;
        foreach (var ctor in returnType.Constructors)
        {
            if (ctor.DeclaredAccessibility != Accessibility.Public
                || ctor.IsStatic
                || ctor.Parameters.Length < minimumParameterCount)
            {
                continue;
            }

            var firstCtorParam = ctor.Parameters[0].Type as INamedTypeSymbol;
            if (firstCtorParam is null
                || !SymbolEqualityComparer.Default.Equals(firstCtorParam.OriginalDefinition, assertionContextSymbol)
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

            // Source overloads may intentionally omit simple constructor defaults, such as
            // StringComparison or comparer parameters. Do not infer hidden boolean/string state:
            // generated negation and expression parameters require body knowledge to forward.
            for (var i = minimumParameterCount; allMatch && i < ctor.Parameters.Length; i++)
            {
                if (!CanOmitConstructorParameter(ctor.Parameters[i]))
                {
                    allMatch = false;
                }
            }
            if (allMatch) return ctor;
        }
        return null;
    }

    private static bool CanOmitConstructorParameter(IParameterSymbol parameter)
    {
        return parameter.HasExplicitDefaultValue
               && parameter.Type.SpecialType != SpecialType.System_Boolean
               && parameter.Type.SpecialType != SpecialType.System_String;
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
                if (current.TypeArguments.Length != 1)
                {
                    break;
                }
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
        sb.AppendLine("#pragma warning disable");
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

        foreach (var declaration in wrapper.ContainingTypeDeclarations)
        {
            sb.AppendLine(declaration);
            sb.AppendLine("{");
        }

        sb.AppendLine($"partial class {wrapper.ClassName}{classGenericList}");
        sb.AppendLine("{");

        foreach (var m in wrapper.Methods)
        {
            EmitWrapperMethod(sb, wrapper, m);
        }

        sb.AppendLine("}");

        for (var i = 0; i < wrapper.ContainingTypeDeclarations.Length; i++)
        {
            sb.AppendLine("}");
        }

        var hintName = wrapper.ClassName.TrimStart('@');
        var hint = $"{hintName}.Generated.g.cs";
        var suffix = 0;
        while (!emittedHints.Add(hint))
        {
            hint = $"{hintName}_{++suffix}.Generated.g.cs";
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
        sb.AppendLine($"        var __tunit_should_because = ((global::TUnit.Assertions.Should.Core.IShouldSource<{wrapper.AssertionTypeArgDisplay}>)this).ConsumeBecauseMessage();");
        sb.AppendLine("        if (__tunit_should_because is not null)");
        sb.AppendLine("        {");
        sb.AppendLine("            inner.Because(__tunit_should_because);");
        sb.AppendLine("        }");
        sb.AppendLine($"        return new global::TUnit.Assertions.Should.Core.ShouldAssertion<{wrapper.AssertionTypeArgDisplay}>(Context, inner);");
        sb.AppendLine("    }");
    }

    private static void EmitContainer(SourceProductionContext ctx, string containerName, MethodData[] methods, HashSet<string> emittedHints)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#pragma warning disable");
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
        sb.AppendLine("        var __tunit_should_because = source.ConsumeBecauseMessage();");
        sb.AppendLine("        if (__tunit_should_because is not null)");
        sb.AppendLine("        {");
        sb.AppendLine("            inner.Because(__tunit_should_because);");
        sb.AppendLine("        }");
        sb.AppendLine($"        return new global::TUnit.Assertions.Should.Core.ShouldAssertion<{m.AssertionTypeArgDisplay}>(innerContext, inner);");
        sb.AppendLine("    }");
    }

    private static string FormatGenericArgs(EquatableArray<string> args)
        => args.Length == 0 ? string.Empty : "<" + string.Join(", ", args) + ">";

    private static MethodData[] DeduplicateMethods(MethodData[] methods)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<MethodData>(methods.Length);
        foreach (var method in methods)
        {
            if (seen.Add(GetMethodKey(method)))
            {
                result.Add(method);
            }
        }

        return result.ToArray();
    }

    private static string GetMethodKey(MethodData method)
    {
        var sb = new StringBuilder(method.ContainerName)
            .Append('|')
            .Append(method.MethodName)
            .Append('|')
            .Append(method.SourceTypeArgDisplay)
            .Append('|')
            .Append(method.AssertionTypeArgDisplay)
            .Append('|')
            .Append(method.ReturnTypeFullName)
            .Append('|')
            .Append(method.MethodGenericParams.Length);

        foreach (var typeArg in method.ReturnTypeGenericArgs)
        {
            sb.Append('|').Append(typeArg);
        }

        foreach (var parameter in method.Parameters)
        {
            sb.Append('|')
                .Append(parameter.TypeName)
                .Append(':')
                .Append(parameter.Name)
                .Append(':')
                .Append(parameter.CallerArgumentExpressionTarget);
        }

        return sb.ToString();
    }

    private static ShouldEntryData[] DeduplicateEntries(ShouldEntryData[] entries)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<ShouldEntryData>(entries.Length);
        foreach (var entry in entries)
        {
            if (seen.Add(entry.SignatureKey))
            {
                result.Add(entry);
            }
        }

        return result.ToArray();
    }

    private static void CollectExistingShouldEntryKeys(IAssemblySymbol assembly, HashSet<string> keys)
    {
        // Extension methods can only be declared on top-level non-generic static classes, so the
        // one type that can contribute keys is found by name instead of walking the assembly.
        var type = assembly.GetTypeByMetadataName(ShouldExtensionsTypeFullName);
        if (type is null
            || type.DeclaredAccessibility != Accessibility.Public
            || !type.IsStatic)
        {
            return;
        }

        foreach (var method in type.GetMembers("Should").OfType<IMethodSymbol>())
        {
            if (!method.IsExtensionMethod || method.Parameters.Length == 0)
            {
                continue;
            }

            keys.Add(CreateShouldMethodSignatureKey(method));
        }
    }

    private static string CreateShouldMethodSignatureKey(IMethodSymbol method, string? methodNameOverride = null)
    {
        var typeParameterOrdinals = new Dictionary<ITypeParameterSymbol, int>(SymbolEqualityComparer.Default);
        for (var i = 0; i < method.TypeParameters.Length; i++)
        {
            typeParameterOrdinals[method.TypeParameters[i]] = i;
        }

        var sb = new StringBuilder(methodNameOverride ?? method.Name).Append('|');
        AppendTypeSignatureKey(sb, method.Parameters[0].Type, typeParameterOrdinals);
        sb.Append('|').Append(method.TypeParameters.Length);

        foreach (var parameter in method.Parameters.Skip(1))
        {
            sb.Append('|');
            AppendTypeSignatureKey(sb, parameter.Type, typeParameterOrdinals);
        }

        return sb.ToString();
    }

    private static void AppendTypeSignatureKey(
        StringBuilder sb,
        ITypeSymbol type,
        Dictionary<ITypeParameterSymbol, int> typeParameterOrdinals)
    {
        switch (type)
        {
            case ITypeParameterSymbol typeParameter:
                if (typeParameterOrdinals.TryGetValue(typeParameter, out var ordinal))
                {
                    sb.Append('!').Append(ordinal);
                }
                else
                {
                    sb.Append('!').Append(typeParameter.Ordinal);
                }
                return;

            case IArrayTypeSymbol arrayType:
                AppendTypeSignatureKey(sb, arrayType.ElementType, typeParameterOrdinals);
                sb.Append('[');
                if (arrayType.Rank > 1)
                {
                    sb.Append(',', arrayType.Rank - 1);
                }
                sb.Append(']');
                return;

            case IPointerTypeSymbol pointerType:
                AppendTypeSignatureKey(sb, pointerType.PointedAtType, typeParameterOrdinals);
                sb.Append('*');
                return;

            case INamedTypeSymbol namedType:
                var originalDefinition = namedType.OriginalDefinition;

                if (originalDefinition.ContainingType is not null)
                {
                    AppendTypeSignatureKey(sb, originalDefinition.ContainingType, typeParameterOrdinals);
                    sb.Append('+');
                }
                else if (!originalDefinition.ContainingNamespace.IsGlobalNamespace)
                {
                    sb.Append(originalDefinition.ContainingNamespace.ToDisplayString()).Append('.');
                }

                sb.Append(originalDefinition.MetadataName);
                if (namedType.TypeArguments.Length > 0)
                {
                    sb.Append('<');
                    for (var i = 0; i < namedType.TypeArguments.Length; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(',');
                        }

                        AppendTypeSignatureKey(sb, namedType.TypeArguments[i], typeParameterOrdinals);
                    }
                    sb.Append('>');
                }
                return;

            default:
                sb.Append(type.ToDisplayString(NoGlobalFormat));
                return;
        }
    }

    private static void CollectShouldEntries(
        INamedTypeSymbol assertType,
        INamedTypeSymbol assertionSource,
        INamedTypeSymbol assertionBase,
        INamedTypeSymbol assertionContext,
        ImmutableArray<ShouldEntryData>.Builder builder)
    {
        foreach (var member in assertType.GetMembers("That").OfType<IMethodSymbol>())
        {
            if (TryDescribeShouldEntry(member, assertionSource, out var entry))
            {
                builder.Add(entry);
            }
        }
    }

    private static bool TryDescribeShouldEntry(
        IMethodSymbol method,
        INamedTypeSymbol assertionSource,
        out ShouldEntryData data)
    {
        data = null!;

        if (method.DeclaredAccessibility != Accessibility.Public
            || !method.IsStatic
            || method.Parameters.Length == 0
            || method.ReturnType is not INamedTypeSymbol returnType)
        {
            return false;
        }

        if (!ImplementsAssertionSource(returnType, assertionSource, out var sourceTypeArg))
        {
            return false;
        }

        if (!ShouldGenerateEntryForReceiver(method.Parameters[0].Type))
        {
            return false;
        }

        var receiver = method.Parameters[0];
        if (TryGetCallerArgumentExpressionTarget(method.Parameters[^1]) is null)
        {
            return false;
        }

        var paramData = ImmutableArray.CreateBuilder<ParameterData>();
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
        }

        var genericParams = ImmutableArray.CreateBuilder<GenericParamData>();
        foreach (var tp in method.TypeParameters)
        {
            genericParams.Add(GenericParamData.From(tp, NoGlobalFormat));
        }

        var priority = TryGetOverloadResolutionPriority(method.GetAttributes());

        data = new ShouldEntryData(
            ReceiverTypeName: receiver.Type.ToDisplayString(NoGlobalFormat),
            SourceTypeArgDisplay: sourceTypeArg.ToDisplayString(NoGlobalFormat),
            MethodGenericParams: new EquatableArray<GenericParamData>(genericParams),
            Parameters: new EquatableArray<ParameterData>(paramData),
            SignatureKey: CreateShouldMethodSignatureKey(method, "Should"),
            Priority: priority,
            RequiresUnreferencedCodeMessage: TryGetRucMessage(method.GetAttributes())
                                          ?? TryGetRucMessage(returnType.GetAttributes()),
            SuppressedTrimWarnings: new EquatableArray<string>(CollectSuppressedTrimWarnings(method.GetAttributes())),
            ForwardedAttributes: new EquatableArray<string>(CollectForwardedAttributes(method.GetAttributes())));
        return true;
    }

    private static bool ShouldGenerateEntryForReceiver(ITypeSymbol receiverType)
    {
        if (receiverType is IArrayTypeSymbol)
        {
            return true;
        }

        if (receiverType is not INamedTypeSymbol named)
        {
            return false;
        }

        return IsOriginalDefinition(named, "System.Collections.Generic.IReadOnlyDictionary`2")
            || IsOriginalDefinition(named, "System.Collections.Generic.IDictionary`2")
            || IsOriginalDefinition(named, "System.Collections.Generic.ISet`1")
            || IsOriginalDefinition(named, "System.Collections.Generic.IReadOnlySet`1")
            || IsOriginalDefinition(named, "System.Collections.Generic.IList`1")
            || IsOriginalDefinition(named, "System.Collections.Generic.IReadOnlyList`1")
            || IsOriginalDefinition(named, "System.Collections.Generic.HashSet`1")
            || IsOriginalDefinition(named, "System.Collections.Generic.Dictionary`2")
            || IsOriginalDefinition(named, "System.Memory`1")
            || IsOriginalDefinition(named, "System.ReadOnlyMemory`1")
            || IsOriginalDefinition(named, "System.Collections.Generic.IAsyncEnumerable`1")
            || IsFuncReturningEnumerable(named)
            || IsFuncReturningTaskOfEnumerable(named);
    }

    private static bool IsOriginalDefinition(INamedTypeSymbol type, string fullMetadataName)
    {
        var original = type.OriginalDefinition;
        var ns = original.ContainingNamespace?.ToDisplayString();
        return string.Equals($"{ns}.{original.MetadataName}", fullMetadataName, StringComparison.Ordinal);
    }

    private static bool IsFuncReturningEnumerable(INamedTypeSymbol type)
    {
        return IsOriginalDefinition(type, "System.Func`1")
            && type.TypeArguments.Length == 1
            && type.TypeArguments[0] is INamedTypeSymbol returnType
            && IsOriginalDefinition(returnType, "System.Collections.Generic.IEnumerable`1");
    }

    private static bool IsFuncReturningTaskOfEnumerable(INamedTypeSymbol type)
    {
        return IsOriginalDefinition(type, "System.Func`1")
            && type.TypeArguments.Length == 1
            && type.TypeArguments[0] is INamedTypeSymbol taskType
            && IsOriginalDefinition(taskType, "System.Threading.Tasks.Task`1")
            && taskType.TypeArguments.Length == 1
            && taskType.TypeArguments[0] is INamedTypeSymbol enumerableType
            && IsOriginalDefinition(enumerableType, "System.Collections.Generic.IEnumerable`1");
    }

    private static bool ImplementsAssertionSource(
        INamedTypeSymbol type,
        INamedTypeSymbol assertionSource,
        out ITypeSymbol sourceTypeArg)
    {
        if (IsAssertionSourceInterface(type, assertionSource))
        {
            sourceTypeArg = type.TypeArguments[0];
            return true;
        }

        foreach (var iface in type.AllInterfaces)
        {
            if (IsAssertionSourceInterface(iface, assertionSource))
            {
                sourceTypeArg = iface.TypeArguments[0];
                return true;
            }
        }

        sourceTypeArg = null!;
        return false;
    }

    private static int TryGetOverloadResolutionPriority(ImmutableArray<AttributeData> attrs)
    {
        foreach (var attr in attrs)
        {
            if (attr.AttributeClass?.Name == "OverloadResolutionPriorityAttribute"
                && attr.ConstructorArguments.Length == 1
                && attr.ConstructorArguments[0].Value is int priority)
            {
                return priority;
            }
        }

        return 0;
    }

    private static void EmitShouldEntries(SourceProductionContext ctx, ShouldEntryData[] entries, HashSet<string> emittedHints)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using TUnit.Assertions;");
        sb.AppendLine("using TUnit.Assertions.Should.Core;");
        sb.AppendLine();
        sb.AppendLine("namespace TUnit.Assertions.Should;");
        sb.AppendLine();
        sb.AppendLine("public static partial class ShouldExtensions");
        sb.AppendLine("{");

        foreach (var entry in entries)
        {
            EmitShouldEntry(sb, entry);
        }

        sb.AppendLine("}");

        var hint = "ShouldExtensions.Generated.g.cs";
        var suffix = 0;
        while (!emittedHints.Add(hint))
        {
            hint = $"ShouldExtensions.Generated_{++suffix}.g.cs";
        }

        ctx.AddSource(hint, sb.ToString());
    }

    private static void EmitShouldEntry(StringBuilder sb, ShouldEntryData entry)
    {
        var genericList = entry.MethodGenericParams.Length > 0
            ? "<" + string.Join(", ", entry.MethodGenericParams.Select(p =>
                p.DynamicallyAccessedMembersAttribute is null
                    ? p.Name
                    : $"{p.DynamicallyAccessedMembersAttribute} {p.Name}")) + ">"
            : string.Empty;

        var constraints = string.Join(" ", entry.MethodGenericParams
            .Select(p => p.ConstraintClause)
            .Where(c => c is not null));

        sb.AppendLine();
        if (entry.Priority != 0)
        {
            sb.AppendLine($"    [global::System.Runtime.CompilerServices.OverloadResolutionPriority({entry.Priority})]");
        }
        if (!string.IsNullOrEmpty(entry.RequiresUnreferencedCodeMessage))
        {
            var escaped = entry.RequiresUnreferencedCodeMessage!.Replace("\"", "\\\"");
            sb.AppendLine($"    [global::System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(\"{escaped}\")]");
        }
        foreach (var code in entry.SuppressedTrimWarnings)
        {
            sb.AppendLine($"    [global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"{code}\", Justification = \"Forwarded from source method\")]");
        }
        foreach (var attr in entry.ForwardedAttributes)
        {
            sb.AppendLine($"    {attr}");
        }

        sb.Append($"    public static global::TUnit.Assertions.Should.Core.ShouldSource<{entry.SourceTypeArgDisplay}> Should{genericList}(this {entry.ReceiverTypeName} value");
        foreach (var p in entry.Parameters)
        {
            sb.Append($", {p.TypeName} {p.Name}");
            if (p.HasDefaultValue)
            {
                sb.Append(" = ").Append(p.DefaultValueLiteral);
            }
        }
        sb.Append(')');

        if (!string.IsNullOrEmpty(constraints))
        {
            sb.AppendLine();
            sb.Append("        ").Append(constraints);
        }

        sb.AppendLine();
        sb.AppendLine("    {");
        sb.Append("        var source = global::TUnit.Assertions.Assert.That").Append(genericList).Append("(value");
        foreach (var p in entry.Parameters)
        {
            sb.Append(", ").Append(p.Name);
        }
        sb.AppendLine(");");
        sb.AppendLine($"        var innerContext = ((global::TUnit.Assertions.Core.IAssertionSource<{entry.SourceTypeArgDisplay}>)source).Context;");
        sb.AppendLine("        innerContext.ExpressionBuilder.Clear();");

        var expressionParam = entry.Parameters.LastOrDefault(p => p.CallerArgumentExpressionTarget == "value");
        if (expressionParam is null)
        {
            sb.AppendLine("        innerContext.ExpressionBuilder.Append(\"?.Should()\");");
        }
        else
        {
            sb.AppendLine($"        innerContext.ExpressionBuilder.Append({expressionParam.Name} ?? \"?\").Append(\".Should()\");");
        }

        sb.AppendLine($"        return new global::TUnit.Assertions.Should.Core.ShouldSource<{entry.SourceTypeArgDisplay}>(innerContext);");
        sb.AppendLine("    }");
    }

    private sealed record GeneratorPayload(
        EquatableArray<MethodData> Methods,
        EquatableArray<WrapperData> Wrappers,
        EquatableArray<ShouldEntryData> Entries)
    {
        public static GeneratorPayload Empty { get; } = new(
            new EquatableArray<MethodData>(Array.Empty<MethodData>()),
            new EquatableArray<WrapperData>(Array.Empty<WrapperData>()),
            new EquatableArray<ShouldEntryData>(Array.Empty<ShouldEntryData>()));
    }

    /// <param name="IsEnabled">False when TUnit.Assertions isn't referenced; nothing is generated.</param>
    /// <param name="ReferenceMethods">Extension methods from referenced assemblies, already filtered against baked Should containers.</param>
    /// <param name="Entries">Final, deduplicated Should entry points.</param>
    private sealed record CompilationData(
        bool IsEnabled,
        EquatableArray<MethodData> ReferenceMethods,
        EquatableArray<ShouldEntryData> Entries)
    {
        public static CompilationData Disabled { get; } = new(
            false,
            new EquatableArray<MethodData>(Array.Empty<MethodData>()),
            new EquatableArray<ShouldEntryData>(Array.Empty<ShouldEntryData>()));
    }

    /// <summary>
    /// Extension methods and wrappers declared in the current compilation.
    /// </summary>
    private sealed record LocalDeclarations(
        EquatableArray<MethodData> Methods,
        EquatableArray<WrapperData> Wrappers)
    {
        public static LocalDeclarations Empty { get; } = new(
            new EquatableArray<MethodData>(Array.Empty<MethodData>()),
            new EquatableArray<WrapperData>(Array.Empty<WrapperData>()));
    }

    private sealed record ShouldEntryData(
        string ReceiverTypeName,
        string SourceTypeArgDisplay,
        EquatableArray<GenericParamData> MethodGenericParams,
        EquatableArray<ParameterData> Parameters,
        string SignatureKey,
        int Priority,
        string? RequiresUnreferencedCodeMessage,
        EquatableArray<string> SuppressedTrimWarnings,
        EquatableArray<string> ForwardedAttributes);

    private sealed record WrapperData(
        string ContainingNamespace,
        EquatableArray<string> ContainingTypeDeclarations,
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
            // 'unmanaged' also sets HasValueTypeConstraint; 'struct, unmanaged' is CS0449 (#6471)
            if (tp.HasValueTypeConstraint && !tp.HasUnmanagedTypeConstraint) constraints.Add("struct");
            if (tp.HasUnmanagedTypeConstraint) constraints.Add("unmanaged");
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

            var name = EscapeIdentifier(tp.Name);
            return new GenericParamData(
                name,
                constraints.Count > 0 ? $"where {name} : {string.Join(", ", constraints)}" : null,
                damAttr);
        }
    }
}
