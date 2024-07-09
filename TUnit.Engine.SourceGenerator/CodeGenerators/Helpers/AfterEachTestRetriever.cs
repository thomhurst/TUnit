using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

public class AfterEachTestRetriever
{
    public static string GenerateCode(INamedTypeSymbol classType)
    {
        var cleanUp = classType
            .GetMembersIncludingBase(reverse: false)
            .OfType<IMethodSymbol>()
            .Where(x => !x.IsStatic)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .Where(x => x.GetAttributes()
                .Any(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                          == "global::TUnit.Core.AfterEachTestAttribute")
            )
            .ToList();
        
        if(!cleanUp.Any())
        {
            return string.Empty;
        }

        var stringBuilder = new StringBuilder();
        
        var fullyQualifiedType = classType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);

        foreach (var oneTimeSetUpMethod in cleanUp)
        {
            stringBuilder.Append($$"""
                                   
                                   				    new InstanceMethod<{{fullyQualifiedType}}>
                                   				    {
                                       				    MethodInfo = typeof({{fullyQualifiedType}}).GetMethod("{{oneTimeSetUpMethod.Name}}", 0, [{{string.Join(", ", oneTimeSetUpMethod.Parameters.Select(x => $"typeof({x.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)})"))}}]),
                                       				    Body = (classInstance, testContext, cancellationToken) => RunHelpers.RunWithTimeoutAsync(() => classInstance.{{oneTimeSetUpMethod.Name}}({{GetArgs(oneTimeSetUpMethod)}}), cancellationToken),
                                   				    },
                                   """);
        }
        
        return stringBuilder.ToString();
    }
    
    private static string GetArgs(IMethodSymbol beforeEachTestMethod)
    {
        var args = new List<string>();
        
        if (beforeEachTestMethod.Parameters.Any(x =>
                x.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ==
                WellKnownFullyQualifiedClassNames.TestContext.WithGlobalPrefix))
        {
            args.Add("testContext");
        }

        if (beforeEachTestMethod.HasTimeoutAttribute())
        {
            args.Add("cancellationToken");
        }

        return string.Join(", ", args);
    }
}