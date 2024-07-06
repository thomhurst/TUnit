using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;

namespace TUnit.Engine.SourceGenerator;

public class BeforeEachTestRetriever
{
    public static string GenerateCode(INamedTypeSymbol classType)
    {
        var beforeEachTestMethods = classType
            .GetMembersIncludingBase()
            .OfType<IMethodSymbol>()
            .Where(x => !x.IsStatic)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.GetAttributes()
                .Any(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                          == "global::TUnit.Core.BeforeEachTestAttribute")
            )
            .ToList();
        
        if(!beforeEachTestMethods.Any())
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        
        foreach (var beforeEachTestMethod in beforeEachTestMethods)
        {
            stringBuilder.Append($"classInstance => RunHelpers.RunAsync(() => classInstance.{beforeEachTestMethod.Name}()),");
        }
        
        return stringBuilder.ToString();
    }
}