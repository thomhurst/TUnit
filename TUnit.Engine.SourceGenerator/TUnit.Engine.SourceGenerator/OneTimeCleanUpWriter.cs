using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;

namespace TUnit.Engine.SourceGenerator;

public class OneTimeCleanUpWriter
{
    public static string GenerateCode(INamedTypeSymbol classType)
    {
        var oneTimeCleanUpMethods = classType
            .GetMembersIncludingBase()
            .OfType<IMethodSymbol>()
            .Where(x => x.IsStatic)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.GetAttributes()
                .Any(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                          == "global::TUnit.Core.OnlyOnceCleanUpAttribute")
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
            stringBuilder.AppendLine($$"""
                                           await RunSafelyAsync(() => {{classType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}}.{{oneTimeCleanUpMethod.Name}}(), teardownExceptions);
                                       """);
        }

        return stringBuilder.ToString();
    }
}