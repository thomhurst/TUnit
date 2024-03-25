using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;

namespace TUnit.Engine.SourceGenerator;

public class OneTimeSetUpWriter
{
    public static string GenerateCode(INamedTypeSymbol classType)
    {
        var oneTimeSetUpMethods = classType
            .GetMembersIncludingBase()
            .OfType<IMethodSymbol>()
            .Where(x => x.IsStatic)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.GetAttributes()
                .Any(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                          == "global::TUnit.Core.OnlyOnceSetUpAttribute")
            )
            .ToList();
        
        if(!oneTimeSetUpMethods.Any())
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine($$"""
                                   await global::TUnit.Engine.OneTimeSetUpOrchestrator.Tasks.GetOrAdd(typeof({{classType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}}), async _ =>
                                   {
                                   """);
        
        foreach (var oneTimeSetUpMethod in oneTimeSetUpMethods)
        {
            stringBuilder.AppendLine($"                   await RunAsync(() => {classType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}.{oneTimeSetUpMethod.Name}());");
        }

        stringBuilder.AppendLine("});");

        return stringBuilder.ToString();
    }
}