using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;

namespace TUnit.Engine.SourceGenerator;

public class OneTimeCleanUpWriter
{
    public static string GenerateLazyOneTimeCleanUpCode(INamedTypeSymbol classType)
    {
        var oneTimeCleanUpMethods = classType
            .GetMembersIncludingBase()
            .OfType<IMethodSymbol>()
            .Where(x => x.IsStatic)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.GetAttributes()
                .Any(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                          == "global::TUnit.Core.OneTimeCleanUpAttribute")
            )
            .Reverse()
            .ToList();
        
        if(!oneTimeCleanUpMethods.Any())
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        
        foreach (var oneTimeCleanUpMethod in oneTimeCleanUpMethods)
        {
            var typeContainingCleanUpMethod = oneTimeCleanUpMethod.ContainingType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
            stringBuilder.AppendLine($"global::TUnit.Engine.OneTimeHookOrchestrator.RegisterOneTimeTearDown(typeof({typeContainingCleanUpMethod}), () => global::TUnit.Engine.RunHelpers.RunAsync(() => {typeContainingCleanUpMethod}.{oneTimeCleanUpMethod.Name}()));");
        }
        
        return stringBuilder.ToString();
    }
}