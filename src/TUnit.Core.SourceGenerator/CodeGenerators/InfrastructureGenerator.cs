using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Models;
using TUnit.Core.SourceGenerator.Models.Extracted;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

/// <summary>
/// Consolidated infrastructure generator that handles:
/// 1. Disabling reflection scanner (sets SourceRegistrar.IsEnabled = true)
/// 2. Pre-loading assemblies that reference TUnit.Core
/// 3. Forcing library module initializers to complete synchronously
///
/// This combines DisableReflectionScannerGenerator and AssemblyLoaderGenerator
/// into a single generator for efficiency.
///
/// Assembly Loading Strategy:
/// Uses RuntimeHelpers.RunClassConstructor to force library module initializers
/// to complete before the test assembly's module initializer finishes. This ensures
/// hooks registered by library assemblies are available when HookDelegateBuilder
/// collects them. Static constructors can only run AFTER module initializers complete,
/// so calling RunClassConstructor blocks until initialization is done.
/// </summary>
[Generator]
public class InfrastructureGenerator : IIncrementalGenerator
{
    private static readonly string[] ExcludedPublicKeyTokens =
    [
        "b77a5c561934e089", // .NET Framework
        "b03f5f7f11d50a3a", // mscorlib
        "31bf3856ad364e35", // Microsoft
        "cc7b13ffcd2ddd51", // System.Private
        "7cec85d7bea7798e", // .NET Core
    ];

    // Shared partial-class shells in TUnit.Generated, one per registration concern. Per-file
    // generated code contributes static field initializers to these; the compiler merges each into
    // a single .cctor, triggered once from the module initializer below via RunClassConstructor.
    private static readonly string[] RegistrationShells =
    [
        "TUnit_TestRegistration",
        "TUnit_HookRegistration",
        "TUnit_PropertyRegistration",
        "TUnit_ConverterRegistration",
        "TUnit_StaticPropertyRegistration",
        "TUnit_DynamicTestRegistration",
    ];

    public const string ExtractAssemblyInfoStep = "ExtractAssemblyInfo";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enabledProvider = context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.EnableTUnitSourceGeneration", out var value);
                return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
            });

        // Extract assembly names as primitives in the transform step. AssemblyInfoModel equality
        // keeps the generated source cached across keystrokes. No custom Compilation comparer here:
        // when a comparer reports "equal", the input node keeps the OLD compilation in its table,
        // pinning it (trees, bound state) until references change. The reference walk is memoized
        // per driver instead, so syntax-only edits still skip it.
        var memo = new AssemblyInfoMemo();
        var assemblyInfoProvider = context.CompilationProvider
            .Select((compilation, _) => memo.GetAssemblyInfo(compilation))
            .WithTrackingName(ExtractAssemblyInfoStep)
            .Combine(enabledProvider);

        context.RegisterSourceOutput(assemblyInfoProvider, (sourceContext, data) =>
        {
            var (assemblyInfo, isEnabled) = data;
            if (!isEnabled)
            {
                return;
            }

            GenerateCode(sourceContext, assemblyInfo);
        });
    }

    /// <summary>
    /// Single-slot memo of the reference walk, created per <see cref="Initialize"/> and so owned by one
    /// generator driver. The walk costs several milliseconds per run on a real test project (about
    /// 6.5 ms for TUnit.TestProject's 211 references), and in the IDE it would otherwise run on every
    /// keystroke.
    /// <para>
    /// The key is every compilation input the walk reads except source:
    /// <see cref="Compilation.ExternalReferences"/> (compared with <see cref="ImmutableArray{T}"/>
    /// <c>==</c>, which compares the backing array by reference; syntax-only edits reuse it, while any
    /// reference change produces a new one), the <see cref="Compilation.Options"/> instance and the
    /// assembly name. Scripts are never memoized. The one source dependency, a source type shadowing a
    /// selected type, is re-checked on every hit.
    /// </para>
    /// <para>
    /// The slot only ever describes the latest compilation this driver saw: every miss replaces or
    /// clears it. It holds that compilation's reference list and options (which the live compilation
    /// holds anyway), never a <see cref="Compilation"/> or syntax tree, and it dies with the driver.
    /// </para>
    /// </summary>
    internal sealed class AssemblyInfoMemo
    {
        // Racy by design: concurrent runs may both miss, both extract and both store. Entries are
        // immutable and each equals a fresh extraction for its key, so the only cost is duplicate work.
        private volatile Entry? _last;

        public AssemblyInfoModel GetAssemblyInfo(Compilation compilation)
        {
            // Script submissions read inputs outside the key (#r directive references, the previous
            // submission), so they always extract fresh.
            if (compilation.ScriptCompilationInfo is not null || !compilation.DirectiveReferences.IsEmpty)
            {
                _last = null;
                return ExtractAssemblyInfo(compilation, out _);
            }

            var last = _last;
            if (last is not null
                && last.References == compilation.ExternalReferences
                && ReferenceEquals(last.Options, compilation.Options)
                && last.AssemblyName == compilation.AssemblyName
                && !IsAnyShadowedBySource(compilation, last.SelectedMetadataNames))
            {
                return last.Model;
            }

            var model = ExtractAssemblyInfo(compilation, out var selectedMetadataNames);
            _last = selectedMetadataNames is null
                ? null
                : new Entry(compilation.ExternalReferences, compilation.Options, compilation.AssemblyName, model, selectedMetadataNames);
            return model;
        }

        private sealed class Entry(
            ImmutableArray<MetadataReference> references,
            CompilationOptions options,
            string? assemblyName,
            AssemblyInfoModel model,
            string[] selectedMetadataNames)
        {
            public ImmutableArray<MetadataReference> References { get; } = references;

            public CompilationOptions Options { get; } = options;

            public string? AssemblyName { get; } = assemblyName;

            public AssemblyInfoModel Model { get; } = model;

            public string[] SelectedMetadataNames { get; } = selectedMetadataNames;
        }
    }

    private static bool IsAnyShadowedBySource(Compilation compilation, string[] selectedMetadataNames)
    {
        var sourceAssembly = compilation.Assembly;
        foreach (var metadataName in selectedMetadataNames)
        {
            if (sourceAssembly.GetTypeByMetadataName(metadataName) is not null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Extracts all needed data as primitives in the transform step.
    /// This enables proper incremental caching - the model contains only strings.
    /// <para>
    /// The result is memoized by <see cref="AssemblyInfoMemo"/> across syntax-only edits, so any
    /// dependency on source must be reported through <paramref name="selectedMetadataNames"/> (as the
    /// shadowing check does) so that reuse can re-check it.
    /// </para>
    /// </summary>
    /// <param name="compilation">The compilation to inspect.</param>
    /// <param name="selectedMetadataNames">
    /// Metadata names of the selected types, or <see langword="null"/> when a source type shadowed a
    /// candidate, in which case the result depends on source and must not be memoized.
    /// </param>
    private static AssemblyInfoModel ExtractAssemblyInfo(Compilation compilation, out string[]? selectedMetadataNames)
    {
        var assembliesToLoad = new List<string>();
        var metadataNames = new List<string>();
        var shadowedBySource = false;

        // Find TUnit.Core assembly - only assemblies referencing this can contain tests
        var tunitCoreAssembly = FindTUnitCoreAssembly(compilation);

        // Collect all assemblies: start with the current assembly, then traverse references
        var visitedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
        var assembliesToVisit = new Queue<IAssemblySymbol>();

        assembliesToVisit.Enqueue(compilation.Assembly);

        while (assembliesToVisit.Count > 0)
        {
            var assembly = assembliesToVisit.Dequeue();

            if (!visitedAssemblies.Add(assembly))
            {
                continue;
            }

            foreach (var referenced in assembly.Modules.SelectMany(m => m.ReferencedAssemblySymbols))
            {
                if (!visitedAssemblies.Contains(referenced))
                {
                    assembliesToVisit.Enqueue(referenced);
                }
            }
        }

        // Build set of assemblies that reference TUnit.Core (directly or transitively)
        var assembliesReferencingTUnit = tunitCoreAssembly != null
            ? FindAssembliesReferencingTUnitCore(visitedAssemblies, tunitCoreAssembly)
            : visitedAssemblies;

        // Extract a public type from each assembly to reference
        foreach (var assembly in assembliesReferencingTUnit)
        {
            if (ShouldLoadAssembly(assembly, compilation))
            {
                var publicType = GetFirstUniquePublicType(assembly, compilation, out var metadataName, ref shadowedBySource);
                if (publicType != null)
                {
                    assembliesToLoad.Add(publicType);
                    metadataNames.Add(metadataName!);
                }
            }
        }

        selectedMetadataNames = shadowedBySource ? null : [.. metadataNames];

        return new AssemblyInfoModel
        {
            AssemblyName = compilation.Assembly.Name,
            TypesToReference = new EquatableArray<string>([.. assembliesToLoad])
        };
    }

    private static IAssemblySymbol? FindTUnitCoreAssembly(Compilation compilation)
    {
        if (compilation.Assembly.Name == "TUnit.Core")
        {
            return compilation.Assembly;
        }

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assemblySymbol
                && assemblySymbol.Name == "TUnit.Core")
            {
                return assemblySymbol;
            }
        }

        return null;
    }

    private static HashSet<IAssemblySymbol> FindAssembliesReferencingTUnitCore(
        HashSet<IAssemblySymbol> allAssemblies,
        IAssemblySymbol tunitCoreAssembly)
    {
        // Build reverse dependency graph
        var referencedBy = new Dictionary<IAssemblySymbol, List<IAssemblySymbol>>(SymbolEqualityComparer.Default);

        foreach (var assembly in allAssemblies)
        {
            foreach (var referenced in assembly.Modules.SelectMany(m => m.ReferencedAssemblySymbols))
            {
                if (!referencedBy.TryGetValue(referenced, out var list))
                {
                    list = [];
                    referencedBy[referenced] = list;
                }
                list.Add(assembly);
            }
        }

        // BFS from TUnit.Core to find all assemblies that transitively reference it
        var result = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
        var queue = new Queue<IAssemblySymbol>();

        result.Add(tunitCoreAssembly);
        queue.Enqueue(tunitCoreAssembly);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (referencedBy.TryGetValue(current, out var dependents))
            {
                foreach (var dependent in dependents)
                {
                    if (result.Add(dependent))
                    {
                        queue.Enqueue(dependent);
                    }
                }
            }
        }

        return result;
    }

    private static bool ShouldLoadAssembly(IAssemblySymbol assembly, Compilation compilation)
    {
        // Skip system assemblies
        if (IsSystemAssembly(assembly))
        {
            return false;
        }

        // Skip TUnit framework assemblies - they don't contain user tests
        if (IsTUnitFrameworkAssembly(assembly))
        {
            return false;
        }

        // Only load assemblies that will be available at runtime
        if (!IsLoadableAtRuntime(assembly, compilation))
        {
            return false;
        }

        return true;
    }

    private static bool IsSystemAssembly(IAssemblySymbol assemblySymbol)
    {
        if (assemblySymbol.Identity.PublicKeyToken.IsDefaultOrEmpty)
        {
            return false;
        }

        var publicKeyToken = BitConverter.ToString(assemblySymbol.Identity.PublicKeyToken.ToArray())
            .Replace("-", "")
            .ToLowerInvariant();

        return ExcludedPublicKeyTokens.Contains(publicKeyToken);
    }

    private static bool IsTUnitFrameworkAssembly(IAssemblySymbol assembly)
    {
        var name = assembly.Name;
        return name == "TUnit" ||
               name == "TUnit.Core" ||
               name == "TUnit.Engine" ||
               name == "TUnit.Assertions" ||
               name == "TUnit.Assertions.FSharp" ||
               name == "TUnit.Playwright" ||
               name == "TUnit.AspNetCore" ||
               name.StartsWith("TUnit.Assertions.", StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines if an assembly will be loadable at runtime via Assembly.Load().
    /// Any assembly that has a corresponding reference in the compilation will be available
    /// at runtime because it will either be:
    /// - A compiled DLL in the output directory (for project references)
    /// - A NuGet package assembly in the probing paths (for package references)
    /// </summary>
    private static bool IsLoadableAtRuntime(IAssemblySymbol assembly, Compilation compilation)
    {
        // Find the MetadataReference that corresponds to this assembly symbol
        var correspondingReference = compilation.References.FirstOrDefault(r =>
            SymbolEqualityComparer.Default.Equals(compilation.GetAssemblyOrModuleSymbol(r), assembly));

        // If there's a corresponding reference, the assembly will be available at runtime.
        // This includes:
        // - PortableExecutableReference: compiled DLLs (NuGet packages, project outputs)
        // - CompilationReference: project-to-project references (will be compiled to DLLs)
        return correspondingReference != null;
    }

    /// <summary>
    /// Gets the first public type from an assembly that can be uniquely resolved by the compilation.
    /// This avoids CS0433 errors when multiple assemblies define types with the same fully-qualified name.
    /// </summary>
    private static string? GetFirstUniquePublicType(
        IAssemblySymbol assembly,
        Compilation compilation,
        out string? selectedMetadataName,
        ref bool shadowedBySource)
    {
        foreach (var type in GetPublicTypesRecursive(assembly.GlobalNamespace))
        {
            // Skip generic types to avoid typeof() formatting complexity
            if (type.IsGenericType)
            {
                continue;
            }

            var metadataName = GetFullMetadataName(type);
            var resolvedType = compilation.GetTypeByMetadataName(metadataName);

            // If Roslyn resolves to the same type, it's unambiguous - use it
            // GetTypeByMetadataName returns null when the type name is ambiguous
            if (SymbolEqualityComparer.Default.Equals(resolvedType, type))
            {
                selectedMetadataName = metadataName;
                return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            // null or different type = ambiguous, try next type
            shadowedBySource |= IsFromSource(resolvedType, compilation);
        }

        // Fallback: try generic types if no non-generic unique type was found
        foreach (var type in GetPublicTypesRecursive(assembly.GlobalNamespace))
        {
            if (!type.IsGenericType)
            {
                continue;
            }

            var metadataName = GetFullMetadataName(type);
            var resolvedType = compilation.GetTypeByMetadataName(metadataName);

            if (SymbolEqualityComparer.Default.Equals(resolvedType, type))
            {
                var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // Use open generic syntax for typeof()
                // Example: global::Foo<T> -> global::Foo<>
                // Example: global::Foo<T1, T2> -> global::Foo<,>
                var openGenericSuffix = type.Arity == 1
                    ? "<>"
                    : $"<{new string(',', type.Arity - 1)}>";

                var genericStart = typeName.LastIndexOf('<');
                if (genericStart > 0)
                {
                    typeName = typeName.Substring(0, genericStart) + openGenericSuffix;
                }

                selectedMetadataName = metadataName;
                return typeName;
            }

            shadowedBySource |= IsFromSource(resolvedType, compilation);
        }

        selectedMetadataName = null;
        return null; // No unique type found, skip this assembly
    }

    // GetTypeByMetadataName returns the compilation's own (source) type when one exists, hiding
    // any referenced type with the same metadata name.
    private static bool IsFromSource(INamedTypeSymbol? resolvedType, Compilation compilation) =>
        resolvedType is not null
        && SymbolEqualityComparer.Default.Equals(resolvedType.ContainingAssembly, compilation.Assembly);

    /// <summary>
    /// Gets the full metadata name for a type (e.g., "Namespace.OuterClass+NestedClass").
    /// This is the format expected by Compilation.GetTypeByMetadataName().
    /// </summary>
    private static string GetFullMetadataName(INamedTypeSymbol type)
    {
        if (type.ContainingType != null)
        {
            return $"{GetFullMetadataName(type.ContainingType)}+{type.MetadataName}";
        }

        if (type.ContainingNamespace.IsGlobalNamespace)
        {
            return type.MetadataName;
        }

        return $"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}";
    }

    private static IEnumerable<INamedTypeSymbol> GetPublicTypesRecursive(INamespaceSymbol namespaceSymbol)
    {
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            if (type.DeclaredAccessibility == Accessibility.Public)
            {
                yield return type;
            }
        }

        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var type in GetPublicTypesRecursive(childNamespace))
            {
                yield return type;
            }
        }
    }

    private static void GenerateCode(SourceProductionContext context, AssemblyInfoModel model)
    {
        var sourceBuilder = new CodeWriter();

        // Add using directive for LogDebug extension method
        sourceBuilder.AppendLine("using TUnit.Core.Logging;");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]");
        sourceBuilder.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCode(\"TUnit\", \"{typeof(InfrastructureGenerator).Assembly.GetName().Version}\")]");

        // Using 'file' keyword ensures no naming collisions without needing GUIDs
        using (sourceBuilder.BeginBlock("file static class TUnitInfrastructure"))
        {
            sourceBuilder.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
            using (sourceBuilder.BeginBlock("public static void Initialize()"))
            {
                // Set source registrar FIRST - this is critical and must run even if logging fails
                // Wrap in try-catch to handle cases where TUnit.Core isn't available
                sourceBuilder.AppendLine("try");
                sourceBuilder.AppendLine("{");
                sourceBuilder.Indent();
                sourceBuilder.AppendLine("global::TUnit.Core.SourceRegistrar.IsEnabled = true;");
                sourceBuilder.Unindent();
                sourceBuilder.AppendLine("}");
                sourceBuilder.AppendLine("catch { /* TUnit.Core not available - skip source registrar */ }");
                sourceBuilder.AppendLine();

                // Logging is optional - wrap separately so it doesn't prevent other work
                sourceBuilder.AppendLine("try");
                sourceBuilder.AppendLine("{");
                sourceBuilder.Indent();
                sourceBuilder.AppendLine($"global::TUnit.Core.GlobalContext.Current.GlobalLogger.LogDebug(\"[ModuleInitializer:{model.AssemblyName}] TUnit infrastructure initializing...\");");
                sourceBuilder.Unindent();
                sourceBuilder.AppendLine("}");
                sourceBuilder.AppendLine("catch { /* TUnit.Core not available - skip logging */ }");
                sourceBuilder.AppendLine();

                // Reference types from assemblies to trigger their module constructors
                if (model.TypesToReference.Length > 0)
                {
                    sourceBuilder.AppendLine("try");
                    sourceBuilder.AppendLine("{");
                    sourceBuilder.Indent();
                    sourceBuilder.AppendLine($"global::TUnit.Core.GlobalContext.Current.GlobalLogger.LogTrace(\"[ModuleInitializer:{model.AssemblyName}] Loading {model.TypesToReference.Length} assembly reference(s)...\");");
                    sourceBuilder.Unindent();
                    sourceBuilder.AppendLine("}");
                    sourceBuilder.AppendLine("catch { /* TUnit.Core not available - skip logging */ }");
                }

                for (var i = 0; i < model.TypesToReference.Length; i++)
                {
                    var typeName = model.TypesToReference[i];
                    sourceBuilder.AppendLine("try");
                    sourceBuilder.AppendLine("{");
                    sourceBuilder.Indent();
                    sourceBuilder.AppendLine("try");
                    sourceBuilder.AppendLine("{");
                    sourceBuilder.Indent();
                    sourceBuilder.AppendLine($"global::TUnit.Core.GlobalContext.Current.GlobalLogger.LogTrace(\"[ModuleInitializer:{model.AssemblyName}] Loading assembly containing: {typeName.Replace("\"", "\\\"")}\");");
                    sourceBuilder.Unindent();
                    sourceBuilder.AppendLine("}");
                    sourceBuilder.AppendLine("catch { /* TUnit.Core not available - skip logging */ }");
                    sourceBuilder.AppendLine($"var type_{i} = typeof({typeName});");
                    sourceBuilder.AppendLine("// Force module initializer to complete before proceeding");
                    sourceBuilder.AppendLine("// RunClassConstructor triggers static constructor, which can only run AFTER module initializer completes");
                    sourceBuilder.AppendLine($"global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type_{i}.TypeHandle);");
                    sourceBuilder.AppendLine("try");
                    sourceBuilder.AppendLine("{");
                    sourceBuilder.Indent();
                    sourceBuilder.AppendLine($"global::TUnit.Core.GlobalContext.Current.GlobalLogger.LogTrace(\"[ModuleInitializer:{model.AssemblyName}] Assembly initialized: {typeName.Replace("\"", "\\\"")}\");");
                    sourceBuilder.Unindent();
                    sourceBuilder.AppendLine("}");
                    sourceBuilder.AppendLine("catch { /* TUnit.Core not available - skip logging */ }");
                    sourceBuilder.Unindent();
                    sourceBuilder.AppendLine("}");
                    sourceBuilder.AppendLine("catch (global::System.Exception ex)");
                    sourceBuilder.AppendLine("{");
                    sourceBuilder.Indent();
                    sourceBuilder.AppendLine("try");
                    sourceBuilder.AppendLine("{");
                    sourceBuilder.Indent();
                    sourceBuilder.AppendLine($"global::TUnit.Core.GlobalContext.Current.GlobalLogger.LogTrace(\"[ModuleInitializer:{model.AssemblyName}] Failed to load {typeName.Replace("\"", "\\\"")}: \" + ex.Message);");
                    sourceBuilder.Unindent();
                    sourceBuilder.AppendLine("}");
                    sourceBuilder.AppendLine("catch { /* TUnit.Core not available - skip logging */ }");
                    sourceBuilder.Unindent();
                    sourceBuilder.AppendLine("}");
                }

                sourceBuilder.AppendLine();

                // Trigger consolidated registration .cctors.
                // Test sources, hooks, property injection sources, AOT converters, static property
                // initializers and dynamic test sources each contribute static field initializers to
                // a shared partial class per concern. RunClassConstructor forces each .cctor to
                // execute, performing all registrations in ONE JIT-compiled method per concern instead
                // of N per-file [ModuleInitializer] methods.
                // No try/catch needed — InfrastructureGenerator always emits these shell classes.
                foreach (var shell in RegistrationShells)
                {
                    sourceBuilder.AppendLine($"global::System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(global::TUnit.Generated.{shell}).TypeHandle);");
                }
                sourceBuilder.AppendLine();

                sourceBuilder.AppendLine("try");
                sourceBuilder.AppendLine("{");
                sourceBuilder.Indent();
                sourceBuilder.AppendLine($"global::TUnit.Core.GlobalContext.Current.GlobalLogger.LogDebug(\"[ModuleInitializer:{model.AssemblyName}] TUnit infrastructure initialized\");");
                sourceBuilder.Unindent();
                sourceBuilder.AppendLine("}");
                sourceBuilder.AppendLine("catch { /* TUnit.Core not available - skip logging */ }");
            }
        }

        // Empty partial class shell in TUnit.Generated namespace — per-class and per-method
        // test source files contribute static field initializers to this class.
        // The compiler merges all contributions into a single .cctor,
        // which is triggered by RunClassConstructor above.
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("namespace TUnit.Generated");
        sourceBuilder.AppendLine("{");
        sourceBuilder.Indent();
        foreach (var shell in RegistrationShells)
        {
            sourceBuilder.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]");
            sourceBuilder.AppendLine($"internal static partial class {shell} {{ }}");
        }
        sourceBuilder.Unindent();
        sourceBuilder.AppendLine("}");

        context.AddSource("TUnitInfrastructure.g.cs", sourceBuilder.ToString());
    }
}
