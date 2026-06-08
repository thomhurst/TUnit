using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Assertions.SourceGenerator.Generators;

/// <summary>
/// Source generator that creates extension methods for assertion classes decorated with [AssertionExtension].
/// Analyzes the constructors of assertion classes and generates corresponding extension methods that:
/// - Extend IAssertionSource&lt;T&gt;
/// - Build the expression string for error messages
/// - Instantiate the assertion with the provided parameters
/// </summary>
[Generator]
public sealed class AssertionExtensionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes decorated with [AssertionExtension]
        var assertionClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Assertions.Attributes.AssertionExtensionAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => GetAssertionExtensionData(ctx, ct))
            .Where(static x => x != null)
            .Select(static (x, _) => x!);

        // Generate extension methods for each assertion class
        context.RegisterSourceOutput(assertionClasses, static (context, data) => GenerateExtensionMethods(context, data));
    }

    private static AssertionExtensionData? GetAssertionExtensionData(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        // Get the [AssertionExtension] attribute
        var attributeData = context.Attributes.FirstOrDefault();
        if (attributeData == null)
        {
            return null;
        }

        // Extract method name from attribute
        string? methodName = null;
        if (attributeData.ConstructorArguments.Length > 0)
        {
            methodName = attributeData.ConstructorArguments[0].Value?.ToString();
        }

        if (string.IsNullOrEmpty(methodName))
        {
            return null;
        }

        // Extract optional negated method name and overload resolution priority
        string? negatedMethodName = null;
        int overloadPriority = 0;  // Default to 0
        foreach (var namedArg in attributeData.NamedArguments)
        {
            if (namedArg.Key == "NegatedMethodName")
            {
                negatedMethodName = namedArg.Value.Value?.ToString();
            }
            else if (namedArg.Key == "OverloadResolutionPriority" && namedArg.Value.Value is int priority)
            {
                overloadPriority = priority;
            }
        }

        // Find the base Assertion<T> type to extract the type parameter
        var assertionBaseType = GetAssertionBaseType(classSymbol);
        if (assertionBaseType == null)
        {
            // Not an Assertion<T> - skip
            return null;
        }

        // Get all public constructors
        var constructors = classSymbol.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .ToImmutableArray();

        if (constructors.Length == 0)
        {
            return null;
        }

        // Check for RequiresUnreferencedCode attribute
        var RequiresUnreferencedCodeAttr = classSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "RequiresUnreferencedCodeAttribute");
        string? RequiresUnreferencedCodeMessage = null;
        if (RequiresUnreferencedCodeAttr != null && RequiresUnreferencedCodeAttr.ConstructorArguments.Length > 0)
        {
            RequiresUnreferencedCodeMessage = RequiresUnreferencedCodeAttr.ConstructorArguments[0].Value?.ToString();
        }

        return new AssertionExtensionData(
            classSymbol,
            methodName!,
            negatedMethodName,
            assertionBaseType,
            constructors,
            overloadPriority,
            RequiresUnreferencedCodeMessage
        );
    }

    private static INamedTypeSymbol? GetAssertionBaseType(INamedTypeSymbol classSymbol)
    {
        var currentType = classSymbol.BaseType;
        while (currentType != null)
        {
            if (currentType.Name == "Assertion" &&
                currentType.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions.Core" &&
                currentType.TypeArguments.Length == 1)
            {
                return currentType;
            }
            currentType = currentType.BaseType;
        }
        return null;
    }

    private static void GenerateExtensionMethods(SourceProductionContext context, AssertionExtensionData data)
    {
        var sourceBuilder = new StringBuilder();

        // Header
        sourceBuilder.AppendLine("// <auto-generated/>");
        sourceBuilder.AppendLine("#pragma warning disable");
        sourceBuilder.AppendLine("#nullable enable");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
        sourceBuilder.AppendLine("using TUnit.Assertions.Core;");
        sourceBuilder.AppendLine("using TUnit.Assertions.Enums;");

        // Add using for the assertion class's namespace if different
        var assertionNamespace = data.ClassSymbol.ContainingNamespace?.ToDisplayString();
        if (!string.IsNullOrEmpty(assertionNamespace) && assertionNamespace != "TUnit.Assertions.Extensions")
        {
            sourceBuilder.AppendLine($"using {assertionNamespace};");
        }

        sourceBuilder.AppendLine();

        // Namespace

        sourceBuilder.AppendLine($"namespace TUnit.Assertions.Extensions;");
        sourceBuilder.AppendLine();

        // Extension class
        var extensionClassName = $"{data.ClassSymbol.Name}Extensions";
        sourceBuilder.AppendLine($"/// <summary>");
        sourceBuilder.AppendLine($"/// Generated extension methods for {data.ClassSymbol.Name}.");
        sourceBuilder.AppendLine($"/// </summary>");
        sourceBuilder.AppendLine($"public static partial class {extensionClassName}");
        sourceBuilder.AppendLine("{");

        // When the receiver type is a covariance candidate AND the assertion class declares its
        // own generic parameter(s), the covariant method signature is <TActual, T...>. Callers
        // that name the class's own arguments (e.g. a non-inferable lambda type) must then also
        // spell out the covariant TActual, because C# forbids partial type-argument specification.
        // To restore inference for the common exact-receiver call site, additionally emit a
        // pinned-receiver overload (IAssertionSource<TConcrete>) that omits TActual. The covariant
        // overload is kept so a more-derived static receiver can still bind. See issue #5922.
        //
        // The pinned overload is only emitted when at least one of the class's own type parameters
        // is NOT inferable from the constructor's value arguments. If every own type parameter is
        // inferable (e.g. a `T tag` value parameter), the caller writes no type arguments at all and
        // the covariant overload binds on its own — the pinned overload would be pure dead weight.
        // Excluding that case also keeps the two overloads arity-disjoint (pinned <T...> vs covariant
        // <TActual, T...>), so they are never both applicable and no OverloadResolutionPriority
        // tiebreaker is required.
        var receiverType = data.AssertionBaseType.TypeArguments[0];
        var hasOwnGenerics = data.ClassSymbol.IsGenericType && data.ClassSymbol.TypeParameters.Length > 0;
        var receiverIsCovariantCandidate = CovarianceHelper.IsCovariantCandidate(receiverType) && hasOwnGenerics;
        var ownTypeParameters = data.ClassSymbol.TypeParameters;

        // Generate extension methods for each constructor
        foreach (var constructor in data.Constructors)
        {
            if (!IsValidConstructor(constructor))
            {
                continue;
            }

            // The pinned overload only earns its place when a non-inferable own type parameter forces
            // the caller to name type arguments; otherwise it is redundant with the covariant overload.
            var emitPinned = receiverIsCovariantCandidate
                && !CovarianceHelper.OwnGenericsAreInferable(constructor, ownTypeParameters);

            // Generate positive assertion method
            EmitMethod(sourceBuilder, data, constructor, negated: false, emitPinned);

            // Generate negated assertion method if requested
            if (!string.IsNullOrEmpty(data.NegatedMethodName))
            {
                EmitMethod(sourceBuilder, data, constructor, negated: true, emitPinned);
            }
        }

        sourceBuilder.AppendLine("}");

        // Add source to compilation
        var fileName = $"{data.ClassSymbol.Name}.Extensions.g.cs";
        context.AddSource(fileName, sourceBuilder.ToString());
    }

    /// <summary>
    /// Emits the covariant extension method and, when <paramref name="emitPinned"/> is set, the
    /// inference-friendly pinned-receiver overload alongside it.
    /// </summary>
    private static void EmitMethod(
        StringBuilder sourceBuilder,
        AssertionExtensionData data,
        IMethodSymbol constructor,
        bool negated,
        bool emitPinned)
    {
        GenerateExtensionMethod(sourceBuilder, data, constructor, negated, isNullableOverload: false);
        if (emitPinned)
        {
            GenerateExtensionMethod(sourceBuilder, data, constructor, negated, isNullableOverload: false, pinnedReceiver: true);
        }
    }

    private static bool IsValidConstructor(IMethodSymbol constructor)
    {
        // Must have at least one parameter
        if (constructor.Parameters.Length == 0)
        {
            return false;
        }

        // First parameter must be AssertionContext<T>
        var firstParam = constructor.Parameters[0];
        if (firstParam.Type is not INamedTypeSymbol firstParamType)
        {
            return false;
        }

        return firstParamType.Name == "AssertionContext" &&
               firstParamType.ContainingNamespace?.ToDisplayString() == "TUnit.Assertions.Core" &&
               firstParamType.TypeArguments.Length == 1;
    }

    private static void GenerateExtensionMethod(
        StringBuilder sourceBuilder,
        AssertionExtensionData data,
        IMethodSymbol constructor,
        bool negated,
        bool isNullableOverload,
        bool pinnedReceiver = false)
    {
        var methodName = negated ? data.NegatedMethodName : data.MethodName;
        var assertionType = data.ClassSymbol;
        var typeParam = data.AssertionBaseType.TypeArguments[0];

        // Skip the first parameter (AssertionContext<T>)
        var additionalParams = constructor.Parameters.Skip(1).ToArray();

        // Check for RequiresUnreferencedCode attribute on the constructor first, then fall back to class-level
        var constructorRequiresUnreferencedCodeAttr = constructor.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "RequiresUnreferencedCodeAttribute");

        string? requiresUnreferencedCodeMessage = null;
        if (constructorRequiresUnreferencedCodeAttr != null && constructorRequiresUnreferencedCodeAttr.ConstructorArguments.Length > 0)
        {
            // Constructor-level attribute takes precedence
            requiresUnreferencedCodeMessage = constructorRequiresUnreferencedCodeAttr.ConstructorArguments[0].Value?.ToString();
        }
        else if (!string.IsNullOrEmpty(data.RequiresUnreferencedCodeMessage))
        {
            // Fall back to class-level attribute
            requiresUnreferencedCodeMessage = data.RequiresUnreferencedCodeMessage;
        }

        var genericParams = new List<string>();
        var typeConstraints = new List<string>();

        if (assertionType.IsGenericType && assertionType.TypeParameters.Length > 0)
        {
            // The assertion class defines its own generic type parameters
            // e.g., GreaterThanAssertion<TValue> or CollectionContainsAssertion<TCollection, TItem>
            foreach (var typeParameter in assertionType.TypeParameters)
            {
                genericParams.Add(typeParameter.Name);

                // Collect constraints for each type parameter
                var constraints = new List<string>();
                if (typeParameter.HasReferenceTypeConstraint)
                {
                    constraints.Add("class");
                }
                if (typeParameter.HasValueTypeConstraint)
                {
                    constraints.Add("struct");
                }
                if (typeParameter.HasNotNullConstraint)
                {
                    constraints.Add("notnull");
                }
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    constraints.Add(constraintType.ToDisplayString());
                }
                if (typeParameter.HasConstructorConstraint)
                {
                    constraints.Add("new()");
                }

                if (constraints.Count > 0)
                {
                    typeConstraints.Add($"where {typeParameter.Name} : {string.Join(", ", constraints)}");
                }
            }
        }
        else if (typeParam is ITypeParameterSymbol typeParamSymbol)
        {
            // The assertion class is not generic, but inherits from Assertion<T>
            // where T is a type parameter from the base class
            genericParams.Add(typeParamSymbol.Name);

            // Collect constraints
            var constraints = new List<string>();
            if (typeParamSymbol.HasReferenceTypeConstraint)
            {
                constraints.Add("class");
            }
            if (typeParamSymbol.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }
            if (typeParamSymbol.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }
            foreach (var constraintType in typeParamSymbol.ConstraintTypes)
            {
                constraints.Add(constraintType.ToDisplayString());
            }
            if (typeParamSymbol.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            if (constraints.Count > 0)
            {
                typeConstraints.Add($"where {typeParamSymbol.Name} : {string.Join(", ", constraints)}");
            }
        }

        var genericParamsString = genericParams.Count > 0 ? $"<{string.Join(", ", genericParams)}>" : "";

        // Build method signature
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    /// <summary>");
        sourceBuilder.AppendLine($"    /// Extension method for {assertionType.Name}.");
        sourceBuilder.AppendLine("    /// </summary>");

        // Add RequiresUnreferencedCode attribute if present (from constructor or class level)
        if (!string.IsNullOrEmpty(requiresUnreferencedCodeMessage))
        {
            var escapedMessage = requiresUnreferencedCodeMessage!.Replace("\"", "\\\"");
            sourceBuilder.AppendLine($"    [global::System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(\"{escapedMessage}\")]");
        }

        // Add OverloadResolutionPriority attribute if specified
        // For nullable overloads (generic with class constraint), increase priority by 1
        // so they're preferred over the base nullable overload when source is non-nullable
        var effectivePriority = data.OverloadResolutionPriority;
        if (isNullableOverload)
        {
            effectivePriority += 1;
        }

        if (effectivePriority > 0)
        {
            sourceBuilder.AppendLine($"    [global::System.Runtime.CompilerServices.OverloadResolutionPriority({effectivePriority})]");
        }

        // Method declaration
        var returnType = assertionType.IsGenericType
            ? $"{assertionType.Name}{genericParamsString}"
            : assertionType.Name;

        // The extension method extends IAssertionSource<T> where T is the type argument
        // from the Assertion<T> base class.
        string sourceType;
        string? genericTypeParam = null;
        string? genericConstraint = null;

        // pinnedReceiver suppresses covariance so the receiver is pinned to the concrete type
        // (IAssertionSource<TConcrete>), dropping the TActual parameter for inference-friendly call sites.
        var isCovariantCandidate = !isNullableOverload && !pinnedReceiver
            && CovarianceHelper.IsCovariantCandidate(typeParam);
        var typeParamDisplay = typeParam.ToDisplayString();

        if (isNullableOverload)
        {
            // For nullable reference types, we can't use two separate overloads for T and T?
            // because NRT annotations are erased at runtime - they're the same type to the CLR.
            // Instead, just use the nullable version and accept both nullable and non-nullable sources.
            sourceType = $"IAssertionSource<{typeParamDisplay}>";
            genericTypeParam = null;
            genericConstraint = null;
        }
        else if (typeParam is ITypeParameterSymbol baseTypeParam)
        {
            sourceType = $"IAssertionSource<{baseTypeParam.Name}>";
        }
        else if (isCovariantCandidate)
        {
            var covariantParam = CovarianceHelper.GetCovariantTypeParamName(genericParams);
            sourceType = $"IAssertionSource<{covariantParam}>";
            genericTypeParam = covariantParam;
            genericConstraint = $"where {covariantParam} : {CovarianceHelper.GetConstraintTypeName(typeParamDisplay, typeParam)}";
        }
        else
        {
            sourceType = $"IAssertionSource<{typeParamDisplay}>";
        }

        sourceBuilder.Append($"    public static {returnType} {methodName}");

        // Merge the covariant receiver-type parameter (if any) with the assertion class's own
        // type parameters into a single generic parameter list. Emitting them as two adjacent
        // <X><Y> blocks produces invalid C# when both are present (e.g. an Assertion<Concrete>
        // subclass that also declares its own <T>: the receiver-type covariance adds TActual,
        // the class adds T, and the method signature must be <TActual, T>, not <TActual><T>).
        var methodGenericParams = new List<string>();
        if (genericTypeParam != null)
        {
            methodGenericParams.Add(genericTypeParam);
        }
        methodGenericParams.AddRange(genericParams);
        if (methodGenericParams.Count > 0)
        {
            sourceBuilder.Append($"<{string.Join(", ", methodGenericParams)}>");
        }
        sourceBuilder.Append("(");
        sourceBuilder.Append($"this {sourceType} source");

        // Add additional parameters
        foreach (var param in additionalParams)
        {
            sourceBuilder.Append($", {param.Type.ToDisplayString()} {param.Name}");

            // Add default value if present
            if (param.HasExplicitDefaultValue)
            {
                var defaultValue = DefaultValueFormatter.FormatDefaultValue(param.ExplicitDefaultValue, param.Type);
                sourceBuilder.Append($" = {defaultValue}");
            }
        }

        // Add CallerArgumentExpression parameters for better error messages
        for (int i = 0; i < additionalParams.Length; i++)
        {
            var param = additionalParams[i];
            sourceBuilder.Append($", [CallerArgumentExpression(nameof({param.Name}))] string? {param.Name}Expression = null");
        }

        sourceBuilder.Append(")");

        // Add type constraints on new line if any
        var allConstraints = new List<string>(typeConstraints);
        if (genericConstraint != null)
        {
            allConstraints.Add(genericConstraint);
        }

        if (allConstraints.Count > 0)
        {
            sourceBuilder.AppendLine();
            sourceBuilder.Append($"        {string.Join(' ', allConstraints)}");
        }

        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    {");

        // Build expression string for error messages
        // Only include parameters that were actually provided (non-null expressions)
        sourceBuilder.Append($"        source.Context.ExpressionBuilder.Append(\".{methodName}(\");");
        if (additionalParams.Length == 1)
        {
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"        source.Context.ExpressionBuilder.Append({additionalParams[0].Name}Expression);");
        }
        else if (additionalParams.Length > 0)
        {
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("        var added = false;");
            foreach (var param in additionalParams)
            {
                sourceBuilder.AppendLine($"        if ({param.Name}Expression != null)");
                sourceBuilder.AppendLine("        {");
                sourceBuilder.AppendLine("            source.Context.ExpressionBuilder.Append(added ? \", \" : \"\");");
                sourceBuilder.AppendLine($"            source.Context.ExpressionBuilder.Append({param.Name}Expression);");
                sourceBuilder.AppendLine($"            added = true;");
                sourceBuilder.AppendLine("        }");

            }
        }
        sourceBuilder.AppendLine("        source.Context.ExpressionBuilder.Append(\")\");");

        // Construct and return the assertion
        sourceBuilder.Append($"        return new {assertionType.Name}");
        if (genericParams.Count > 0)
        {
            sourceBuilder.Append($"<{string.Join(", ", genericParams)}>");
        }
        sourceBuilder.Append("(");

        if (isNullableOverload)
        {
            sourceBuilder.Append("source.Context.AsNullable()");
        }
        else if (isCovariantCandidate)
        {
            sourceBuilder.Append(CovarianceHelper.GetCovariantContextExpr(typeParamDisplay));
        }
        else
        {
            sourceBuilder.Append("source.Context");
        }

        foreach (var param in additionalParams)
        {
            sourceBuilder.Append($", {param.Name}");
        }

        sourceBuilder.AppendLine(");");
        sourceBuilder.AppendLine("    }");
    }

    private record AssertionExtensionData(
        INamedTypeSymbol ClassSymbol,
        string MethodName,
        string? NegatedMethodName,
        INamedTypeSymbol AssertionBaseType,
        ImmutableArray<IMethodSymbol> Constructors,
        int OverloadResolutionPriority,
        string? RequiresUnreferencedCodeMessage
    );
}
