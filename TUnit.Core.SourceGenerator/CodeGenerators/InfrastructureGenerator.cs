using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Equality;
using TUnit.Core.SourceGenerator.Models;
using TUnit.Core.SourceGenerator.Models.Extracted;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

/// <summary>
/// Consolidated infrastructure generator that handles:
/// 1. Disabling reflection scanner (sets SourceRegistrar.IsEnabled = true)
/// 2. Pre-loading assemblies that reference TUnit.Core
///
/// This combines DisableReflectionScannerGenerator and AssemblyLoaderGenerator
/// into a single generator for efficiency.
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

        // Extract assembly names as primitives
        foreach (var assembly in assembliesReferencingTUnit)
        {
            if (ShouldLoadAssembly(assembly, compilation))
            {
                assembliesToLoad.Add(GetAssemblyFullName(assembly));
            }
        }

        return new AssemblyInfoModel
        {
            AssembliesToLoad = new EquatableArray<string>([.. assembliesToLoad])
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

        // Only load assemblies with physical locations
        if (!HasPhysicalLocation(assembly, compilation))
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

    private static bool HasPhysicalLocation(IAssemblySymbol assembly, Compilation compilation)
    {
        var correspondingReference = compilation.References.FirstOrDefault(r =>
            SymbolEqualityComparer.Default.Equals(compilation.GetAssemblyOrModuleSymbol(r), assembly));

        return correspondingReference is PortableExecutableReference { FilePath: not null and not "" };
    }

    private static string GetAssemblyFullName(IAssemblySymbol assemblySymbol)
    {
        var identity = assemblySymbol.Identity;
        var culture = string.IsNullOrEmpty(identity.CultureName) ? "neutral" : identity.CultureName;
        var publicKeyToken = identity.PublicKeyToken.Length > 0
            ? BitConverter.ToString(identity.PublicKeyToken.ToArray()).Replace("-", "").ToLowerInvariant()
            : "null";

        return $"{identity.Name}, Version={identity.Version}, Culture={culture}, PublicKeyToken={publicKeyToken}";
    }

    private static void GenerateCode(SourceProductionContext context, AssemblyInfoModel model)
    {
        var sourceBuilder = new CodeWriter();

        sourceBuilder.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]");
        sourceBuilder.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCode(\"TUnit\", \"{typeof(InfrastructureGenerator).Assembly.GetName().Version}\")]");

        // Using 'file' keyword ensures no naming collisions without needing GUIDs
        using (sourceBuilder.BeginBlock("file static class TUnitInfrastructure"))
        {
            sourceBuilder.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
            using (sourceBuilder.BeginBlock("public static void Initialize()"))
            {
                // Disable reflection scanner - source generation is active
                sourceBuilder.AppendLine("global::TUnit.Core.SourceRegistrar.IsEnabled = true;");

                // Pre-load assemblies that may contain tests
                foreach (var assemblyName in model.AssembliesToLoad)
                {
                    sourceBuilder.AppendLine($"global::TUnit.Core.SourceRegistrar.RegisterAssembly(() => global::System.Reflection.Assembly.Load(\"{assemblyName}\"));");
                }
            }
        }

        context.AddSource("TUnitInfrastructure.g.cs", sourceBuilder.ToString());
    }
}
