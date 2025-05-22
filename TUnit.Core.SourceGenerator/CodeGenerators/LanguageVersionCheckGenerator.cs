using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Core.SourceGenerator;

[Generator]
public class LanguageVersionCheckGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var settings = context.CompilationProvider
            .Select((c, _)  => 
            {
                LanguageVersion? csharpVersion = c is CSharpCompilation comp
                    ? comp.LanguageVersion
                    : null;
                
                return csharpVersion;
            });

        context.RegisterSourceOutput(settings, static (sourceProductionContext, languageVersion) =>
        {
            if (languageVersion is null)
            {
                return;
            }
            
            if((int)languageVersion.Value < 1200)
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