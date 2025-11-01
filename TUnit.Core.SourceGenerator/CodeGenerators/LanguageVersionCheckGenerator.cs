using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Core.SourceGenerator;

[Generator]
public class LanguageVersionCheckGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enabledProvider = context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.EnableTUnitSourceGeneration", out var value);
                return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
            });

        var settings = context.CompilationProvider
            .Select((c, _) =>
            {
                LanguageVersion? csharpVersion = c is CSharpCompilation comp
                    ? comp.LanguageVersion
                    : null;

                return csharpVersion;
            })
            .Combine(enabledProvider);

        context.RegisterSourceOutput(settings, static (sourceProductionContext, data) =>
        {
            var (languageVersion, isEnabled) = data;

            if (!isEnabled)
            {
                return;
            }

            if (languageVersion is null)
            {
                return;
            }

            if ((int) languageVersion.Value < 1200)
            {
                sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TUNIT_LANG_001",
                        "Language Version Check",
                        "TUnit requires C# 12 or higher when using Source Generation.",
                        "Usage",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
            }
        });
    }
}
