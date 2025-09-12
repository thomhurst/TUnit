using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Helpers;

namespace TUnit.Core.SourceGenerator.Generators;

[Generator]
public sealed class AssertionMethodGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var attributeData = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsClassWithCreateAssertionAttribute(node),
                transform: (ctx, _) => GetCreateAssertionAttributeData(ctx))
            .Where(x => x != null)
            .SelectMany((x, _) => x!);

        context.RegisterSourceOutput(attributeData, GenerateAssertionForAttribute);
    }

    private static bool IsClassWithCreateAssertionAttribute(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDecl &&
               classDecl.AttributeLists.Any() &&
               classDecl.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword));
    }

    private static IEnumerable<AttributeWithClassData>? GetCreateAssertionAttributeData(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (semanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        var attributeDataList = new List<AttributeWithClassData>();

        foreach (var attributeData in classSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass?.Name == "CreateAssertionAttribute")
            {
                if (attributeData.ConstructorArguments.Length >= 3)
                {
                    var targetType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
                    INamedTypeSymbol? containingType = null;
                    string? methodName = null;
                    int assertionType = 0;

                    if (attributeData.ConstructorArguments.Length == 3)
                    {
                        methodName = attributeData.ConstructorArguments[1].Value?.ToString();
                        assertionType = (int)(attributeData.ConstructorArguments[2].Value ?? 0);
                        containingType = targetType;
                    }
                    else if (attributeData.ConstructorArguments.Length >= 4)
                    {
                        containingType = attributeData.ConstructorArguments[1].Value as INamedTypeSymbol;
                        methodName = attributeData.ConstructorArguments[2].Value?.ToString();
                        assertionType = (int)(attributeData.ConstructorArguments[3].Value ?? 0);
                    }

                    if (targetType != null && containingType != null && !string.IsNullOrEmpty(methodName))
                    {
                        string? customMethodName = null;
                        if (attributeData.NamedArguments.Any(na => na.Key == "CustomMethodName"))
                        {
                            customMethodName = attributeData.NamedArguments
                                .FirstOrDefault(na => na.Key == "CustomMethodName")
                                .Value.Value?.ToString();
                        }

                        var createAssertionAttributeData = new CreateAssertionAttributeData(
                            targetType,
                            containingType,
                            methodName,
                            assertionType,
                            customMethodName
                        );

                        attributeDataList.Add(new AttributeWithClassData(classSymbol, createAssertionAttributeData));
                    }
                }
            }
        }

        return attributeDataList.Count > 0 ? attributeDataList : null;
    }

    private static void GenerateAssertionForAttribute(SourceProductionContext context, AttributeWithClassData attributeWithClassData)
    {
        var classSymbol = attributeWithClassData.ClassSymbol;
        var attributeData = attributeWithClassData.AttributeData;
        
        var matchingMethods = attributeData.ContainingType.GetMembers(attributeData.MethodName)
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && 
                       m.ReturnType.SpecialType == SpecialType.System_Boolean &&
                       m.Parameters.Length > 0 &&
                       SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, attributeData.TargetType))
            .OrderBy(m => m.Parameters.Length)
            .ToArray();

        if (!matchingMethods.Any())
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "TU0001",
                    "Method not found",
                    $"No static boolean method '{attributeData.MethodName}' found on type '{attributeData.ContainingType.ToDisplayString()}' with first parameter of type '{attributeData.TargetType.ToDisplayString()}'",
                    "TUnit.Assertions",
                    DiagnosticSeverity.Error,
                    true),
                Location.None));
            return;
        }
        for (int i = 0; i < matchingMethods.Length; i++)
        {
            var method = matchingMethods[i];
            var sourceBuilder = new StringBuilder();
            var namespaceName = classSymbol.ContainingNamespace?.ToDisplayString();

            sourceBuilder.AppendLine("#nullable disable");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Threading.Tasks;");
            sourceBuilder.AppendLine("using TUnit.Assertions.AssertConditions;");
            sourceBuilder.AppendLine("using TUnit.Assertions.AssertConditions.Interfaces;");
            sourceBuilder.AppendLine("using TUnit.Assertions.AssertionBuilders;");
            sourceBuilder.AppendLine();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                sourceBuilder.AppendLine($"namespace {namespaceName};");
                sourceBuilder.AppendLine();
            }

            GenerateAssertConditionClassForMethod(context, sourceBuilder, attributeData, method);

            sourceBuilder.AppendLine($"public static partial class {classSymbol.Name}");
            sourceBuilder.AppendLine("{");
            GenerateMethodsForSpecificOverload(context, sourceBuilder, attributeData, method);
            sourceBuilder.AppendLine("}");
            var className = GenerateAssertConditionClassNameForMethod(attributeData.TargetType, attributeData.ContainingType, attributeData.MethodName, method);
            var fileName = $"{classSymbol.Name}_{className}.g.cs";
            context.AddSource(fileName, sourceBuilder.ToString());
        }
    }

    private static void GenerateAssertConditionClassForMethod(SourceProductionContext context, StringBuilder sourceBuilder, CreateAssertionAttributeData attributeData, IMethodSymbol staticMethod)
    {
        var targetTypeName = attributeData.TargetType.ToDisplayString();
        var containingType = attributeData.ContainingType;
        var methodName = attributeData.MethodName;

        var className = GenerateAssertConditionClassNameForMethod(attributeData.TargetType, attributeData.ContainingType, methodName, staticMethod);
        var parameters = staticMethod.Parameters.Skip(1).ToArray();

        sourceBuilder.AppendLine($"public class {className} : BaseAssertCondition<{targetTypeName}>");
        sourceBuilder.AppendLine("{");

        foreach (var param in parameters)
        {
            sourceBuilder.AppendLine($"    private readonly {param.Type.ToDisplayString()} _{param.Name};");
        }
        sourceBuilder.AppendLine("    private readonly bool _negated;");
        sourceBuilder.AppendLine();
        sourceBuilder.Append($"    public {className}(");
        var constructorParams = new List<string>();
        foreach (var param in parameters)
        {
            constructorParams.Add($"{param.Type.ToDisplayString()} {param.Name}");
        }
        constructorParams.Add("bool negated = false");
        sourceBuilder.Append(string.Join(", ", constructorParams));
        sourceBuilder.AppendLine(")");
        sourceBuilder.AppendLine("    {");
        
        foreach (var param in parameters)
        {
            sourceBuilder.AppendLine($"        _{param.Name} = {param.Name};");
        }
        sourceBuilder.AppendLine("        _negated = negated;");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        sourceBuilder.AppendLine($"    protected override ValueTask<AssertionResult> GetResult({targetTypeName}? actualValue, Exception? exception, AssertionMetadata assertionMetadata)");
        sourceBuilder.AppendLine("    {");
        
        if (!attributeData.TargetType.IsValueType)
        {
            sourceBuilder.AppendLine("        if (actualValue is null)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            return AssertionResult.Fail(\"Actual value is null\");");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();
        }
        sourceBuilder.Append($"        var result = {containingType.ToDisplayString()}.{methodName}(actualValue");
        foreach (var param in parameters)
        {
            sourceBuilder.Append($", _{param.Name}");
        }
        sourceBuilder.AppendLine(");");
        
        sourceBuilder.AppendLine("        var condition = _negated ? result : !result;");
        
        sourceBuilder.Append("        return AssertionResult.FailIf(condition, ");
        sourceBuilder.Append($"$\"'{{actualValue}}' was expected {{(_negated ? \"not \" : \"\")}}to satisfy {methodName}");
        if (parameters.Any())
        {
            sourceBuilder.Append($"({string.Join(", ", parameters.Select(p => $"{{{p.Name}}}"))})");
        }
        sourceBuilder.AppendLine("\");");
        
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    protected override string GetExpectation()");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.Append($"        return $\"{{(_negated ? \"not \" : \"\")}}to satisfy {methodName}");
        if (parameters.Any())
        {
            sourceBuilder.Append($"({string.Join(", ", parameters.Select(p => $"{{{p.Name}}}"))})");
        }
        sourceBuilder.AppendLine("\";");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine("}");
        sourceBuilder.AppendLine();
    }



    private static string GenerateAssertConditionClassNameForMethod(INamedTypeSymbol targetType, INamedTypeSymbol containingType, string methodName, IMethodSymbol method)
    {
        var targetTypeName = GetSimpleTypeName(targetType);
        var containingTypeName = SymbolEqualityComparer.Default.Equals(targetType, containingType) 
            ? "" 
            : GetSimpleTypeName(containingType);
        
        var parameterSuffix = method.Parameters.Length > 1 ? $"With{method.Parameters.Length}Parameters" : "";
        
        return $"{targetTypeName}{containingTypeName}{methodName}{parameterSuffix}AssertCondition";
    }

    private static string GetSimpleTypeName(INamedTypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_Boolean => "Bool",
            SpecialType.System_Char => "Char",
            SpecialType.System_String => "String",
            SpecialType.System_Int32 => "Int",
            SpecialType.System_Double => "Double",
            SpecialType.System_Single => "Float",
            _ => type.Name
        };
    }

    private static void GenerateMethodsForSpecificOverload(SourceProductionContext context, StringBuilder sourceBuilder, CreateAssertionAttributeData attributeData, IMethodSymbol staticMethod)
    {
        var targetTypeName = attributeData.TargetType.ToDisplayString();
        var methodName = attributeData.MethodName;
        var assertionType = attributeData.AssertionType;

        var baseMethodName = attributeData.CustomMethodName ?? methodName;
        var className = GenerateAssertConditionClassNameForMethod(attributeData.TargetType, attributeData.ContainingType, methodName, staticMethod);

        string actualMethodName = baseMethodName;
        if (staticMethod.Parameters.Length > 1)
        {
            if (methodName == "IsDigit" && staticMethod.Parameters.Length == 2 && 
                staticMethod.Parameters[1].Type.SpecialType == SpecialType.System_Int32)
            {
                actualMethodName = "IsDigitAt";
            }
        }

        if ((assertionType & 1) != 0)
        {
            GenerateMethod(sourceBuilder, targetTypeName, actualMethodName, staticMethod, className, false);
        }

        if ((assertionType & 2) != 0)
        {
            var isNotMethodName = actualMethodName.StartsWith("Is") ? $"IsNot{actualMethodName.Substring(2)}" : $"IsNot{actualMethodName}";
            GenerateMethod(sourceBuilder, targetTypeName, isNotMethodName, staticMethod, className, true);
        }
    }

    private static void GenerateMethod(StringBuilder sourceBuilder, string targetTypeName, string generatedMethodName, IMethodSymbol staticMethod, string assertConditionClassName, bool negated)
    {
        var parameters = staticMethod.Parameters;
        var additionalParameters = parameters.Skip(1);
        
        sourceBuilder.Append($"    public static InvokableValueAssertionBuilder<{targetTypeName}> {generatedMethodName}(this IValueSource<{targetTypeName}> valueSource");
        
        foreach (var param in additionalParameters)
        {
            sourceBuilder.Append($", {param.Type.ToDisplayString()} {param.Name}");
        }
        
        sourceBuilder.AppendLine(")");
        sourceBuilder.AppendLine("    {");
        
        sourceBuilder.AppendLine($"        return valueSource.RegisterAssertion(");
        sourceBuilder.Append($"            new {assertConditionClassName}(");
        
        var constructorArgs = new List<string>();
        foreach (var param in additionalParameters)
        {
            constructorArgs.Add(param.Name);
        }
        constructorArgs.Add(negated.ToString().ToLowerInvariant());
        
        sourceBuilder.Append(string.Join(", ", constructorArgs));
        sourceBuilder.AppendLine("),");
        sourceBuilder.AppendLine("            []);");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
    }

    private record AttributeWithClassData(
        INamedTypeSymbol ClassSymbol,
        CreateAssertionAttributeData AttributeData
    );

    private record CreateAssertionAttributeData(
        INamedTypeSymbol TargetType,
        INamedTypeSymbol ContainingType,
        string MethodName,
        int AssertionType,
        string? CustomMethodName
    );
}