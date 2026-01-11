using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Equality;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

[Generator]
public class AssemblyLoaderGenerator : IIncrementalGenerator
{
    private static readonly string[] _excludedAssemblies =
    [
        "b77a5c561934e089",
        "b03f5f7f11d50a3a",
        "31bf3856ad364e35",
        "cc7b13ffcd2ddd51",
        "7cec85d7bea7798e",

    ];
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enabledProvider = context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.EnableTUnitSourceGeneration", out var value);
                return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
            });

        var provider = context.CompilationProvider
            .WithComparer(new PreventCompilationTriggerOnEveryKeystrokeComparer())
            .Combine(enabledProvider);

        context.RegisterSourceOutput(provider, (sourceProductionContext, data) =>
        {
            var (compilation, isEnabled) = data;
            if (!isEnabled)
            {
                return;
            }

            GenerateCode(sourceProductionContext, compilation);
        });
    }

    private void GenerateCode(SourceProductionContext context, Compilation compilation)
    {
        // Find TUnit.Core assembly - only assemblies referencing this can contain tests
        var tunitCoreAssembly = FindTUnitCoreAssembly(compilation);

        // Collect all assemblies: start with the current assembly, then traverse references
        var visitedAssemblies = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        var assembliesToVisit = new Queue<IAssemblySymbol>();

        var currentAssembly = compilation.Assembly;

        assembliesToVisit.Enqueue(currentAssembly);

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
            : visitedAssemblies; // Fallback: register all if TUnit.Core not found

        var sourceBuilder = new CodeWriter();

        sourceBuilder.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]");
        sourceBuilder.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCode(\"TUnit\", \"{typeof(AssemblyLoaderGenerator).Assembly.GetName().Version}\")]");
        using (sourceBuilder.BeginBlock("file static class AssemblyLoader" + Guid.NewGuid().ToString("N")))
        {
            sourceBuilder.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
            using (sourceBuilder.BeginBlock("public static void Initialize()"))
            {
                foreach (var assembly in assembliesReferencingTUnit)
                {
                    WriteAssemblyLoad(sourceBuilder, assembly, compilation);
                }
            }
        }
        context.AddSource("AssemblyLoader.g.cs", sourceBuilder.ToString());
    }

    private static IAssemblySymbol? FindTUnitCoreAssembly(Compilation compilation)
    {
        // Check current assembly first
        if (compilation.Assembly.Name == "TUnit.Core")
        {
            return compilation.Assembly;
        }

        // Search referenced assemblies
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
        // Build reverse dependency graph: for each assembly, which assemblies reference it?
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

        // Start with TUnit.Core itself
        result.Add(tunitCoreAssembly);
        queue.Enqueue(tunitCoreAssembly);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            // Find all assemblies that reference the current assembly
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

    private static void WriteAssemblyLoad(ICodeWriter sourceBuilder, IAssemblySymbol assembly, Compilation compilation)
    {
        if (IsSystemAssembly(assembly))
        {
            return;
        }

        // Skip TUnit framework assemblies - they don't contain user tests
        if (IsTUnitFrameworkAssembly(assembly))
        {
            return;
        }

        // Only emit assembly load code if the assembly has a physical location that exists
        if (!HasPhysicalLocation(assembly, compilation))
        {
            return;
        }

        sourceBuilder.AppendLine($"global::TUnit.Core.SourceRegistrar.RegisterAssembly(() => global::System.Reflection.Assembly.Load(\"{GetAssemblyFullName(assembly)}\"));");
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
        // Find the corresponding MetadataReference for this assembly
        var correspondingReference = compilation.References.FirstOrDefault(r => 
            SymbolEqualityComparer.Default.Equals(compilation.GetAssemblyOrModuleSymbol(r), assembly));

        // If there's no corresponding reference, the assembly doesn't have a physical location
        if (correspondingReference is not PortableExecutableReference peRef)
        {
            return false;
        }

        // Check if the file path exists (don't check file system - that's not allowed in source generators)
        return !string.IsNullOrWhiteSpace(peRef.FilePath);
    }

    private static bool IsSystemAssembly(IAssemblySymbol assemblySymbol)
    {
        if (assemblySymbol.Identity.PublicKeyToken.IsDefaultOrEmpty)
        {
            return false;
        }

        var stringPublicTokenKey = BitConverter.ToString(assemblySymbol.Identity.PublicKeyToken.ToArray())
            .Replace("-", "")
            .ToLowerInvariant();

        return _excludedAssemblies.Contains(stringPublicTokenKey);
    }

    private static string GetAssemblyFullName(IAssemblySymbol assemblySymbol)
    {
        var identity = assemblySymbol.Identity;
        return $"{identity.Name}, Version={identity.Version}, Culture={(string.IsNullOrEmpty(identity.CultureName) ? "neutral" : identity.CultureName)}, PublicKeyToken={(identity.PublicKeyToken.Length > 0 ? BitConverter.ToString(identity.PublicKeyToken.ToArray()).Replace("-", "").ToLowerInvariant() : "null")}";
    }
}

