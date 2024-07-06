using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;

namespace TUnit.Engine.SourceGenerator;

public class AfterAllTestsInClassWriter
{
    public static string GenerateLazyAfterAllTestsInClassCode(INamedTypeSymbol classType)
    {
        var oneTimeCleanUpMethods = classType
            .GetMembersIncludingBase()
            .OfType<IMethodSymbol>()
            .Where(x => x.IsStatic)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.GetAttributes()
                .Any(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                          == "global::TUnit.Core.AfterAllTestsInClassAttribute")
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
            stringBuilder.AppendLine($"global::TUnit.Engine.AfterAllTestsInClassOrchestrator.RegisterOneTimeTearDown(typeof({typeContainingCleanUpMethod}), () => RunHelpers.RunAsync(() => {typeContainingCleanUpMethod}.{oneTimeCleanUpMethod.Name}()));");
        }
        
        return stringBuilder.ToString();
    }
}