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
using Microsoft.CodeAnalysis.Text;
using TUnit.Assertions.SourceGenerator.Models;

namespace TUnit.Assertions.SourceGenerator.Generators;

[Generator]
public sealed class AssertionMethodGenerator : IIncrementalGenerator
{
    public const string NonGenericAssertionFromStep = "NonGenericAssertionFrom";
    public const string GenericAssertionFromStep = "GenericAssertionFrom";

    private static readonly DiagnosticDescriptor MethodNotFound = new(
        "TU0001",
        "Method not found",
        "{0}",
        "TUnit.Assertions",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

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

        RegisterMissingMethodDiagnostics(context, nonGenericAssertionFromData);
        RegisterMissingMethodDiagnostics(context, genericAssertionFromData);
    }

    /// <summary>
    /// Reports each TU0001 in the current compilation's syntax tree so that #pragma and per-file .editorconfig
    /// severities still apply; a path-only location has no SourceTree to read them from. Pairing each diagnostic
    /// (not the collected array) with the compilation means this output only re-runs while there are diagnostics,
    /// and the source output above stays independent of the compilation.
    /// </summary>
    private static void RegisterMissingMethodDiagnostics(
        IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<AssertionClassModel> models)
    {
        var diagnostics = models.SelectMany(static (model, _) => model.Entries
            .Where(static entry => entry.MissingMethodDiagnostic is not null)
            .Select(static entry => entry.MissingMethodDiagnostic!));

        context.RegisterSourceOutput(diagnostics.Combine(context.CompilationProvider), static (ctx, data) =>
            ctx.ReportDiagnostic(data.Left.ToDiagnostic(data.Right)));
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
                    isUnboundGeneric,
                    attributeData.ApplicationSyntaxReference
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
                    isUnboundGeneric,
                    attributeData.ApplicationSyntaxReference
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
                _names[typeParameter] = EscapeIdentifier(name);
            }

            TypeArgumentList = $"<{string.Join(", ", typeParameters.Select(NameOf))}>";
        }

        public List<ITypeParameterSymbol> TypeParameters { get; }

        /// <summary>E.g. <c>&lt;T, T1&gt;</c>.</summary>
        public string TypeArgumentList { get; }

        public string NameOf(ITypeParameterSymbol typeParameter) =>
            _names.TryGetValue(typeParameter, out var name) ? name : EscapeIdentifier(typeParameter.Name);

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

    /// <summary>
    /// A name as it must be written in C# source: a keyword declared as <c>@class</c> is written <c>@class</c>.
    /// </summary>
    private static string EscapeIdentifier(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

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

        // Group by class and generate one file per class. Non-generic attribute data comes first,
        // then generic, so a class using both keeps its existing member order.
        var allData = nonGenericData.Concat(genericData)
            .SelectMany(model => model.Entries.Select(entry => (Model: model, Entry: entry)))
            .ToArray();

        // Condition classes are shared across every extension class, so each is emitted once, into the first file
        // that uses it
        var classNames = AssignConditionClassNames(allData.SelectMany(x => x.Entry.ConditionClasses));
        var emittedClasses = new HashSet<string>(StringComparer.Ordinal);

        foreach (var classGroup in allData.GroupBy(x => x.Model.ClassKey, StringComparer.Ordinal))
        {
            var first = classGroup.First().Model;
            GenerateAssertionsForSpecificClass(context, first.ClassName, first.OriginalNamespace, classGroup.Select(x => x.Entry).ToArray(), classNames, emittedClasses);
        }
    }

    /// <summary>
    /// The name each condition class is emitted under. A condition class is named after the member it asserts on, so
    /// attributes on the same member share it while their sources match (e.g. <c>IsNullOrEmpty</c> and its
    /// <c>NegateLogic</c> counterpart, which passes the negation to the constructor). Classes that would share a name
    /// and arity otherwise each keep their own class:
    /// <list type="bullet">
    /// <item>A different member, such as <c>Box&lt;T&gt;.IsEmpty</c> declared in two namespaces: the member that sorts
    /// first keeps the plain name, and the others are prefixed with their declaring namespace.</item>
    /// <item>The same member with a different source, such as another <c>ExpectationMessage</c>: the source that sorts
    /// first keeps the member's name, and the others get a hash of their source.</item>
    /// </list>
    /// That makes every name a function of the declarations alone, never of the order attributes are declared or
    /// emitted in.
    /// </summary>
    /// <remarks>
    /// This is the second of two naming steps. <see cref="GetConditionClassName"/> already told apart overloads that
    /// share a name, using only the method's own overloads. Telling apart attributes needs every attribute's class, so
    /// it can only happen here.
    /// </remarks>
    private static Dictionary<ConditionClassModel, string> AssignConditionClassNames(IEnumerable<ConditionClassModel> conditionClasses)
    {
        var distinct = conditionClasses.Distinct().ToArray();
        var names = new Dictionary<ConditionClassModel, string>(distinct.Length);
        var taken = new HashSet<string>(distinct.Select(conditionClass => Key(conditionClass.Name, conditionClass.Arity)), StringComparer.Ordinal);

        string Claim(string name, int arity, string hashKey)
        {
            var claimed = name;
            for (var salt = 0; !taken.Add(Key(claimed, arity)); salt++)
            {
                claimed = WithHash(name, salt == 0 ? hashKey : hashKey + salt.ToString(CultureInfo.InvariantCulture));
            }

            return claimed;
        }

        foreach (var group in distinct
                     .GroupBy(conditionClass => Key(conditionClass.Name, conditionClass.Arity), StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var members = group
                .GroupBy(conditionClass => conditionClass.Identity, StringComparer.Ordinal)
                .OrderBy(member => member.Key, StringComparer.Ordinal)
                .ToArray();

            for (var memberIndex = 0; memberIndex < members.Length; memberIndex++)
            {
                var variants = members[memberIndex]
                    .Select(conditionClass => (Class: conditionClass, Key: HostIndependent(conditionClass.Source)))
                    .OrderBy(variant => variant.Key, StringComparer.Ordinal)
                    .ToArray();

                var first = variants[0].Class;
                var memberName = memberIndex == 0
                    ? first.Name
                    : Claim(first.Disambiguator + first.Name, first.Arity, members[memberIndex].Key);
                names.Add(first, memberName);

                foreach (var (variant, key) in variants.Skip(1))
                {
                    names.Add(variant, Claim(WithHash(memberName, key), variant.Arity, key));
                }
            }
        }

        return names;
    }

    private static string Key(string name, int arity) => $"{name}`{arity.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// <paramref name="source"/> with <c>\n</c> line endings. The source is built with <see cref="StringBuilder.AppendLine()"/>,
    /// which uses the host's line ending, and names derived from it are public, so they must not depend on the host.
    /// </summary>
    private static string HostIndependent(string source) => source.Replace("\r\n", "\n");

    /// <summary>
    /// Renames the identifier <paramref name="from"/> to <paramref name="to"/> in <paramref name="source"/>, token by
    /// token, so a name inside a longer identifier or a string literal is left alone. With an
    /// <paramref name="arity"/>, only references with exactly that many type arguments are renamed, so a same-named
    /// class of another arity keeps its name. Only runs for the rare class that has to be renamed because another
    /// class would be emitted under the same name.
    /// </summary>
    private static string RenameIdentifier(string source, string from, string to, int? arity = null)
    {
        var tokens = SyntaxFactory.ParseTokens(source).ToArray();
        var builder = new StringBuilder(source.Length);
        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == from && (arity is null || TypeArgumentCount(tokens, i + 1) == arity))
            {
                builder.Append(token.LeadingTrivia.ToFullString()).Append(to).Append(token.TrailingTrivia.ToFullString());
            }
            else
            {
                builder.Append(token.ToFullString());
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// The number of type arguments in the type argument list starting at <paramref name="start"/>, or 0 when there is
    /// none. Only commas at the list's own depth count, so <c>&lt;Dictionary&lt;int, string&gt;, T&gt;</c> has two.
    /// </summary>
    private static int TypeArgumentCount(SyntaxToken[] tokens, int start)
    {
        if (start >= tokens.Length || !tokens[start].IsKind(SyntaxKind.LessThanToken))
        {
            return 0;
        }

        var count = 1;
        var depth = 0;
        for (var i = start; i < tokens.Length; i++)
        {
            switch (tokens[i].Kind())
            {
                case SyntaxKind.LessThanToken:
                    depth++;
                    break;
                case SyntaxKind.GreaterThanToken:
                    depth--;
                    break;
                case SyntaxKind.GreaterThanGreaterThanToken:
                    depth -= 2;
                    break;
                case SyntaxKind.CommaToken when depth == 1:
                    count++;
                    break;
            }

            if (depth <= 0)
            {
                return count;
            }
        }

        return count;
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
            NamespaceHelper.GetNamespaceOrNull(classSymbol.ContainingNamespace),
            entries.ToImmutableEquatableArray());
    }

    private static void GenerateAssertionsForSpecificClass(
        SourceProductionContext context,
        string className,
        string? originalNamespace,
        AssertionEntryModel[] entries,
        Dictionary<ConditionClassModel, string> classNames,
        HashSet<string> emittedClasses)
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
            var extensionMethodsSource = entry.ExtensionMethodsSource;
            foreach (var conditionClass in entry.ConditionClasses)
            {
                var name = classNames[conditionClass];
                var renamed = !string.Equals(name, conditionClass.Name, StringComparison.Ordinal);
                if (renamed)
                {
                    extensionMethodsSource = RenameIdentifier(extensionMethodsSource, conditionClass.Name, name, conditionClass.Arity);
                }

                if (emittedClasses.Add(Key(name, conditionClass.Arity)))
                {
                    sourceBuilder.Append(renamed ? RenameIdentifier(conditionClass.Source, conditionClass.Name, name) : conditionClass.Source);
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

        // Only keep the location when there is something to report: it changes with every edit above the attribute,
        // and would otherwise stop unrelated edits from being cached
        var missingMethodDiagnostic = missingMethodMessage is not null
            ? DiagnosticInfo.Create(MethodNotFound, GetLocation(attributeData.AttributeSyntax), missingMethodMessage)
            : null;

        return new AssertionEntryModel(conditionClasses, extensionMethodsSource, missingMethodDiagnostic);
    }

    /// <remarks>
    /// Takes the <see cref="SyntaxReference"/> rather than a <see cref="Location"/> so the location is only
    /// computed for the attributes that report a diagnostic, not for every attribute on every pass.
    /// </remarks>
    private static Location GetLocation(SyntaxReference? syntax) =>
        syntax is null ? Location.None : Location.Create(syntax.SyntaxTree, syntax.Span);

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
            var classBuilder = new StringBuilder();
            var arity = GenerateAssertConditionClassForMethod(classBuilder, attributeData, method);
            conditionClasses.Add(new ConditionClassModel(
                GetConditionClassName(attributeData, method),
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

        var className = GetConditionClassName(attributeData, staticMethod);

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
            genericTypeParams = $"<{string.Join(", ", staticMethod.TypeParameters.Select(tp => EscapeIdentifier(tp.Name)))}>";
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

        return arity;
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

    /// <summary>
    /// Tokens of an <c>ExpectationMessage</c>: escaped brace pairs, <c>{...}</c> placeholders and stray braces. Any
    /// brace-free text between single braces is a placeholder candidate; it is only substituted when it names a
    /// parameter, so matching broadly keeps non-ASCII identifiers working without a hand-written identifier grammar.
    /// </summary>
    private static readonly Regex MessageTokenRegex = new(@"\{\{|\}\}|\{(?<name>[^{}]*)\}|[{}]");

    /// <summary>
    /// Escapes <paramref name="text"/> for use inside a C# string literal. With <c>quote: false</c>,
    /// <see cref="SymbolDisplay.FormatLiteral(string, bool)"/> leaves double quotes unescaped, so this quotes the text
    /// and strips the outer pair instead.
    /// </summary>
    private static string FormatLiteral(string text)
    {
        var literal = SymbolDisplay.FormatLiteral(text, quote: true);
        return literal.Substring(1, literal.Length - 2);
    }

    /// <summary>
    /// The condition class name for <paramref name="method"/>, final when the attribute is rendered. Overloads that
    /// map to the same name are told apart by a hash of their signature, except the first by parameter count and
    /// then signature, which keeps the plain name. That depends only on the method's own overloads, so neither other
    /// attributes nor the order the overloads are declared in can change it.
    /// </summary>
    /// <remarks>
    /// The first of two naming steps; <see cref="AssignConditionClassNames"/> then tells apart attributes that generate
    /// different classes for the same overload.
    /// </remarks>
    private static string GetConditionClassName(CreateAssertionAttributeData attributeData, IMethodSymbol method)
    {
        var name = GenerateAssertConditionClassNameForMethod(attributeData.TargetType, attributeData.ContainingType, attributeData.MethodName, method);
        if (method.MethodKind == MethodKind.PropertyGet)
        {
            return name;
        }

        IMethodSymbol? first = null;
        var shared = false;
        foreach (var overload in method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>())
        {
            // Overloads with a different number of type parameters get classes of a different arity, which can share a name
            if (!IsValidReturnType(overload.ReturnType, out _)
                || overload.TypeParameters.Length != method.TypeParameters.Length
                || GenerateAssertConditionClassNameForMethod(attributeData.TargetType, attributeData.ContainingType, attributeData.MethodName, overload) != name)
            {
                continue;
            }

            shared |= first is not null;
            if (first is null || CompareOverloads(overload, first) < 0)
            {
                first = overload;
            }
        }

        return !shared || SymbolEqualityComparer.Default.Equals(first!.OriginalDefinition, method.OriginalDefinition)
            ? name
            : WithHash(name, Signature(method));
    }

    private static int CompareOverloads(IMethodSymbol x, IMethodSymbol y)
    {
        var byParameterCount = x.Parameters.Length.CompareTo(y.Parameters.Length);
        return byParameterCount != 0 ? byParameterCount : string.CompareOrdinal(Signature(x), Signature(y));
    }

    private static string Signature(IMethodSymbol method) => method.OriginalDefinition.ToDisplayString();

    /// <summary>Inserts a hash of <paramref name="key"/> before the class name's "Assertion" suffix.</summary>
    private static string WithHash(string className, string key)
    {
        const string suffix = "Assertion";
        var stem = className.EndsWith(suffix, StringComparison.Ordinal) ? className.Substring(0, className.Length - suffix.Length) : className;
        return stem + StableHash(key) + suffix;
    }

    /// <summary>
    /// 32-bit FNV-1a, as eight hex digits. <see cref="string.GetHashCode()"/> is randomized per process, and generated
    /// names must be the same on every build.
    /// </summary>
    private static string StableHash(string text)
    {
        var hash = 2166136261u;
        foreach (var c in text)
        {
            hash = unchecked((hash ^ c) * 16777619u);
        }

        return hash.ToString("X8", CultureInfo.InvariantCulture);
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
                constraints.Add($"where {names?.NameOf(typeParameter) ?? EscapeIdentifier(typeParameter.Name)} : {string.Join(", ", typeConstraints)}");
            }
        }

        return constraints;
    }

    private static void GenerateMethodsForSpecificOverload(StringBuilder sourceBuilder, CreateAssertionAttributeData attributeData, IMethodSymbol staticMethod)
    {
        var targetTypeName = attributeData.TargetType.ToDisplayString();
        var methodName = attributeData.MethodName;

        var baseMethodName = attributeData.CustomName ?? methodName;
        var className = GetConditionClassName(attributeData, staticMethod);

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
            methodGenericTypeParams = $"<{string.Join(", ", method.TypeParameters.Select(tp => EscapeIdentifier(tp.Name)))}>";
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
    /// its extension methods, and the "method not found" diagnostic when nothing matched.
    /// </summary>
    /// <remarks>
    /// Rendering in the transform is deliberate: the emission code is bound to symbols, and
    /// strings give cheap value equality. The rendered text must stay deterministic (no
    /// timestamps, no hash-ordered collections), or every run would compare as modified.
    /// </remarks>
    private sealed record AssertionEntryModel(
        ImmutableEquatableArray<ConditionClassModel> ConditionClasses,
        string ExtensionMethodsSource,
        DiagnosticInfo? MissingMethodDiagnostic
    );

    /// <param name="Name">
    /// The class's name, which the source and the extension methods use. Another class that would be emitted under
    /// the same name and arity can still have it renamed at emit time; see <see cref="AssignConditionClassNames"/>.
    /// </param>
    /// <param name="Arity">The class's generic arity. Classes with the same name and different arities can coexist.</param>
    /// <param name="Identity">The member the class asserts on: target type, containing type, member and arity.</param>
    /// <param name="Disambiguator">A prefix for the class's name when a different member's class has the same name.</param>
    /// <param name="Source">The class's source.</param>
    private sealed record ConditionClassModel(
        string Name,
        int Arity,
        string Identity,
        string Disambiguator,
        string Source
    );

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

    /// <summary>
    /// Symbol-based attribute data. Only used inside the transform; never flows through the pipeline.
    /// </summary>
    /// <remarks>
    /// <see cref="AttributeSyntax"/> roots its compilation's syntax tree, so this record must never cross an
    /// incremental cache boundary; only the string models rendered from it may.
    /// </remarks>
    private record CreateAssertionAttributeData(
        INamedTypeSymbol TargetType,
        INamedTypeSymbol ContainingType,
        string MethodName,
        string? CustomName,
        bool NegateLogic,
        bool RequiresGenericTypeParameter,
        bool TreatAsInstance,
        string? ExpectationMessage,
        bool IsUnboundGeneric,
        SyntaxReference? AttributeSyntax
    );
}
