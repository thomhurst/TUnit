using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.Attributes;

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
    };

    public static Assembly[] AssemblyReferencesForCodegen =>
        AppDomain
            .CurrentDomain.GetAssemblies()
            .Concat(ImportantAssemblies)
            .Distinct()
            .Where(a => !a.IsDynamic)
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
        var references = new List<MetadataReference>();
        var assemblies = AssemblyReferencesForCodegen;
        foreach (Assembly assembly in assemblies)
        {
            if (!assembly.IsDynamic)
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        var compilation = CSharpCompilation.Create(
            "Library",
            source,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        return compilation;
    }

    public static async Task<string> SourceFromResourceFile(string file)
    {
        var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        Assert.NotNull(currentDir);
        var resourcesDir = Path.Combine(currentDir, "resources");

        return await File.ReadAllTextAsync(Path.Combine(resourcesDir, file));
    }
}
