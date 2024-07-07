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

        var fullyQualifiedType = classType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        
        foreach (var beforeEachTestMethod in beforeEachTestMethods)
        {
            var args = beforeEachTestMethod.HasTimeoutAttribute() ? "cancellationToken" : string.Empty;

            stringBuilder.Append($$"""
                                 
                                 new InstanceMethod<{{fullyQualifiedType}}>
                                 {
                                     MethodInfo = typeof({{fullyQualifiedType}}).GetMethod("{{beforeEachTestMethod.Name}}", 0, [{{string.Join(", ", beforeEachTestMethod.Parameters.Select(x => $"typeof({x.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)})"))}}]),
                                     Body = (classInstance, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.{{beforeEachTestMethod.Name}}({{args}}), cancellationToken),
                                 },
                                 """);
        }
        
        return stringBuilder.ToString();
    }
}