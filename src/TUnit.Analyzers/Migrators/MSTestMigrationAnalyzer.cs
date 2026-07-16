using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Migrators.Base;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MSTestMigrationAnalyzer : BaseMigrationAnalyzer
{
    protected override string TargetFrameworkNamespace => "Microsoft.VisualStudio.TestTools.UnitTesting";
    
    protected override DiagnosticDescriptor DiagnosticRule => Rules.MSTestMigration;
    
    protected override bool IsFrameworkUsing(string usingName)
    {
        return usingName == "Microsoft.VisualStudio.TestTools.UnitTesting" || 
               usingName.StartsWith("Microsoft.VisualStudio.TestTools.UnitTesting.");
    }
    
    protected override bool IsFrameworkNamespace(string? namespaceName)
    {
        if (namespaceName == null)
        {
            return false;
        }

        return namespaceName == "Microsoft.VisualStudio.TestTools.UnitTesting" ||
               namespaceName.StartsWith("Microsoft.VisualStudio.TestTools.UnitTesting.");
    }

    protected override bool IsFrameworkTypeName(string typeName)
    {
        // Check for MSTest assertion types by name (fallback when semantic model doesn't resolve)
        // Note: "Assert" is intentionally NOT included here because TUnit also uses Assert.
        // For plain "Assert" calls, we rely on namespace checks in the semantic analysis.
        // These other types are MSTest-specific and safe to detect by name alone.
        return typeName == "CollectionAssert" ||
               typeName == "StringAssert";
    }

    protected override bool IsFrameworkAvailable(Compilation compilation)
    {
        // Check if MSTest types are available in the compilation
        // This prevents false positives after migration when MSTest assembly has been removed
        return compilation.GetTypeByMetadataName("Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute") != null;
    }
}