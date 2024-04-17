using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;

namespace TUnit.Engine.SourceGenerator;

public class AfterEachTestRetriever
{
    public static string GenerateCode(INamedTypeSymbol classType)
    {
        var cleanUp = classType
            .GetMembersIncludingBase()
            .OfType<IMethodSymbol>()
            .Where(x => !x.IsStatic)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.GetAttributes()
                .Any(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                          == "global::TUnit.Core.AfterEachTestAttribute")
            )
            .Reverse()
            .ToList();
        
        if(!cleanUp.Any())
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        
        foreach (var oneTimeSetUpMethod in cleanUp)
        {
            stringBuilder.Append($"classInstance => global::TUnit.Core.Helpers.RunHelpers.RunAsync(() => classInstance.{oneTimeSetUpMethod.Name}()),");
        }
        
        return stringBuilder.ToString();
    }
}