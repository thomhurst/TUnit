using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Migrators.Base;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NUnitMigrationAnalyzer : BaseMigrationAnalyzer
{
    protected override string TargetFrameworkNamespace => "NUnit";
    
    protected override DiagnosticDescriptor DiagnosticRule => Rules.NUnitMigration;
    
    protected override bool IsFrameworkUsing(string usingName)
    {
        return usingName == "NUnit" || 
               usingName == "NUnit.Framework" || 
               usingName.StartsWith("NUnit.Framework.");
    }
    
    protected override bool IsFrameworkNamespace(string? namespaceName)
    {
        if (namespaceName == null)
        {
            return false;
        }

        return namespaceName == "NUnit.Framework" ||
               namespaceName.StartsWith("NUnit.Framework.");
    }

    protected override bool IsFrameworkTypeName(string typeName)
    {
        // Check for NUnit-specific assertion types by name (fallback when semantic model doesn't resolve)
        // Note: "Assert" is intentionally NOT included here because TUnit also uses Assert.
        // For plain "Assert" calls, we rely on namespace checks in the semantic analysis.
        // These other types are NUnit-specific and safe to detect by name alone.
        return typeName == "ClassicAssert" ||
               typeName == "CollectionAssert" ||
               typeName == "StringAssert" ||
               typeName == "FileAssert" ||
               typeName == "DirectoryAssert";
    }

    protected override bool IsFrameworkAvailable(Compilation compilation)
    {
        // Check if NUnit.Framework types are available in the compilation
        // This prevents false positives after migration when NUnit assembly has been removed
        return compilation.GetTypeByMetadataName("NUnit.Framework.TestAttribute") != null ||
               compilation.GetTypeByMetadataName("NUnit.Framework.Legacy.ClassicAssert") != null ||
               compilation.GetTypeByMetadataName("NUnit.Framework.Legacy.StringAssert") != null;
    }
}