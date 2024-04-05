using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator;

public class OneTimeSetUpWriter
{
    public static string GenerateCode(INamedTypeSymbol classType)
    {
        var oneTimeSetUpMethods = classType
            //.GetMembersIncludingBase()
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(x => x.IsStatic)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.GetAttributes()
                .Any(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                          == "global::TUnit.Core.OneTimeSetUpAttribute")
            )
            .ToList();
        
        if(!oneTimeSetUpMethods.Any())
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        
        foreach (var oneTimeSetUpMethod in oneTimeSetUpMethods)
        {
            stringBuilder.Append($"() => global::TUnit.Engine.RunHelpers.RunAsync(() => {classType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}.{oneTimeSetUpMethod.Name}()),");
        }
        
        return stringBuilder.ToString();
    }
}