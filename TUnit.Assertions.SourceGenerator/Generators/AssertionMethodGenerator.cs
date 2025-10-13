using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Assertions.SourceGenerator.Generators;

[Generator]
public sealed class AssertionMethodGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Handle non-generic CreateAssertionAttribute (deprecated)
        var nonGenericCreateAttributeData = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Assertions.Attributes.CreateAssertionAttribute",
                predicate: (node, _) => true,
                transform: (ctx, _) => GetCreateAssertionAttributeData(ctx))
            .Where(x => x != null);

        // Handle non-generic AssertionFromAttribute (new)
        var nonGenericAssertionFromData = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Assertions.Attributes.AssertionFromAttribute",
                predicate: (node, _) => true,
                transform: (ctx, _) => GetCreateAssertionAttributeData(ctx))
            .Where(x => x != null);

        // Handle generic CreateAssertionAttribute<T> (deprecated)
        var genericCreateAttributeData = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: (ctx, _) => GetGenericCreateAssertionAttributeData(ctx, "CreateAssertionAttribute"))
            .Where(x => x != null)
            .SelectMany((x, _) => x!.ToImmutableArray());

        // Handle generic AssertionFromAttribute<T> (new)
        var genericAssertionFromData = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: (ctx, _) => GetGenericCreateAssertionAttributeData(ctx, "AssertionFromAttribute"))
            .Where(x => x != null)
            .SelectMany((x, _) => x!.ToImmutableArray());

        // Combine all sources
        var allAttributeData = nonGenericCreateAttributeData.Collect()
            .Combine(nonGenericAssertionFromData.Collect())
            .Combine(genericCreateAttributeData.Collect())
            .Combine(genericAssertionFromData.Collect())
            .Select((data, _) =>
            {
                var result = new List<AttributeWithClassData>();
                result.AddRange(data.Left.Left.Left.Where(x => x != null).SelectMany(x => x!));
                result.AddRange(data.Left.Left.Right.Where(x => x != null).SelectMany(x => x!));
                result.AddRange(data.Left.Right);
                result.AddRange(data.Right);
                return result.AsEnumerable();
            });

        context.RegisterSourceOutput(allAttributeData, GenerateAssertionsForClass);
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

                string? expectationMessage = null;
                if (attributeData.NamedArguments.Any(na => na.Key == "ExpectationMessage"))
                {
                    expectationMessage = attributeData.NamedArguments
                        .FirstOrDefault(na => na.Key == "ExpectationMessage")
                        .Value.Value?.ToString();
                }

                var createAssertionAttributeData = new CreateAssertionAttributeData(
                    targetType,
                    containingType,
                    methodName!,
                    customName,
                    negateLogic,
                    requiresGenericTypeParameter,
                    treatAsInstance,
                    expectationMessage
                );

                attributeDataList.Add(new AttributeWithClassData(classSymbol, createAssertionAttributeData));
            }
        }

        return attributeDataList.Count > 0 ? attributeDataList : null;
    }

    private static IEnumerable<AttributeWithClassData>? GetGenericCreateAssertionAttributeData(GeneratorSyntaxContext context, string attributeName)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
        {
            return null;
        }

        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        
        if (classSymbol == null)
        {
            return null;
        }

        var attributeDataList = new List<AttributeWithClassData>();

        foreach (var attributeData in classSymbol.GetAttributes())
        {
            var attributeClass = attributeData.AttributeClass;
            if (attributeClass == null || !attributeClass.IsGenericType)
            {
                continue;
            }

            // Check if it's the specified attribute type<T>
            var unboundType = attributeClass.ConstructedFrom;
            if (unboundType.Name != attributeName ||
                unboundType.TypeArguments.Length != 1 ||
                unboundType.ContainingNamespace?.ToDisplayString() != "TUnit.Assertions.Attributes")
            {
                continue;
            }

            // Extract the target type from the generic type argument
            var targetType = attributeClass.TypeArguments[0] as INamedTypeSymbol;
            if (targetType == null)
            {
                continue;
            }

            INamedTypeSymbol? containingType = null;
            string? methodName = null;

            // Handle constructor arguments
            if (attributeData.ConstructorArguments.Length == 1)
            {
                // CreateAssertionAttribute<T>(string methodName)
                methodName = attributeData.ConstructorArguments[0].Value?.ToString();
                containingType = targetType;
            }
            else if (attributeData.ConstructorArguments.Length == 2)
            {
                // CreateAssertionAttribute<T>(Type containingType, string methodName)
                containingType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
                methodName = attributeData.ConstructorArguments[1].Value?.ToString();
            }

            if (targetType != null && containingType != null && !string.IsNullOrEmpty(methodName))
            {
                // Skip error symbols
                if (targetType.TypeKind == TypeKind.Error || containingType.TypeKind == TypeKind.Error)
                {
                    continue;
                }

                string? customName = null;
                bool negateLogic = false;
                bool requiresGenericTypeParameter = false;
                bool treatAsInstance = false;
                string? expectationMessage = null;

                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    switch (namedArgument.Key)
                    {
                        case "CustomName":
                            customName = namedArgument.Value.Value?.ToString();
                            break;
                        case "NegateLogic":
                            negateLogic = namedArgument.Value.Value is true;
                            break;
                        case "RequiresGenericTypeParameter":
                            requiresGenericTypeParameter = namedArgument.Value.Value is true;
                            break;
                        case "TreatAsInstance":
                            treatAsInstance = namedArgument.Value.Value is true;
                            break;
                        case "ExpectationMessage":
                            expectationMessage = namedArgument.Value.Value?.ToString();
                            break;
                    }
                }

                var createAssertionAttributeData = new CreateAssertionAttributeData(
                    targetType,
                    containingType,
                    methodName!,
                    customName,
                    negateLogic,
                    requiresGenericTypeParameter,
                    treatAsInstance,
                    expectationMessage
                );

                attributeDataList.Add(new AttributeWithClassData(classSymbol, createAssertionAttributeData));
            }
        }

        return attributeDataList.Count > 0 ? attributeDataList : null;
    }

    private static bool IsValidReturnType(ITypeSymbol returnType, out ReturnTypeKind kind)
    {
        // Handle Task<T>
        if (returnType is INamedTypeSymbol namedType)
        {
            if (namedType.Name == "Task" &&
                namedType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks")
            {
                if (namedType.TypeArguments.Length == 1)
                {
                    var innerType = namedType.TypeArguments[0];

                    // Task<bool>
                    if (innerType.SpecialType == SpecialType.System_Boolean)
                    {
                        kind = ReturnTypeKind.TaskBool;
                        return true;
                    }

                    // Task<AssertionResult>
                    if (innerType.Name == "AssertionResult" &&
                        innerType.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions.Core")
                    {
                        kind = ReturnTypeKind.TaskAssertionResult;
                        return true;
                    }
                }
            }

            // AssertionResult
            if (namedType.Name == "AssertionResult" &&
                namedType.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions.Core")
            {
                kind = ReturnTypeKind.AssertionResult;
                return true;
            }
        }

        // bool
        if (returnType.SpecialType == SpecialType.System_Boolean)
        {
            kind = ReturnTypeKind.Bool;
            return true;
        }

        kind = ReturnTypeKind.Bool;
        return false;
    }

    private enum ReturnTypeKind
    {
        Bool,
        AssertionResult,
        TaskBool,
        TaskAssertionResult
    }

    private static void GenerateAssertionsForClass(SourceProductionContext context, IEnumerable<AttributeWithClassData> classAttributeData)
    {
        var allData = classAttributeData.ToArray();
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
        // Always generate extension methods in TUnit.Assertions.Extensions namespace
        // so they're available via implicit usings in consuming projects
        var namespaceName = "TUnit.Assertions.Extensions";

        // Get the original namespace where the helper methods/properties are defined
        var originalNamespace = classSymbol.ContainingNamespace?.ToDisplayString();

        sourceBuilder.AppendLine("#nullable enable");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
        sourceBuilder.AppendLine("using System.Threading.Tasks;");
        sourceBuilder.AppendLine("using TUnit.Assertions.Core;");

        // Add using for the original namespace to access helper methods/properties
        if (!string.IsNullOrEmpty(originalNamespace) && originalNamespace != namespaceName)
        {
            sourceBuilder.AppendLine($"using {originalNamespace};");
        }

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
                .Where(m => IsValidReturnType(m.ReturnType, out _) &&
                           (attributeData.TreatAsInstance ?
                               // If treating as instance and containing type is different, look for static methods that take target as first param
                               (!SymbolEqualityComparer.Default.Equals(attributeData.ContainingType, attributeData.TargetType) ?
                                   m.IsStatic && m.Parameters.Length > 0 && 
                                   SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, attributeData.TargetType) :
                                   !m.IsStatic) :
                               m.IsStatic ?
                                   // Static method: check first parameter matches target type or is generic Type
                                   m.Parameters.Length > 0 &&
                                   (attributeData.RequiresGenericTypeParameter ?
                                       m.Parameters[0].Type.Name == "Type" && m.Parameters[0].Type.ContainingNamespace.Name == "System" :
                                       SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, attributeData.TargetType)) :
                                   // Instance method: method must be on the target type
                                   SymbolEqualityComparer.Default.Equals(m.ContainingType, attributeData.TargetType)))
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
                .Where(m => IsValidReturnType(m.ReturnType, out _) &&
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

    private static void GenerateNonGenericTaskMethod(StringBuilder sourceBuilder, string targetTypeName, string generatedMethodName, string assertConditionClassName, bool negated, CreateAssertionAttributeData attributeData)
    {
        // Generate non-generic version for IAssertionSource<Task>
        sourceBuilder.Append($"    public static {assertConditionClassName}<{targetTypeName}> {generatedMethodName}(this IAssertionSource<{targetTypeName}> source)");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine($"        source.Context.ExpressionBuilder.Append(\".{generatedMethodName}()\");");
        sourceBuilder.Append($"        return new {assertConditionClassName}<{targetTypeName}>(source.Context");
        sourceBuilder.Append($", {negated.ToString().ToLowerInvariant()}");
        sourceBuilder.AppendLine(");");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
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

        // Check if target type is Task (for special handling of Task and Task<T>)
        var isTaskType = attributeData.TargetType.Name == "Task" &&
                        attributeData.TargetType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks" &&
                        attributeData.TargetType.TypeParameters.Length == 0; // Non-generic Task

        // For Enum.IsDefined, we need to use a generic type parameter instead of the concrete Enum type
        if (attributeData is { RequiresGenericTypeParameter: true, TargetType.Name: "Enum" })
        {
            // Generate: public class EnumIsDefinedAssertion<T> : Assertion<T> where T : Enum
            sourceBuilder.AppendLine($"public class {className}<T> : Assertion<T>");
            sourceBuilder.AppendLine($"    where T : Enum");
        }
        else if (isTaskType)
        {
            // Generate: public class TaskIsCompletedAssertion<TTask> : Assertion<TTask> where TTask : System.Threading.Tasks.Task
            sourceBuilder.AppendLine($"public class {className}<TTask> : Assertion<TTask>");
            sourceBuilder.AppendLine($"    where TTask : {targetTypeName}");
        }
        else
        {
            sourceBuilder.AppendLine($"public class {className} : Assertion<{targetTypeName}>");
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

        // Determine the AssertionContext type
        string contextType;
        if (attributeData is { RequiresGenericTypeParameter: true, TargetType.Name: "Enum" })
        {
            contextType = "AssertionContext<T>";
        }
        else if (isTaskType)
        {
            contextType = "AssertionContext<TTask>";
        }
        else
        {
            contextType = $"AssertionContext<{targetTypeName}>";
        }

        sourceBuilder.Append($"    public {constructorName}({contextType} context");
        foreach (var param in parameters)
        {
            sourceBuilder.Append($", {param.Type.ToDisplayString()} {param.Name}");
        }
        sourceBuilder.AppendLine(", bool negated = false)");
        sourceBuilder.AppendLine("        : base(context)");
        sourceBuilder.AppendLine("    {");

        foreach (var param in parameters)
        {
            sourceBuilder.AppendLine($"        _{param.Name} = {param.Name};");
        }
        sourceBuilder.AppendLine("        _negated = negated;");
        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();

        // Generate CheckAsync method
        string metadataType;
        if (attributeData is { RequiresGenericTypeParameter: true, TargetType.Name: "Enum" })
        {
            metadataType = "EvaluationMetadata<T>";
        }
        else if (isTaskType)
        {
            metadataType = "EvaluationMetadata<TTask>";
        }
        else
        {
            metadataType = $"EvaluationMetadata<{targetTypeName}>";
        }

        // Determine return type to decide if we need async
        if (!IsValidReturnType(staticMethod.ReturnType, out var methodReturnTypeKind))
        {
            methodReturnTypeKind = ReturnTypeKind.Bool;
        }

        // Only add async for Task<T> return types
        var needsAsync = methodReturnTypeKind == ReturnTypeKind.TaskBool || methodReturnTypeKind == ReturnTypeKind.TaskAssertionResult;
        var asyncKeyword = needsAsync ? "async " : "";

        sourceBuilder.AppendLine($"    protected override {asyncKeyword}Task<AssertionResult> CheckAsync({metadataType} metadata)");
        sourceBuilder.AppendLine("    {");
        sourceBuilder.AppendLine("        var actualValue = metadata.Value;");
        sourceBuilder.AppendLine("        var exception = metadata.Exception;");
        sourceBuilder.AppendLine();

        // For Task state assertions (IsFaulted, IsCanceled, IsCompleted, etc.),
        // we should NOT check for exceptions in metadata because:
        // 1. We don't await the task in the evaluator, so there shouldn't be exceptions
        // 2. Even if there are, we want to check the task's state properties regardless
        if (!isTaskType)
        {
            sourceBuilder.AppendLine("        if (exception != null)");
            sourceBuilder.AppendLine("        {");
            if (needsAsync)
            {
                sourceBuilder.AppendLine("            return AssertionResult.Failed($\"threw {exception.GetType().FullName}\");");
            }
            else
            {
                sourceBuilder.AppendLine("            return Task.FromResult(AssertionResult.Failed($\"threw {exception.GetType().FullName}\"));");
            }
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();
        }

        if (!attributeData.TargetType.IsValueType)
        {
            sourceBuilder.AppendLine("        if (actualValue is null)");
            sourceBuilder.AppendLine("        {");
            if (needsAsync)
            {
                sourceBuilder.AppendLine("            return AssertionResult.Failed(\"Actual value is null\");");
            }
            else
            {
                sourceBuilder.AppendLine("            return Task.FromResult(AssertionResult.Failed(\"Actual value is null\"));");
            }
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();
        }
        if (staticMethod.IsStatic)
        {
            // Check if this is an extension method
            var isExtensionMethod = staticMethod.IsExtensionMethod ||
                                    (staticMethod.Parameters.Length > 0 && staticMethod.Parameters[0].IsThis);

            if (isExtensionMethod)
            {
                // Extension method - call it like an instance method
                sourceBuilder.Append($"        var result = actualValue.{methodName}(");
                var paramList = parameters.Select(p => $"_{p.Name}").ToArray();
                sourceBuilder.Append(string.Join(", ", paramList));
                sourceBuilder.AppendLine(");");
            }
            else if (attributeData.TreatAsInstance)
            {
                // Static method treated as instance - call the static method with actualValue as first parameter
                sourceBuilder.Append($"        var result = {containingType.ToDisplayString()}.{methodName}(actualValue");
                foreach (var param in parameters)
                {
                    sourceBuilder.Append($", _{param.Name}");
                }
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

        // Generate result handling based on return type (use cached value from earlier)
        switch (methodReturnTypeKind)
        {
            case ReturnTypeKind.Bool:
                // For bool: negate if needed, then wrap in AssertionResult and Task
                sourceBuilder.AppendLine("        var condition = _negated ? result : !result;");
                sourceBuilder.Append("        return Task.FromResult(AssertionResult.FailIf(condition, ");
                sourceBuilder.Append($"$\"'{{actualValue}}' was expected {{(_negated ? \"not \" : \"\")}}to satisfy {methodName}");
                if (parameters.Any())
                {
                    sourceBuilder.Append($"({string.Join(", ", parameters.Select(p => $"{{_{p.Name}}}"))})");
                }
                sourceBuilder.AppendLine("\"));");
                break;

            case ReturnTypeKind.AssertionResult:
                // For AssertionResult: wrap in Task.FromResult (no negation support)
                sourceBuilder.AppendLine("        return Task.FromResult(result);");
                break;

            case ReturnTypeKind.TaskBool:
                // For Task<bool>: await, then negate if needed
                sourceBuilder.AppendLine("        var boolResult = await result;");
                sourceBuilder.AppendLine("        var condition = _negated ? boolResult : !boolResult;");
                sourceBuilder.Append("        return AssertionResult.FailIf(condition, ");
                sourceBuilder.Append($"$\"'{{actualValue}}' was expected {{(_negated ? \"not \" : \"\")}}to satisfy {methodName}");
                if (parameters.Any())
                {
                    sourceBuilder.Append($"({string.Join(", ", parameters.Select(p => $"{{_{p.Name}}}"))})");
                }
                sourceBuilder.AppendLine("\");");
                break;

            case ReturnTypeKind.TaskAssertionResult:
                // For Task<AssertionResult>: await and return
                sourceBuilder.AppendLine("        return await result;");
                break;
        }

        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    protected override string GetExpectation()");
        sourceBuilder.AppendLine("    {");

        if (!string.IsNullOrEmpty(attributeData.ExpectationMessage))
        {
            // Use custom expectation message
            var expectation = attributeData.ExpectationMessage;
            if (parameters.Any())
            {
                // Use interpolated string for parameter substitution
                sourceBuilder.AppendLine($"        return $\"{{(_negated ? \"not \" : \"\")}} {expectation}\";");
            }
            else
            {
                // No parameters, just return the literal string with negation support
                sourceBuilder.AppendLine($"        return $\"{{(_negated ? \"not \" : \"\")}} {expectation}\";");
            }
        }
        else
        {
            // Use default expectation message
            sourceBuilder.Append($"        return $\"{{(_negated ? \"not \" : \"\")}}to satisfy {methodName}");
            if (parameters.Any())
            {
                sourceBuilder.Append($"({string.Join(", ", parameters.Select(p => $"{{_{p.Name}}}"))})");
            }
            sourceBuilder.AppendLine("\";");
        }

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

        return $"{targetTypeName}{containingTypeName}{methodName}{parameterSuffix}Assertion";
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

        // Check if target type is Task (for special handling of Task and Task<T>)
        var isTaskType = attributeData.TargetType.Name == "Task" &&
                        attributeData.TargetType.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks" &&
                        attributeData.TargetType.TypeParameters.Length == 0; // Non-generic Task

        // Generate the extension method using the modern IAssertionSource<T> pattern
        if (attributeData is { RequiresGenericTypeParameter: true, TargetType.Name: "Enum" })
        {
            sourceBuilder.Append($"    public static {assertConditionClassName}<T> {generatedMethodName}<T>(this IAssertionSource<T> source");
        }
        else if (isTaskType)
        {
            // For Task, generate a generic method that works with Task and Task<T>
            sourceBuilder.Append($"    public static {assertConditionClassName}<TTask> {generatedMethodName}<TTask>(this IAssertionSource<TTask> source");
        }
        else
        {
            sourceBuilder.Append($"    public static {assertConditionClassName} {generatedMethodName}(this IAssertionSource<{targetTypeName}> source");
        }

        foreach (var param in additionalParameters)
        {
            sourceBuilder.Append($", {param.Type.ToDisplayString()} {param.Name}");
        }

        // Add CallerArgumentExpression parameters for better error messages
        var additionalParametersArray = additionalParameters.ToArray();
        for (var i = 0; i < additionalParametersArray.Length; i++)
        {
            sourceBuilder.Append($", [CallerArgumentExpression(nameof({additionalParametersArray[i].Name}))] string? {additionalParametersArray[i].Name}Expression = null");
        }

        sourceBuilder.Append(")");

        if (attributeData is { RequiresGenericTypeParameter: true, TargetType.Name: "Enum" })
        {
            sourceBuilder.AppendLine();
            sourceBuilder.Append("        where T : Enum");
        }
        else if (isTaskType)
        {
            sourceBuilder.AppendLine();
            sourceBuilder.Append($"        where TTask : {targetTypeName}");
        }

        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    {");

        // Build the expression for ExpressionBuilder.Append()
        if (additionalParametersArray.Length > 0)
        {
            var exprList = string.Join(", ", additionalParametersArray.Select(p => $"{{{p.Name}Expression}}"));
            sourceBuilder.AppendLine($"        source.Context.ExpressionBuilder.Append($\".{generatedMethodName}({exprList})\");");
        }
        else
        {
            sourceBuilder.AppendLine($"        source.Context.ExpressionBuilder.Append(\".{generatedMethodName}()\");");
        }

        // Construct and return the assertion directly (modern pattern)
        if (attributeData is { RequiresGenericTypeParameter: true, TargetType.Name: "Enum" })
        {
            sourceBuilder.Append($"        return new {assertConditionClassName}<T>(source.Context");
        }
        else if (isTaskType)
        {
            sourceBuilder.Append($"        return new {assertConditionClassName}<TTask>(source.Context");
        }
        else
        {
            sourceBuilder.Append($"        return new {assertConditionClassName}(source.Context");
        }

        foreach (var param in additionalParameters)
        {
            sourceBuilder.Append($", {param.Name}");
        }

        sourceBuilder.Append($", {negated.ToString().ToLowerInvariant()}");
        sourceBuilder.AppendLine(");");

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
        bool TreatAsInstance,
        string? ExpectationMessage
    );
}
