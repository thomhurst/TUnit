using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._6162;

// Repro for https://github.com/thomhurst/TUnit/issues/6162
// An instance MethodDataSource declared on an abstract base class, combined with
// [InheritsTests] on a derived class whose instances are produced by a
// DependencyInjectionDataSourceAttribute (no parameterless constructor),
// previously fell back to Activator.CreateInstance and failed with
// "No parameterless constructor defined".

public interface IExportService
{
    string Export(string path);
}

public sealed class ExportService : IExportService
{
    public string Export(string path) => $"exported:{path}";
}

public sealed class SimpleDependencyInjectionAttribute : DependencyInjectionDataSourceAttribute<SimpleDependencyInjectionAttribute.Scope>
{
    public sealed class Scope;

    public override Scope CreateScope(DataGeneratorMetadata dataGeneratorMetadata) => new();

    public override object? Create(Scope scope, Type type)
    {
        if (type == typeof(IExportService))
        {
            return new ExportService();
        }

        return null;
    }
}

public abstract class BaseExportTests(IExportService exportService)
{
    public IEnumerable<string> DocumentPaths => ["doc1", "doc2"];

    [Test]
    [MethodDataSource(nameof(DocumentPaths))]
    public async Task Export_ReturnsResult(string path)
    {
        var result = exportService.Export(path);

        await Assert.That(result).IsEqualTo($"exported:{path}");
    }
}

[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
[SimpleDependencyInjection]
public sealed class InheritedExportTests(IExportService exportService) : BaseExportTests(exportService);
