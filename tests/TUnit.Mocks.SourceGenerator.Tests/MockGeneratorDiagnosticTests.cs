using Microsoft.CodeAnalysis;
using TUnit.Mocks.SourceGenerator.Models;

namespace TUnit.Mocks.SourceGenerator.Tests;

public class MockGeneratorDiagnosticTests : SnapshotTestBase
{
    [Test]
    public async Task Unexpected_Generation_Failure_Reports_Diagnostic_And_Does_Not_Stop_Other_Mocks()
    {
        var source = """
            using TUnit.Mocks;

            [assembly: GenerateMock(typeof(IBroken))]

            public interface IBroken { void Break(); }
            public interface IHealthy { void Run(); }

            public class Usage
            {
                public void Create() => _ = Mock.Of<IHealthy>();
            }
            """;

        var generator = new MockGenerator(EmitWithInjectedFailure);
        var (sources, diagnostics) = RunGeneratorForDiagnostics(source, generator: generator);

        var diagnostic = diagnostics.Single(d => d.Id == "TM009");
        await Assert.That(diagnostic.Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostic.GetMessage()).Contains("global::IBroken");
        await Assert.That(diagnostic.GetMessage()).Contains("InvalidOperationException");
        await Assert.That(diagnostic.GetMessage()).Contains("Injected generation failure");
        await Assert.That(diagnostic.Location.Kind).IsEqualTo(LocationKind.ExternalFile);
        await Assert.That(diagnostic.Location.GetLineSpan().StartLinePosition.Line).IsEqualTo(2);
        await Assert.That(sources.Any(s => s.Contains("IHealthyMock", StringComparison.Ordinal))).IsTrue();
    }

    private static void EmitWithInjectedFailure(
        SourceProductionContext context,
        MockTypeModel model)
    {
        if (model.FullyQualifiedName == "global::IBroken")
        {
            throw new InvalidOperationException("Injected generation failure");
        }

        MockGenerator.EmitSources(context, model);
    }
}
