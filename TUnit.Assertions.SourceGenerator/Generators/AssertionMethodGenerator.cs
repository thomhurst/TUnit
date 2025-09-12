using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Assertions.SourceGenerator.Generators;

[Generator]
public sealed class AssertionMethodGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classAttributeData = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Assertions.Attributes.CreateAssertionAttribute",
                predicate: (node, _) => node is ClassDeclarationSyntax classDecl && 
                                       classDecl.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)),
                transform: (ctx, _) => GetCreateAssertionAttributeData(ctx))
            .Where(x => x != null);

        context.RegisterSourceOutput(classAttributeData.Collect(), GenerateAssertionsForClass);
    }

    private static IEnumerable<AttributeWithClassData>? GetCreateAssertionAttributeData(GeneratorAttributeSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.TargetNode;
        var classSymbol = context.TargetSymbol as INamedTypeSymbol;

        if (classSymbol == null)
        {
            return null;
        }

        var attributeDataList = new List<AttributeWithClassData>();

        foreach (var attributeData in context.Attributes)
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

                    bool requiresGenericTypeParameter = false;
                    if (attributeData.NamedArguments.Any(na => na.Key == "RequiresGenericTypeParameter"))
                    {
                        requiresGenericTypeParameter = (bool)(attributeData.NamedArguments
                            .FirstOrDefault(na => na.Key == "RequiresGenericTypeParameter")
                            .Value.Value ?? false);
                    }

                    var createAssertionAttributeData = new CreateAssertionAttributeData(
                        targetType,
                        containingType,
                        methodName,
                        assertionType,
                        customMethodName,
                        requiresGenericTypeParameter
                    );

                    attributeDataList.Add(new AttributeWithClassData(classSymbol, createAssertionAttributeData));
                }
            }
        }

        return attributeDataList.Count > 0 ? attributeDataList : null;
    }

    private static void GenerateAssertionsForClass(SourceProductionContext context, ImmutableArray<IEnumerable<AttributeWithClassData>?> classAttributeData)
    {
        var allData = classAttributeData.SelectMany(x => x ?? []).ToArray();
        if (!allData.Any())
            return;
            
        // Group by class and generate one file per class
        foreach (var classGroup in allData.GroupBy(x => x.ClassSymbol, SymbolEqualityComparer.Default))
        {
            GenerateAssertionsForSpecificClass(context, classGroup.Key as INamedTypeSymbol, classGroup.ToArray());
        }
    }
    
    private static void GenerateAssertionsForSpecificClass(SourceProductionContext context, INamedTypeSymbol? classSymbol, AttributeWithClassData[] dataList)
    {
        if (classSymbol == null || !dataList.Any())
            return;
        var sourceBuilder = new StringBuilder();
        var namespaceName = classSymbol.ContainingNamespace?.ToDisplayString();
        
        sourceBuilder.AppendLine("#nullable enable");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Threading.Tasks;");
        sourceBuilder.AppendLine("using TUnit.Assertions.AssertConditions;");
        sourceBuilder.AppendLine("using TUnit.Assertions.AssertConditions.Interfaces;");
        sourceBuilder.AppendLine("using TUnit.Assertions.AssertionBuilders;");
        sourceBuilder.AppendLine("using TUnit.Assertions.Extensions;");
        sourceBuilder.AppendLine();

        if (!string.IsNullOrEmpty(namespaceName))
        {
            sourceBuilder.AppendLine($"namespace {namespaceName};");
            sourceBuilder.AppendLine();
        }

        var allGeneratedClasses = new List<string>();

        // Generate all assert condition classes first
        foreach (var attributeWithClassData in dataList)
        {
            var attributeData = attributeWithClassData.AttributeData;
            
            var matchingMethods = attributeData.ContainingType.GetMembers(attributeData.MethodName)
                .OfType<IMethodSymbol>()
                .Where(m => m.ReturnType.SpecialType == SpecialType.System_Boolean &&
                           (m.IsStatic ? 
                               // Static method: check first parameter matches target type or is generic Type
                               (m.Parameters.Length > 0 &&
                                (attributeData.RequiresGenericTypeParameter ? 
                                    m.Parameters[0].Type.Name == "Type" && m.Parameters[0].Type.ContainingNamespace.Name == "System" :
                                    SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, attributeData.TargetType))) :
                               // Instance method: method must be on the target type
                               SymbolEqualityComparer.Default.Equals(m.ContainingType, attributeData.TargetType)))
                .OrderBy(m => m.Parameters.Length)
                .ToArray();

            if (!matchingMethods.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TU0001",
                        "Method not found",
                        $"No boolean method '{attributeData.MethodName}' found on type '{attributeData.ContainingType.ToDisplayString()}'",
                        "TUnit.Assertions",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
                continue;
            }

            foreach (var method in matchingMethods)
            {
                var className = GenerateAssertConditionClassNameForMethod(attributeData.TargetType, attributeData.ContainingType, attributeData.MethodName, method);
                if (allGeneratedClasses.Contains(className))
                    continue;
                    
                allGeneratedClasses.Add(className);
                GenerateAssertConditionClassForMethod(context, sourceBuilder, attributeData, method);
            }
        }

        // Generate extension methods class
        sourceBuilder.AppendLine($"public static partial class {classSymbol.Name}");
        sourceBuilder.AppendLine("{");
        
        foreach (var attributeWithClassData in dataList)
        {
            var attributeData = attributeWithClassData.AttributeData;
            
            var matchingMethods = attributeData.ContainingType.GetMembers(attributeData.MethodName)
                .OfType<IMethodSymbol>()
                .Where(m => m.ReturnType.SpecialType == SpecialType.System_Boolean &&
                           (m.IsStatic ? 
                               (m.Parameters.Length > 0 &&
                                (attributeData.RequiresGenericTypeParameter ? 
                                    m.Parameters[0].Type.Name == "Type" && m.Parameters[0].Type.ContainingNamespace.Name == "System" :
                                    SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, attributeData.TargetType))) :
                               SymbolEqualityComparer.Default.Equals(m.ContainingType, attributeData.TargetType)))
                .OrderBy(m => m.Parameters.Length)
                .ToArray();

            foreach (var method in matchingMethods)
            {
                GenerateMethodsForSpecificOverload(context, sourceBuilder, attributeData, method);
            }
        }
        
        sourceBuilder.AppendLine("}");
        
        var fileName = $"{classSymbol.Name}.g.cs";
        context.AddSource(fileName, sourceBuilder.ToString());
    }

    private static void GenerateAssertConditionClassForMethod(SourceProductionContext context, StringBuilder sourceBuilder, CreateAssertionAttributeData attributeData, IMethodSymbol staticMethod)
    {
        var targetTypeName = attributeData.TargetType.ToDisplayString();
        var containingType = attributeData.ContainingType;
        var methodName = attributeData.MethodName;

        var className = GenerateAssertConditionClassNameForMethod(attributeData.TargetType, attributeData.ContainingType, methodName, staticMethod);
        var parameters = staticMethod.IsStatic
            ? (attributeData.RequiresGenericTypeParameter 
                ? staticMethod.Parameters.Skip(2).ToArray()  // Static + Generic: Skip Type and the actual value parameter
                : staticMethod.Parameters.Skip(1).ToArray()) // Static: Skip just the actual value parameter  
            : staticMethod.Parameters.ToArray();             // Instance: All parameters are additional

        // For Enum.IsDefined, we need to use a generic type parameter instead of the concrete Enum type
        if (attributeData.RequiresGenericTypeParameter && attributeData.TargetType.Name == "Enum")
        {
            // Generate: public class EnumIsDefinedAssertCondition<T> : BaseAssertCondition<T> where T : Enum
            sourceBuilder.AppendLine($"public class {className}<T> : BaseAssertCondition<T>");
            sourceBuilder.AppendLine($"    where T : Enum");
        }
        else
        {
            sourceBuilder.AppendLine($"public class {className} : BaseAssertCondition<{targetTypeName}>");
        }
        sourceBuilder.AppendLine("{");

        foreach (var param in parameters)
        {
            sourceBuilder.AppendLine($"    private readonly {param.Type.ToDisplayString()} _{param.Name};");
        }
        sourceBuilder.AppendLine("    private readonly bool _negated;");
        sourceBuilder.AppendLine();
        // Constructor name should not include generic type parameter
        var constructorName = className;
        sourceBuilder.Append($"    public {constructorName}(");
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

        string parameterType;
        if (attributeData.RequiresGenericTypeParameter && attributeData.TargetType.Name == "Enum")
        {
            parameterType = "T?"; // Generic enum parameter  
        }
        else
        {
            parameterType = attributeData.TargetType.IsValueType ? targetTypeName : $"{targetTypeName}?";
        }
        sourceBuilder.AppendLine($"    protected override ValueTask<AssertionResult> GetResult({parameterType} actualValue, Exception? exception, AssertionMetadata assertionMetadata)");
        sourceBuilder.AppendLine("    {");
        
        if (!attributeData.TargetType.IsValueType)
        {
            sourceBuilder.AppendLine("        if (actualValue is null)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            return AssertionResult.Fail(\"Actual value is null\");");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();
        }
        if (staticMethod.IsStatic)
        {
            if (attributeData.RequiresGenericTypeParameter)
            {
                sourceBuilder.Append($"        var result = {containingType.ToDisplayString()}.{methodName}(typeof(T), actualValue");
                foreach (var param in parameters)
                {
                    sourceBuilder.Append($", _{param.Name}");
                }
                sourceBuilder.AppendLine(");");
            }
            else
            {
                sourceBuilder.Append($"        var result = {containingType.ToDisplayString()}.{methodName}(actualValue");
                foreach (var param in parameters)
                {
                    sourceBuilder.Append($", _{param.Name}");
                }
                sourceBuilder.AppendLine(");");
            }
        }
        else
        {
            // Instance method
            sourceBuilder.Append($"        var result = actualValue.{methodName}(");
            var paramList = parameters.Select(p => $"_{p.Name}").ToArray();
            sourceBuilder.Append(string.Join(", ", paramList));
            sourceBuilder.AppendLine(");");
        }
        
        sourceBuilder.AppendLine("        var condition = _negated ? result : !result;");
        
        sourceBuilder.Append("        return AssertionResult.FailIf(condition, ");
        sourceBuilder.Append($"$\"'{{actualValue}}' was expected {{(_negated ? \"not \" : \"\")}}to satisfy {methodName}");
        if (parameters.Any())
        {
            sourceBuilder.Append($"({string.Join(", ", parameters.Select(p => $"{{_{p.Name}}}"))})");
        }
        sourceBuilder.AppendLine("\");");
        
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    protected internal override string GetExpectation()");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.Append($"        return $\"{{(_negated ? \"not \" : \"\")}}to satisfy {methodName}");
        if (parameters.Any())
        {
            sourceBuilder.Append($"({string.Join(", ", parameters.Select(p => $"{{_{p.Name}}}"))})");
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
        
        var parameterSuffix = "";
        
        if (method.Parameters.Length >= 1)
        {
            // Include first parameter type to disambiguate overloads like StartsWith(string) vs StartsWith(char)
            var firstParamType = GetSimpleTypeNameFromTypeSymbol(method.Parameters[0].Type);
            if (!string.IsNullOrEmpty(firstParamType))
            {
                parameterSuffix = $"With{firstParamType}";
                if (method.Parameters.Length > 1)
                {
                    parameterSuffix += $"And{method.Parameters.Length - 1}More";
                }
            }
            else if (method.Parameters.Length > 1)
            {
                parameterSuffix = $"With{method.Parameters.Length}Parameters";
            }
        }
        
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

    private static string GetSimpleTypeNameFromTypeSymbol(ITypeSymbol type)
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
            GenerateMethod(sourceBuilder, targetTypeName, actualMethodName, staticMethod, className, false, attributeData);
        }

        if ((assertionType & 2) != 0)
        {
            var isNotMethodName = actualMethodName.StartsWith("Is") ? $"IsNot{actualMethodName.Substring(2)}" : $"IsNot{actualMethodName}";
            GenerateMethod(sourceBuilder, targetTypeName, isNotMethodName, staticMethod, className, true, attributeData);
        }
    }

    private static void GenerateMethod(StringBuilder sourceBuilder, string targetTypeName, string generatedMethodName, IMethodSymbol method, string assertConditionClassName, bool negated, CreateAssertionAttributeData attributeData)
    {
        var parameters = method.Parameters;
        var additionalParameters = method.IsStatic
            ? (attributeData.RequiresGenericTypeParameter 
                ? parameters.Skip(2)  // Static + Generic: Skip Type and the actual value parameter
                : parameters.Skip(1)) // Static: Skip just the actual value parameter
            : parameters;             // Instance: All parameters are additional
        
        string extensionTargetType;
        if (attributeData.RequiresGenericTypeParameter && attributeData.TargetType.Name == "Enum")
        {
            extensionTargetType = "T";
            sourceBuilder.Append($"    public static InvokableValueAssertionBuilder<T> {generatedMethodName}<T>(this IValueSource<T> valueSource");
        }
        else
        {
            extensionTargetType = targetTypeName;
            sourceBuilder.Append($"    public static InvokableValueAssertionBuilder<{targetTypeName}> {generatedMethodName}(this IValueSource<{targetTypeName}> valueSource");
        }
        
        foreach (var param in additionalParameters)
        {
            sourceBuilder.Append($", {param.Type.ToDisplayString()} {param.Name}");
        }
        
        sourceBuilder.Append(")");
        
        if (attributeData.RequiresGenericTypeParameter && attributeData.TargetType.Name == "Enum")
        {
            sourceBuilder.AppendLine()
                         .Append("        where T : Enum");
        }
        
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    {");
        
        sourceBuilder.AppendLine($"        return valueSource.RegisterAssertion(");
        
        string constructorCall;
        if (attributeData.RequiresGenericTypeParameter && attributeData.TargetType.Name == "Enum")
        {
            constructorCall = $"new {assertConditionClassName}<T>(";
        }
        else
        {
            constructorCall = $"new {assertConditionClassName}(";
        }
        sourceBuilder.Append($"            {constructorCall}");
        
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
        string? CustomMethodName,
        bool RequiresGenericTypeParameter
    );
}