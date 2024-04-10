using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;

namespace TUnit.Engine.SourceGenerator;

public class OneTimeSetUpWriter
{
    public static string GenerateCode(INamedTypeSymbol classType)
    {
        var oneTimeSetUpMethodGroupings = classType
            .GetMembersIncludingBase()
            .OfType<IMethodSymbol>()
            .Where(x => x.IsStatic)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.GetAttributes()
                .Any(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                          == "global::TUnit.Core.OneTimeSetUpAttribute")
            )
            .GroupBy(x => x.ContainingType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))
            .ToList();
        
        if(!oneTimeSetUpMethodGroupings.Any())
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        
        foreach (var oneTimeSetUpMethods in oneTimeSetUpMethodGroupings)
        {
            var methodFuncs = string.Join(", ",
                oneTimeSetUpMethods.Select(x =>
                    $"() => global::TUnit.Core.RunHelpers.RunAsync(() => {classType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}.{x.Name}())"));
            
            stringBuilder.Append($"new global::TUnit.Core.OneTimeSetUpModel(typeof({oneTimeSetUpMethods.Key}), [{methodFuncs}]),");
        }
        
        return stringBuilder.ToString();
    }
}