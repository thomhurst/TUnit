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
                predicate: (node, _) => true,
                transform: (ctx, _) => GetCreateAssertionAttributeData(ctx))
            .Where(x => x != null);

        context.RegisterSourceOutput(classAttributeData.Collect(), GenerateAssertionsForClass);
    }

    private static IEnumerable<AttributeWithClassData>? GetCreateAssertionAttributeData(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        var attributeDataList = new List<AttributeWithClassData>();

        foreach (var attributeData in context.Attributes)
        {
            var targetType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
            INamedTypeSymbol? containingType = null;
            string? methodName = null;

            if (attributeData.ConstructorArguments.Length == 2)
            {
                methodName = attributeData.ConstructorArguments[1].Value?.ToString();
                containingType = targetType;
            }
            else if (attributeData.ConstructorArguments.Length >= 3)
            {
                containingType = attributeData.ConstructorArguments[1].Value as INamedTypeSymbol;
                methodName = attributeData.ConstructorArguments[2].Value?.ToString();
            }

            if (targetType != null && containingType != null && !string.IsNullOrEmpty(methodName))
            {
                // Skip error symbols - they'll be reported as missing methods later
            if (targetType.TypeKind == TypeKind.Error || containingType.TypeKind == TypeKind.Error)
            {
                continue;
            }
                string? customName = null;
                if (attributeData.NamedArguments.Any(na => na.Key == "CustomName"))
                {
                    customName = attributeData.NamedArguments
                        .FirstOrDefault(na => na.Key == "CustomName")
                        .Value.Value?.ToString();
                }

                var negateLogic = false;
                if (attributeData.NamedArguments.Any(na => na.Key == "NegateLogic"))
                {
                    negateLogic = (bool)(attributeData.NamedArguments
                        .FirstOrDefault(na => na.Key == "NegateLogic")
                        .Value.Value ?? false);
                }

                var requiresGenericTypeParameter = false;
                if (attributeData.NamedArguments.Any(na => na.Key == "RequiresGenericTypeParameter"))
                {
                    requiresGenericTypeParameter = (bool)(attributeData.NamedArguments
                        .FirstOrDefault(na => na.Key == "RequiresGenericTypeParameter")
                        .Value.Value ?? false);
                }

                var treatAsInstance = false;
                if (attributeData.NamedArguments.Any(na => na.Key == "TreatAsInstance"))
                {
                    treatAsInstance = (bool)(attributeData.NamedArguments
                        .FirstOrDefault(na => na.Key == "TreatAsInstance")
                        .Value.Value ?? false);
                }

                var createAssertionAttributeData = new CreateAssertionAttributeData(
                    targetType,
                    containingType,
                    methodName,
                    customName,
                    negateLogic,
                    requiresGenericTypeParameter,
                    treatAsInstance
                );

                attributeDataList.Add(new AttributeWithClassData(classSymbol, createAssertionAttributeData));
            }
        }

        return attributeDataList.Count > 0 ? attributeDataList : null;
    }

    private static void GenerateAssertionsForClass(SourceProductionContext context, ImmutableArray<IEnumerable<AttributeWithClassData>?> classAttributeData)
    {
        var allData = classAttributeData.SelectMany(x => x ?? []).ToArray();
        if (!allData.Any())
        {
            return;
        }

        // Track all generated classes globally to avoid duplicates across different extension classes
        var allGeneratedClasses = new HashSet<string>();

        // Group by class and generate one file per class
        foreach (var classGroup in allData.GroupBy(x => x.ClassSymbol, SymbolEqualityComparer.Default))
        {
            GenerateAssertionsForSpecificClass(context, classGroup.Key as INamedTypeSymbol, classGroup.ToArray(), allGeneratedClasses);
        }
    }

    private static void GenerateAssertionsForSpecificClass(SourceProductionContext context, INamedTypeSymbol? classSymbol, AttributeWithClassData[] dataList, HashSet<string> allGeneratedClasses)
    {
        if (classSymbol == null || !dataList.Any())
        {
            return;
        }
        var sourceBuilder = new StringBuilder();
        var namespaceName = classSymbol.ContainingNamespace?.ToDisplayString();

        sourceBuilder.AppendLine("#nullable enable");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
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

        // Generate all assert condition classes first
        foreach (var attributeWithClassData in dataList)
        {
            var attributeData = attributeWithClassData.AttributeData;

            // First try to find methods
            var methodMembers = attributeData.ContainingType.GetMembers(attributeData.MethodName)
                .OfType<IMethodSymbol>()
                .Where(m => m.ReturnType.SpecialType == SpecialType.System_Boolean &&
                           (attributeData.TreatAsInstance ?
                               // If explicitly treating as instance, allow instance methods on any type
                               !m.IsStatic :
                               m.IsStatic ?
                                   // Static method: check first parameter matches target type or is generic Type
                                   m.Parameters.Length > 0 &&
                                   (attributeData.RequiresGenericTypeParameter ?
                                       m.Parameters[0].Type.Name == "Type" && m.Parameters[0].Type.ContainingNamespace.Name == "System" :
                                       SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, attributeData.TargetType)) :
                                   // Instance method: method must be on the target type OR TreatAsInstance is true
                                   SymbolEqualityComparer.Default.Equals(m.ContainingType, attributeData.TargetType) ||
                                   attributeData.TreatAsInstance))
                .OrderBy(m => m.Parameters.Length)
                .ToArray();

            // If no methods found, try properties
            var propertyMembers = new List<IPropertySymbol>();
            if (!methodMembers.Any())
            {
                propertyMembers = attributeData.ContainingType.GetMembers(attributeData.MethodName)
                    .OfType<IPropertySymbol>()
                    .Where(p => p.Type.SpecialType == SpecialType.System_Boolean &&
                        p is { GetMethod: not null, IsStatic: false } && SymbolEqualityComparer.Default.Equals(p.ContainingType, attributeData.TargetType))
                    .ToList();
            }

            var matchingMethods = methodMembers.ToList();

            // Convert properties to method-like representation for uniform handling
            foreach (var property in propertyMembers)
            {
                if (property.GetMethod != null)
                {
                    matchingMethods.Add(property.GetMethod);
                }
            }

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

                if (!allGeneratedClasses.Add(className))
                {
                    continue;
                }

                GenerateAssertConditionClassForMethod(context, sourceBuilder, attributeData, method);
            }
        }

        // Generate extension methods class
        sourceBuilder.AppendLine($"public static partial class {classSymbol.Name}");
        sourceBuilder.AppendLine("{");

        foreach (var attributeWithClassData in dataList)
        {
            var attributeData = attributeWithClassData.AttributeData;

            // Try to find methods first
            var methodMembers = attributeData.ContainingType.GetMembers(attributeData.MethodName)
                .OfType<IMethodSymbol>()
                .Where(m => m.ReturnType.SpecialType == SpecialType.System_Boolean &&
                           (m.IsStatic ?
                               m.Parameters.Length > 0 &&
                               (attributeData.RequiresGenericTypeParameter ?
                                   m.Parameters[0].Type.Name == "Type" && m.Parameters[0].Type.ContainingNamespace.Name == "System" :
                                   SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, attributeData.TargetType)) :
                               SymbolEqualityComparer.Default.Equals(m.ContainingType, attributeData.TargetType)))
                .OrderBy(m => m.Parameters.Length)
                .ToArray();

            var propertyMembers = attributeData.ContainingType.GetMembers(attributeData.MethodName)
                .OfType<IPropertySymbol>()
                .Where(p => p.Type.SpecialType == SpecialType.System_Boolean &&
                    p is { GetMethod: not null, IsStatic: false } &&
                    SymbolEqualityComparer.Default.Equals(p.ContainingType, attributeData.TargetType))
                .ToList();

            var matchingMethods = methodMembers.ToList();

            // Convert properties to method-like representation
            foreach (var property in propertyMembers)
            {
                if (property.GetMethod != null)
                {
                    matchingMethods.Add(property.GetMethod);
                }
            }

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

        // Determine which parameters to skip
        IParameterSymbol[] parameters;
        if (staticMethod.IsStatic)
        {
            var isExtensionMethod = staticMethod.IsExtensionMethod ||
                                    (staticMethod.Parameters.Length > 0 && staticMethod.Parameters[0].IsThis);

            if (attributeData.RequiresGenericTypeParameter)
            {
                parameters = staticMethod.Parameters.Skip(2).ToArray(); // Skip Type and the actual value parameter
            }
            else if (isExtensionMethod || attributeData.TreatAsInstance)
            {
                parameters = staticMethod.Parameters.Skip(1).ToArray(); // Skip the 'this' parameter or first parameter
            }
            else
            {
                parameters = staticMethod.Parameters.Skip(1).ToArray(); // Skip just the actual value parameter
            }
        }
        else
        {
            parameters = staticMethod.Parameters.ToArray(); // Instance: All parameters are additional
        }

        // For Enum.IsDefined, we need to use a generic type parameter instead of the concrete Enum type
        if (attributeData is { RequiresGenericTypeParameter: true, TargetType.Name: "Enum" })
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
        if (attributeData is { RequiresGenericTypeParameter: true, TargetType.Name: "Enum" })
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
            // Check if this is an extension method
            var isExtensionMethod = staticMethod.IsExtensionMethod ||
                                    (staticMethod.Parameters.Length > 0 && staticMethod.Parameters[0].IsThis);

            if (isExtensionMethod || attributeData.TreatAsInstance)
            {
                // Extension method or static method treated as instance - call it like an instance method
                sourceBuilder.Append($"        var result = actualValue.{methodName}(");
                var paramList = parameters.Select(p => $"_{p.Name}").ToArray();
                sourceBuilder.Append(string.Join(", ", paramList));
                sourceBuilder.AppendLine(");");
            }
            else if (attributeData.RequiresGenericTypeParameter)
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
            // Instance method or property getter
            if (SymbolEqualityComparer.Default.Equals(staticMethod.ContainingType, attributeData.TargetType))
            {
                // Check if this is a property getter (MethodKind.PropertyGet)
                if (staticMethod.MethodKind == MethodKind.PropertyGet)
                {
                    // Property getter - access as property, not method
                    var propertyName = methodName.StartsWith("get_") ? methodName.Substring(4) : methodName;
                    sourceBuilder.AppendLine($"        var result = actualValue.{propertyName};");
                }
                else
                {
                    // Instance method on the target type itself
                    sourceBuilder.Append($"        var result = actualValue.{methodName}(");
                    var paramList = parameters.Select(p => $"_{p.Name}").ToArray();
                    sourceBuilder.Append(string.Join(", ", paramList));
                    sourceBuilder.AppendLine(");");
                }
            }
            else if (attributeData.TreatAsInstance)
            {
                // Instance method on a different type - check if it takes the target as first parameter
                if (staticMethod.Parameters.Length > 0 &&
                    SymbolEqualityComparer.Default.Equals(staticMethod.Parameters[0].Type, attributeData.TargetType))
                {
                    // The instance method takes the target type as first parameter
                    sourceBuilder.AppendLine($"        var instance = new {containingType.ToDisplayString()}();");
                    sourceBuilder.Append($"        var result = instance.{methodName}(actualValue");
                    foreach (var param in parameters.Skip(1))
                    {
                        sourceBuilder.Append($", _{param.Name}");
                    }
                    sourceBuilder.AppendLine(");");
                }
                else
                {
                    // Instance method that doesn't take the target type - might be a validation method
                    sourceBuilder.AppendLine($"        var instance = new {containingType.ToDisplayString()}();");
                    sourceBuilder.Append($"        var result = instance.{methodName}(");
                    var paramList = parameters.Select(p => $"_{p.Name}").ToArray();
                    sourceBuilder.Append(string.Join(", ", paramList));
                    sourceBuilder.AppendLine(");");
                }
            }
            else
            {
                // Default instance method behavior
                sourceBuilder.Append($"        var result = actualValue.{methodName}(");
                var paramList = parameters.Select(p => $"_{p.Name}").ToArray();
                sourceBuilder.Append(string.Join(", ", paramList));
                sourceBuilder.AppendLine(");");
            }
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

        var baseMethodName = attributeData.CustomName ?? methodName;
        var className = GenerateAssertConditionClassNameForMethod(attributeData.TargetType, attributeData.ContainingType, methodName, staticMethod);

        var actualMethodName = baseMethodName;
        if (staticMethod.Parameters.Length > 1)
        {
            if (methodName == "IsDigit" && staticMethod.Parameters is
                [
                    _, { Type.SpecialType: SpecialType.System_Int32 }
                ])
            {
                actualMethodName = "IsDigitAt";
            }
        }

        // If NegateLogic is true, we're generating a negated assertion (e.g., DoesNotContain from Contains)
        if (attributeData.NegateLogic)
        {
            // Generate only the negated version with the custom name
            GenerateMethod(sourceBuilder, targetTypeName, actualMethodName, staticMethod, className, true, attributeData);
        }
        else
        {
            // Generate positive assertion (always generate for non-negated attributes)
            GenerateMethod(sourceBuilder, targetTypeName, actualMethodName, staticMethod, className, false, attributeData);
        }
    }

    private static void GenerateMethod(StringBuilder sourceBuilder, string targetTypeName, string generatedMethodName, IMethodSymbol method, string assertConditionClassName, bool negated, CreateAssertionAttributeData attributeData)
    {
        var parameters = method.Parameters;
        var additionalParameters = method.IsStatic
            ? attributeData.RequiresGenericTypeParameter
                ? parameters.Skip(2)  // Static + Generic: Skip Type and the actual value parameter
                : parameters.Skip(1) // Static: Skip just the actual value parameter
            : parameters;             // Instance: All parameters are additional

        string extensionTargetType;
        if (attributeData is { RequiresGenericTypeParameter: true, TargetType.Name: "Enum" })
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

        // Add CallerArgumentExpression parameters for better error messages
        var additionalParametersArray = additionalParameters.ToArray();
        for (var i = 0; i < additionalParametersArray.Length; i++)
        {
            sourceBuilder.Append($", [CallerArgumentExpression(nameof({additionalParametersArray[i].Name}))] string? doNotPopulateThisValue{i + 1} = null");
        }

        sourceBuilder.Append(")");

        if (attributeData is { RequiresGenericTypeParameter: true, TargetType.Name: "Enum" })
        {
            sourceBuilder.AppendLine()
                         .Append("        where T : Enum");
        }

        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    {");

        sourceBuilder.AppendLine($"        return valueSource.RegisterAssertion(");

        string constructorCall;
        if (attributeData is { RequiresGenericTypeParameter: true, TargetType.Name: "Enum" })
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

        // Generate the array of CallerArgumentExpression parameters
        var callerArgParams = new List<string>();
        for (var i = 0; i < additionalParametersArray.Length; i++)
        {
            callerArgParams.Add($"doNotPopulateThisValue{i + 1}");
        }
        sourceBuilder.AppendLine($"            [{string.Join(", ", callerArgParams)}]);");
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
        string? CustomName,
        bool NegateLogic,
        bool RequiresGenericTypeParameter,
        bool TreatAsInstance
    );
}
