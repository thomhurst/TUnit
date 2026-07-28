using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Mocks.SourceGenerator.Tests;

public class TempProtectedProbe : SnapshotTestBase
{
    [Test]
    public async Task Probe()
    {
        var lib = """
            namespace ExternalLib
            {
                public interface IProt { protected string Hidden { get; set; } string Ok(); }
                public interface IInt { internal string Hidden { get; set; } string Ok(); }
            }
            """;

        var consumer = """
            public class ProtImpl : ExternalLib.IProt
            {
                string ExternalLib.IProt.Hidden { get; set; }
                public string Ok() => "x";
            }
            """;

        var parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create("Probe",
            [CSharpSyntaxTree.ParseText(consumer, parse)],
            GetCachedReferences().Concat([CreateExternalAssemblyReference(lib)]),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Console.WriteLine("PROT_IMPL_ERRORS: " + (errors.Count == 0 ? "<none>" : string.Join(" | ", errors.Select(e => e.ToString()))));

        var iprot = compilation.GetTypeByMetadataName("ExternalLib.IProt")!;
        var iint = compilation.GetTypeByMetadataName("ExternalLib.IInt")!;
        var implType = compilation.GetTypeByMetadataName("ProtImpl")!;
        foreach (var (name, t) in new[] { ("IProt", iprot), ("IInt", iint) })
        {
            foreach (var m in t.GetMembers().Where(m => m.IsAbstract && m is not IMethodSymbol { AssociatedSymbol: not null }))
            {
                Console.WriteLine($"{name}.{m.Name} acc={m.DeclaredAccessibility} " +
                    $"vsAssembly={compilation.IsSymbolAccessibleWithin(m, compilation.Assembly)} " +
                    $"vsImplType={compilation.IsSymbolAccessibleWithin(m, implType)}");
            }
        }

        await Task.CompletedTask;
    }
}
