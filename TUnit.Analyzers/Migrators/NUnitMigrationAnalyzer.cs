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
        // Check for NUnit assertion types by name (fallback when semantic model doesn't resolve)
        return typeName == "Assert" ||
               typeName == "ClassicAssert" ||
               typeName == "CollectionAssert" ||
               typeName == "StringAssert" ||
               typeName == "FileAssert" ||
               typeName == "DirectoryAssert";
    }
}