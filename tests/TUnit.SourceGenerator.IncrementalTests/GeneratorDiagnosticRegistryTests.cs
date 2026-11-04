using System.Text.RegularExpressions;

namespace TUnit.SourceGenerator.IncrementalTests;

public class GeneratorDiagnosticRegistryTests
{
    [Test]
    public async Task GeneratorIdsAreUniqueAndEveryRuleUsesTheRegistry()
    {
        var root = GetRepositoryRoot();
        var registryPath = Path.Combine(root, "src", "TUnit.Core.SourceGenerator", "GeneratorDiagnosticIds.cs");
        var registry = await File.ReadAllTextAsync(registryPath);
        var entries = Regex.Matches(registry, "const string (\\w+) = \"((?:TUNITGEN|TUNIT|TUnit|TU|THG)[0-9]+)\"")
            .Select(match => (Name: match.Groups[1].Value, Id: match.Groups[2].Value)).ToArray();

        await Assert.That(entries.Length).IsGreaterThan(0);
        await Assert.That(entries.Select(entry => entry.Id).Distinct().Count()).IsEqualTo(entries.Length);

        // Inspect source rather than only static descriptor fields: some rules construct descriptors
        // inside exception handlers. Exclude build output and linked Roslyn variants.
        var sources = Directory.GetDirectories(Path.Combine(root, "src"), "*.SourceGenerator")
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            .Where(path => path != registryPath && !path.Split(Path.DirectorySeparatorChar).Any(part => part is "obj" or "bin"))
            .ToArray();
        var usages = new List<string>();
        foreach (var path in sources)
        {
            var source = await File.ReadAllTextAsync(path);
            await Assert.That(Regex.IsMatch(source, "\"(?:TUNITGEN|TUNIT|TUnit|TU|THG)[0-9]+\"")).IsFalse();
            usages.AddRange(Regex.Matches(source, @"GeneratorDiagnosticIds\.(\w+)")
                .Select(match => match.Groups[1].Value));
        }

        await Assert.That(usages.Count).IsEqualTo(entries.Length);
        foreach (var entry in entries)
        {
            await Assert.That(usages.Count(name => name == entry.Name)).IsEqualTo(1);
        }
    }

    private static string GetRepositoryRoot()
    {
        // Runtime paths are not remapped by deterministic CI builds like CallerFilePath is.
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "TUnit.Core.SourceGenerator", "GeneratorDiagnosticIds.cs")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException($"Could not find the generator sources above {AppContext.BaseDirectory}.");
    }
}
