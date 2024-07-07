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
        
        var fullyQualifiedType = classType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);

        foreach (var oneTimeSetUpMethod in cleanUp)
        {
            var args = oneTimeSetUpMethod.HasTimeoutAttribute() ? "cancellationToken" : string.Empty;
            
            stringBuilder.Append($$"""
                                   
                                   new InstanceMethod<{{fullyQualifiedType}}>
                                   {
                                       MethodInfo = typeof({{fullyQualifiedType}}).GetMethod("{{oneTimeSetUpMethod.Name}}", 0, [{{string.Join(", ", oneTimeSetUpMethod.Parameters.Select(x => $"typeof({x.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)})"))}}]),
                                       Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.{{oneTimeSetUpMethod.Name}}({{args}}), cancellationToken),
                                   },
                                   """);
        }
        
        return stringBuilder.ToString();
    }
}