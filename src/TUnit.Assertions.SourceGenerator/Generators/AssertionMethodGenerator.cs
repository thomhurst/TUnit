using TUnit.Core.SourceGenerator.Extensions;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Assertions.SourceGenerator.Models;

namespace TUnit.Assertions.SourceGenerator.Generators;

[Generator]
public sealed class AssertionMethodGenerator : IIncrementalGenerator
{
    public const string NonGenericAssertionFromStep = "NonGenericAssertionFrom";
    public const string GenericAssertionFromStep = "GenericAssertionFrom";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Both transforms render the generated snippets eagerly and return string-only models,
        // so no symbols (and no compilation) are rooted by the pipeline and unchanged classes
        // compare equal across edits.

        // Handle non-generic AssertionFromAttribute
        var nonGenericAssertionFromData = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Assertions.Attributes.AssertionFromAttribute",
                predicate: static (node, _) => true,
                transform: static (ctx, _) => GetCreateAssertionAttributeData(ctx))
            .Where(static x => x != null)
            .Select(static (x, _) => x!)
            .WithTrackingName(NonGenericAssertionFromStep);

        // Handle generic AssertionFromAttribute<T>
        var genericAssertionFromData = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Assertions.Attributes.AssertionFromAttribute`1",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetGenericCreateAssertionAttributeData(ctx, "AssertionFromAttribute"))
            .Where(static x => x != null)
            .Select(static (x, _) => x!)
            .WithTrackingName(GenericAssertionFromStep);

        // Combine all sources
        var allAttributeData = nonGenericAssertionFromData.Collect()
            .Combine(genericAssertionFromData.Collect());

        context.RegisterSourceOutput(allAttributeData, static (ctx, data) => GenerateAssertionsForClass(ctx, data.Left, data.Right));
    }

    private static AssertionClassModel? GetCreateAssertionAttributeData(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        var attributeDataList = new List<CreateAssertionAttributeData>();

        foreach (var attributeData in context.Attributes)
        {
            var targetType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
            INamedTypeSymbol? containingType = null;
            string? methodName = null;

            if (attributeData.ConstructorArguments.Length == 2)
            {
                methodName = attributeData.ConstructorArguments[1].Value.ToInvariantString();
                containingType = targetType;
            }
            else if (attributeData.ConstructorArguments.Length >= 3)
            {
                containingType = attributeData.ConstructorArguments[1].Value as INamedTypeSymbol;
                methodName = attributeData.ConstructorArguments[2].Value.ToInvariantString();
            }

            if (targetType != null && containingType != null && !string.IsNullOrEmpty(methodName))
            {
                // Skip error symbols - they'll be reported as missing methods later
            if (targetType.TypeKind == TypeKind.Error || containingType.TypeKind == TypeKind.Error)
            {
                continue;
            }

                // Track if this is an unbound generic type for special handling
                var isUnboundGeneric = containingType.IsUnboundGenericType;

                string? customName = null;
                if (attributeData.NamedArguments.Any(na => na.Key == "CustomName"))
                {
                    customName = attributeData.NamedArguments
                        .FirstOrDefault(na => na.Key == "CustomName")
                        .Value.Value.ToInvariantString();
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
                        .Value.Value.ToInvariantString();
                }

                var createAssertionAttributeData = new CreateAssertionAttributeData(
                    targetType,
                    containingType,
                    methodName!,
                    customName,
                    negateLogic,
                    requiresGenericTypeParameter,
                    treatAsInstance,
                    expectationMessage,
                    isUnboundGeneric
                );

                attributeDataList.Add(createAssertionAttributeData);
            }
        }

        return attributeDataList.Count > 0 ? BuildClassModel(classSymbol, attributeDataList) : null;
    }

    private static AssertionClassModel? GetGenericCreateAssertionAttributeData(GeneratorAttributeSyntaxContext context, string attributeName)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        var attributeDataList = new List<CreateAssertionAttributeData>();

        foreach (var attributeData in context.Attributes)
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
                methodName = attributeData.ConstructorArguments[0].Value.ToInvariantString();
                containingType = targetType;
            }
            else if (attributeData.ConstructorArguments.Length == 2)
            {
                // CreateAssertionAttribute<T>(Type containingType, string methodName)
                containingType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
                methodName = attributeData.ConstructorArguments[1].Value.ToInvariantString();
            }

            if (targetType != null && containingType != null && !string.IsNullOrEmpty(methodName))
            {
                // Skip error symbols
                if (targetType.TypeKind == TypeKind.Error || containingType.TypeKind == TypeKind.Error)
                {
                    continue;
                }

                // Track if this is an unbound generic type for special handling
                var isUnboundGeneric = containingType.IsUnboundGenericType;

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
                            customName = namedArgument.Value.Value.ToInvariantString();
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
                            expectationMessage = namedArgument.Value.Value.ToInvariantString();
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
                    expectationMessage,
                    isUnboundGeneric
                );

                attributeDataList.Add(createAssertionAttributeData);
            }
        }

        return attributeDataList.Count > 0 ? BuildClassModel(classSymbol, attributeDataList) : null;
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

    private static void GenerateAssertionsForClass(
        SourceProductionContext context,
        ImmutableArray<AssertionClassModel> nonGenericData,
        ImmutableArray<AssertionClassModel> genericData)
    {
        if (nonGenericData.IsDefaultOrEmpty && genericData.IsDefaultOrEmpty)
        {
            return;
        }

        // Track all generated classes globally to avoid duplicates across different extension classes
        var allGeneratedClasses = new GeneratedConditionClasses();

        // Group by class and generate one file per class. Non-generic attribute data comes first,
        // then generic, so a class using both keeps its existing member order.
        var allData = nonGenericData.Concat(genericData)
            .SelectMany(model => model.Entries.Select(entry => (Model: model, Entry: entry)));

        foreach (var classGroup in allData.GroupBy(x => x.Model.ClassKey, StringComparer.Ordinal))
        {
            var first = classGroup.First().Model;
            GenerateAssertionsForSpecificClass(context, first.ClassName, first.OriginalNamespace, classGroup.Select(x => x.Entry).ToArray(), allGeneratedClasses);
        }
    }

    private static string RenameClass(string source, string className, string newName) =>
        className == newName
            ? source
            : Regex.Replace(source, $@"\b{Regex.Escape(className)}\b", newName);

    /// <summary>
    /// The condition classes emitted so far, across every extension class. A condition class is named after its
    /// target method, so attributes on the same method share it, which is fine while their sources match (e.g.
    /// <c>IsNullOrEmpty</c> and its <c>NegateLogic</c> counterpart, which passes the negation to the constructor).
    /// A class with the same name but a different source, such as a different <c>ExpectationMessage</c>, gets a
    /// numbered name instead of being dropped, so its attribute keeps its own message.
    /// </summary>
    private sealed class GeneratedConditionClasses
    {
        private readonly HashSet<string> _names = new(StringComparer.Ordinal);
        private readonly Dictionary<(string ClassName, string Source), string> _emitted = new();

        /// <summary>
        /// Returns the name to use for <paramref name="conditionClass"/>, and whether it still has to be emitted.
        /// </summary>
        public (string Name, bool IsNew) Claim(ConditionClassModel conditionClass)
        {
            var key = (conditionClass.ClassName, conditionClass.Source);
            if (_emitted.TryGetValue(key, out var existing))
            {
                return (existing, false);
            }

            var name = conditionClass.ClassName;
            for (var suffix = 2; !_names.Add(name); suffix++)
            {
                name = conditionClass.ClassName + suffix.ToString(CultureInfo.InvariantCulture);
            }

            _emitted.Add(key, name);
            return (name, true);
        }
    }

    private static AssertionClassModel BuildClassModel(INamedTypeSymbol classSymbol, List<CreateAssertionAttributeData> attributeDataList)
    {
        var entries = new List<AssertionEntryModel>(attributeDataList.Count);
        foreach (var attributeData in attributeDataList)
        {
            entries.Add(RenderEntry(attributeData));
        }

        return new AssertionClassModel(
            classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            classSymbol.Name,
            classSymbol.ContainingNamespace?.ToDisplayString(),
            entries.ToImmutableEquatableArray());
    }

    private static void GenerateAssertionsForSpecificClass(
        SourceProductionContext context,
        string className,
        string? originalNamespace,
        AssertionEntryModel[] entries,
        GeneratedConditionClasses allGeneratedClasses)
    {
        var sourceBuilder = new StringBuilder();
        // Always generate extension methods in TUnit.Assertions.Extensions namespace
        // so they're available via implicit usings in consuming projects
        var namespaceName = "TUnit.Assertions.Extensions";

        sourceBuilder.AppendLine("// <auto-generated/>");
        sourceBuilder.AppendLine("#pragma warning disable");
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
        var extensionMethodSources = new List<string>(entries.Length);
        foreach (var entry in entries)
        {
            if (entry.MissingMethodMessage is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "TU0001",
                        "Method not found",
                        entry.MissingMethodMessage,
                        "TUnit.Assertions",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
            }

            var extensionMethodsSource = entry.ExtensionMethodsSource;
            var namesInEntry = new HashSet<string>(StringComparer.Ordinal);
            foreach (var conditionClass in entry.ConditionClasses)
            {
                // Overloads that map to the same class name can't be told apart in the extension methods, so the
                // first one wins, as before
                if (!namesInEntry.Add(conditionClass.ClassName))
                {
                    continue;
                }

                var (name, isNew) = allGeneratedClasses.Claim(conditionClass);
                if (name != conditionClass.ClassName)
                {
                    extensionMethodsSource = RenameClass(extensionMethodsSource, conditionClass.ClassName, name);
                }

                if (isNew)
                {
                    sourceBuilder.Append(RenameClass(conditionClass.Source, conditionClass.ClassName, name));
                }
            }

            extensionMethodSources.Add(extensionMethodsSource);
        }

        // Generate extension methods class
        sourceBuilder.AppendLine($"public static partial class {className}");
        sourceBuilder.AppendLine("{");

        foreach (var extensionMethodsSource in extensionMethodSources)
        {
            sourceBuilder.Append(extensionMethodsSource);
        }

        sourceBuilder.AppendLine("}");

        var fileName = $"{className}.g.cs";
        context.AddSource(fileName, sourceBuilder.ToString());
    }

    /// <summary>
    /// Renders every snippet one attribute contributes, while its symbols are still available,
    /// so the incremental pipeline only carries strings.
    /// </summary>
    private static AssertionEntryModel RenderEntry(CreateAssertionAttributeData attributeData)
    {
        var conditionClasses = RenderConditionClasses(attributeData, out var missingMethodMessage);
        var extensionMethodsSource = RenderExtensionMethods(attributeData);

        return new AssertionEntryModel(conditionClasses, missingMethodMessage, extensionMethodsSource);
    }

    private static ImmutableEquatableArray<ConditionClassModel> RenderConditionClasses(CreateAssertionAttributeData attributeData, out string? missingMethodMessage)
    {
        missingMethodMessage = null;
        var conditionClasses = new List<ConditionClassModel>();

        // For unbound generic types, we need to temporarily construct them for member lookup
        var typeForMemberLookup = attributeData.ContainingType;
        if (attributeData.IsUnboundGeneric && typeForMemberLookup.IsUnboundGenericType)
        {
            // Get System.Object by searching through the type's containing assembly references
            INamedTypeSymbol? objectType = null;

            // Look through the referenced assemblies to find System.Object
            foreach (var refAssembly in typeForMemberLookup.ContainingAssembly.Modules.FirstOrDefault()?.ReferencedAssemblySymbols ?? Enumerable.Empty<IAssemblySymbol>())
            {
                var systemNs = refAssembly.GlobalNamespace.GetNamespaceMembers().FirstOrDefault(ns => ns.Name == "System");
                if (systemNs != null)
                {
                    objectType = systemNs.GetTypeMembers("Object").FirstOrDefault();
                    if (objectType != null) break;
                }
            }

            if (objectType != null)
            {
                var typeArgs = Enumerable.Repeat<ITypeSymbol>(objectType, typeForMemberLookup.TypeParameters.Length).ToArray();
                typeForMemberLookup = typeForMemberLookup.Construct(typeArgs);
            }
        }

        // First try to find methods
        var methodMembers = typeForMemberLookup.GetMembers(attributeData.MethodName)
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
            propertyMembers = typeForMemberLookup.GetMembers(attributeData.MethodName)
                .OfType<IPropertySymbol>()
                .Where(p => p.Type.SpecialType == SpecialType.System_Boolean &&
                    p is { GetMethod: not null, IsStatic: false })
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
            // For unbound generic types, we'll generate generic methods even if we can't find the property now
            // The property will exist on the constructed type at runtime
            if (!attributeData.IsUnboundGeneric)
            {
                missingMethodMessage = $"No boolean method '{attributeData.MethodName}' found on type '{attributeData.ContainingType.ToDisplayString()}'";
            }
            return ImmutableEquatableArray<ConditionClassModel>.Empty;
        }

        foreach (var method in matchingMethods)
        {
            var className = GenerateAssertConditionClassNameForMethod(attributeData.TargetType, attributeData.ContainingType, attributeData.MethodName, method);

            // Duplicate class names are dropped at emit time, where the set of classes
            // already written across every extension class is known.
            var classBuilder = new StringBuilder();
            GenerateAssertConditionClassForMethod(classBuilder, attributeData, method);
            conditionClasses.Add(new ConditionClassModel(className, classBuilder.ToString()));
        }

        return conditionClasses.ToImmutableEquatableArray();
    }

    private static string RenderExtensionMethods(CreateAssertionAttributeData attributeData)
    {
        var sourceBuilder = new StringBuilder();

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
                SymbolEqualityComparer.Default.Equals(p.ContainingType, attributeData.TargetType));

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
            GenerateMethodsForSpecificOverload(sourceBuilder, attributeData, method);
        }

        return sourceBuilder.ToString();
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

    private static void GenerateAssertConditionClassForMethod(StringBuilder sourceBuilder, CreateAssertionAttributeData attributeData, IMethodSymbol staticMethod)
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

        // Collect generic constraints from the method
        var genericConstraints = CollectGenericConstraints(staticMethod);

        // Determine if we need to generate a generic assertion class
        var hasMethodTypeParameters = staticMethod.IsGenericMethod && staticMethod.TypeParameters.Length > 0;
        string? genericTypeParams = null;
        if (hasMethodTypeParameters)
        {
            genericTypeParams = $"<{string.Join(", ", staticMethod.TypeParameters.Select(tp => tp.Name))}>";
        }

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
        else if (attributeData.IsUnboundGeneric)
        {
            // For unbound generic types like Memory<T>, generate generic assertion class
            // Generate type parameter names: T, T1, T2, etc.
            var typeParamCount = attributeData.TargetType.TypeParameters.Length;
            var typeParamNames = typeParamCount == 1 ? new[] { "T" } :
                                 Enumerable.Range(1, typeParamCount).Select(i => $"T{i.ToString(CultureInfo.InvariantCulture)}").ToArray();
            var typeParamsList = string.Join(", ", typeParamNames);

            // Generate assertion class with generic parameters
            // Example: public class MemoryIsEmptyAssertion<T> : Assertion<Memory<T>>
            var unboundTypeName = attributeData.TargetType.Name;  // e.g., "Memory"
            var constructedTargetType = $"{unboundTypeName}<{typeParamsList}>";  // e.g., "Memory<T>"
            sourceBuilder.AppendLine($"public class {className}<{typeParamsList}> : Assertion<{constructedTargetType}>");
        }
        else if (hasMethodTypeParameters)
        {
            // Generate generic assertion class with the method's type parameters
            sourceBuilder.AppendLine($"public class {className}{genericTypeParams} : Assertion<{targetTypeName}>");
            // Apply constraints to the assertion class
            if (genericConstraints.Count > 0)
            {
                foreach (var constraint in genericConstraints)
                {
                    sourceBuilder.AppendLine($"    {constraint}");
                }
            }
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
        else if (hasMethodTypeParameters)
        {
            // Use target type with no generic parameters from method (method generics are separate)
            contextType = $"AssertionContext<{targetTypeName}>";
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
        else if (hasMethodTypeParameters)
        {
            // Use target type with no generic parameters from method
            metadataType = $"EvaluationMetadata<{targetTypeName}>";
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

        // Check if the method's first parameter accepts null values
        // For static methods like string.IsNullOrEmpty(string? value), the first parameter
        // is the value being asserted. If it's marked as nullable (NullableAnnotation.Annotated),
        // we should skip the null check and let the method handle null values.
        var shouldGenerateNullCheck = !attributeData.TargetType.IsValueType;
        if (shouldGenerateNullCheck && staticMethod.Parameters.Length > 0)
        {
            var firstParameter = staticMethod.Parameters[0];
            // Skip null check if the parameter explicitly accepts null (e.g., string? value)
            if (firstParameter.NullableAnnotation == NullableAnnotation.Annotated)
            {
                shouldGenerateNullCheck = false;
            }
            // For backwards compatibility with .NET Framework where NullableAnnotation might not be set,
            // also check for well-known methods that accept null by design
            else if (methodName == "IsNullOrEmpty" || methodName == "IsNullOrWhiteSpace")
            {
                shouldGenerateNullCheck = false;
            }
        }

        if (shouldGenerateNullCheck)
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
                sourceBuilder.AppendJoin(", ", paramList);
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
                    var propertyName = methodName.StartsWith("get_", StringComparison.Ordinal) ? methodName.Substring(4) : methodName;
                    sourceBuilder.AppendLine($"        var result = actualValue.{propertyName};");
                }
                else
                {
                    // Instance method on the target type itself
                    sourceBuilder.Append($"        var result = actualValue.{methodName}(");
                    var paramList = parameters.Select(p => $"_{p.Name}").ToArray();
                    sourceBuilder.AppendJoin(", ", paramList);
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
                    sourceBuilder.AppendJoin(", ", paramList);
                    sourceBuilder.AppendLine(");");
                }
            }
            else
            {
                // Default instance method behavior
                sourceBuilder.Append($"        var result = actualValue.{methodName}(");
                var paramList = parameters.Select(p => $"_{p.Name}").ToArray();
                sourceBuilder.AppendJoin(", ", paramList);
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
            sourceBuilder.AppendLine($"        return $\"{RenderCustomExpectation(attributeData.ExpectationMessage!, parameters)}\";");
        }
        else
        {
            // Use default expectation message
            var arguments = parameters.Any()
                ? $"({string.Join(", ", parameters.Select(p => $"{{_{p.Name}}}"))})"
                : string.Empty;
            sourceBuilder.AppendLine($"        return $\"{RenderNegatableExpectation($"satisfy {methodName}{arguments}")}\";");
        }

        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine("}");
        sourceBuilder.AppendLine();
    }

    /// <summary>
    /// Renders the interpolated-string body of <c>GetExpectation()</c> for a custom <c>ExpectationMessage</c>.
    /// </summary>
    /// <remarks>
    /// The canonical form is the positive phrase without the leading "to" ("be null or empty"); the generator adds
    /// "to" and, with <c>NegateLogic</c>, "not". The documented style that spells these out ("to be null or empty",
    /// or "to not contain '{value}'" with <c>NegateLogic</c>) is also accepted, so neither renders as "to to ..." or
    /// "to not not ...".
    /// </remarks>
    private static string RenderCustomExpectation(string expectationMessage, IParameterSymbol[] parameters)
    {
        var expectation = expectationMessage.StartsWith("to ", StringComparison.Ordinal)
            ? expectationMessage.Substring("to ".Length)
            : expectationMessage;

        var phrase = RenderMessageText(expectation, parameters);

        return expectation.StartsWith("not ", StringComparison.Ordinal)
            ? $"to {phrase}"
            : RenderNegatableExpectation(phrase);
    }

    /// <summary>
    /// The one place the expectation's "to"/"to not" wording is decided, for custom and default messages alike.
    /// </summary>
    private static string RenderNegatableExpectation(string phrase) => $"to {{(_negated ? \"not \" : \"\")}}{phrase}";

    /// <summary>
    /// Renders message text as the body of an interpolated string literal in a single pass: a <c>{name}</c>
    /// placeholder for a method parameter (any valid identifier, not only ASCII) becomes the generated
    /// <c>{_name}</c> field, as does <c>{_name}</c> itself, which messages could reference before placeholders were
    /// parsed. Everything else stays literal text, including escaped braces (<c>{{name}}</c> prints
    /// <c>{name}</c>), placeholders that name no parameter, stray braces, quotes and backslashes.
    /// </summary>
    private static string RenderMessageText(string message, IParameterSymbol[] parameters)
    {
        var parameterNames = new HashSet<string>(parameters.Select(p => p.Name), StringComparer.Ordinal);
        var result = new StringBuilder(message.Length);
        var literalStart = 0;

        foreach (Match match in MessageTokenRegex.Matches(message))
        {
            result.Append(FormatLiteral(message.Substring(literalStart, match.Index - literalStart)));

            var name = match.Groups["name"];
            var parameterName = !name.Success ? null
                : parameterNames.Contains(name.Value) ? name.Value
                : name.Value.StartsWith("_", StringComparison.Ordinal) && parameterNames.Contains(name.Value.Substring(1)) ? name.Value.Substring(1)
                : null;

            if (parameterName is not null)
            {
                result.Append("{_").Append(parameterName).Append('}');
            }
            else if (name.Success)
            {
                // A placeholder that names no parameter: keep its text, braces included, literal
                result.Append("{{").Append(FormatLiteral(name.Value)).Append("}}");
            }
            else
            {
                // An escaped brace pair or a stray brace: keep the brace literal
                result.Append(match.Value[0] is '{' ? "{{" : "}}");
            }

            literalStart = match.Index + match.Length;
        }

        result.Append(FormatLiteral(message.Substring(literalStart)));
        return result.ToString();
    }

    // Any brace-free text between single braces is a placeholder candidate; it is only substituted when it names a
    // parameter, so matching broadly keeps non-ASCII identifiers working without a hand-written identifier grammar
    private static readonly Regex MessageTokenRegex = new(@"\{\{|\}\}|\{(?<name>[^{}]*)\}|[{}]");

    // With quote: false, FormatLiteral leaves double quotes unescaped, so quote and strip the outer pair instead
    private static string FormatLiteral(string text)
    {
        var literal = SymbolDisplay.FormatLiteral(text, quote: true);
        return literal.Substring(1, literal.Length - 2);
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
                    parameterSuffix += $"And{(method.Parameters.Length - 1).ToString(CultureInfo.InvariantCulture)}More";
                }
            }
            else if (method.Parameters.Length > 1)
            {
                parameterSuffix = $"With{method.Parameters.Length.ToString(CultureInfo.InvariantCulture)}Parameters";
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

    /// <summary>
    /// Collects generic constraints from method type parameters.
    /// Returns a list of constraint strings in the format "where T : constraint1, constraint2"
    /// </summary>
    private static List<string> CollectGenericConstraints(IMethodSymbol method)
    {
        var constraints = new List<string>();

        if (!method.IsGenericMethod || method.TypeParameters.Length == 0)
        {
            return constraints;
        }

        foreach (var typeParameter in method.TypeParameters)
        {
            var typeConstraints = new List<string>();

            if (typeParameter.HasReferenceTypeConstraint)
            {
                typeConstraints.Add("class");
            }
            // 'unmanaged' also sets HasValueTypeConstraint; 'struct, unmanaged' is CS0449 (#6471)
            if (typeParameter.HasValueTypeConstraint && !typeParameter.HasUnmanagedTypeConstraint)
            {
                typeConstraints.Add("struct");
            }
            if (typeParameter.HasUnmanagedTypeConstraint)
            {
                typeConstraints.Add("unmanaged");
            }
            if (typeParameter.HasNotNullConstraint)
            {
                typeConstraints.Add("notnull");
            }
            foreach (var constraintType in typeParameter.ConstraintTypes)
            {
                typeConstraints.Add(constraintType.ToDisplayString());
            }
            if (typeParameter.HasConstructorConstraint)
            {
                typeConstraints.Add("new()");
            }

            if (typeConstraints.Count > 0)
            {
                constraints.Add($"where {typeParameter.Name} : {string.Join(", ", typeConstraints)}");
            }
        }

        return constraints;
    }

    private static void GenerateMethodsForSpecificOverload(StringBuilder sourceBuilder, CreateAssertionAttributeData attributeData, IMethodSymbol staticMethod)
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

        // Collect generic constraints from the method
        var genericConstraints = CollectGenericConstraints(method);

        // Determine if the method has generic type parameters
        var hasMethodTypeParameters = method.IsGenericMethod && method.TypeParameters.Length > 0;
        string? methodGenericTypeParams = null;
        if (hasMethodTypeParameters)
        {
            methodGenericTypeParams = $"<{string.Join(", ", method.TypeParameters.Select(tp => tp.Name))}>";
        }

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
        else if (attributeData.IsUnboundGeneric)
        {
            // For unbound generic types like Memory<T>, generate generic extension method
            var typeParamCount = attributeData.TargetType.TypeParameters.Length;
            var typeParamNames = typeParamCount == 1 ? new[] { "T" } :
                                 Enumerable.Range(1, typeParamCount).Select(i => $"T{i.ToString(CultureInfo.InvariantCulture)}").ToArray();
            var typeParamsList = string.Join(", ", typeParamNames);

            var unboundTypeName = attributeData.TargetType.Name;
            var constructedTargetType = $"{unboundTypeName}<{typeParamsList}>";
            // Example: public static MemoryIsEmptyAssertion<T> IsEmpty<T>(this IAssertionSource<Memory<T>> source
            sourceBuilder.Append($"    public static {assertConditionClassName}<{typeParamsList}> {generatedMethodName}<{typeParamsList}>(this IAssertionSource<{constructedTargetType}> source");
        }
        else if (hasMethodTypeParameters)
        {
            // Method has generic type parameters - include them in the extension method
            sourceBuilder.Append($"    public static {assertConditionClassName}{methodGenericTypeParams} {generatedMethodName}{methodGenericTypeParams}(this IAssertionSource<{targetTypeName}> source");
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
        else if (hasMethodTypeParameters && genericConstraints.Count > 0)
        {
            // Add generic constraints from the method
            sourceBuilder.AppendLine();
            sourceBuilder.Append($"        {string.Join(' ', genericConstraints)}");
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
        else if (hasMethodTypeParameters)
        {
            // Include generic type parameters in the assertion class instantiation
            sourceBuilder.Append($"        return new {assertConditionClassName}{methodGenericTypeParams}(source.Context");
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

    /// <summary>
    /// Pipeline model for one attributed class. Holds only strings so it compares by value
    /// across compilations and never roots a compilation.
    /// </summary>
    private sealed record AssertionClassModel(
        string ClassKey,
        string ClassName,
        string? OriginalNamespace,
        ImmutableEquatableArray<AssertionEntryModel> Entries
    );

    /// <summary>
    /// Pre-rendered output for one [AssertionFrom] attribute: its assertion condition classes,
    /// the "method not found" message when nothing matched, and its extension methods.
    /// </summary>
    /// <remarks>
    /// Rendering in the transform is deliberate: the emission code is bound to symbols, and
    /// strings give cheap value equality. The rendered text must stay deterministic (no
    /// timestamps, no hash-ordered collections), or every run would compare as modified.
    /// </remarks>
    private sealed record AssertionEntryModel(
        ImmutableEquatableArray<ConditionClassModel> ConditionClasses,
        string? MissingMethodMessage,
        string ExtensionMethodsSource
    );

    private sealed record ConditionClassModel(
        string ClassName,
        string Source
    );

    /// <summary>
    /// Symbol-based attribute data. Only used inside the transform; never flows through the pipeline.
    /// </summary>
    private record CreateAssertionAttributeData(
        INamedTypeSymbol TargetType,
        INamedTypeSymbol ContainingType,
        string MethodName,
        string? CustomName,
        bool NegateLogic,
        bool RequiresGenericTypeParameter,
        bool TreatAsInstance,
        string? ExpectationMessage,
        bool IsUnboundGeneric
    );
}
