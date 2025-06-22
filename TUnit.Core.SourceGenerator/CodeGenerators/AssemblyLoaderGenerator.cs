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
        var provider = context.CompilationProvider
            .WithComparer(new PreventCompilationTriggerOnEveryKeystrokeComparer());

        context.RegisterSourceOutput(provider, (sourceProductionContext, source) => GenerateCode(sourceProductionContext, source));
    }

    private void GenerateCode(SourceProductionContext context, Compilation compilation)
    {
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

        var sourceBuilder = new CodeWriter();
        sourceBuilder.AppendLine($"[System.CodeDom.Compiler.GeneratedCode(\"TUnit\", \"{typeof(AssemblyLoaderGenerator).Assembly.GetName().Version}\")]");
        sourceBuilder.AppendLine("file static class AssemblyLoader" + Guid.NewGuid().ToString("N"));
        using (sourceBuilder.Block())
        {
            sourceBuilder.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
            sourceBuilder.AppendLine("public static void Initialize()");
            using (sourceBuilder.Block())
            {
                foreach (var assembly in visitedAssemblies)
                {
                    WriteAssemblyLoad(sourceBuilder, assembly);
                }
            }
        }
        context.AddSource("AssemblyLoader.g.cs", sourceBuilder.ToString());
    }

    private static void WriteAssemblyLoad(ICodeWriter sourceBuilder, IAssemblySymbol assembly)
    {
        if (IsSystemAssembly(assembly))
        {
            return;
        }

        sourceBuilder.AppendLine($"global::TUnit.Core.SourceRegistrar.RegisterAssembly(() => global::System.Reflection.Assembly.Load(\"{GetAssemblyFullName(assembly)}\"));");
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

