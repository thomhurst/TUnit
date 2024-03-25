using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;

namespace TUnit.Engine.SourceGenerator;

public class SetUpWriter
{
    public static string GenerateCode(INamedTypeSymbol classType)
    {
        var oneTimeSetUpMethods = classType
            .GetMembersIncludingBase()
            .OfType<IMethodSymbol>()
            .Where(x => !x.IsStatic)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.GetAttributes()
                .Any(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                          == "global::TUnit.Core.SetUpAttribute")
            )
            .ToList();
        
        if(!oneTimeSetUpMethods.Any())
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        
        foreach (var oneTimeSetUpMethod in oneTimeSetUpMethods)
        {
            stringBuilder.AppendLine($"               await RunAsync(classInstance.{oneTimeSetUpMethod.Name});");
        }
        
        return stringBuilder.ToString();
    }
}