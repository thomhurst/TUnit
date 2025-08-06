using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public class AttributeWriter
{
    public static void WriteAttributes(ICodeWriter sourceCodeWriter, Compilation compilation,
        ImmutableArray<AttributeData> attributeDatas)
    {
        for (var index = 0; index < attributeDatas.Length; index++)
        {
            var attributeData = attributeDatas[index];

            if (attributeData.ApplicationSyntaxReference is null)
            {
                continue;
            }

            WriteAttribute(sourceCodeWriter, compilation, attributeData);

            if (index != attributeDatas.Length - 1)
            {
                sourceCodeWriter.AppendLine(",");
            }
        }
    }

    public static void WriteAttribute(ICodeWriter sourceCodeWriter, Compilation compilation,
        AttributeData attributeData)
    {
        sourceCodeWriter.Append(GetAttributeObjectInitializer(compilation, attributeData));
    }

    public static void WriteAttributeMetadata(ICodeWriter sourceCodeWriter, Compilation compilation,
        AttributeData attributeData, string targetElement, string? targetMemberName, string? targetTypeName, bool includeClassMetadata = false)
    {
        sourceCodeWriter.Append("new global::TUnit.Core.AttributeMetadata");
        sourceCodeWriter.Append(" { ");
        sourceCodeWriter.Append($"Instance = {GetAttributeObjectInitializer(compilation, attributeData)}, ");
        sourceCodeWriter.Append($"TargetElement = global::TUnit.Core.TestAttributeTarget.{targetElement}, ");

        if (targetMemberName != null)
        {
            sourceCodeWriter.Append($"TargetMemberName = \"{targetMemberName}\", ");
        }

        if (targetTypeName != null)
        {
            sourceCodeWriter.Append($"TargetType = typeof({targetTypeName}), ");
        }

        // Add ClassMetadata if requested and not a system attribute
        if (includeClassMetadata && attributeData.AttributeClass?.ContainingNamespace?.ToDisplayString()?.StartsWith("System") != true)
        {
            sourceCodeWriter.Append("ClassMetadata = ");
            SourceInformationWriter.GenerateClassInformation(sourceCodeWriter, compilation, attributeData.AttributeClass!);
            sourceCodeWriter.Append(", ");
        }

        if (attributeData.ConstructorArguments.Length > 0)
        {
            sourceCodeWriter.Append("ConstructorArguments = new object?[] { ");

            foreach (var typedConstant in attributeData.ConstructorArguments)
            {
                sourceCodeWriter.Append($"{TypedConstantParser.GetRawTypedConstantValue(typedConstant)}, ");
            }

            sourceCodeWriter.Append("}, ");
        }

        if (attributeData.NamedArguments.Length > 0)
        {
            sourceCodeWriter.Append("NamedArguments = new global::System.Collections.Generic.Dictionary<string, object?>() { ");
            foreach (var namedArg in attributeData.NamedArguments)
            {
                sourceCodeWriter.Append($"[\"{namedArg.Key}\"] = {TypedConstantParser.GetRawTypedConstantValue(namedArg.Value)}, ");
            }
            sourceCodeWriter.Append("}, ");
        }

        sourceCodeWriter.Append("}");
    }

    public static string GetAttributeObjectInitializer(Compilation compilation,
        AttributeData attributeData)
    {
        var sourceCodeWriter = new CodeWriter("", includeHeader: false);

        var syntax = attributeData.ApplicationSyntaxReference?.GetSyntax();

        if (syntax is null)
        {
            WriteAttributeWithoutSyntax(sourceCodeWriter, attributeData);
            return sourceCodeWriter.ToString();
        }

        var arguments = syntax.ChildNodes()
            .OfType<AttributeArgumentListSyntax>()
            .FirstOrDefault()
            ?.Arguments ?? [];

        var properties = arguments.Where(x => x.NameEquals != null);

        var constructorArgs = arguments.Where(x => x.NameEquals == null);

        var attributeName = attributeData.AttributeClass!.GloballyQualified();

        var formattedConstructorArgs = string.Join(", ", constructorArgs.Select(x => FormatConstructorArgument(compilation, x)));

        var formattedProperties = properties.Select(x => FormatProperty(compilation, x)).ToArray();

        sourceCodeWriter.Append($"new {attributeName}({formattedConstructorArgs})");

        if (formattedProperties.Length == 0
            && !HasNestedDataGeneratorProperties(attributeData))
        {
            return sourceCodeWriter.ToString();
        }

        sourceCodeWriter.AppendLine();
        sourceCodeWriter.Append("{");
        foreach (var property in formattedProperties)
        {
            sourceCodeWriter.Append($"{property},");
        }

        WriteDataSourceGeneratorProperties(sourceCodeWriter, compilation, attributeData);

        sourceCodeWriter.Append("}");

        return sourceCodeWriter.ToString();
    }

    private static bool HasNestedDataGeneratorProperties(AttributeData attributeData)
    {
        if (attributeData.AttributeClass is not { } attributeClass)
        {
            return false;
        }

        if (attributeClass.GetMembersIncludingBase().OfType<IPropertySymbol>().Any(x => x.GetAttributes().Any(a => a.IsDataSourceAttribute())))
        {
            return true;
        }

        return false;
    }

    private static void WriteDataSourceGeneratorProperties(ICodeWriter sourceCodeWriter, Compilation compilation, AttributeData attributeData)
    {
        foreach (var propertySymbol in attributeData.AttributeClass?.GetMembers().OfType<IPropertySymbol>() ?? [])
        {
            if (propertySymbol.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (propertySymbol.GetAttributes().FirstOrDefault(x => x.IsDataSourceAttribute()) is not { } dataSourceAttribute)
            {
                continue;
            }

            sourceCodeWriter.Append($"{propertySymbol.Name} = ");

            var propertyType = propertySymbol.Type.GloballyQualified();
            var isNullable = propertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated;
            
            if (propertySymbol.Type.IsReferenceType && !isNullable)
            {
                sourceCodeWriter.Append("null!,");
            }
            else if (propertySymbol.Type.IsValueType && !isNullable)
            {
                sourceCodeWriter.Append($"default({propertyType}),");
            }
            else
            {
                sourceCodeWriter.Append("null,");
            }
        }
    }

    private static string FormatConstructorArgument(Compilation compilation, AttributeArgumentSyntax attributeArgumentSyntax)
    {
        // Special handling for numeric literals that might need decimal precision
        // Check if this is a numeric literal expression
        var expression = attributeArgumentSyntax.Expression;
        var expressionText = expression.ToString();
        
        // For numeric literals with many decimal places (likely intended as decimal),
        // preserve the full precision by adding 'm' suffix
        // This regex matches decimal numbers with more than 15 digits after the decimal point
        // (beyond double's precision)
        if (System.Text.RegularExpressions.Regex.IsMatch(expressionText, @"^\d+\.\d{15,}$"))
        {
            // This is a high-precision decimal literal - add 'm' suffix
            return expressionText + "m";
        }
        
        // Check if this looks like a numeric literal (not an identifier or complex expression)
        var isNumericLiteral = System.Text.RegularExpressions.Regex.IsMatch(expressionText, @"^-?\d+(\.\d+)?([eE][+-]?\d+)?[dDfFmMlLuU]?$");
        
        if (isNumericLiteral)
        {
            // Check if the target parameter expects a decimal type
            var semanticModel = compilation.GetSemanticModel(attributeArgumentSyntax.SyntaxTree);
            if (attributeArgumentSyntax.Parent?.Parent is AttributeSyntax attributeSyntax)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(attributeSyntax);
                if (symbolInfo.Symbol is IMethodSymbol constructorSymbol)
                {
                    // Find which parameter this argument corresponds to
                    var argumentList = attributeArgumentSyntax.Parent as AttributeArgumentListSyntax;
                    var argumentIndex = argumentList?.Arguments.IndexOf(attributeArgumentSyntax) ?? -1;
                    
                    if (argumentIndex >= 0 && argumentIndex < constructorSymbol.Parameters.Length)
                    {
                        var parameterType = constructorSymbol.Parameters[argumentIndex].Type;
                        if (parameterType.SpecialType == SpecialType.System_Decimal)
                        {
                            // This is a numeric literal for a decimal parameter
                            // Preserve the full precision by using the original text with 'm' suffix
                            var withoutSuffix = System.Text.RegularExpressions.Regex.Replace(expressionText, @"[dDfFmMlLuU]+$", "");
                            return withoutSuffix + "m";
                        }
                    }
                }
            }
        }
        
        // Default behavior for non-decimal or non-numeric cases
        var defaultSemanticModel = compilation.GetSemanticModel(attributeArgumentSyntax.SyntaxTree);
        if (attributeArgumentSyntax.NameColon is not null)
        {
            return $"{attributeArgumentSyntax.NameColon!.Name}: {expression.Accept(new FullyQualifiedWithGlobalPrefixRewriter(defaultSemanticModel))!.ToFullString()}";
        }

        return attributeArgumentSyntax.Accept(new FullyQualifiedWithGlobalPrefixRewriter(defaultSemanticModel))!.ToFullString();
    }

    private static string FormatProperty(Compilation compilation, AttributeArgumentSyntax attributeArgumentSyntax)
    {
        return $"{attributeArgumentSyntax.NameEquals!.Name} = {attributeArgumentSyntax.Expression.Accept(new FullyQualifiedWithGlobalPrefixRewriter(compilation.GetSemanticModel(attributeArgumentSyntax.SyntaxTree)))!.ToFullString()}";
    }

    public static void WriteAttributeWithoutSyntax(ICodeWriter sourceCodeWriter, AttributeData attributeData)
    {
        var attributeName = attributeData.AttributeClass!.GloballyQualified();

        var constructorArgs = attributeData.ConstructorArguments.Select(TypedConstantParser.GetRawTypedConstantValue);
        var formattedConstructorArgs = string.Join(", ", constructorArgs);

        var namedArgs = attributeData.NamedArguments.Select(arg => $"{arg.Key} = {TypedConstantParser.GetRawTypedConstantValue(arg.Value)}");
        var formattedNamedArgs = string.Join(", ", namedArgs);

        sourceCodeWriter.Append($"new {attributeName}({formattedConstructorArgs})");

        if (string.IsNullOrEmpty(formattedNamedArgs))
        {
            return;
        }

        sourceCodeWriter.AppendLine();
        sourceCodeWriter.Append("{");
        sourceCodeWriter.Append($"{formattedNamedArgs}");
        sourceCodeWriter.Append("}");
    }
}
