using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Equality;
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

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enabledProvider = context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.EnableTUnitSourceGeneration", out var value);
                return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
            });

        // Extract assembly names as primitives in the transform step
        // This enables proper incremental caching
        var assemblyInfoProvider = context.CompilationProvider
            .WithComparer(new PreventCompilationTriggerOnEveryKeystrokeComparer())
            .Select((compilation, _) => ExtractAssemblyInfo(compilation))
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
    /// Extracts all needed data as primitives in the transform step.
    /// This enables proper incremental caching - the model contains only strings.
    /// </summary>
    private static AssemblyInfoModel ExtractAssemblyInfo(Compilation compilation)
    {
        var assembliesToLoad = new List<string>();

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
                var publicType = GetFirstUniquePublicType(assembly, compilation);
                if (publicType != null)
                {
                    assembliesToLoad.Add(publicType);
                }
            }
        }

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
    private static string? GetFirstUniquePublicType(IAssemblySymbol assembly, Compilation compilation)
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
                return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            // null or different type = ambiguous, try next type
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

                return typeName;
            }
        }

        return null; // No unique type found, skip this assembly
    }

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
                    sourceBuilder.AppendLine($"global::TUnit.Core.GlobalContext.Current.GlobalLogger.LogDebug(\"[ModuleInitializer:{model.AssemblyName}] Loading {model.TypesToReference.Length} assembly reference(s)...\");");
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
                    sourceBuilder.AppendLine($"global::TUnit.Core.GlobalContext.Current.GlobalLogger.LogDebug(\"[ModuleInitializer:{model.AssemblyName}] Loading assembly containing: {typeName.Replace("\"", "\\\"")}\");");
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
                    sourceBuilder.AppendLine($"global::TUnit.Core.GlobalContext.Current.GlobalLogger.LogDebug(\"[ModuleInitializer:{model.AssemblyName}] Assembly initialized: {typeName.Replace("\"", "\\\"")}\");");
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
                    sourceBuilder.AppendLine($"global::TUnit.Core.GlobalContext.Current.GlobalLogger.LogDebug(\"[ModuleInitializer:{model.AssemblyName}] Failed to load {typeName.Replace("\"", "\\\"")}: \" + ex.Message);");
                    sourceBuilder.Unindent();
                    sourceBuilder.AppendLine("}");
                    sourceBuilder.AppendLine("catch { /* TUnit.Core not available - skip logging */ }");
                    sourceBuilder.Unindent();
                    sourceBuilder.AppendLine("}");
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

        context.AddSource("TUnitInfrastructure.g.cs", sourceBuilder.ToString());
    }
}
