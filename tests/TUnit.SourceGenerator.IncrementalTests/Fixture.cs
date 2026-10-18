using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.Attributes;
using TUnit.Core;
using TUnit.Tests.Shared;

namespace TUnit.Assertions.SourceGenerator.IncrementalTests;

public static class Fixture
{
    public static readonly Assembly[] ImportantAssemblies = new[]
    {
        typeof(object).Assembly,
        typeof(Console).Assembly,
        typeof(GenerateAssertionAttribute).Assembly,
        typeof(MulticastDelegate).Assembly,
        typeof(IServiceProvider).Assembly,
        typeof(TestAttribute).Assembly,
    };

    private static readonly string[] AssemblyPathsForCodegen = TestHostReferences.AssemblyPaths
        .Concat(ImportantAssemblies.Select(static assembly => assembly.Location))
        .Distinct()
        .ToArray();

    public static DirectoryInfo GetSolutionDirectoryInfo()
    {
        var slnDir = SolutionDir();
        var directory = new DirectoryInfo(slnDir);
        // Assert.True(directory.Exists);
        return directory;
    }

    private static string SolutionDir([CallerFilePath] string thisFilePath = "") =>
        Path.GetFullPath(Path.Join(thisFilePath, "../../../"));

    public static CSharpCompilation CreateLibrary(params string[] source) =>
        CreateLibrary(source.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray());

    public static CSharpCompilation CreateLibrary(params SyntaxTree[] source)
    {
        var references = AssemblyPathsForCodegen
            .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();

        var compilation = CSharpCompilation.Create(
            "Library",
            source,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        return compilation;
    }
}
