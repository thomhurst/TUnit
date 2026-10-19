using TUnit.Core.SourceGenerator.Extensions;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
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

                var isUnboundGeneric = NormalizeUnboundGenerics(ref targetType, ref containingType);

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

                var isUnboundGeneric = NormalizeUnboundGenerics(ref targetType, ref containingType);

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

    /// <summary>
    /// Replaces an unbound generic type such as <c>typeof(Memory&lt;&gt;)</c> with its original definition
    /// (<c>Memory&lt;T&gt;</c>). An unbound type has no members to look up and cannot be named in generated code;
    /// its definition has both, and the generated assertion is made generic over the definition's type parameters.
    /// </summary>
    /// <returns>Whether either type was an unbound generic type.</returns>
    private static bool NormalizeUnboundGenerics(ref INamedTypeSymbol targetType, ref INamedTypeSymbol containingType)
    {
        var isUnboundGeneric = false;

        if (targetType.IsUnboundGenericType)
        {
            targetType = targetType.OriginalDefinition;
            isUnboundGeneric = true;
        }

        if (containingType.IsUnboundGenericType)
        {
            containingType = containingType.OriginalDefinition;
            isUnboundGeneric = true;
        }

        return isUnboundGeneric;
    }

    /// <summary>
    /// The type parameters a generated assertion for an unbound generic target is generic over: the target
    /// definition's own (e.g. <c>T</c> of <c>Memory&lt;T&gt;</c>), including those of any generic containing types,
    /// followed by the method's.
    /// </summary>
    private static GeneratedTypeParameters GetUnboundGenericTypeParameters(INamedTypeSymbol targetType, IMethodSymbol method)
    {
        var typeParameters = new List<ITypeParameterSymbol>();
        var outerTypes = new Stack<INamedTypeSymbol>();
        for (var type = targetType; type != null; type = type.ContainingType)
        {
            outerTypes.Push(type);
        }

        while (outerTypes.Count > 0)
        {
            typeParameters.AddRange(outerTypes.Pop().TypeParameters);
        }

        typeParameters.AddRange(method.TypeParameters);
        return new GeneratedTypeParameters(typeParameters);
    }

    /// <summary>
    /// An unbound generic type or a member it declares is only supported when the member is declared on that same
    /// generic type, because the generated assertion can only be made generic over the target's type parameters.
    /// </summary>
    private static bool IsUnsupportedUnboundGeneric(CreateAssertionAttributeData attributeData) =>
        attributeData.IsUnboundGeneric &&
        !SymbolEqualityComparer.Default.Equals(attributeData.ContainingType, attributeData.TargetType);

    /// <summary>
    /// The type parameters of a generated generic assertion, with a distinct name for each. They are collected from
    /// several declarations (<c>Box&lt;T&gt;</c> and its method <c>Holds&lt;T&gt;</c>, or <c>Outer&lt;T&gt;.Inner&lt;T&gt;</c>),
    /// which may reuse a name; a reused name gets a numeric suffix and every type rendered through
    /// <see cref="Display"/> refers to it by that name.
    /// </summary>
    private sealed class GeneratedTypeParameters
    {
        private readonly Dictionary<ISymbol, string> _names = new(SymbolEqualityComparer.Default);

        public GeneratedTypeParameters(List<ITypeParameterSymbol> typeParameters)
        {
            TypeParameters = typeParameters;

            var declaredNames = new HashSet<string>(typeParameters.Select(tp => tp.Name), StringComparer.Ordinal);
            var usedNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var typeParameter in typeParameters)
            {
                // A generated name never equals a declared one, so it cannot collide with a later parameter
                var name = typeParameter.Name;
                var suffix = 0;
                while (usedNames.Contains(name))
                {
                    do
                    {
                        suffix++;
                        name = typeParameter.Name + suffix.ToString(CultureInfo.InvariantCulture);
                    }
                    while (declaredNames.Contains(name));
                }

                usedNames.Add(name);
                _names[typeParameter] = name;
            }

            TypeArgumentList = $"<{string.Join(", ", typeParameters.Select(NameOf))}>";
        }

        public List<ITypeParameterSymbol> TypeParameters { get; }

        /// <summary>E.g. <c>&lt;T, T1&gt;</c>.</summary>
        public string TypeArgumentList { get; }

        public string NameOf(ITypeParameterSymbol typeParameter) =>
            _names.TryGetValue(typeParameter, out var name) ? name : typeParameter.Name;

        /// <summary>Renders <paramref name="type"/> like <c>ToDisplayString()</c>, using the distinct type parameter names.</summary>
        public string Display(ITypeSymbol type)
        {
            var builder = new StringBuilder();
            foreach (var part in type.ToDisplayParts())
            {
                builder.Append(part is { Kind: SymbolDisplayPartKind.TypeParameterName, Symbol: ITypeParameterSymbol typeParameter }
                    ? NameOf(typeParameter)
                    : part.ToString());
            }

            return builder.ToString();
        }
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
        var allGeneratedClasses = new GeneratedClassRegistry();

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
        GeneratedClassRegistry allGeneratedClasses)
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

        // Generate all assert condition classes first. Each class was rendered with a placeholder in place of its
        // name, so the final name is substituted here, both in the class and in the extension methods that use it.
        // A name that is already used by a different member (Box<T>.IsEmpty in two namespaces) gets a prefix.
        var extensionSources = new string[entries.Length];
        for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
        {
            var entry = entries[entryIndex];
            var extensionSource = entry.ExtensionMethodsSource;

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

            foreach (var conditionClass in entry.ConditionClasses)
            {
                var name = allGeneratedClasses.Claim(conditionClass, out var isNew);

                extensionSource = extensionSource.Replace(conditionClass.Placeholder, name);

                if (isNew)
                {
                    sourceBuilder.Append(conditionClass.Source.Replace(conditionClass.Placeholder, name));
                }
            }

            extensionSources[entryIndex] = ReplaceRemainingPlaceholders(extensionSource);
        }

        // Generate extension methods class
        sourceBuilder.AppendLine($"public static partial class {className}");
        sourceBuilder.AppendLine("{");

        foreach (var extensionSource in extensionSources)
        {
            sourceBuilder.Append(extensionSource);
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

        // An unbound generic type was replaced by its definition (Memory<T>), so its members can be looked up
        // directly. The generated assertion can only be made generic over the target's type parameters, so the
        // member has to be declared on that same generic type; anything else is reported instead of silently
        // generating nothing.
        if (IsUnsupportedUnboundGeneric(attributeData))
        {
            missingMethodMessage = GetUnsupportedUnboundGenericMessage(attributeData);
            return ImmutableEquatableArray<ConditionClassModel>.Empty;
        }

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
            missingMethodMessage = $"No boolean method '{attributeData.MethodName}' found on type '{attributeData.ContainingType.ToDisplayString()}'";
            return ImmutableEquatableArray<ConditionClassModel>.Empty;
        }

        foreach (var method in matchingMethods)
        {
            var className = GenerateAssertConditionClassNameForMethod(attributeData.TargetType, attributeData.ContainingType, attributeData.MethodName, method);

            // Duplicate class names are dropped at emit time, where the set of classes
            // already written across every extension class is known.
            // The arity is part of the key: MemoryIsEmptyAssertion<T> (from typeof(Memory<>)) and
            // MemoryIsEmptyAssertion (from typeof(Memory<int>)) are different classes and can both be needed.
            var classBuilder = new StringBuilder();
            var arity = GenerateAssertConditionClassForMethod(classBuilder, attributeData, method);
            conditionClasses.Add(new ConditionClassModel(
                className,
                GetClassNamePlaceholder(className, method),
                arity,
                GetConditionClassIdentity(attributeData, method, arity),
                GetConditionClassDisambiguator(attributeData),
                classBuilder.ToString()));
        }

        return conditionClasses.ToImmutableEquatableArray();
    }

    private static string GetUnsupportedUnboundGenericMessage(CreateAssertionAttributeData attributeData) =>
        $"[AssertionFrom] cannot generate '{attributeData.MethodName}': an unbound generic type is only supported when " +
        $"the method or property is declared on that same generic type, e.g. [AssertionFrom(typeof(Memory<>), nameof(Memory<int>.IsEmpty))], " +
        $"but the target type is '{attributeData.TargetType.ToDisplayString()}' and the containing type is '{attributeData.ContainingType.ToDisplayString()}'";

    private static string RenderExtensionMethods(CreateAssertionAttributeData attributeData)
    {
        var sourceBuilder = new StringBuilder();

        if (IsUnsupportedUnboundGeneric(attributeData))
        {
            // Reported by RenderConditionClasses
            return string.Empty;
        }

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

    /// <returns>The number of type parameters of the generated class.</returns>
    private static int GenerateAssertConditionClassForMethod(StringBuilder sourceBuilder, CreateAssertionAttributeData attributeData, IMethodSymbol staticMethod)
    {
        // For an unbound generic target, types are rendered with the class's distinct type parameter names
        var unboundTypeParameters = attributeData.IsUnboundGeneric
            ? GetUnboundGenericTypeParameters(attributeData.TargetType, staticMethod)
            : null;
        string DisplayType(ITypeSymbol type) => unboundTypeParameters?.Display(type) ?? type.ToDisplayString();

        var targetTypeName = DisplayType(attributeData.TargetType);
        var containingTypeName = DisplayType(attributeData.ContainingType);
        int arity;
        var methodName = attributeData.MethodName;

        var className = GetClassNamePlaceholder(
            GenerateAssertConditionClassNameForMethod(attributeData.TargetType, attributeData.ContainingType, methodName, staticMethod),
            staticMethod);

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

        // A generic method is called with explicit type arguments, since they cannot always be inferred from its
        // arguments (bool Matches<U>()). Only the classes below that declare the method's type parameters do this.
        var methodTypeArguments = "";

        // For Enum.IsDefined, we need to use a generic type parameter instead of the concrete Enum type
        if (attributeData is { RequiresGenericTypeParameter: true, TargetType.Name: "Enum" })
        {
            // Generate: public class EnumIsDefinedAssertion<T> : Assertion<T> where T : Enum
            sourceBuilder.AppendLine($"public class {className}<T> : Assertion<T>");
            sourceBuilder.AppendLine($"    where T : Enum");
            arity = 1;
        }
        else if (isTaskType)
        {
            // Generate: public class TaskIsCompletedAssertion<TTask> : Assertion<TTask> where TTask : System.Threading.Tasks.Task
            sourceBuilder.AppendLine($"public class {className}<TTask> : Assertion<TTask>");
            sourceBuilder.AppendLine($"    where TTask : {targetTypeName}");
            arity = 1;
        }
        else if (unboundTypeParameters != null)
        {
            // For unbound generic types like Memory<>, the target is the definition (Memory<T>), so the class is
            // generic over its type parameters: public class MemoryIsEmptyAssertion<T> : Assertion<System.Memory<T>>
            sourceBuilder.AppendLine($"public class {className}{unboundTypeParameters.TypeArgumentList} : Assertion<{targetTypeName}>");
            foreach (var constraint in CollectGenericConstraints(unboundTypeParameters.TypeParameters, unboundTypeParameters))
            {
                sourceBuilder.AppendLine($"    {constraint}");
            }

            arity = unboundTypeParameters.TypeParameters.Count;
            if (hasMethodTypeParameters)
            {
                methodTypeArguments = $"<{string.Join(", ", staticMethod.TypeParameters.Select(unboundTypeParameters.NameOf))}>";
            }
        }
        else if (hasMethodTypeParameters)
        {
            arity = staticMethod.TypeParameters.Length;
            methodTypeArguments = genericTypeParams!;

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
            arity = 0;
        }
        sourceBuilder.AppendLine("{");

        foreach (var param in parameters)
        {
            sourceBuilder.AppendLine($"    private readonly {DisplayType(param.Type)} _{param.Name};");
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
            sourceBuilder.Append($", {DisplayType(param.Type)} {param.Name}");
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
                sourceBuilder.Append($"        var result = actualValue.{methodName}{methodTypeArguments}(");
                var paramList = parameters.Select(p => $"_{p.Name}").ToArray();
                sourceBuilder.AppendJoin(", ", paramList);
                sourceBuilder.AppendLine(");");
            }
            else if (attributeData.TreatAsInstance)
            {
                // Static method treated as instance - call the static method with actualValue as first parameter
                sourceBuilder.Append($"        var result = {containingTypeName}.{methodName}{methodTypeArguments}(actualValue");
                foreach (var param in parameters)
                {
                    sourceBuilder.Append($", _{param.Name}");
                }
                sourceBuilder.AppendLine(");");
            }
            else if (attributeData.RequiresGenericTypeParameter)
            {
                sourceBuilder.Append($"        var result = {containingTypeName}.{methodName}{methodTypeArguments}(typeof(T), actualValue");
                foreach (var param in parameters)
                {
                    sourceBuilder.Append($", _{param.Name}");
                }
                sourceBuilder.AppendLine(");");
            }
            else
            {
                sourceBuilder.Append($"        var result = {containingTypeName}.{methodName}{methodTypeArguments}(actualValue");
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
                    sourceBuilder.Append($"        var result = actualValue.{methodName}{methodTypeArguments}(");
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
                    sourceBuilder.AppendLine($"        var instance = new {containingTypeName}();");
                    sourceBuilder.Append($"        var result = instance.{methodName}{methodTypeArguments}(actualValue");
                    foreach (var param in parameters.Skip(1))
                    {
                        sourceBuilder.Append($", _{param.Name}");
                    }
                    sourceBuilder.AppendLine(");");
                }
                else
                {
                    // Instance method that doesn't take the target type - might be a validation method
                    sourceBuilder.AppendLine($"        var instance = new {containingTypeName}();");
                    sourceBuilder.Append($"        var result = instance.{methodName}{methodTypeArguments}(");
                    var paramList = parameters.Select(p => $"_{p.Name}").ToArray();
                    sourceBuilder.AppendJoin(", ", paramList);
                    sourceBuilder.AppendLine(");");
                }
            }
            else
            {
                // Default instance method behavior
                sourceBuilder.Append($"        var result = actualValue.{methodName}{methodTypeArguments}(");
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

        return arity;
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
        if (!method.IsGenericMethod || method.TypeParameters.Length == 0)
        {
            return new List<string>();
        }

        return CollectGenericConstraints(method.TypeParameters);
    }

    /// <param name="names">When given, renders type parameters with these distinct names.</param>
    private static List<string> CollectGenericConstraints(IEnumerable<ITypeParameterSymbol> typeParameters, GeneratedTypeParameters? names = null)
    {
        var constraints = new List<string>();

        foreach (var typeParameter in typeParameters)
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
                typeConstraints.Add(names?.Display(constraintType) ?? constraintType.ToDisplayString());
            }
            if (typeParameter.HasConstructorConstraint)
            {
                typeConstraints.Add("new()");
            }

            if (typeConstraints.Count > 0)
            {
                constraints.Add($"where {names?.NameOf(typeParameter) ?? typeParameter.Name} : {string.Join(", ", typeConstraints)}");
            }
        }

        return constraints;
    }

    private static void GenerateMethodsForSpecificOverload(StringBuilder sourceBuilder, CreateAssertionAttributeData attributeData, IMethodSymbol staticMethod)
    {
        var targetTypeName = attributeData.TargetType.ToDisplayString();
        var methodName = attributeData.MethodName;

        var baseMethodName = attributeData.CustomName ?? methodName;
        var className = GetClassNamePlaceholder(
            GenerateAssertConditionClassNameForMethod(attributeData.TargetType, attributeData.ContainingType, methodName, staticMethod),
            staticMethod);

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

        // For an unbound generic target, types are rendered with the assertion class's distinct type parameter names
        string? unboundGenericTypeParams = null;
        GeneratedTypeParameters? unboundTypeParameters = null;
        if (attributeData.IsUnboundGeneric && !isTaskType)
        {
            unboundTypeParameters = GetUnboundGenericTypeParameters(attributeData.TargetType, method);
            unboundGenericTypeParams = unboundTypeParameters.TypeArgumentList;
            genericConstraints = CollectGenericConstraints(unboundTypeParameters.TypeParameters, unboundTypeParameters);
            targetTypeName = unboundTypeParameters.Display(attributeData.TargetType);
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
        else if (unboundGenericTypeParams != null)
        {
            // For unbound generic types like Memory<>, the target is the definition (Memory<T>), so the extension is
            // generic over its type parameters, which are inferred from the source:
            // public static MemoryIsEmptyAssertion<T> IsEmpty<T>(this IAssertionSource<System.Memory<T>> source)
            sourceBuilder.Append($"    public static {assertConditionClassName}{unboundGenericTypeParams} {generatedMethodName}{unboundGenericTypeParams}(this IAssertionSource<{targetTypeName}> source");
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
            sourceBuilder.Append($", {unboundTypeParameters?.Display(param.Type) ?? param.Type.ToDisplayString()} {param.Name}");
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
        else if ((hasMethodTypeParameters || unboundGenericTypeParams != null) && genericConstraints.Count > 0)
        {
            // Add generic constraints from the method (and, for unbound generic targets, the target type)
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
        else if (unboundGenericTypeParams != null)
        {
            sourceBuilder.Append($"        return new {assertConditionClassName}{unboundGenericTypeParams}(source.Context");
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

    /// <param name="Name">The class's name before any collision is resolved.</param>
    /// <param name="Placeholder">The text standing in for the class name in the rendered class and extension methods.</param>
    /// <param name="Arity">The class's generic arity. Classes with the same name and different arities can coexist.</param>
    /// <param name="Identity">The member the class asserts on: target type, containing type, member and arity. Attributes with the same identity share one class.</param>
    /// <param name="Disambiguator">A prefix used to rename the class when a different identity already took its name.</param>
    /// <param name="Source">The rendered class.</param>
    private sealed record ConditionClassModel(
        string Name,
        string Placeholder,
        int Arity,
        string Identity,
        string Disambiguator,
        string Source
    );

    /// <summary>
    /// Gives each generated condition class a unique name across every extension class in the compilation.
    /// Attributes that assert on the same member share one class. A different member that would get the same
    /// name, such as <c>Box&lt;T&gt;.IsEmpty</c> declared in two namespaces, gets a prefixed name. Dropping it
    /// instead would leave its extension method constructing the other member's class.
    /// </summary>
    private sealed class GeneratedClassRegistry
    {
        private readonly HashSet<string> _takenKeys = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _nameByIdentity = new(StringComparer.Ordinal);

        /// <param name="conditionClass">The rendered class.</param>
        /// <param name="isNew">Whether the class still has to be emitted.</param>
        /// <returns>The name to use for the class.</returns>
        public string Claim(ConditionClassModel conditionClass, out bool isNew)
        {
            if (_nameByIdentity.TryGetValue(conditionClass.Identity, out var existingName))
            {
                isNew = false;
                return existingName;
            }

            var name = conditionClass.Name;
            if (_takenKeys.Contains(Key(name, conditionClass.Arity)))
            {
                var prefixed = conditionClass.Disambiguator + conditionClass.Name;
                name = prefixed;
                for (var suffix = 2; _takenKeys.Contains(Key(name, conditionClass.Arity)); suffix++)
                {
                    name = prefixed + suffix.ToString(CultureInfo.InvariantCulture);
                }
            }

            _takenKeys.Add(Key(name, conditionClass.Arity));
            _nameByIdentity.Add(conditionClass.Identity, name);
            isNew = true;
            return name;
        }

        private static string Key(string name, int arity) => $"{name}`{arity.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string GetConditionClassIdentity(CreateAssertionAttributeData attributeData, IMethodSymbol method, int arity) =>
        string.Join("|",
            attributeData.TargetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            attributeData.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            method.ToDisplayString(),
            arity.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// The namespace, and any containing types, of the type that declares the member, as an identifier prefix.
    /// For example <c>Repro.Two</c> becomes <c>ReproTwo</c>.
    /// </summary>
    private static string GetConditionClassDisambiguator(CreateAssertionAttributeData attributeData)
    {
        var container = attributeData.ContainingType.ContainingSymbol;
        if (container is null or INamespaceSymbol { IsGlobalNamespace: true })
        {
            return "Global";
        }

        var builder = new StringBuilder();
        foreach (var character in container.ToDisplayString())
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                builder.Append(builder.Length == 0 ? char.ToUpperInvariant(character) : character);
            }
        }

        return builder.Length == 0 || char.IsDigit(builder[0]) ? "Global" + builder : builder.ToString();
    }

    private const char PlaceholderStart = '\u0001';
    private const char PlaceholderSeparator = '\u0002';
    private const char PlaceholderEnd = '\u0003';

    /// <summary>
    /// The text rendered in place of a condition class name until its final name is known. It is keyed by the
    /// member, so each overload is substituted separately, and it is made of control characters that never occur
    /// in generated code, so substituting it cannot touch string literals or other identifiers.
    /// </summary>
    private static string GetClassNamePlaceholder(string className, IMethodSymbol method) =>
        $"{PlaceholderStart}{className}{PlaceholderSeparator}{method.ToDisplayString()}{PlaceholderEnd}";

    /// <summary>
    /// Replaces placeholders that no condition class claimed with their original class name. That happens when an
    /// extension method is rendered for a member that its attribute rendered no condition class for.
    /// </summary>
    private static string ReplaceRemainingPlaceholders(string source)
    {
        var start = source.IndexOf(PlaceholderStart);
        if (start < 0)
        {
            return source;
        }

        var builder = new StringBuilder(source.Length);
        var position = 0;
        while (start >= 0)
        {
            var separator = source.IndexOf(PlaceholderSeparator, start);
            var end = source.IndexOf(PlaceholderEnd, start);
            builder.Append(source, position, start - position);
            builder.Append(source, start + 1, separator - start - 1);
            position = end + 1;
            start = source.IndexOf(PlaceholderStart, position);
        }

        builder.Append(source, position, source.Length - position);
        return builder.ToString();
    }

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
