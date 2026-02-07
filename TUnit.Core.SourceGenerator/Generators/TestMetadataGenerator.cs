using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using TUnit.Core.SourceGenerator.CodeGenerators.Formatting;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators;

[Generator]
public sealed class TestMetadataGenerator : IIncrementalGenerator
{
    private const string GeneratedNamespace = "TUnit.Generated";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enabledProvider = context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.EnableTUnitSourceGeneration", out var value);
                return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
            });

        var compilationContext = context
            .CompilationProvider
            .Select(static (c, _) =>
            {
                var wellKnownTypes = new WellKnownTypes(c);
                return new CompilationContext(
                    (CSharpCompilation)c,
                    new AttributeWriter(c, wellKnownTypes),
                    wellKnownTypes
                );
            });

        var testMethodsProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.TestAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => ctx)
            .Combine(compilationContext)
            .Select(static (ctx, _) => GetTestMethodMetadata(ctx.Left, ctx.Right))
            .Where(static m => m is not null)
            .Combine(enabledProvider);

        var inheritsTestsClassesProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.InheritsTestsAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => ctx)
            .Combine(compilationContext)
            .Select(static (ctx, _) => GetInheritsTestsClassMetadata(ctx.Left, ctx.Right))
            .Where(static m => m is not null)
            .Combine(enabledProvider);

        context.RegisterSourceOutput(testMethodsProvider,
            static (context, data) =>
            {
                var (testMethod, isEnabled) = data;
                if (!isEnabled)
                {
                    return;
                }
                GenerateTestMethodSource(context, testMethod);
            });

        context.RegisterSourceOutput(inheritsTestsClassesProvider,
            static (context, data) =>
            {
                var (classInfo, isEnabled) = data;
                if (!isEnabled)
                {
                    return;
                }
                GenerateInheritedTestSources(context, classInfo);
            });
    }

    private static InheritsTestsClassMetadata? GetInheritsTestsClassMetadata(GeneratorAttributeSyntaxContext context, CompilationContext compilationContext)
    {
        var classSyntax = (ClassDeclarationSyntax)context.TargetNode;

        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        if (classSymbol.IsAbstract)
        {
            return null;
        }

        return new InheritsTestsClassMetadata
        {
            TypeSymbol = classSymbol,
            ClassSyntax = classSyntax,
            Context = context,
            CompilationContext = compilationContext
        };
    }

    private static TestMethodMetadata? GetTestMethodMetadata(GeneratorAttributeSyntaxContext context, CompilationContext compilationContext)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.TargetNode;
        var methodSymbol = context.TargetSymbol as IMethodSymbol;

        var containingType = methodSymbol?.ContainingType;

        if (containingType == null)
        {
            return null;
        }

        var testAttribute = methodSymbol!.GetRequiredTestAttribute();

        if (containingType.IsAbstract)
        {
            return null;
        }

        // Skip types nested inside open generic containing types
        // These can't be instantiated without knowing the outer type's type arguments
        if (HasOpenGenericContainingType(containingType))
        {
            return null;
        }

        var isGenericType = containingType is { IsGenericType: true, TypeParameters.Length: > 0 };
        var isGenericMethod = methodSymbol is { IsGenericMethod: true };

        var (filePath, lineNumber) = GetTestMethodSourceLocation(methodSyntax, testAttribute);

        return new TestMethodMetadata
        {
            MethodSymbol = methodSymbol ?? throw new InvalidOperationException("Symbol is not a method"),
            TypeSymbol = containingType,
            FilePath = filePath,
            LineNumber = lineNumber,
            TestAttribute = context.Attributes.First(),
            Context = context,
            CompilationContext = compilationContext,
            MethodSyntax = methodSyntax,
            IsGenericType = isGenericType,
            IsGenericMethod = isGenericMethod,
            MethodAttributes = methodSymbol.GetAttributes()
        };
    }

    private static bool HasOpenGenericContainingType(INamedTypeSymbol type)
    {
        var current = type.ContainingType;
        while (current != null)
        {
            if (current is { IsGenericType: true, TypeParameters.Length: > 0 })
            {
                return true;
            }
            current = current.ContainingType;
        }
        return false;
    }

    private static void GenerateInheritedTestSources(SourceProductionContext context, InheritsTestsClassMetadata? classInfo)
    {
        if (classInfo?.TypeSymbol == null)
        {
            return;
        }

        var inheritedTestMethods = CollectInheritedTestMethods(classInfo.TypeSymbol);

        foreach (var method in inheritedTestMethods)
        {
            var testAttribute = method.GetAttributes().FirstOrDefault(a => a.IsTestAttribute());

            if (testAttribute == null)
            {
                continue;
            }

            var concreteMethod = FindConcreteMethodImplementation(classInfo.TypeSymbol, method);

            // Calculate inheritance depth using concrete method if available
            var methodToCheck = concreteMethod ?? method;
            var inheritanceDepth = CalculateInheritanceDepth(classInfo.TypeSymbol, methodToCheck);

            // Skip methods declared directly on this class (inheritance depth = 0)
            // Those are already handled by the regular test method registration
            if (inheritanceDepth == 0)
            {
                continue;
            }
            var (filePath, lineNumber) = GetTestMethodSourceLocation(method, testAttribute, classInfo);

            // If the method is from a generic base class, use the constructed version from the inheritance hierarchy
            var typeForMetadata = classInfo.TypeSymbol;
            if (method.ContainingType.IsGenericType && method.ContainingType.IsDefinition)
            {
                // Find the constructed generic type in the inheritance chain
                var baseType = classInfo.TypeSymbol.BaseType;
                while (baseType != null)
                {
                    if (baseType.IsGenericType &&
                        SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, method.ContainingType))
                    {
                        typeForMetadata = baseType;
                        break;
                    }
                    baseType = baseType.BaseType;
                }
            }

            var testMethodMetadata = new TestMethodMetadata
            {
                MethodSymbol = concreteMethod ?? method, // Use concrete method if found, otherwise base method
                TypeSymbol = typeForMetadata, // Use constructed generic base if applicable
                FilePath = filePath,
                LineNumber = lineNumber,
                TestAttribute = testAttribute,
                Context = classInfo.Context, // Use class context to access Compilation
                CompilationContext = classInfo.CompilationContext,
                MethodSyntax = null, // No syntax for inherited methods
                IsGenericType = typeForMetadata.IsGenericType,
                IsGenericMethod = (concreteMethod ?? method).IsGenericMethod,
                MethodAttributes = (concreteMethod ?? method).GetAttributes(), // Use concrete method attributes
                InheritanceDepth = inheritanceDepth
            };

            GenerateTestMethodSource(context, testMethodMetadata);
        }
    }

    private static int CalculateInheritanceDepth(INamedTypeSymbol testClass, IMethodSymbol testMethod)
    {
        var methodContainingType = testMethod.ContainingType.OriginalDefinition;
        var testClassOriginal = testClass.OriginalDefinition;

        if (SymbolEqualityComparer.Default.Equals(methodContainingType, testClassOriginal))
        {
            return 0;
        }

        var depth = 0;
        var currentType = testClass.BaseType;

        while (currentType != null)
        {
            depth++;
            var currentTypeOriginal = currentType.OriginalDefinition;
            if (SymbolEqualityComparer.Default.Equals(methodContainingType, currentTypeOriginal))
            {
                return depth;
            }
            currentType = currentType.BaseType;
        }

        return depth;
    }

    private static void GenerateTestMethodSource(SourceProductionContext context, TestMethodMetadata? testMethod)
    {
        try
        {
            if (testMethod?.MethodSymbol == null || testMethod.Context == null)
            {
                return;
            }

            var writer = new CodeWriter();
            GenerateFileHeader(writer);
            GenerateTestMetadata(writer, testMethod);

            var fileName = FileNameHelper.GetDeterministicFileNameForMethod(testMethod.TypeSymbol, testMethod.MethodSymbol);
            context.AddSource(fileName, SourceText.From(writer.ToString(), Encoding.UTF8));
        }
        catch (Exception ex)
        {
            var methodName = testMethod?.MethodSymbol?.Name ?? "Unknown";
            var className = testMethod?.TypeSymbol?.Name ?? "Unknown";

            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "TUNIT0999",
                    "Source Generation Error",
                    "Failed to generate test metadata for {0}.{1}: {2}",
                    "TUnit",
                    DiagnosticSeverity.Error,
                    true),
                Location.None,
                className,
                methodName,
                ex.ToString())); // Use ToString() to get full stack trace for debugging
        }
    }

    private static void GenerateFileHeader(CodeWriter writer)
    {
        writer.AppendLine("#nullable enable");
        writer.AppendLine();
        writer.AppendLine();
        writer.AppendLine($"namespace {GeneratedNamespace};");
        writer.AppendLine();
    }

    private static void GenerateTestMetadata(CodeWriter writer, TestMethodMetadata testMethod)
    {
        var className = testMethod.TypeSymbol.GloballyQualified();

        // Generate unique class name using same pattern as filename (without .g.cs extension)
        var uniqueClassName = FileNameHelper.GetDeterministicFileNameForMethod(testMethod.TypeSymbol, testMethod.MethodSymbol)
            .Replace(".g.cs", "_TestSource");

        writer.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]");
        writer.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCode(\"TUnit\", \"{typeof(TestMetadataGenerator).Assembly.GetName().Version}\")]");
        writer.AppendLine($"internal sealed class {uniqueClassName} : global::TUnit.Core.Interfaces.SourceGenerator.ITestSource, global::TUnit.Core.Interfaces.SourceGenerator.ITestDescriptorSource");
        writer.AppendLine("{");
        writer.Indent();

        GenerateReflectionFieldAccessors(writer, testMethod.TypeSymbol);

        writer.AppendLine("public async global::System.Collections.Generic.IAsyncEnumerable<global::TUnit.Core.TestMetadata> GetTestsAsync(string testSessionId, [global::System.Runtime.CompilerServices.EnumeratorCancellation] global::System.Threading.CancellationToken cancellationToken = default)");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine();

        if (testMethod.IsGenericType || testMethod is { IsGenericMethod: true, MethodSymbol.TypeParameters.Length: > 0 })
        {
            var hasTypedDataSource = testMethod.MethodAttributes
                .Any(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass) &&
                         InferTypesFromDataSourceAttribute(testMethod.MethodSymbol, a) != null);

            var hasGenerateGenericTest = (testMethod.IsGenericMethod && testMethod.MethodAttributes
                .Any(a => a.AttributeClass?.IsOrInherits("global::TUnit.Core.GenerateGenericTestAttribute") is true)) ||
                (testMethod.IsGenericType && testMethod.TypeSymbol.GetAttributes()
                .Any(a => a.AttributeClass?.IsOrInherits("global::TUnit.Core.GenerateGenericTestAttribute") is true));

            var hasClassArguments = testMethod.TypeSymbol.GetAttributes()
                .Any(a => a.AttributeClass?.IsOrInherits("global::TUnit.Core.ArgumentsAttribute") is true);

            var hasTypedDataSourceForGenericType = testMethod is { IsGenericType: true, IsGenericMethod: false } && testMethod.MethodAttributes
                .Any(a => a.AttributeClass != null &&
                    a.AttributeClass.IsOrInherits("global::TUnit.Core.AsyncDataSourceGeneratorAttribute") &&
                    InferTypesFromTypedDataSourceForClass(testMethod.TypeSymbol, testMethod.MethodSymbol) != null);

            var hasMethodArgumentsForGenericType = testMethod is { IsGenericType: true, IsGenericMethod: false } && testMethod.MethodAttributes
                .Any(a => a.AttributeClass?.IsOrInherits("global::TUnit.Core.ArgumentsAttribute") is true);

            var hasMethodDataSourceForGenericType = testMethod is { IsGenericType: true, IsGenericMethod: false } && testMethod.MethodAttributes
                .Any(a => a.AttributeClass?.Name == "MethodDataSourceAttribute" &&
                          InferClassTypesFromMethodDataSource(testMethod, a) != null);

            // Check for class-level data sources that could help resolve generic type arguments
            var hasClassDataSources = testMethod.IsGenericType && testMethod.TypeSymbol.GetAttributesIncludingBaseTypes()
                .Any(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));

            if (hasTypedDataSource || hasGenerateGenericTest || testMethod.IsGenericMethod || hasClassArguments || hasTypedDataSourceForGenericType || hasMethodArgumentsForGenericType || hasMethodDataSourceForGenericType || hasClassDataSources)
            {
                GenerateGenericTestWithConcreteTypes(writer, testMethod, className);
            }
            else
            {
                // For generic classes with no way to resolve type arguments, this will generate
                // GenericTestMetadata that the engine will fail with a clear error message
                GenerateTestMetadataInstance(writer, testMethod, className);
            }
        }
        else
        {
            GenerateTestMetadataInstance(writer, testMethod, className);
        }

        writer.AppendLine("yield break;");
        writer.Unindent();
        writer.AppendLine("}");

        writer.AppendLine();

        // Generate EnumerateTestDescriptors method for fast filtering
        GenerateEnumerateTestDescriptors(writer, testMethod);

        writer.Unindent();
        writer.AppendLine("}");

        GenerateModuleInitializer(writer, testMethod, uniqueClassName);
    }

    private static void GenerateTestMetadataInstance(CodeWriter writer, TestMethodMetadata testMethod, string className)
    {
        var methodName = testMethod.MethodSymbol.Name;

        // For generic types or methods, use GenericTestMetadata; for concrete types, use TestMetadata<T>
        if (testMethod.IsGenericType || testMethod.IsGenericMethod)
        {
            writer.AppendLine("var metadata = new global::TUnit.Core.GenericTestMetadata");
        }
        else
        {
            writer.AppendLine($"var metadata = new global::TUnit.Core.TestMetadata<{className}>");
        }

        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"TestName = \"{methodName}\",");
        writer.AppendLine($"TestClassType = {GenerateTypeReference(testMethod.TypeSymbol, testMethod.IsGenericType)},");
        writer.AppendLine($"TestMethodName = \"{methodName}\",");

        GenerateMetadata(writer, testMethod);

        if (testMethod.IsGenericType)
        {
            GenerateGenericTypeInfo(writer, testMethod.TypeSymbol);
        }

        // Generate generic method info if needed
        if (testMethod.IsGenericMethod)
        {
            GenerateGenericMethodInfo(writer, testMethod.MethodSymbol);
        }

        // Generate typed invokers and factory
        GenerateTypedInvokers(writer, testMethod, className);

        writer.Unindent();
        writer.AppendLine("};");

        if (testMethod is { IsGenericType: false, IsGenericMethod: false })
        {
            writer.AppendLine("metadata.UseRuntimeDataGeneration(testSessionId);");
        }
        else
        {
            // For generic types/methods, set TestSessionId directly
            writer.AppendLine("metadata.TestSessionId = testSessionId;");
        }

        writer.AppendLine("yield return metadata;");
    }

    private static void GenerateMetadata(CodeWriter writer, TestMethodMetadata testMethod)
    {
        var compilation = testMethod.Context!.Value.SemanticModel.Compilation;
        var methodSymbol = testMethod.MethodSymbol;

        GenerateDependencies(writer, methodSymbol);

        writer.AppendLine("AttributeFactory = static () =>");
        writer.AppendLine("[");
        writer.Indent();

        var attributes = methodSymbol.GetAttributes()
            .Where(a => !DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
            .Concat(testMethod.TypeSymbol.GetAttributesIncludingBaseTypes())
            .Concat(testMethod.TypeSymbol.ContainingAssembly.GetAttributes());

        testMethod.CompilationContext.AttributeWriter.WriteAttributes(writer, attributes);

        writer.Unindent();
        writer.AppendLine("],");

        // Extract and emit RepeatCount if present
        var repeatCount = ExtractRepeatCount(methodSymbol, testMethod.TypeSymbol);
        if (repeatCount.HasValue)
        {
            writer.AppendLine($"RepeatCount = {repeatCount.Value},");
        }

        GenerateDataSources(writer, testMethod);

        GeneratePropertyInjections(writer, testMethod.TypeSymbol, testMethod.TypeSymbol.GloballyQualified());

        // Inheritance depth
        writer.AppendLine($"InheritanceDepth = {testMethod.InheritanceDepth},");

        // File location metadata
        writer.AppendLine($"FilePath = @\"{(testMethod.FilePath ?? "").Replace("\\", "\\\\")}\",");
        writer.AppendLine($"LineNumber = {testMethod.LineNumber},");

        // Method metadata
        writer.Append("MethodMetadata = ");
        SourceInformationWriter.GenerateMethodInformation(writer, compilation, testMethod.TypeSymbol, testMethod.MethodSymbol, null, ',');
    }

    private static void GenerateMetadataForConcreteInstantiation(CodeWriter writer, TestMethodMetadata testMethod)
    {
        var compilation = testMethod.Context!.Value.SemanticModel.Compilation;
        var methodSymbol = testMethod.MethodSymbol;

        GenerateDependencies(writer, methodSymbol);

        writer.AppendLine("AttributeFactory = static () =>");
        writer.AppendLine("[");
        writer.Indent();

        // Filter out ALL data source attributes - we'll add back only the specific one if provided
        var attributes = methodSymbol.GetAttributes()
            .Where(a => !DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
            .Concat(testMethod.TypeSymbol.GetAttributesIncludingBaseTypes())
            .Concat(testMethod.TypeSymbol.ContainingAssembly.GetAttributes());

        testMethod.CompilationContext.AttributeWriter.WriteAttributes(writer, attributes);

        writer.Unindent();
        writer.AppendLine("],");

        // Extract and emit RepeatCount if present
        var repeatCount = ExtractRepeatCount(methodSymbol, testMethod.TypeSymbol);
        if (repeatCount.HasValue)
        {
            writer.AppendLine($"RepeatCount = {repeatCount.Value},");
        }

        // No data sources for concrete instantiations
        writer.AppendLine("DataSources = global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>(),");
        writer.AppendLine("ClassDataSources = global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>(),");
        writer.AppendLine("PropertyDataSources = global::System.Array.Empty<global::TUnit.Core.PropertyDataSource>(),");

        GeneratePropertyInjections(writer, testMethod.TypeSymbol, testMethod.TypeSymbol.GloballyQualified());

        // Inheritance depth
        writer.AppendLine($"InheritanceDepth = {testMethod.InheritanceDepth},");

        // File location metadata
        writer.AppendLine($"FilePath = @\"{(testMethod.FilePath ?? "").Replace("\\", "\\\\")}\",");
        writer.AppendLine($"LineNumber = {testMethod.LineNumber},");

        // Method metadata
        writer.Append("MethodMetadata = ");
        SourceInformationWriter.GenerateMethodInformation(writer, compilation, testMethod.TypeSymbol, testMethod.MethodSymbol, null, ',');
    }

    private static void GenerateDataSources(CodeWriter writer, TestMethodMetadata testMethod)
    {
        var methodSymbol = testMethod.MethodSymbol;
        var typeSymbol = testMethod.TypeSymbol;

        // Extract data source attributes from method
        var methodDataSources = methodSymbol.GetAttributes()
            .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
            .ToArray();

        // Extract data source attributes from class
        var classDataSources = typeSymbol.GetAttributesIncludingBaseTypes()
            .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
            .ToArray();

        // Generate method data sources
        if (methodDataSources.Length == 0)
        {
            writer.AppendLine("DataSources = global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>(),");
        }
        else
        {
            writer.AppendLine("DataSources = new global::TUnit.Core.IDataSourceAttribute[]");
            writer.AppendLine("{");
            writer.Indent();

            foreach (var attr in methodDataSources)
            {
                GenerateDataSourceAttribute(writer, testMethod.CompilationContext, attr, methodSymbol, typeSymbol);
            }

            writer.Unindent();
            writer.AppendLine("},");
        }

        // Generate class data sources
        if (classDataSources.Length == 0)
        {
            writer.AppendLine("ClassDataSources = global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>(),");
        }
        else
        {
            writer.AppendLine("ClassDataSources = new global::TUnit.Core.IDataSourceAttribute[]");
            writer.AppendLine("{");
            writer.Indent();

            foreach (var attr in classDataSources)
            {
                GenerateDataSourceAttribute(writer, testMethod.CompilationContext, attr, methodSymbol, typeSymbol);
            }

            writer.Unindent();
            writer.AppendLine("},");
        }

        // Generate property data sources
        GeneratePropertyDataSources(writer, testMethod);
    }

    private static void GenerateDataSourceAttribute(CodeWriter writer, CompilationContext compilationContext, AttributeData attr, IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
    {
        var attrClass = attr.AttributeClass;
        if (attrClass == null)
        {
            return;
        }

        var attrName = attrClass.GloballyQualifiedNonGeneric();

        if (attrName == "global::TUnit.Core.MethodDataSourceAttribute")
        {
            GenerateMethodDataSourceAttribute(writer, attr, typeSymbol);
        }
        else if (attrName == "global::TUnit.Core.ArgumentsAttribute")
        {
            try
            {
                GenerateArgumentsAttributeWithParameterTypes(writer, compilationContext.Compilation, attr, methodSymbol);
            }
            catch
            {
                // Fall back to default behavior if parameter type matching fails
                compilationContext.AttributeWriter.WriteAttribute(writer, attr);
                writer.AppendLine(",");
            }
        }
        else
        {
            compilationContext.AttributeWriter.WriteAttribute(writer, attr);
            writer.AppendLine(",");
        }
    }

    private static void GenerateArgumentsAttributeWithParameterTypes(CodeWriter writer, Compilation compilation, AttributeData attr, IMethodSymbol methodSymbol)
    {
        if (attr.AttributeClass == null)
        {
            return;
        }

        var attrTypeName = attr.AttributeClass.GloballyQualified();
        var testMethodParameters = methodSymbol.Parameters;

        // Get the attribute syntax to access source text (preserves precision for decimals)
        var attributeSyntax = attr.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
        if (attributeSyntax == null)
        {
            // No syntax available - fall back to TypedConstant-based formatting
            var formatter = new TypedConstantFormatter();
            writer.Append($"new {attrTypeName}(");

            if (attr.ConstructorArguments is
                [
                    { Kind: TypedConstantKind.Array } _
                ])
            {
                var arrayValues = attr.ConstructorArguments[0].Values;
                for (var i = 0; i < arrayValues.Length; i++)
                {
                    var targetType = i < testMethodParameters.Length ? testMethodParameters[i].Type : null;
                    writer.Append(formatter.FormatForCode(arrayValues[i], targetType));
                    if (i < arrayValues.Length - 1) writer.Append(", ");
                }
            }
            else
            {
                for (var i = 0; i < attr.ConstructorArguments.Length; i++)
                {
                    var targetType = i < testMethodParameters.Length ? testMethodParameters[i].Type : null;
                    writer.Append(formatter.FormatForCode(attr.ConstructorArguments[i], targetType));
                    if (i < attr.ConstructorArguments.Length - 1) writer.Append(", ");
                }
            }

            writer.AppendLine("),");
            return;
        }

        // Get the argument expressions from syntax
        var argumentList = attributeSyntax.ArgumentList;
        if (argumentList == null || argumentList.Arguments.Count == 0)
        {
            writer.AppendLine($"new {attrTypeName}(),");
            return;
        }

        // Get semantic model for rewriting expressions with fully qualified names
        var semanticModel = compilation.GetSemanticModel(attributeSyntax.SyntaxTree);

        writer.Append($"new {attrTypeName}(");

        // Only process positional arguments (exclude named arguments)
        var positionalArgs = argumentList.Arguments.Where(a => a.NameEquals == null).ToArray();

        for (var i = 0; i < positionalArgs.Length; i++)
        {
            var argumentSyntax = positionalArgs[i];
            var expression = argumentSyntax.Expression;

            // Get target parameter type
            var targetParameterType = i < testMethodParameters.Length
                ? testMethodParameters[i].Type
                : null;

            // For decimal parameters, preserve source text and add 'm' suffix (only for numeric literals)
            if (expression is not IdentifierNameSyntax
                && targetParameterType?.SpecialType == SpecialType.System_Decimal
                && expression.Kind() != SyntaxKind.StringLiteralExpression
                && expression.Kind() != SyntaxKind.NullLiteralExpression)
            {
                var sourceText = expression.ToString().TrimEnd('d', 'D', 'f', 'F', 'm', 'M').Trim();
                writer.Append($"{sourceText}m");
            }
            else
            {
                // For other types (including strings and nulls), rewrite with fully qualified names
                var fullyQualifiedExpression = expression.Accept(new FullyQualifiedWithGlobalPrefixRewriter(semanticModel))!;
                writer.Append(fullyQualifiedExpression.ToFullString());
            }

            if (i < positionalArgs.Length - 1)
            {
                writer.Append(", ");
            }
        }

        writer.Append(")");

        // Handle named arguments (like Skip property)
        var namedArgs = argumentList.Arguments.Where(a => a.NameEquals != null).ToArray();
        if (namedArgs.Length > 0)
        {
            writer.AppendLine();
            writer.AppendLine("{");
            writer.Indent();

            for (var i = 0; i < namedArgs.Length; i++)
            {
                var namedArg = namedArgs[i];
                var propertyName = namedArg.NameEquals!.Name.ToString();
                var fullyQualifiedExpression = namedArg.Expression.Accept(new FullyQualifiedWithGlobalPrefixRewriter(semanticModel))!;
                writer.Append($"{propertyName} = {fullyQualifiedExpression.ToFullString()}");

                if (i < namedArgs.Length - 1)
                {
                    writer.AppendLine(",");
                }
            }

            writer.AppendLine();
            writer.Unindent();
            writer.AppendLine("},");
        }
        else
        {
            writer.AppendLine(",");
        }
    }

    private static void GenerateMethodDataSourceAttribute(CodeWriter writer, AttributeData attr, INamedTypeSymbol typeSymbol)
    {
        // Extract method name and target type
        string? methodName = null;
        ITypeSymbol? targetType = null;

        if (attr.ConstructorArguments is
            [
                { Value: ITypeSymbol } _, _, ..
            ])
        {
            // MethodDataSource(Type, string) overload
            targetType = (ITypeSymbol?)attr.ConstructorArguments[0].Value;
            methodName = attr.ConstructorArguments[1].Value?.ToString();
        }
        else if (attr.ConstructorArguments.Length >= 1)
        {
            // MethodDataSource(string) overload
            methodName = attr.ConstructorArguments[0].Value?.ToString();
            targetType = typeSymbol;
        }

        if (string.IsNullOrEmpty(methodName))
        {
            writer.AppendLine("// Error: No method name in MethodDataSourceAttribute");
            return;
        }

        if (targetType == null)
        {
            writer.AppendLine("// Error: No target type for MethodDataSourceAttribute");
            return;
        }

        // Find the data source method, property, or field
        var dataSourceMember = targetType.GetMembers(methodName!).FirstOrDefault();
        var dataSourceMethod = dataSourceMember as IMethodSymbol;
        var dataSourceProperty = dataSourceMember as IPropertySymbol;

        if (dataSourceMember == null || (dataSourceMethod == null && dataSourceProperty == null))
        {
            // Still generate the attribute even if method not found - it will fail at runtime with proper error
            // Use CodeGenerationHelpers to properly handle any generics on the attribute
            var generatedCode = CodeGenerationHelpers.GenerateAttributeInstantiation(attr);
            writer.AppendLine($"{generatedCode},");
            return;
        }

        // Generate the attribute with factory
        // We need to manually construct this to properly add the Factory property

        // Determine if the data source is static or instance-based
        var isStatic = dataSourceMethod?.IsStatic ?? dataSourceProperty?.GetMethod?.IsStatic ?? true;

        // Use InstanceMethodDataSourceAttribute for instance-based data sources
        // This implements IAccessesInstanceData which tells the engine to create an instance early
        var attrTypeName = isStatic
            ? "global::TUnit.Core.MethodDataSourceAttribute"
            : "global::TUnit.Core.InstanceMethodDataSourceAttribute";

        if (attr.ConstructorArguments is
            [
                { Value: ITypeSymbol typeArg } _, _, ..
            ])
        {
            // MethodDataSource(Type, string) constructor - only available on MethodDataSourceAttribute
            // For instance data sources, we still use the same constructor signature
            writer.AppendLine($"new {attrTypeName}(typeof({typeArg.GloballyQualified()}), \"{methodName}\")");
        }
        else
        {
            // MethodDataSource(string) constructor
            writer.AppendLine($"new {attrTypeName}(\"{methodName}\")");
        }
        writer.AppendLine("{");
        writer.Indent();

        // Check if the attribute has Arguments property set
        var argumentsProperty = attr.NamedArguments.FirstOrDefault(x => x.Key == "Arguments");
        var hasArguments = argumentsProperty is { Key: not null, Value.IsNull: false };

        // Copy over any Arguments property if present
        if (hasArguments)
        {
            writer.Append("Arguments = ");
            WriteTypedConstant(writer, argumentsProperty.Value);
            writer.AppendLine(",");
        }

        // Set the Factory property with a strongly-typed function
        writer.AppendLine("Factory = (dataGeneratorMetadata) =>");
        writer.AppendLine("{");
        writer.Indent();

        // If we have arguments, capture them in local variables
        if (hasArguments)
        {
            writer.AppendLine("// Capture Arguments from the attribute");
            writer.Append("var arguments = ");
            WriteTypedConstant(writer, argumentsProperty.Value);
            writer.AppendLine(";");
            writer.AppendLine();
        }

        // Generate the factory implementation
        if (dataSourceMethod != null)
        {
            GenerateMethodDataSourceFactory(writer, dataSourceMethod, targetType, hasArguments);
        }
        else if (dataSourceProperty != null)
        {
            GeneratePropertyDataSourceFactory(writer, dataSourceProperty, targetType);
        }

        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("},");
    }

    private static void GenerateMethodDataSourceFactory(CodeWriter writer, IMethodSymbol dataSourceMethod, ITypeSymbol targetType, bool hasArguments)
    {
        var isStatic = dataSourceMethod.IsStatic;
        var returnType = dataSourceMethod.ReturnType;
        var fullyQualifiedType = targetType.GloballyQualified();

        // Generate async enumerable that yields Func<Task<object?[]?>>
        writer.AppendLine("async global::System.Collections.Generic.IAsyncEnumerable<global::System.Func<global::System.Threading.Tasks.Task<object?[]?>>> Factory()");
        writer.AppendLine("{");
        writer.Indent();

        // Build the method call with arguments if needed
        string methodCall;
        if (hasArguments && dataSourceMethod.Parameters.Length > 0)
        {
            // Use the captured arguments
            var argsList = string.Join(", ", dataSourceMethod.Parameters.Select((p, i) =>
                $"(({p.Type.GloballyQualified()})arguments[{i}])"));
            methodCall = $"{dataSourceMethod.Name}({argsList})";
        }
        else
        {
            methodCall = $"{dataSourceMethod.Name}()";
        }

        // For collections (IEnumerable, IAsyncEnumerable), we need to evaluate once to iterate
        // For single values, we'll generate a lambda that invokes the method each time
        returnType.ToDisplayString();

        if (IsAsyncEnumerable(returnType))
        {
            // IAsyncEnumerable<T> - must evaluate once to iterate
            if (isStatic)
            {
                writer.AppendLine($"var result = {fullyQualifiedType}.{methodCall};");
            }
            else
            {
                writer.AppendLine("object? instance;");
                writer.AppendLine("if (dataGeneratorMetadata.TestClassInstance != null)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("instance = dataGeneratorMetadata.TestClassInstance;");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine("else");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("throw new global::System.InvalidOperationException(\"Instance method data source requires TestClassInstance. This should have been provided by the engine.\");");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine($"var result = (({fullyQualifiedType})instance).{methodCall};");
            }
            writer.AppendLine();
            writer.AppendLine("await foreach (var item in result)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(item));");
            writer.Unindent();
            writer.AppendLine("}");
        }
        else if (IsTask(returnType))
        {
            // Task<T> - must evaluate and await once
            if (isStatic)
            {
                writer.AppendLine($"var result = {fullyQualifiedType}.{methodCall};");
            }
            else
            {
                writer.AppendLine("object? instance;");
                writer.AppendLine("if (dataGeneratorMetadata.TestClassInstance != null)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("instance = dataGeneratorMetadata.TestClassInstance;");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine("else");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("throw new global::System.InvalidOperationException(\"Instance method data source requires TestClassInstance. This should have been provided by the engine.\");");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine($"var result = (({fullyQualifiedType})instance).{methodCall};");
            }
            writer.AppendLine();
            writer.AppendLine("var taskResult = await result;");
            writer.AppendLine("if (taskResult is global::System.Collections.IEnumerable enumerable && !(taskResult is string))");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("foreach (var item in enumerable)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(item));");
            writer.Unindent();
            writer.AppendLine("}");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("else");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(taskResult));");
            writer.Unindent();
            writer.AppendLine("}");
        }
        else if (IsEnumerable(returnType))
        {
            // IEnumerable<T>
            if (isStatic)
            {
                writer.AppendLine($"var result = {fullyQualifiedType}.{methodCall};");
            }
            else
            {
                writer.AppendLine("object? instance;");
                writer.AppendLine("if (dataGeneratorMetadata.TestClassInstance != null)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("instance = dataGeneratorMetadata.TestClassInstance;");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine("else");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("throw new global::System.InvalidOperationException(\"Instance method data source requires TestClassInstance. This should have been provided by the engine.\");");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine($"var result = (({fullyQualifiedType})instance).{methodCall};");
            }
            writer.AppendLine();
            writer.AppendLine("if (result is global::System.Collections.IEnumerable enumerable && !(result is string))");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("foreach (var item in enumerable)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(item));");
            writer.Unindent();
            writer.AppendLine("}");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("else");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(result));");
            writer.Unindent();
            writer.AppendLine("}");
        }
        else
        {
            // Single value
            if (isStatic)
            {
                writer.AppendLine($"var result = {fullyQualifiedType}.{methodCall};");
            }
            else
            {
                writer.AppendLine("object? instance;");
                writer.AppendLine("if (dataGeneratorMetadata.TestClassInstance != null)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("instance = dataGeneratorMetadata.TestClassInstance;");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine("else");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("throw new global::System.InvalidOperationException(\"Instance method data source requires TestClassInstance. This should have been provided by the engine.\");");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine($"var result = (({fullyQualifiedType})instance).{methodCall};");
            }
            writer.AppendLine();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(result));");
        }

        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();


        writer.AppendLine("return Factory();");
    }

    private static void GeneratePropertyDataSourceFactory(CodeWriter writer, IPropertySymbol dataSourceProperty, ITypeSymbol targetType)
    {
        var isStatic = dataSourceProperty.IsStatic;
        var returnType = dataSourceProperty.Type;
        var fullyQualifiedType = targetType.GloballyQualified();

        // Generate async enumerable that yields Func<Task<object?[]?>>
        writer.AppendLine("async global::System.Collections.Generic.IAsyncEnumerable<global::System.Func<global::System.Threading.Tasks.Task<object?[]?>>> Factory()");
        writer.AppendLine("{");
        writer.Indent();

        // Properties don't have arguments, just access them
        var propertyAccess = dataSourceProperty.Name;

        if (IsAsyncEnumerable(returnType))
        {
            // IAsyncEnumerable<T> - must evaluate once to iterate
            if (isStatic)
            {
                writer.AppendLine($"var result = {fullyQualifiedType}.{propertyAccess};");
            }
            else
            {
                writer.AppendLine("object? instance;");
                writer.AppendLine("if (dataGeneratorMetadata.TestClassInstance != null)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("instance = dataGeneratorMetadata.TestClassInstance;");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine("else");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("throw new global::System.InvalidOperationException(\"Instance method data source requires TestClassInstance. This should have been provided by the engine.\");");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine($"var result = (({fullyQualifiedType})instance).{propertyAccess};");
            }
            writer.AppendLine();
            writer.AppendLine("await foreach (var item in result)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(item));");
            writer.Unindent();
            writer.AppendLine("}");
        }
        else if (IsTask(returnType))
        {
            // Task<T> - must evaluate and await once
            if (isStatic)
            {
                writer.AppendLine($"var result = {fullyQualifiedType}.{propertyAccess};");
            }
            else
            {
                writer.AppendLine("object? instance;");
                writer.AppendLine("if (dataGeneratorMetadata.TestClassInstance != null)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("instance = dataGeneratorMetadata.TestClassInstance;");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine("else");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("throw new global::System.InvalidOperationException(\"Instance method data source requires TestClassInstance. This should have been provided by the engine.\");");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine($"var result = (({fullyQualifiedType})instance).{propertyAccess};");
            }
            writer.AppendLine();
            writer.AppendLine("var taskResult = await result;");
            writer.AppendLine("if (taskResult is global::System.Collections.IEnumerable enumerable && !(taskResult is string))");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("foreach (var item in enumerable)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(item));");
            writer.Unindent();
            writer.AppendLine("}");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("else");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(taskResult));");
            writer.Unindent();
            writer.AppendLine("}");
        }
        else if (IsEnumerable(returnType))
        {
            // IEnumerable<T>
            if (isStatic)
            {
                writer.AppendLine($"var result = {fullyQualifiedType}.{propertyAccess};");
            }
            else
            {
                writer.AppendLine("object? instance;");
                writer.AppendLine("if (dataGeneratorMetadata.TestClassInstance != null)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("instance = dataGeneratorMetadata.TestClassInstance;");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine("else");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("throw new global::System.InvalidOperationException(\"Instance method data source requires TestClassInstance. This should have been provided by the engine.\");");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine($"var result = (({fullyQualifiedType})instance).{propertyAccess};");
            }
            writer.AppendLine();
            writer.AppendLine("if (result is global::System.Collections.IEnumerable enumerable && !(result is string))");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("foreach (var item in enumerable)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(item));");
            writer.Unindent();
            writer.AppendLine("}");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("else");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(result));");
            writer.Unindent();
            writer.AppendLine("}");
        }
        else
        {
            // Single value
            if (isStatic)
            {
                writer.AppendLine($"var result = {fullyQualifiedType}.{propertyAccess};");
            }
            else
            {
                writer.AppendLine("object? instance;");
                writer.AppendLine("if (dataGeneratorMetadata.TestClassInstance != null)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("instance = dataGeneratorMetadata.TestClassInstance;");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine("else");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("throw new global::System.InvalidOperationException(\"Instance method data source requires TestClassInstance. This should have been provided by the engine.\");");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine($"var result = (({fullyQualifiedType})instance).{propertyAccess};");
            }
            writer.AppendLine();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(global::TUnit.Core.Helpers.DataSourceHelpers.ToObjectArray(result));");
        }

        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();


        writer.AppendLine("return Factory();");
    }

    private static bool IsAsyncEnumerable(ITypeSymbol type)
    {
        // Use cached interface check
        return InterfaceCache.IsAsyncEnumerable(type);
    }

    private static bool IsTask(ITypeSymbol type)
    {
        var typeName = type.ToDisplayString();
        return typeName.StartsWith("System.Threading.Tasks.Task") ||
               typeName.StartsWith("System.Threading.Tasks.ValueTask");
    }

    private static bool IsEnumerable(ITypeSymbol type)
    {
        // Use cached interface check (already handles string exclusion)
        return InterfaceCache.IsEnumerable(type);
    }

    private static void WriteTypedConstant(CodeWriter writer, TypedConstant constant)
    {
        if (constant.IsNull)
        {
            writer.Append("null");
            return;
        }

        switch (constant.Kind)
        {
            case TypedConstantKind.Primitive:
                if (constant.Type?.SpecialType == SpecialType.System_String)
                {
                    var stringValue = constant.Value?.ToString() ?? "";
                    var escapedValue = stringValue
                        .Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\r", "\\r")
                        .Replace("\n", "\\n")
                        .Replace("\t", "\\t");
                    writer.Append($"\"{escapedValue}\"");
                }
                else if (constant.Type?.SpecialType == SpecialType.System_Char)
                {
                    var charValue = constant.Value?.ToString() ?? "";
                    var escapedChar = charValue
                        .Replace("\\", "\\\\")
                        .Replace("'", "\\'")
                        .Replace("\r", "\\r")
                        .Replace("\n", "\\n")
                        .Replace("\t", "\\t");
                    writer.Append($"'{escapedChar}'");
                }
                else if (constant.Type?.SpecialType == SpecialType.System_Boolean)
                {
                    writer.Append(constant.Value?.ToString()?.ToLowerInvariant() ?? "false");
                }
                else
                {
                    // Handle numeric types with InvariantCulture to ensure consistent formatting
                    if (constant.Value is IFormattable formattable)
                    {
                        writer.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        writer.Append(constant.Value?.ToString() ?? "null");
                    }
                }
                break;

            case TypedConstantKind.Type:
                var type = constant.Value as ITypeSymbol;
                writer.Append($"typeof({type?.GloballyQualified()})");
                break;

            case TypedConstantKind.Array:
                var elementType = constant.Type is IArrayTypeSymbol arrayType
                    ? arrayType.ElementType.GloballyQualified()
                    : "object";
                writer.Append($"new {elementType}[] {{ ");
                for (var i = 0; i < constant.Values.Length; i++)
                {
                    WriteTypedConstant(writer, constant.Values[i]);
                    if (i < constant.Values.Length - 1)
                    {
                        writer.Append(", ");
                    }
                }
                writer.Append(" }");
                break;

            default:
                writer.Append("null");
                break;
        }
    }

    private static void GeneratePropertyInjections(CodeWriter writer, INamedTypeSymbol typeSymbol, string className)
    {
        // Walk inheritance hierarchy to find properties with data source attributes
        var currentType = typeSymbol;
        var processedProperties = new HashSet<string>();
        var hasPropertyInjections = false;

        // First check if we have any property injections
        var tempType = currentType;
        while (tempType != null)
        {
            foreach (var member in tempType.GetMembers())
            {
                if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, SetMethod.DeclaredAccessibility: Accessibility.Public, IsStatic: false } property &&
                    !processedProperties.Contains(property.Name))
                {
                    var dataSourceAttr = property.GetAttributes()
                        .FirstOrDefault(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));

                    if (dataSourceAttr != null)
                    {
                        hasPropertyInjections = true;
                        processedProperties.Add(property.Name);
                    }
                }
            }
            tempType = tempType.BaseType;
        }

        // Reset for actual generation
        processedProperties.Clear();

        if (!hasPropertyInjections)
        {
            writer.AppendLine("PropertyInjections = global::System.Array.Empty<global::TUnit.Core.PropertyInjectionData>(),");
        }
        else
        {
            writer.AppendLine("PropertyInjections = new global::TUnit.Core.PropertyInjectionData[]");
            writer.AppendLine("{");
            writer.Indent();

            currentType = typeSymbol;
            while (currentType != null)
            {
                foreach (var member in currentType.GetMembers())
                {
                    if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, SetMethod.DeclaredAccessibility: Accessibility.Public, IsStatic: false } property &&
                        !processedProperties.Contains(property.Name))
                    {
                        var dataSourceAttr = property.GetAttributes()
                            .FirstOrDefault(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));

                        if (dataSourceAttr != null)
                        {
                            processedProperties.Add(property.Name);
                            var propertyType = property.Type.GloballyQualified();

                            writer.AppendLine("new global::TUnit.Core.PropertyInjectionData");
                            writer.AppendLine("{");
                            writer.Indent();
                            writer.AppendLine($"PropertyName = \"{property.Name}\",");
                            writer.AppendLine($"PropertyType = typeof({propertyType}),");

                            // Generate appropriate setter based on whether property is init-only
                            if (property.SetMethod.IsInitOnly)
                            {
                                // For init-only properties, use UnsafeAccessor on .NET 8+ (but not for generic types)
                                // UnsafeAccessor doesn't work with open generic types
                                // IMPORTANT: Use currentType (which is the closed generic type from the inheritance chain)
                                // instead of property.ContainingType (which is the open generic type definition)
                                var containingTypeName = currentType.GloballyQualified();
                                var isGenericContainingType = currentType.IsGenericType;

                                if (isGenericContainingType)
                                {
                                    // For init-only properties on generic types, use reflection with the closed generic type.
                                    // UnsafeAccessor doesn't work with generic base classes, but reflection does.
                                    // This is AOT-compatible because we use the closed generic type known at compile time.
                                    writer.AppendLine("Setter = (instance, value) =>");
                                    writer.AppendLine("{");
                                    writer.Indent();
                                    writer.AppendLine($"var backingField = typeof({containingTypeName}).GetField(\"<{property.Name}>k__BackingField\",");
                                    writer.AppendLine("    global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.NonPublic);");
                                    writer.AppendLine("if (backingField != null)");
                                    writer.AppendLine("{");
                                    writer.Indent();
                                    writer.AppendLine("backingField.SetValue(instance, value);");
                                    writer.Unindent();
                                    writer.AppendLine("}");
                                    writer.AppendLine("else");
                                    writer.AppendLine("{");
                                    writer.Indent();
                                    writer.AppendLine($"throw new global::System.InvalidOperationException(\"Could not find backing field for property {property.Name} on type {containingTypeName}\");");
                                    writer.Unindent();
                                    writer.AppendLine("}");
                                    writer.Unindent();
                                    writer.AppendLine("},");
                                }
                                else
                                {
                                    writer.AppendLine("#if NET8_0_OR_GREATER");
                                    // Cast to the property's containing type if needed
                                    if (containingTypeName != className)
                                    {
                                        writer.AppendLine($"Setter = (instance, value) => Get{property.Name}BackingField(({containingTypeName})instance) = ({propertyType})value,");
                                    }
                                    else
                                    {
                                        writer.AppendLine($"Setter = (instance, value) => Get{property.Name}BackingField(({className})instance) = ({propertyType})value,");
                                    }
                                    writer.AppendLine("#else");
                                    writer.AppendLine("Setter = (instance, value) => throw new global::System.NotSupportedException(\"Setting init-only properties requires .NET 8 or later\"),");
                                    writer.AppendLine("#endif");
                                }
                            }
                            else
                            {
                                // For regular properties, use normal property assignment
                                // For regular properties, use direct assignment (tuple conversion happens at runtime)
                                writer.AppendLine($"Setter = (instance, value) => (({className})instance).{property.Name} = ({propertyType})value,");
                            }

                            // ValueFactory will be provided by the TestDataCombination at runtime
                            writer.AppendLine("ValueFactory = () => throw new global::System.InvalidOperationException(\"ValueFactory should be provided by TestDataCombination\"),");

                            // Generate nested property injections
                            GenerateNestedPropertyInjections(writer, property.Type, processedProperties);

                            // Generate nested property value factory
                            GenerateNestedPropertyValueFactory(writer, property.Type);

                            writer.Unindent();
                            writer.AppendLine("},");
                        }
                    }
                }
                currentType = currentType.BaseType;
            }

            writer.Unindent();
            writer.AppendLine("},");
        }
    }

    private static void GeneratePropertyDataSources(CodeWriter writer, TestMethodMetadata testMethod)
    {
        var compilation = testMethod.Context!.Value.SemanticModel.Compilation;
        var typeSymbol = testMethod.TypeSymbol;
        var currentType = typeSymbol;
        var processedProperties = new HashSet<string>();
        var hasPropertyDataSources = false;

        // First check if we have any property data sources
        var tempType = currentType;
        while (tempType != null)
        {
            foreach (var member in tempType.GetMembers())
            {
                if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: false } property &&
                    !processedProperties.Contains(property.Name))
                {
                    var dataSourceAttr = property.GetAttributes()
                        .FirstOrDefault(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));

                    if (dataSourceAttr != null)
                    {
                        hasPropertyDataSources = true;
                        processedProperties.Add(property.Name);
                    }
                }
            }
            tempType = tempType.BaseType;
        }

        // Reset for actual generation
        processedProperties.Clear();

        if (!hasPropertyDataSources)
        {
            writer.AppendLine("PropertyDataSources = global::System.Array.Empty<global::TUnit.Core.PropertyDataSource>(),");
        }
        else
        {
            writer.AppendLine("PropertyDataSources = new global::TUnit.Core.PropertyDataSource[]");
            writer.AppendLine("{");
            writer.Indent();

            currentType = typeSymbol;
            while (currentType != null)
            {
                foreach (var member in currentType.GetMembers())
                {
                    if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: false } property &&
                        !processedProperties.Contains(property.Name))
                    {
                        var dataSourceAttr = property.GetAttributes()
                            .FirstOrDefault(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));

                        if (dataSourceAttr != null)
                        {
                            processedProperties.Add(property.Name);

                            writer.AppendLine("new global::TUnit.Core.PropertyDataSource");
                            writer.AppendLine("{");
                            writer.Indent();
                            writer.AppendLine($"PropertyName = \"{property.Name}\",");
                            writer.AppendLine($"PropertyType = typeof({property.Type.GloballyQualified()}),");
                            writer.Append("DataSource = ");
                            GenerateDataSourceAttribute(writer, testMethod.CompilationContext, dataSourceAttr, testMethod.MethodSymbol, typeSymbol);
                            writer.Unindent();
                            writer.AppendLine("},");
                        }
                    }
                }
                currentType = currentType.BaseType;
            }

            writer.Unindent();
            writer.AppendLine("},");
        }
    }

    private static void GenerateNestedPropertyInjections(CodeWriter writer, ITypeSymbol propertyType, HashSet<string> processedProperties)
    {
        // Only generate nested injections for reference types that aren't basic types
        if (ShouldGenerateNestedInjections(propertyType))
        {
            writer.AppendLine("NestedPropertyInjections = new global::TUnit.Core.PropertyInjectionData[]");
            writer.AppendLine("{");
            writer.Indent();
            GeneratePropertyInjectionsForType(writer, propertyType, processedProperties);
            writer.Unindent();
            writer.AppendLine("},");
        }
        else
        {
            writer.AppendLine("NestedPropertyInjections = global::System.Array.Empty<global::TUnit.Core.PropertyInjectionData>(),");
        }
    }

    private static void GenerateNestedPropertyValueFactory(CodeWriter writer, ITypeSymbol propertyType)
    {
        writer.AppendLine("NestedPropertyValueFactory = obj =>");
        writer.AppendLine("{");
        writer.Indent();

        if (ShouldGenerateNestedInjections(propertyType))
        {
            writer.AppendLine("var nestedValues = new global::System.Collections.Generic.Dictionary<string, object?>();");

            // Generate code to extract property values from the nested object
            GeneratePropertyValueExtraction(writer, propertyType);

            writer.AppendLine("return nestedValues;");
        }
        else
        {
            writer.AppendLine("return new global::System.Collections.Generic.Dictionary<string, object?>();");
        }

        writer.Unindent();
        writer.AppendLine("}");
    }

    private static bool ShouldGenerateNestedInjections(ITypeSymbol type)
    {
        // Don't generate for basic types, primitives, or system types
        if (type.TypeKind == TypeKind.Enum ||
            type.SpecialType != SpecialType.None ||
            type.ToDisplayString().StartsWith("System."))
        {
            return false;
        }

        // Don't generate for arrays or collections
        if (type.TypeKind == TypeKind.Array)
        {
            return false;
        }

        // Check if type has any properties with data source attributes
        return type.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p => p.GetAttributes()
                .Any(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass)));
    }

    private static void GeneratePropertyInjectionsForType(CodeWriter writer, ITypeSymbol typeSymbol, HashSet<string> processedProperties)
    {
        var currentType = typeSymbol;
        var nestedProcessedProperties = new HashSet<string>(processedProperties);

        while (currentType != null)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, SetMethod.DeclaredAccessibility: Accessibility.Public, IsStatic: false } property &&
                    !nestedProcessedProperties.Contains(property.Name))
                {
                    var dataSourceAttr = property.GetAttributes()
                        .FirstOrDefault(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));

                    if (dataSourceAttr != null)
                    {
                        nestedProcessedProperties.Add(property.Name);
                        var propertyType = property.Type.GloballyQualified();
                        var className = typeSymbol.GloballyQualified();

                        writer.AppendLine("new global::TUnit.Core.PropertyInjectionData");
                        writer.AppendLine("{");
                        writer.Indent();
                        writer.AppendLine($"PropertyName = \"{property.Name}\",");
                        writer.AppendLine($"PropertyType = typeof({propertyType}),");

                        // Generate setter
                        if (property.SetMethod.IsInitOnly)
                        {
                            // For nested init-only properties with ClassDataSource, create the value if null
                            if (dataSourceAttr != null &&
                                dataSourceAttr.AttributeClass?.IsOrInherits("global::TUnit.Core.ClassDataSourceAttribute") == true &&
                                dataSourceAttr.AttributeClass is { IsGenericType: true, TypeArguments.Length: > 0 })
                            {
                                var dataSourceType = dataSourceAttr.AttributeClass.TypeArguments[0];
                                var fullyQualifiedType = dataSourceType.GloballyQualified();

                                writer.AppendLine("Setter = (instance, value) =>");
                                writer.AppendLine("{");
                                writer.Indent();
                                writer.AppendLine("// If value is null, create it using Activator");
                                writer.AppendLine("if (value == null)");
                                writer.AppendLine("{");
                                writer.Indent();
                                writer.AppendLine($"value = global::System.Activator.CreateInstance<{fullyQualifiedType}>();");
                                writer.Unindent();
                                writer.AppendLine("}");
                                writer.AppendLine($"var backingField = instance.GetType().GetField(\"<{property.Name}>k__BackingField\", ");
                                writer.AppendLine("    global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.NonPublic);");
                                writer.AppendLine("if (backingField != null)");
                                writer.AppendLine("{");
                                writer.Indent();
                                writer.AppendLine("backingField.SetValue(instance, value);");
                                writer.Unindent();
                                writer.AppendLine("}");
                                writer.AppendLine("else");
                                writer.AppendLine("{");
                                writer.Indent();
                                writer.AppendLine($"throw new global::System.InvalidOperationException(\"Could not find backing field for property {property.Name}\");");
                                writer.Unindent();
                                writer.AppendLine("}");
                                writer.Unindent();
                                writer.AppendLine("},");
                            }
                            else
                            {
                                // For other init-only properties, use reflection to set the backing field
                                writer.AppendLine("Setter = (instance, value) =>");
                                writer.AppendLine("{");
                                writer.Indent();
                                writer.AppendLine($"var backingField = instance.GetType().GetField(\"<{property.Name}>k__BackingField\", ");
                                writer.AppendLine("    global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.NonPublic);");
                                writer.AppendLine("if (backingField != null)");
                                writer.AppendLine("{");
                                writer.Indent();
                                writer.AppendLine("backingField.SetValue(instance, value);");
                                writer.Unindent();
                                writer.AppendLine("}");
                                writer.AppendLine("else");
                                writer.AppendLine("{");
                                writer.Indent();
                                writer.AppendLine($"throw new global::System.InvalidOperationException(\"Could not find backing field for property {property.Name}\");");
                                writer.Unindent();
                                writer.AppendLine("}");
                                writer.Unindent();
                                writer.AppendLine("},");
                            }
                        }
                        else
                        {
                            // For regular properties, use direct assignment
                            writer.AppendLine($"Setter = (instance, value) => (({className})instance).{property.Name} = ({propertyType})value,");
                        }

                        writer.AppendLine("ValueFactory = () => throw new global::System.InvalidOperationException(\"ValueFactory should be provided by TestDataCombination\")");

                        writer.Unindent();
                        writer.AppendLine("},");
                    }
                }
            }
            currentType = currentType.BaseType;
        }
    }

    private static void GeneratePropertyValueExtraction(CodeWriter writer, ITypeSymbol typeSymbol)
    {
        var currentType = typeSymbol;
        var processedProperties = new HashSet<string>();
        var className = typeSymbol.GloballyQualified();

        // Generate a single cast check and extract all properties
        var propertiesWithDataSource = new List<IPropertySymbol>();

        while (currentType != null)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, GetMethod.DeclaredAccessibility: Accessibility.Public, IsStatic: false } property &&
                    !processedProperties.Contains(property.Name))
                {
                    var dataSourceAttr = property.GetAttributes()
                        .FirstOrDefault(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));

                    if (dataSourceAttr != null)
                    {
                        processedProperties.Add(property.Name);
                        propertiesWithDataSource.Add(property);
                    }
                }
            }
            currentType = currentType.BaseType;
        }

        // Generate a single if statement with all property extractions
        if (propertiesWithDataSource.Any())
        {
            writer.AppendLine($"if (obj is {className} typedObj)");
            writer.AppendLine("{");
            writer.Indent();

            foreach (var property in propertiesWithDataSource)
            {
                writer.AppendLine($"nestedValues[\"{property.Name}\"] = typedObj.{property.Name};");
            }

            writer.Unindent();
            writer.AppendLine("}");
        }
    }

    private static void GenerateTypedInvokers(CodeWriter writer, TestMethodMetadata testMethod, string className)
    {
        var methodName = testMethod.MethodSymbol.Name;
        var parameters = testMethod.MethodSymbol.Parameters;

        // Check if last parameter is CancellationToken (regardless of whether it has a default value)
        var hasCancellationToken = parameters.Length > 0 &&
            parameters.Last().Type.GloballyQualified() == "global::System.Threading.CancellationToken";

        // Parameters that come from args (excluding CancellationToken)
        var parametersFromArgs = hasCancellationToken
            ? parameters.Take(parameters.Length - 1).ToArray()
            : parameters.ToArray();

        // Use centralized instance factory generator for all types (generic and non-generic)
        InstanceFactoryGenerator.GenerateInstanceFactory(writer, testMethod.TypeSymbol, testMethod);

        // Generate InvokeTypedTest for non-generic tests
        var returnPattern = GetReturnPattern(testMethod.MethodSymbol);
        if (testMethod is { IsGenericType: false, IsGenericMethod: false })
        {
            GenerateConcreteTestInvoker(writer, methodName, returnPattern, hasCancellationToken, parametersFromArgs);
        }
    }

    private static void GenerateConcreteTestInvoker(CodeWriter writer, string methodName, TestReturnPattern returnPattern, bool hasCancellationToken, IParameterSymbol[] parametersFromArgs)
    {
        // Generate InvokeTypedTest which is required by CreateExecutableTestFactory
        writer.AppendLine("InvokeTypedTest = static (instance, args, cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();

        // Wrap entire lambda body in try-catch to handle synchronous exceptions
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();

        // Only declare context if it's needed (when hasCancellationToken is true and there are parameters)
        if (hasCancellationToken && parametersFromArgs.Length > 0)
        {
            writer.AppendLine("var context = global::TUnit.Core.TestContext.Current;");
        }

        // Special case: Single tuple parameter (same as in TestInvoker)
        // If we have exactly one parameter that's a tuple type, we need to handle it specially
        // In source-generated mode, tuples are always unwrapped into their elements
        if (parametersFromArgs is
            [
                { Type: INamedTypeSymbol { IsTupleType: true } singleTupleParam }
            ])
        {
            writer.AppendLine("// Special handling for single tuple parameter");
            writer.AppendLine($"if (args.Length == {singleTupleParam.TupleElements.Length})");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("// Arguments are unwrapped tuple elements, reconstruct the tuple");

            // Build tuple reconstruction with proper casting
            var tupleElements = singleTupleParam.TupleElements.Select((elem, i) =>
                $"global::TUnit.Core.Helpers.CastHelper.Cast<{elem.Type.GloballyQualified()}>(args[{i}])");
            var tupleConstruction = $"({string.Join(", ", tupleElements)})";

            var methodCallReconstructed = hasCancellationToken
                ? $"instance.{methodName}({tupleConstruction}, cancellationToken)"
                : $"instance.{methodName}({tupleConstruction})";
            GenerateReturnHandling(writer, methodCallReconstructed, returnPattern);
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("else if (args.Length == 1 && global::TUnit.Core.Helpers.DataSourceHelpers.IsTuple(args[0]))");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("// Rare case: tuple is wrapped as a single argument");
            var methodCallDirect = hasCancellationToken
                ? $"instance.{methodName}(global::TUnit.Core.Helpers.CastHelper.Cast<{singleTupleParam.GloballyQualified()}>(args[0]), cancellationToken)"
                : $"instance.{methodName}(global::TUnit.Core.Helpers.CastHelper.Cast<{singleTupleParam.GloballyQualified()}>(args[0]))";
            GenerateReturnHandling(writer, methodCallDirect, returnPattern);
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("else");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"throw new global::System.ArgumentException($\"Expected {singleTupleParam.TupleElements.Length} unwrapped elements or 1 wrapped tuple, but got {{args.Length}} arguments\");");
            writer.Unindent();
            writer.AppendLine("}");
        }
        else if (parametersFromArgs.Length == 0)
        {
            var typedMethodCall = hasCancellationToken
                ? $"instance.{methodName}(cancellationToken)"
                : $"instance.{methodName}()";
            GenerateReturnHandling(writer, typedMethodCall, returnPattern);
        }
        else
        {
            // Count required parameters (those without default values, excluding CancellationToken and params parameters)
            var requiredParamCount = parametersFromArgs.Count(p => !p.HasExplicitDefaultValue && p is { IsOptional: false, IsParams: false });

            // Generate runtime logic to handle variable argument counts
            writer.AppendLine("switch (args.Length)");
            writer.AppendLine("{");
            writer.Indent();

            // Check if last parameter is params array
            var hasParams = parametersFromArgs.Length > 0 && parametersFromArgs[parametersFromArgs.Length - 1].IsParams;

            // For params arrays, we need to handle any number of arguments >= required count
            // Generate a reasonable number of cases plus a default that handles the rest
            var casesToGenerate = hasParams ? 10 : parametersFromArgs.Length - requiredParamCount + 1;

            // Generate cases for each valid argument count
            for (var i = 0; i < casesToGenerate && requiredParamCount + i <= parametersFromArgs.Length + 5; i++)
            {
                var argCount = requiredParamCount + i;
                writer.AppendLine($"case {argCount}:");
                writer.Indent();
                writer.AppendLine("{");
                writer.Indent();

                // Build the arguments to pass, handling params arrays correctly
                var argsToPass = TupleArgumentHelper.GenerateArgumentAccessWithParams(parametersFromArgs, "args", argCount);

                // Add CancellationToken if present
                if (hasCancellationToken)
                {
                    argsToPass.Add("context?.Execution.CancellationToken ?? global::System.Threading.CancellationToken.None");
                }

                var typedMethodCall = $"instance.{methodName}({string.Join(", ", argsToPass)})";
                GenerateReturnHandling(writer, typedMethodCall, returnPattern);
                writer.Unindent();
                writer.AppendLine("}");
                writer.Unindent();
            }

            writer.AppendLine("default:");
            writer.Indent();
            if (requiredParamCount == parametersFromArgs.Length && !hasParams)
            {
                writer.AppendLine($"throw new global::System.ArgumentException($\"Expected exactly {parametersFromArgs.Length} argument{(parametersFromArgs.Length == 1 ? "" : "s")}, but got {{args.Length}}\");");
            }
            else
            {
                writer.AppendLine($"throw new global::System.ArgumentException($\"Expected between {requiredParamCount} and {parametersFromArgs.Length} arguments, but got {{args.Length}}\");");
            }
            writer.Unindent();

            writer.Unindent();
            writer.AppendLine("}");
        }

        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch (global::System.Exception ex)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("return new global::System.Threading.Tasks.ValueTask(global::System.Threading.Tasks.Task.FromException(ex));");
        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("},");
    }

    private static void GenerateEnumerateTestDescriptors(CodeWriter writer, TestMethodMetadata testMethod)
    {
        var methodName = testMethod.MethodSymbol.Name;
        var namespaceName = testMethod.TypeSymbol.ContainingNamespace?.ToDisplayString() ?? "";
        var simpleClassName = testMethod.TypeSymbol.GetNestedClassName();
        var fullyQualifiedName = string.IsNullOrEmpty(namespaceName)
            ? $"{simpleClassName}.{methodName}"
            : $"{namespaceName}.{simpleClassName}.{methodName}";

        // Extract categories from CategoryAttribute at compile time
        var categories = ExtractCategories(testMethod);
        var categoriesArray = categories.Count == 0
            ? "global::System.Array.Empty<string>()"
            : $"new string[] {{ {string.Join(", ", categories.Select(c => $"\"{EscapeString(c)}\""))} }}";

        // Extract properties from PropertyAttribute at compile time
        var properties = ExtractProperties(testMethod);
        var propertiesArray = properties.Length == 0
            ? "global::System.Array.Empty<string>()"
            : $"new string[] {{ {string.Join(", ", properties.Select(p => $"\"{EscapeString(p)}\""))} }}";

        // Check if test has data sources
        var hasDataSource = HasDataSources(testMethod);

        // Extract repeat count from RepeatAttribute
        var repeatCount = ExtractRepeatCount(testMethod);

        // Extract dependencies from DependsOnAttribute
        var dependsOn = ExtractDependsOn(testMethod);
        var dependsOnArray = dependsOn.Length == 0
            ? "global::System.Array.Empty<string>()"
            : $"new string[] {{ {string.Join(", ", dependsOn.Select(d => $"\"{EscapeString(d)}\""))} }}";

        writer.AppendLine("public global::System.Collections.Generic.IEnumerable<global::TUnit.Core.TestDescriptor> EnumerateTestDescriptors()");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine("yield return new global::TUnit.Core.TestDescriptor");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"TestId = \"{EscapeString(fullyQualifiedName)}\",");
        writer.AppendLine($"ClassName = \"{EscapeString(simpleClassName)}\",");
        writer.AppendLine($"MethodName = \"{EscapeString(methodName)}\",");
        writer.AppendLine($"FullyQualifiedName = \"{EscapeString(fullyQualifiedName)}\",");
        writer.AppendLine($"FilePath = @\"{(testMethod.FilePath ?? "").Replace("\\", "\\\\")}\",");
        writer.AppendLine($"LineNumber = {testMethod.LineNumber},");
        writer.AppendLine($"Categories = {categoriesArray},");
        writer.AppendLine($"Properties = {propertiesArray},");
        writer.AppendLine($"HasDataSource = {(hasDataSource ? "true" : "false")},");
        writer.AppendLine($"RepeatCount = {repeatCount},");
        writer.AppendLine($"DependsOn = {dependsOnArray},");
        writer.AppendLine("Materializer = GetTestsAsync");

        writer.Unindent();
        writer.AppendLine("};");

        writer.Unindent();
        writer.AppendLine("}");
    }

    private static HashSet<string> ExtractCategories(TestMethodMetadata testMethod)
    {
        var categories = new HashSet<string>();

        // Check method attributes
        foreach (var attr in testMethod.MethodAttributes)
        {
            if (attr.AttributeClass?.Name == "CategoryAttribute" &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is string category)
            {
                categories.Add(category);
            }
        }

        // Check class attributes
        foreach (var attr in testMethod.TypeSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "CategoryAttribute" &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is string category)
            {
                categories.Add(category);
            }
        }

        // Check assembly attributes
        foreach (var attr in testMethod.TypeSymbol.ContainingAssembly.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "CategoryAttribute" &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is string category)
            {
                categories.Add(category);
            }
        }

        return categories;
    }

    private static string[] ExtractProperties(TestMethodMetadata testMethod)
    {
        var properties = new List<string>();

        // Check method attributes
        foreach (var attr in testMethod.MethodAttributes)
        {
            if (attr.AttributeClass?.Name == "PropertyAttribute" &&
                attr.ConstructorArguments is [{ Value: string key } _, _, ..] &&
                attr.ConstructorArguments[1].Value is string value)
            {
                properties.Add($"{key}={value}");
            }
        }

        // Check class attributes
        foreach (var attr in testMethod.TypeSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "PropertyAttribute" &&
                attr.ConstructorArguments.Length >= 2 &&
                attr.ConstructorArguments[0].Value is string key &&
                attr.ConstructorArguments[1].Value is string value)
            {
                properties.Add($"{key}={value}");
            }
        }

        // Check assembly attributes
        foreach (var attr in testMethod.TypeSymbol.ContainingAssembly.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "PropertyAttribute" &&
                attr.ConstructorArguments.Length >= 2 &&
                attr.ConstructorArguments[0].Value is string key &&
                attr.ConstructorArguments[1].Value is string value)
            {
                properties.Add($"{key}={value}");
            }
        }

        return properties.Distinct().ToArray();
    }

    private static bool HasDataSources(TestMethodMetadata testMethod)
    {
        // Check for data source attributes on the method
        foreach (var attr in testMethod.MethodAttributes)
        {
            if (DataSourceAttributeHelper.IsDataSourceAttribute(attr.AttributeClass))
            {
                return true;
            }
        }

        // Check for data source attributes on the class
        foreach (var attr in testMethod.TypeSymbol.GetAttributes())
        {
            if (DataSourceAttributeHelper.IsDataSourceAttribute(attr.AttributeClass))
            {
                return true;
            }
        }

        return false;
    }

    private static int ExtractRepeatCount(TestMethodMetadata testMethod)
    {
        // Check method attributes first
        foreach (var attr in testMethod.MethodAttributes)
        {
            if (attr.AttributeClass?.Name == "RepeatAttribute" &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is int count)
            {
                return count;
            }
        }

        // Check class attributes
        foreach (var attr in testMethod.TypeSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "RepeatAttribute" &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is int count)
            {
                return count;
            }
        }

        return 0;
    }

    private static string[] ExtractDependsOn(TestMethodMetadata testMethod)
    {
        var dependencies = new List<string>();

        // Check method attributes
        foreach (var attr in testMethod.MethodAttributes)
        {
            var attrClass = attr.AttributeClass;
            if (attrClass == null)
            {
                continue;
            }

            // Handle DependsOnAttribute and DependsOnAttribute<T>
            if (attrClass.Name == "DependsOnAttribute" ||
                (attrClass.IsGenericType && attrClass.ConstructedFrom?.Name == "DependsOnAttribute"))
            {
                string? className = null;
                string? methodName = null;

                // For generic DependsOnAttribute<T>, get the type argument
                if (attrClass.IsGenericType && attrClass.TypeArguments.Length > 0)
                {
                    className = attrClass.TypeArguments[0].Name;
                }

                // Check constructor arguments for class type and method name
                for (int i = 0; i < attr.ConstructorArguments.Length; i++)
                {
                    var arg = attr.ConstructorArguments[i];
                    // Skip array arguments (like parameterTypes)
                    if (arg.Kind == TypedConstantKind.Array)
                    {
                        continue;
                    }
                    if (arg.Value is INamedTypeSymbol typeSymbol)
                    {
                        className = typeSymbol.Name;
                    }
                    else if (arg.Value is string strValue)
                    {
                        methodName = strValue;
                    }
                }

                // Format: "ClassName:MethodName", ":MethodName" for same-class, "ClassName:" for all in class
                var dependency = $"{className ?? ""}:{methodName ?? ""}";
                if (dependency != ":")
                {
                    dependencies.Add(dependency);
                }
            }
        }

        // Check class attributes (class-level DependsOn)
        foreach (var attr in testMethod.TypeSymbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass == null)
            {
                continue;
            }

            if (attrClass.Name == "DependsOnAttribute" ||
                (attrClass.IsGenericType && attrClass.ConstructedFrom?.Name == "DependsOnAttribute"))
            {
                string? className = null;
                string? methodName = null;

                if (attrClass.IsGenericType && attrClass.TypeArguments.Length > 0)
                {
                    className = attrClass.TypeArguments[0].Name;
                }

                for (int i = 0; i < attr.ConstructorArguments.Length; i++)
                {
                    var arg = attr.ConstructorArguments[i];
                    // Skip array arguments (like parameterTypes)
                    if (arg.Kind == TypedConstantKind.Array)
                    {
                        continue;
                    }
                    if (arg.Value is INamedTypeSymbol typeSymbol)
                    {
                        className = typeSymbol.Name;
                    }
                    else if (arg.Value is string strValue)
                    {
                        methodName = strValue;
                    }
                }

                var dependency = $"{className ?? ""}:{methodName ?? ""}";
                if (dependency != ":")
                {
                    dependencies.Add(dependency);
                }
            }
        }

        return dependencies.Distinct().ToArray();
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static void GenerateModuleInitializer(CodeWriter writer, TestMethodMetadata testMethod, string uniqueClassName)
    {
        writer.AppendLine();
        writer.AppendLine($"internal static class {uniqueClassName.Replace("_TestSource", "_ModuleInitializer")}");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
        writer.AppendLine("public static void Initialize()");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"global::TUnit.Core.SourceRegistrar.Register({GenerateTypeReference(testMethod.TypeSymbol, testMethod.IsGenericType)}, new {uniqueClassName}());");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
    }

    private enum TestReturnPattern
    {
        Void,        // void methods
        ValueTask,   // ValueTask or ValueTask<T>
        Task,        // Task or Task<T>
        Unknown      // F# Async, custom awaitables, etc.
    }

    private static TestReturnPattern GetReturnPattern(IMethodSymbol method)
    {
        if (method.ReturnType.SpecialType == SpecialType.System_Void)
        {
            return TestReturnPattern.Void;
        }

        var returnTypeName = method.ReturnType.ToDisplayString();

        if (returnTypeName.StartsWith("System.Threading.Tasks.ValueTask"))
        {
            return TestReturnPattern.ValueTask;
        }

        if (returnTypeName.StartsWith("System.Threading.Tasks.Task") ||
            returnTypeName.StartsWith("Task<"))
        {
            return TestReturnPattern.Task;
        }

        return TestReturnPattern.Unknown;
    }

    private static void GenerateReturnHandling(
        CodeWriter writer,
        string methodCall,
        TestReturnPattern returnPattern)
    {
        switch (returnPattern)
        {
            case TestReturnPattern.Void:
                writer.AppendLine($"{methodCall};");
                writer.AppendLine("return default(global::System.Threading.Tasks.ValueTask);");
                break;

            case TestReturnPattern.ValueTask:
                writer.AppendLine($"return {methodCall};");
                break;

            case TestReturnPattern.Task:
                writer.AppendLine($"return new global::System.Threading.Tasks.ValueTask({methodCall});");
                break;

            case TestReturnPattern.Unknown:
                // F# Async, custom awaitables
                writer.AppendLine($"var methodResult = {methodCall};");
                writer.AppendLine("if (methodResult == null) return default(global::System.Threading.Tasks.ValueTask);");
                writer.AppendLine("return global::TUnit.Core.AsyncConvert.ConvertObject(methodResult);");
                break;
        }
    }

    private static void GenerateDependencies(CodeWriter writer, IMethodSymbol methodSymbol)
    {
        var dependsOnAttributes = methodSymbol.GetAttributes()
            .Concat(methodSymbol.ContainingType.GetAttributes())
            .Where(attr => attr.AttributeClass?.Name == "DependsOnAttribute" &&
                          attr.AttributeClass.ContainingNamespace?.ToDisplayString() == "TUnit.Core")
            .ToArray();

        if (!dependsOnAttributes.Any())
        {
            writer.AppendLine("Dependencies = global::System.Array.Empty<global::TUnit.Core.TestDependency>(),");
            return;
        }

        writer.AppendLine("Dependencies = new global::TUnit.Core.TestDependency[]");
        writer.AppendLine("{");
        writer.Indent();

        for (var i = 0; i < dependsOnAttributes.Length; i++)
        {
            var attr = dependsOnAttributes[i];
            GenerateTestDependency(writer, attr);

            if (i < dependsOnAttributes.Length - 1)
            {
                writer.AppendLine(",");
            }
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    private static void GenerateTestDependency(CodeWriter writer, AttributeData attributeData)
    {
        var constructorArgs = attributeData.ConstructorArguments;

        // Extract ProceedOnFailure property value
        var proceedOnFailure = GetProceedOnFailureValue(attributeData);

        // Check if this is a generic DependsOnAttribute<T> - extract the type from the type argument
        ITypeSymbol? genericTypeArgument = null;
        if (attributeData.AttributeClass is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } genericAttr)
        {
            genericTypeArgument = genericAttr.TypeArguments[0];
        }

        // Handle the different constructor overloads of DependsOnAttribute
        if (constructorArgs.Length == 0 && genericTypeArgument != null)
        {
            // DependsOnAttribute<T>() - dependency on all tests in class T
            var className = genericTypeArgument.GloballyQualified();
            var genericArity = genericTypeArgument is INamedTypeSymbol { IsGenericType: true } namedType
                ? namedType.Arity
                : 0;
            writer.AppendLine($"new global::TUnit.Core.TestDependency {{ ClassType = typeof({className}), ClassGenericArity = {genericArity}, ProceedOnFailure = {proceedOnFailure.ToString().ToLower()} }}");
        }
        else if (constructorArgs.Length == 1)
        {
            var arg = constructorArgs[0];
            if (arg.Type?.Name == "String")
            {
                var testName = arg.Value?.ToString() ?? "";

                if (genericTypeArgument != null)
                {
                    // DependsOnAttribute<T>(string testName) - dependency on specific test in class T
                    var className = genericTypeArgument.GloballyQualified();
                    var genericArity = genericTypeArgument is INamedTypeSymbol { IsGenericType: true } namedType
                        ? namedType.Arity
                        : 0;
                    writer.AppendLine($"new global::TUnit.Core.TestDependency {{ ClassType = typeof({className}), ClassGenericArity = {genericArity}, MethodName = \"{testName}\", ProceedOnFailure = {proceedOnFailure.ToString().ToLower()} }}");
                }
                else
                {
                    // DependsOnAttribute(string testName) - dependency on test in same class
                    writer.AppendLine($"new global::TUnit.Core.TestDependency {{ MethodName = \"{testName}\", ProceedOnFailure = {proceedOnFailure.ToString().ToLower()} }}");
                }
            }
            else if (arg.Type?.TypeKind == TypeKind.Class || arg.Type?.Name == "Type")
            {
                // DependsOnAttribute(Type testClass) - dependency on all tests in a class
                if (arg.Value is ITypeSymbol classType)
                {
                    var className = classType.GloballyQualified();
                    var genericArity = classType is INamedTypeSymbol { IsGenericType: true } namedType
                        ? namedType.Arity
                        : 0;
                    writer.AppendLine($"new global::TUnit.Core.TestDependency {{ ClassType = typeof({className}), ClassGenericArity = {genericArity}, ProceedOnFailure = {proceedOnFailure.ToString().ToLower()} }}");
                }
            }
        }
        else if (constructorArgs.Length == 2)
        {
            var firstArg = constructorArgs[0];
            var secondArg = constructorArgs[1];

            if (firstArg.Type?.Name == "String" && secondArg.Type is IArrayTypeSymbol)
            {
                var testName = firstArg.Value?.ToString() ?? "";

                if (genericTypeArgument != null)
                {
                    // DependsOnAttribute<T>(string testName, Type[] parameterTypes) - dependency on specific test with parameters in class T
                    var className = genericTypeArgument.GloballyQualified();
                    var genericArity = genericTypeArgument is INamedTypeSymbol { IsGenericType: true } namedType
                        ? namedType.Arity
                        : 0;
                    writer.Append($"new global::TUnit.Core.TestDependency {{ ClassType = typeof({className}), ClassGenericArity = {genericArity}, MethodName = \"{testName}\"");
                }
                else
                {
                    // DependsOnAttribute(string testName, Type[] parameterTypes)
                    writer.Append($"new global::TUnit.Core.TestDependency {{ MethodName = \"{testName}\"");
                }

                // Handle parameter types
                if (secondArg.Values.Length > 0)
                {
                    writer.Append(", MethodParameters = new global::System.Type[] { ");
                    for (var i = 0; i < secondArg.Values.Length; i++)
                    {
                        if (secondArg.Values[i].Value is ITypeSymbol paramType)
                        {
                            writer.Append($"typeof({paramType.GloballyQualified()})");
                            if (i < secondArg.Values.Length - 1)
                            {
                                writer.Append(", ");
                            }
                        }
                    }
                    writer.Append(" }");
                }

                writer.AppendLine($", ProceedOnFailure = {proceedOnFailure.ToString().ToLower()} }}");
            }
            else if (firstArg.Type?.TypeKind == TypeKind.Class || firstArg.Type?.Name == "Type")
            {
                // DependsOnAttribute(Type testClass, string testName)
                var classType = firstArg.Value as ITypeSymbol;
                var testName = secondArg.Value?.ToString() ?? "";

                if (classType != null)
                {
                    var className = classType.GloballyQualified();
                    var genericArity = classType is INamedTypeSymbol { IsGenericType: true } namedType
                        ? namedType.Arity
                        : 0;
                    writer.AppendLine($"new global::TUnit.Core.TestDependency {{ ClassType = typeof({className}), ClassGenericArity = {genericArity}, MethodName = \"{testName}\", ProceedOnFailure = {proceedOnFailure.ToString().ToLower()} }}");
                }
            }
        }
        else if (constructorArgs.Length == 3)
        {
            // DependsOnAttribute(Type testClass, string testName, Type[] parameterTypes)
            var classType = constructorArgs[0].Value as ITypeSymbol;
            var testName = constructorArgs[1].Value?.ToString() ?? "";

            if (classType != null)
            {
                var className = classType.GloballyQualified();
                var genericArity = classType is INamedTypeSymbol { IsGenericType: true } namedType
                    ? namedType.Arity
                    : 0;
                writer.Append($"new global::TUnit.Core.TestDependency {{ ClassType = typeof({className}), ClassGenericArity = {genericArity}, MethodName = \"{testName}\"");

                // Handle parameter types
                var paramTypesArg = constructorArgs[2];
                if (paramTypesArg.Values.Length > 0)
                {
                    writer.Append(", MethodParameters = new global::System.Type[] { ");
                    for (var i = 0; i < paramTypesArg.Values.Length; i++)
                    {
                        if (paramTypesArg.Values[i].Value is ITypeSymbol paramType)
                        {
                            writer.Append($"typeof({paramType.GloballyQualified()})");
                            if (i < paramTypesArg.Values.Length - 1)
                            {
                                writer.Append(", ");
                            }
                        }
                    }
                    writer.Append(" }");
                }

                writer.AppendLine($", ProceedOnFailure = {proceedOnFailure.ToString().ToLower()} }}");
            }
        }
    }

    private static bool GetProceedOnFailureValue(AttributeData attributeData)
    {
        // Look for ProceedOnFailure property in named arguments
        foreach (var namedArg in attributeData.NamedArguments)
        {
            if (namedArg is { Key: "ProceedOnFailure", Value.Value: bool proceedOnFailure })
            {
                return proceedOnFailure;
            }
        }

        // Default value is false
        return false;
    }

    private static bool IsMethodHiding(IMethodSymbol derivedMethod, IMethodSymbol baseMethod)
    {
        // Must have same name
        if (derivedMethod.Name != baseMethod.Name)
        {
            return false;
        }

        // Must NOT be an override (overrides are different from hiding)
        if (derivedMethod.IsOverride)
        {
            return false;
        }

        // Must have matching parameters
        if (!ParametersMatch(derivedMethod.Parameters, baseMethod.Parameters))
        {
            return false;
        }

        // Derived method's containing type must be derived from base method's containing type
        var derivedType = derivedMethod.ContainingType;
        var baseType = baseMethod.ContainingType;

        // Can't hide yourself
        if (SymbolEqualityComparer.Default.Equals(derivedType.OriginalDefinition, baseType.OriginalDefinition))
        {
            return false;
        }

        // Check if derived type inherits from base type
        var current = derivedType.BaseType;
        while (current is not null && current.SpecialType != SpecialType.System_Object)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType.OriginalDefinition))
            {
                return true;
            }
            current = current.BaseType;
        }

        return false;
    }

    private static List<IMethodSymbol> CollectInheritedTestMethods(INamedTypeSymbol derivedClass)
    {
        var allTestMethods = derivedClass.GetMembersIncludingBase()
            .OfType<IMethodSymbol>()
            .Where(m => m.GetAttributes().Any(attr => attr.IsTestAttribute()))
            .ToArray();

        // Find methods declared directly on the derived class
        var derivedClassMethods = allTestMethods
            .Where(m => SymbolEqualityComparer.Default.Equals(m.ContainingType.OriginalDefinition, derivedClass.OriginalDefinition))
            .ToArray();

        // Filter out base methods that are hidden by derived class methods or declared directly on derived class
        var result = new List<IMethodSymbol>();
        foreach (var method in allTestMethods)
        {
            // Skip methods declared directly on derived class
            // (they're handled by regular test registration)
            if (SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, derivedClass.OriginalDefinition))
            {
                continue;
            }

            // Check if this base method is hidden by any derived class method
            var isHidden = derivedClassMethods.Any(derived => IsMethodHiding(derived, method));

            if (!isHidden)
            {
                result.Add(method);
            }
        }

        return result;
    }

    private static IMethodSymbol? FindConcreteMethodImplementation(INamedTypeSymbol derivedClass, IMethodSymbol baseMethod)
    {
        // Look for a method in the derived class that overrides or implements the base method
        var candidateMethods = derivedClass.GetMembers(baseMethod.Name).OfType<IMethodSymbol>();

        foreach (var candidate in candidateMethods)
        {
            // Check if this method is an override or implementation of the base method
            if (candidate.IsOverride && SymbolEqualityComparer.Default.Equals(candidate.OverriddenMethod, baseMethod))
            {
                return candidate;
            }

            // For abstract methods, also check if signatures match (same name and parameters)
            if (baseMethod.IsAbstract && candidate.Name == baseMethod.Name)
            {
                if (ParametersMatch(candidate.Parameters, baseMethod.Parameters))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static bool ParametersMatch(ImmutableArray<IParameterSymbol> params1, ImmutableArray<IParameterSymbol> params2)
    {
        if (params1.Length != params2.Length)
        {
            return false;
        }

        for (var i = 0; i < params1.Length; i++)
        {
            // For generic types, we need to consider that T in base class becomes concrete type in derived class
            if (!TypesMatchForInheritance(params1[i].Type, params2[i].Type))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TypesMatchForInheritance(ITypeSymbol derivedType, ITypeSymbol baseType)
    {
        // If both types are the same, they match
        if (SymbolEqualityComparer.Default.Equals(derivedType, baseType))
        {
            return true;
        }

        // If base type is a type parameter, it can match any concrete type in derived class
        if (baseType.TypeKind == TypeKind.TypeParameter)
        {
            return true;
        }

        return false;
    }

    private static (string filePath, int lineNumber) GetTestMethodSourceLocation(
        MethodDeclarationSyntax methodSyntax,
        AttributeData testAttribute)
    {
        // Prioritize TestAttribute's File/Line from [CallerFilePath]/[CallerLineNumber] first
        var attrFilePath = testAttribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString();
        if (!string.IsNullOrEmpty(attrFilePath))
        {
            var attrLineNumber = (int?)testAttribute.ConstructorArguments.ElementAtOrDefault(1).Value ?? 0;
            if (attrLineNumber > 0)
            {
                return (attrFilePath!, attrLineNumber);
            }
        }

        // Fall back to method syntax location
        var methodLocation = methodSyntax.GetLocation();
        var filePath = methodLocation.SourceTree?.FilePath;
        if (!string.IsNullOrEmpty(filePath))
        {
            var lineNumber = methodLocation.GetLineSpan().StartLinePosition.Line + 1;
            return (filePath!, lineNumber);
        }

        // Final fallback
        filePath = methodSyntax.SyntaxTree.FilePath ?? "";
        var fallbackLineNumber = methodLocation.GetLineSpan().StartLinePosition.Line + 1;
        return (filePath, fallbackLineNumber);
    }

    private static (string filePath, int lineNumber) GetTestMethodSourceLocation(
        IMethodSymbol method,
        AttributeData testAttribute,
        InheritsTestsClassMetadata classInfo)
    {
        // Prioritize TestAttribute's File/Line from [CallerFilePath]/[CallerLineNumber] first
        var attrFilePath = testAttribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString();
        if (!string.IsNullOrEmpty(attrFilePath))
        {
            var attrLineNumber = (int?)testAttribute.ConstructorArguments.ElementAtOrDefault(1).Value ?? 0;
            if (attrLineNumber > 0)
            {
                return (attrFilePath!, attrLineNumber);
            }
        }

        // Fall back to method symbol location
        var methodLocation = method.Locations.FirstOrDefault();
        if (methodLocation != null && methodLocation.IsInSource)
        {
            var filePath = methodLocation.SourceTree?.FilePath;
            if (!string.IsNullOrEmpty(filePath))
            {
                var lineNumber = methodLocation.GetLineSpan().StartLinePosition.Line + 1;
                return (filePath!, lineNumber);
            }
        }

        // Final fallback to class location
        var classLocation = classInfo.ClassSyntax.GetLocation();
        var derivedFilePath = classLocation.SourceTree?.FilePath ?? classInfo.ClassSyntax.SyntaxTree.FilePath ?? "";
        var derivedLineNumber = classLocation.GetLineSpan().StartLinePosition.Line + 1;
        return (derivedFilePath, derivedLineNumber);
    }

    private static void GenerateReflectionFieldAccessors(CodeWriter writer, INamedTypeSymbol typeSymbol)
    {
        // Find all init-only properties with data source attributes
        var initOnlyPropertiesWithDataSources = new List<IPropertySymbol>();

        var currentType = typeSymbol;
        while (currentType != null)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, SetMethod: { DeclaredAccessibility: Accessibility.Public, IsInitOnly: true }, IsStatic: false } property)
                {
                    var dataSourceAttr = property.GetAttributes()
                        .FirstOrDefault(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));

                    if (dataSourceAttr != null)
                    {
                        initOnlyPropertiesWithDataSources.Add(property);
                    }
                }
            }
            currentType = currentType.BaseType;
        }

        // Generate UnsafeAccessor methods for init-only properties (only on .NET 8+)
        // Skip for generic types since UnsafeAccessor doesn't work with open generic types
        var nonGenericProperties = initOnlyPropertiesWithDataSources
            .Where(p => !p.ContainingType.IsGenericType)
            .ToArray();

        if (nonGenericProperties.Any())
        {
            writer.AppendLine("#if NET8_0_OR_GREATER");
            foreach (var property in nonGenericProperties)
            {
                var backingFieldName = $"<{property.Name}>k__BackingField";
                var propertyType = property.Type.GloballyQualified();
                // Use the property's containing type for the UnsafeAccessor, not the derived class
                var containingTypeName = property.ContainingType.GloballyQualified();

                writer.AppendLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = \"{backingFieldName}\")]");
                writer.AppendLine($"private static extern ref {propertyType} Get{property.Name}BackingField({containingTypeName} instance);");
                writer.AppendLine();
            }
            writer.AppendLine("#endif");
        }
    }

    private static string GetOpenGenericTypeName(INamedTypeSymbol typeSymbol)
    {
        // For generic types, we need to generate the open generic type definition
        // e.g., MyClass<T> becomes MyClass<>
        var typeName = typeSymbol.GloballyQualified();

        // Remove the generic arguments to get the open generic type
        var genericIndex = typeName.IndexOf('<');
        if (genericIndex > 0)
        {
            var baseTypeName = typeName.Substring(0, genericIndex);
            // Add empty angle brackets for each type parameter
            var commas = new string(',', typeSymbol.TypeParameters.Length - 1);
            return $"{baseTypeName}<{commas}>";
        }

        return typeName;
    }

    private static string GenerateTypeReference(INamedTypeSymbol typeSymbol, bool isGeneric)
    {
        if (isGeneric)
        {
            var openGenericTypeName = GetOpenGenericTypeName(typeSymbol);
            return $"typeof({openGenericTypeName})";
        }
        var fullyQualifiedName = typeSymbol.GloballyQualified();
        return $"typeof({fullyQualifiedName})";
    }

    private static string BuildTypeKey(IEnumerable<ITypeSymbol> types)
    {
        var typesList = types as IList<ITypeSymbol> ?? types.ToArray();
        if (typesList.Count == 0)
        {
            return string.Empty;
        }

        var formattedTypes = new string[typesList.Count];
        for (var i = 0; i < typesList.Count; i++)
        {
            formattedTypes[i] = typesList[i].ToDisplayString(DisplayFormats.FullyQualifiedGenericWithoutGlobalPrefix);
        }
        return string.Join(",", formattedTypes);
    }

    /// <summary>
    /// Generates code that resolves the type name at runtime (FullName ?? Name).
    /// Caches ToDisplayString result to avoid calling it twice.
    /// </summary>
    private static string FormatTypeForRuntimeName(ITypeSymbol type)
    {
        var typeString = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return $"(typeof({typeString}).FullName ?? typeof({typeString}).Name)";
    }

    private static void GenerateGenericTypeInfo(CodeWriter writer, INamedTypeSymbol typeSymbol)
    {
        writer.AppendLine("GenericTypeInfo = new global::TUnit.Core.GenericTypeInfo");
        writer.AppendLine("{");
        writer.Indent();

        // Generate type parameter names
        writer.AppendLine("ParameterNames = new string[]");
        writer.AppendLine("{");
        writer.Indent();
        foreach (var typeParam in typeSymbol.TypeParameters)
        {
            writer.AppendLine($"\"{typeParam.Name}\",");
        }
        writer.Unindent();
        writer.AppendLine("},");

        // Generate constraints
        writer.AppendLine("Constraints = new global::TUnit.Core.GenericParameterConstraints[]");
        writer.AppendLine("{");
        writer.Indent();
        foreach (var typeParam in typeSymbol.TypeParameters)
        {
            GenerateGenericParameterConstraints(writer, typeParam);
        }
        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("},");
    }

    private static void GenerateGenericMethodInfo(CodeWriter writer, IMethodSymbol methodSymbol)
    {
        writer.AppendLine("GenericMethodInfo = new global::TUnit.Core.GenericMethodInfo");
        writer.AppendLine("{");
        writer.Indent();

        // Generate type parameter names
        writer.AppendLine("ParameterNames = new string[]");
        writer.AppendLine("{");
        writer.Indent();
        foreach (var typeParam in methodSymbol.TypeParameters)
        {
            writer.AppendLine($"\"{typeParam.Name}\",");
        }
        writer.Unindent();
        writer.AppendLine("},");

        // Generate constraints
        writer.AppendLine("Constraints = new global::TUnit.Core.GenericParameterConstraints[]");
        writer.AppendLine("{");
        writer.Indent();
        foreach (var typeParam in methodSymbol.TypeParameters)
        {
            GenerateGenericParameterConstraints(writer, typeParam);
        }
        writer.Unindent();
        writer.AppendLine("},");

        // Generate parameter positions (for type inference)
        writer.AppendLine("ParameterPositions = new int[]");
        writer.AppendLine("{");
        writer.Indent();
        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("},");
    }

    private static void GenerateGenericParameterConstraints(CodeWriter writer, ITypeParameterSymbol typeParam)
    {
        writer.AppendLine("new global::TUnit.Core.GenericParameterConstraints");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"ParameterName = \"{typeParam.Name}\",");
        writer.AppendLine($"HasDefaultConstructorConstraint = {typeParam.HasConstructorConstraint.ToString().ToLowerInvariant()},");

        // Find base type constraint (class constraint)
        var baseTypeConstraint = typeParam.ConstraintTypes.FirstOrDefault(c => c.TypeKind == TypeKind.Class);
        if (baseTypeConstraint != null)
        {
            writer.AppendLine($"BaseTypeConstraint = typeof({baseTypeConstraint.GloballyQualified()}),");
        }

        // Generate interface constraints
        var interfaceConstraints = typeParam.ConstraintTypes.Where(c => c.TypeKind == TypeKind.Interface);
        writer.AppendLine("InterfaceConstraints = new global::System.Type[]");
        writer.AppendLine("{");
        writer.Indent();
        foreach (var constraintType in interfaceConstraints)
        {
            var constraintTypeName = constraintType.GloballyQualified();
            writer.AppendLine($"typeof({constraintTypeName}),");
        }
        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("},");
    }

    private static void GenerateGenericTestWithConcreteTypes(
        CodeWriter writer,
        TestMethodMetadata testMethod,
        string className)
    {
        var compilation = testMethod.Context!.Value.SemanticModel.Compilation;
        var methodName = testMethod.MethodSymbol.Name;

        writer.AppendLine("// Create generic metadata with concrete type registrations");
        writer.AppendLine("var genericMetadata = new global::TUnit.Core.GenericTestMetadata");
        writer.AppendLine("{");
        writer.Indent();

        // Generate basic metadata
        writer.AppendLine($"TestName = \"{methodName}\",");

        // For generic classes, use the open generic type definition
        if (testMethod.IsGenericType)
        {
            var openGenericTypeName = GetOpenGenericTypeName(testMethod.TypeSymbol);
            writer.AppendLine($"TestClassType = typeof({openGenericTypeName}),");
        }
        else
        {
            writer.AppendLine($"TestClassType = typeof({className}),");
        }

        writer.AppendLine($"TestMethodName = \"{methodName}\",");

        // Add basic metadata (excluding data source attributes for concrete instantiations)
        GenerateMetadataForConcreteInstantiation(writer, testMethod);

        // Generate instance factory that works with generic types
        writer.AppendLine("InstanceFactory = static (typeArgs, args) =>");
        writer.AppendLine("{");
        writer.Indent();

        if (testMethod.IsGenericType)
        {
            // For generic classes, we need to use runtime type construction
            var openGenericTypeName = GetOpenGenericTypeName(testMethod.TypeSymbol);
            writer.AppendLine($"var genericType = typeof({openGenericTypeName});");
            writer.AppendLine("if (typeArgs.Length > 0)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("var closedType = genericType.MakeGenericType(typeArgs);");
            writer.AppendLine("return global::System.Activator.CreateInstance(closedType, args)!;");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("throw new global::System.InvalidOperationException(\"No type arguments provided for generic class\");");
        }
        else
        {
            writer.AppendLine($"return new {className}();");
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Generate concrete instantiations dictionary
        writer.AppendLine("ConcreteInstantiations = new global::System.Collections.Generic.Dictionary<string, global::TUnit.Core.TestMetadata>");
        writer.AppendLine("{");
        writer.Indent();

        var methodArgumentsAttributes = testMethod.MethodAttributes
            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
            .ToArray();

        var classArgumentsAttributes = Array.Empty<AttributeData>();

        // For generic classes, collect class-level Arguments attributes separately
        if (testMethod.IsGenericType)
        {
            classArgumentsAttributes = testMethod.TypeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
                .ToArray();
        }

        var processedTypeCombinations = new HashSet<string>();

        // Handle the combination of class and method Arguments attributes
        if (testMethod is { IsGenericType: true, IsGenericMethod: true } && classArgumentsAttributes.Any() && methodArgumentsAttributes.Any())
        {
            // Generate combinations of class and method Arguments
            foreach (var classAttr in classArgumentsAttributes)
            {
                var classTypes = InferTypesFromClassArgumentsAttribute(testMethod.TypeSymbol, classAttr, compilation);
                if (classTypes == null || classTypes.Length == 0)
                {
                    continue;
                }

                foreach (var methodAttr in methodArgumentsAttributes)
                {
                    var methodTypes = InferTypesFromArgumentsAttribute(testMethod.MethodSymbol, methodAttr, compilation);
                    if (methodTypes == null || methodTypes.Length == 0)
                    {
                        continue;
                    }

                    // Combine class and method types
                    var combinedTypes = new ITypeSymbol[classTypes.Length + methodTypes.Length];
                    Array.Copy(classTypes, 0, combinedTypes, 0, classTypes.Length);
                    Array.Copy(methodTypes, 0, combinedTypes, classTypes.Length, methodTypes.Length);

                    var typeKey = BuildTypeKey(combinedTypes);

                    // Skip if we've already processed this type combination
                    if (!processedTypeCombinations.Add(typeKey))
                    {
                        continue;
                    }

                    // Validate constraints for both class and method separately
                    var constraintsValid = ValidateClassTypeConstraints(testMethod.TypeSymbol, classTypes) &&
                                          ValidateTypeConstraints(testMethod.MethodSymbol, methodTypes);

                    if (!constraintsValid)
                    {
                        continue;
                    }

                    // Generate a concrete instantiation for this type combination
                    writer.AppendLine($"[{string.Join(" + \",\" + ", combinedTypes.Select(FormatTypeForRuntimeName))}] = ");
                    GenerateConcreteTestMetadata(writer, testMethod, className, combinedTypes, classAttr);
                    writer.AppendLine(",");
                }
            }
        }
        else
        {
            // Handle class-only or method-only Arguments attributes
            var allArgumentsAttributes = new List<AttributeData>();

            // For generic classes with non-generic methods, don't include method Arguments here
            // They will be processed separately with InferClassTypesFromMethodArguments
            if (!(testMethod is { IsGenericType: true, IsGenericMethod: false }))
            {
                allArgumentsAttributes.AddRange(methodArgumentsAttributes);
            }
            allArgumentsAttributes.AddRange(classArgumentsAttributes);

            // Process Arguments attributes individually
            foreach (var argAttr in allArgumentsAttributes)
            {
                // Infer types from this specific Arguments attribute
                ITypeSymbol[]? inferredTypes = null;

                // Check if this is a class-level attribute on a generic type
                if (testMethod.IsGenericType && classArgumentsAttributes.Contains(argAttr))
                {
                    inferredTypes = InferTypesFromClassArgumentsAttribute(testMethod.TypeSymbol, argAttr, compilation);
                }
                else
                {
                    inferredTypes = InferTypesFromArgumentsAttribute(testMethod.MethodSymbol, argAttr, compilation);
                }

                if (inferredTypes is { Length: > 0 })
                {
                    var typeKey = BuildTypeKey(inferredTypes);

                    // Skip if we've already processed this type combination
                    if (!processedTypeCombinations.Add(typeKey))
                    {
                        continue;
                    }

                    // Validate constraints
                    var constraintsValid = true;
                    if (testMethod is { IsGenericType: true, IsGenericMethod: false })
                    {
                        // For generic class only, validate class constraints
                        constraintsValid = ValidateClassTypeConstraints(testMethod.TypeSymbol, inferredTypes);
                    }
                    else if (testMethod.IsGenericMethod)
                    {
                        // For generic method (with or without generic class), validate method constraints
                        constraintsValid = ValidateTypeConstraints(testMethod.MethodSymbol, inferredTypes);
                    }

                    if (!constraintsValid)
                    {
                        continue;
                    }

                    // Generate a concrete instantiation for this type combination
                    writer.AppendLine($"[{string.Join(" + \",\" + ", inferredTypes.Select(FormatTypeForRuntimeName))}] = ");
                    GenerateConcreteTestMetadata(writer, testMethod, className, inferredTypes, argAttr);
                    writer.AppendLine(",");
                }
            }
        }

        // Handle generic classes with non-generic methods that have method-level Arguments
        // These were skipped in the main loop and need special processing
        if (testMethod is { IsGenericType: true, IsGenericMethod: false } && methodArgumentsAttributes.Length > 0)
        {
            foreach (var methodArgAttr in methodArgumentsAttributes)
            {
                var inferredTypes = InferClassTypesFromMethodArguments(testMethod.TypeSymbol, testMethod.MethodSymbol, methodArgAttr, compilation);
                if (inferredTypes is { Length: > 0 })
                {
                    var typeKey = BuildTypeKey(inferredTypes);

                    // Skip if we've already processed this type combination
                    if (!processedTypeCombinations.Add(typeKey))
                    {
                        continue;
                    }

                    // Validate class type constraints
                    var constraintsValid = ValidateClassTypeConstraints(testMethod.TypeSymbol, inferredTypes);

                    if (!constraintsValid)
                    {
                        continue;
                    }

                    // Generate a concrete instantiation for this type combination
                    writer.AppendLine($"[{string.Join(" + \",\" + ", inferredTypes.Select(FormatTypeForRuntimeName))}] = ");
                    GenerateConcreteTestMetadata(writer, testMethod, className, inferredTypes, methodArgAttr);
                    writer.AppendLine(",");
                }
            }
        }

        // Process typed data source attributes
        var dataSourceAttributes = testMethod.MethodAttributes
            .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass));

        foreach (var dataSourceAttr in dataSourceAttributes)
        {
            var inferredTypes = InferTypesFromDataSourceAttribute(testMethod.MethodSymbol, dataSourceAttr);
            if (inferredTypes is { Length: > 0 })
            {
                var typeKey = BuildTypeKey(inferredTypes);

                // Skip if we've already processed this type combination
                if (!processedTypeCombinations.Add(typeKey))
                {
                    continue;
                }

                // Validate constraints
                var constraintsValid = true;
                if (testMethod is { IsGenericType: true, IsGenericMethod: false })
                {
                    // For generic class only, validate class constraints
                    constraintsValid = ValidateClassTypeConstraints(testMethod.TypeSymbol, inferredTypes);
                }
                else if (testMethod.IsGenericMethod)
                {
                    // For generic method (with or without generic class), validate method constraints
                    constraintsValid = ValidateTypeConstraints(testMethod.MethodSymbol, inferredTypes);
                }

                if (!constraintsValid)
                {
                    continue;
                }

                // Generate a concrete instantiation for this type combination
                writer.AppendLine($"[{string.Join(" + \",\" + ", inferredTypes.Select(FormatTypeForRuntimeName))}] = ");
                GenerateConcreteTestMetadata(writer, testMethod, className, inferredTypes, dataSourceAttr);
                writer.AppendLine(",");
            }
        }

        // Process attributes that implement IInfersType<T> on parameters
        if (testMethod.IsGenericMethod)
        {
            var inferredTypes = InferTypesFromTypeInferringAttributes(testMethod.MethodSymbol);
            if (inferredTypes is { Length: > 0 })
            {
                var typeKey = BuildTypeKey(inferredTypes);

                // Skip if we've already processed this type combination
                if (processedTypeCombinations.Add(typeKey))
                {
                    // Validate constraints
                    if (ValidateTypeConstraints(testMethod.MethodSymbol, inferredTypes))
                    {
                        // Generate a concrete instantiation for this type combination
                        writer.AppendLine($"[{string.Join(" + \",\" + ", inferredTypes.Select(FormatTypeForRuntimeName))}] = ");
                        GenerateConcreteTestMetadata(writer, testMethod, className, inferredTypes);
                        writer.AppendLine(",");
                    }
                }
            }
        }

        // Process MethodDataSource attributes for generic classes (non-generic methods)
        if (testMethod is { IsGenericType: true, IsGenericMethod: false })
        {
            var methodDataSourceAttributes = testMethod.MethodAttributes
                .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");

            foreach (var mdsAttr in methodDataSourceAttributes)
            {
                // Try to infer types from the method data source
                var inferredTypes = InferClassTypesFromMethodDataSource(testMethod, mdsAttr);
                if (inferredTypes is { Length: > 0 })
                {
                    var typeKey = BuildTypeKey(inferredTypes);

                    // Skip if we've already processed this type combination
                    if (processedTypeCombinations.Add(typeKey))
                    {
                        // Validate constraints for the generic class
                        if (ValidateClassTypeConstraints(testMethod.TypeSymbol, inferredTypes))
                        {
                            // Generate a concrete instantiation for this type combination
                            writer.AppendLine($"[{string.Join(" + \",\" + ", inferredTypes.Select(FormatTypeForRuntimeName))}] = ");
                            GenerateConcreteTestMetadata(writer, testMethod, className, inferredTypes);
                            writer.AppendLine(",");
                        }
                    }
                }
            }
        }

        // Process typed data source attributes for generic classes (non-generic methods)
        if (testMethod is { IsGenericType: true, IsGenericMethod: false })
        {
            var typedDataSourceInferredTypes = InferTypesFromTypedDataSourceForClass(testMethod.TypeSymbol, testMethod.MethodSymbol);
            if (typedDataSourceInferredTypes is { Length: > 0 })
            {
                var typeKey = BuildTypeKey(typedDataSourceInferredTypes);

                // Skip if we've already processed this type combination
                if (processedTypeCombinations.Add(typeKey))
                {
                    // Validate constraints for the generic class
                    if (ValidateClassTypeConstraints(testMethod.TypeSymbol, typedDataSourceInferredTypes))
                    {
                        // Generate a concrete instantiation for this type combination
                        writer.AppendLine($"[{string.Join(" + \",\" + ", typedDataSourceInferredTypes.Select(FormatTypeForRuntimeName))}] = ");
                        GenerateConcreteTestMetadata(writer, testMethod, className, typedDataSourceInferredTypes);
                        writer.AppendLine(",");
                    }
                }
            }
        }

        // Process MethodDataSource attributes for generic methods
        if (testMethod.IsGenericMethod)
        {
            var methodDataSourceAttributes = testMethod.MethodAttributes
                .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute");

            foreach (var mdsAttr in methodDataSourceAttributes)
            {
                // Try to infer types from the method data source
                var inferredTypes = InferTypesFromMethodDataSource(testMethod, mdsAttr);
                if (inferredTypes is { Length: > 0 })
                {
                    var typeKey = BuildTypeKey(inferredTypes);

                    // Skip if we've already processed this type combination
                    if (processedTypeCombinations.Add(typeKey))
                    {
                        // Validate constraints
                        if (ValidateTypeConstraints(testMethod.MethodSymbol, inferredTypes))
                        {
                            // Generate a concrete instantiation for this type combination
                            writer.AppendLine($"[{string.Join(" + \",\" + ", inferredTypes.Select(FormatTypeForRuntimeName))}] = ");
                            GenerateConcreteTestMetadata(writer, testMethod, className, inferredTypes);
                            writer.AppendLine(",");
                        }
                    }
                }
            }
        }

        // Process class-level Arguments attributes for generic classes (combined with generic methods)
        if (testMethod.IsGenericType)
        {
            var argumentsAttributes = testMethod.TypeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute");

            foreach (var argAttr in argumentsAttributes)
            {
                // Try to infer types from the class-level arguments
                var classInferredTypes = InferTypesFromClassArgumentsAttribute(testMethod.TypeSymbol, argAttr, compilation);
                if (classInferredTypes is { Length: > 0 })
                {
                    // If the method is also generic, we need to combine class and method type arguments
                    if (testMethod.IsGenericMethod)
                    {
                        // Get method-level Arguments attributes to infer method type parameters
                        var methodLevelArgumentsAttributes = testMethod.MethodAttributes
                            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute");

                        foreach (var methodArgAttr in methodLevelArgumentsAttributes)
                        {
                            var methodInferredTypes = InferTypesFromArgumentsAttribute(testMethod.MethodSymbol, methodArgAttr, compilation);
                            if (methodInferredTypes is { Length: > 0 })
                            {
                                // Combine class types and method types
                                var combinedTypes = classInferredTypes.Concat(methodInferredTypes).ToArray();
                                var typeKey = BuildTypeKey(combinedTypes);

                                // Skip if we've already processed this type combination
                                if (processedTypeCombinations.Add(typeKey))
                                {
                                    // Validate constraints for both class and method type parameters
                                    if (ValidateClassTypeConstraints(testMethod.TypeSymbol, classInferredTypes) &&
                                        ValidateTypeConstraints(testMethod.MethodSymbol, methodInferredTypes))
                                    {
                                        // Generate a concrete instantiation for this type combination
                                        writer.AppendLine($"[{string.Join(" + \",\" + ", combinedTypes.Select(FormatTypeForRuntimeName))}] = ");
                                        GenerateConcreteTestMetadata(writer, testMethod, className, combinedTypes, argAttr);
                                        writer.AppendLine(",");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // For non-generic methods, just use class types
                        var typeKey = BuildTypeKey(classInferredTypes);

                        // Skip if we've already processed this type combination
                        if (processedTypeCombinations.Add(typeKey))
                        {
                            // Validate constraints for the generic class type parameters
                            if (ValidateClassTypeConstraints(testMethod.TypeSymbol, classInferredTypes))
                            {
                                // Generate a concrete instantiation for this type combination
                                writer.AppendLine($"[{string.Join(" + \",\" + ", classInferredTypes.Select(FormatTypeForRuntimeName))}] = ");
                                GenerateConcreteTestMetadata(writer, testMethod, className, classInferredTypes, argAttr);
                                writer.AppendLine(",");
                            }
                        }
                    }
                }
            }
        }

        // Process class-level Arguments attributes for non-generic classes with parameterized constructors
        if (testMethod is { IsGenericType: false, IsGenericMethod: false })
        {
            var nonGenericClassArguments = testMethod.TypeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
                .ToArray();

            var nonGenericMethodArguments = testMethod.MethodAttributes
                .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
                .ToArray();

            // Also get class-level data source generators for non-generic classes
            var nonGenericClassDataSourceGenerators = testMethod.TypeSymbol.GetAttributes()
                .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass) &&
                           a.AttributeClass?.Name != "ArgumentsAttribute")
                .ToArray();

            if (nonGenericClassArguments.Any() && nonGenericMethodArguments.Any())
            {
                // Generate all combinations of class and method arguments
                foreach (var classArgAttr in nonGenericClassArguments)
                {
                    foreach (var methodArgAttr in nonGenericMethodArguments)
                    {
                        // Generate a concrete test metadata for this combination
                        writer.AppendLine($"// Class arguments: {string.Join(", ", classArgAttr.ConstructorArguments.SelectMany(a => a.Values.Select(v => v.Value?.ToString() ?? "null")))}");
                        writer.AppendLine($"// Method arguments: {string.Join(", ", methodArgAttr.ConstructorArguments.SelectMany(a => a.Values.Select(v => v.Value?.ToString() ?? "null")))}");
                        GenerateConcreteTestMetadataForNonGeneric(writer, testMethod, className, classArgAttr, methodArgAttr);
                        writer.AppendLine();
                    }
                }
            }
            else if (nonGenericClassArguments.Any())
            {
                // Only class arguments, no method arguments
                foreach (var classArgAttr in nonGenericClassArguments)
                {
                    writer.AppendLine($"// Class arguments: {string.Join(", ", classArgAttr.ConstructorArguments.SelectMany(a => a.Values.Select(v => v.Value?.ToString() ?? "null")))}");
                    GenerateConcreteTestMetadataForNonGeneric(writer, testMethod, className, classArgAttr, null);
                    writer.AppendLine();
                }
            }
            else if (nonGenericMethodArguments.Any())
            {
                // Only method arguments, no class arguments
                foreach (var methodArgAttr in nonGenericMethodArguments)
                {
                    writer.AppendLine($"// Method arguments: {string.Join(", ", methodArgAttr.ConstructorArguments.SelectMany(a => a.Values.Select(v => v.Value?.ToString() ?? "null")))}");
                    GenerateConcreteTestMetadataForNonGeneric(writer, testMethod, className, null, methodArgAttr);
                    writer.AppendLine();
                }
            }

            // Process class-level data source generators for non-generic classes
            if (nonGenericClassDataSourceGenerators.Any())
            {
                foreach (var dataSourceAttr in nonGenericClassDataSourceGenerators)
                {
                    writer.AppendLine($"// Class data source generator: {dataSourceAttr.AttributeClass?.Name}");
                    GenerateConcreteTestMetadataForNonGeneric(writer, testMethod, className, dataSourceAttr, null);
                    writer.AppendLine();
                }
            }
        }

        // Process GenerateGenericTest attributes from both class and method levels
        // When both class and method are generic, we need to generate cartesian products
        var methodGenerateGenericTestAttributes = testMethod.MethodAttributes
            .Where(a => a.AttributeClass?.Name == "GenerateGenericTestAttribute");

        var classLevelAttributes = testMethod.IsGenericType
            ? testMethod.TypeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "GenerateGenericTestAttribute")
            : Array.Empty<AttributeData>();

        // Extract type arguments from class-level attributes
        var classTypeArgSets = ExtractTypeArgumentSets(classLevelAttributes);

        // Extract type arguments from method-level attributes
        var methodTypeArgSets = ExtractTypeArgumentSets(methodGenerateGenericTestAttributes);

        // Handle all combinations
        if (classTypeArgSets.Count > 0 && methodTypeArgSets.Count > 0 && testMethod.IsGenericMethod)
        {
            // Both class and method are generic - generate cartesian product
            foreach (var classTypeArgs in classTypeArgSets)
            {
                foreach (var methodTypeArgs in methodTypeArgSets)
                {
                    // Validate both class and method constraints
                    if (ValidateClassTypeConstraints(testMethod.TypeSymbol, classTypeArgs) &&
                        ValidateTypeConstraints(testMethod.MethodSymbol, methodTypeArgs))
                    {
                        // Combine class + method type arguments
                        var combinedTypes = classTypeArgs.Concat(methodTypeArgs).ToArray();
                        var typeKey = BuildTypeKey(combinedTypes);

                        if (processedTypeCombinations.Add(typeKey))
                        {
                            writer.AppendLine($"[{string.Join(" + \",\" + ", combinedTypes.Select(FormatTypeForRuntimeName))}] = ");
                            GenerateConcreteTestMetadata(writer, testMethod, className, combinedTypes);
                            writer.AppendLine(",");
                        }
                    }
                }
            }
        }
        else if (classTypeArgSets.Count > 0)
        {
            // Only class is generic
            foreach (var classTypeArgs in classTypeArgSets)
            {
                if (ValidateClassTypeConstraints(testMethod.TypeSymbol, classTypeArgs))
                {
                    var typeKey = BuildTypeKey(classTypeArgs);
                    if (processedTypeCombinations.Add(typeKey))
                    {
                        writer.AppendLine($"[{string.Join(" + \",\" + ", classTypeArgs.Select(FormatTypeForRuntimeName))}] = ");
                        GenerateConcreteTestMetadata(writer, testMethod, className, classTypeArgs);
                        writer.AppendLine(",");
                    }
                }
            }
        }
        else if (methodTypeArgSets.Count > 0)
        {
            // Only method is generic
            foreach (var methodTypeArgs in methodTypeArgSets)
            {
                if (ValidateTypeConstraints(testMethod.MethodSymbol, methodTypeArgs))
                {
                    var typeKey = BuildTypeKey(methodTypeArgs);
                    if (processedTypeCombinations.Add(typeKey))
                    {
                        writer.AppendLine($"[{string.Join(" + \",\" + ", methodTypeArgs.Select(FormatTypeForRuntimeName))}] = ");
                        GenerateConcreteTestMetadata(writer, testMethod, className, methodTypeArgs);
                        writer.AppendLine(",");
                    }
                }
            }
        }

        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("};");

        writer.AppendLine("genericMetadata.TestSessionId = testSessionId;");
        writer.AppendLine("yield return genericMetadata;");
    }

    private static List<ITypeSymbol[]> ExtractTypeArgumentSets(IEnumerable<AttributeData> attributes)
    {
        var result = new List<ITypeSymbol[]>();

        foreach (var attr in attributes)
        {
            if (attr.ConstructorArguments.Length == 0)
            {
                continue;
            }

            var typeArgs = new List<ITypeSymbol>();
            foreach (var arg in attr.ConstructorArguments)
            {
                if (arg is { Kind: TypedConstantKind.Type, Value: ITypeSymbol typeSymbol })
                {
                    typeArgs.Add(typeSymbol);
                }
                else if (arg.Kind == TypedConstantKind.Array)
                {
                    foreach (var arrayElement in arg.Values)
                    {
                        if (arrayElement is { Kind: TypedConstantKind.Type, Value: ITypeSymbol arrayTypeSymbol })
                        {
                            typeArgs.Add(arrayTypeSymbol);
                        }
                    }
                }
            }

            if (typeArgs.Count > 0)
            {
                result.Add(typeArgs.ToArray());
            }
        }

        return result;
    }

    private static bool ValidateClassTypeConstraints(INamedTypeSymbol classSymbol, ITypeSymbol[] typeArguments)
    {
        var typeParams = classSymbol.TypeParameters;

        if (typeParams.Length != typeArguments.Length)
        {
            return false;
        }

        for (var i = 0; i < typeParams.Length; i++)
        {
            var typeParam = typeParams[i];
            var typeArg = typeArguments[i];

            // Check struct constraint
            if (typeParam.HasValueTypeConstraint)
            {
                if (!typeArg.IsValueType || typeArg.IsReferenceType)
                {
                    return false;
                }
            }

            // Check class constraint
            if (typeParam.HasReferenceTypeConstraint)
            {
                if (!typeArg.IsReferenceType)
                {
                    return false;
                }
            }

            // Check specific type constraints
            foreach (var constraintType in typeParam.ConstraintTypes)
            {
                // Substitute type parameters in the constraint type with the actual type arguments
                var substitutedConstraint = SubstituteTypeParameters(constraintType, typeParams, typeArguments);

                // For interface constraints, check if the type implements the interface
                if (substitutedConstraint.TypeKind == TypeKind.Interface)
                {
                    if (!TypeImplementsInterface(typeArg, substitutedConstraint))
                    {
                        return false;
                    }
                }
                // For base class constraints, check if the type derives from the class
                else if (substitutedConstraint.TypeKind == TypeKind.Class)
                {
                    var baseType = typeArg.BaseType;
                    var found = false;
                    while (baseType != null)
                    {
                        if (SymbolEqualityComparer.Default.Equals(baseType, substitutedConstraint))
                        {
                            found = true;
                            break;
                        }
                        baseType = baseType.BaseType;
                    }
                    if (!found && !SymbolEqualityComparer.Default.Equals(typeArg, substitutedConstraint))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static ITypeSymbol SubstituteTypeParameters(ITypeSymbol type, ImmutableArray<ITypeParameterSymbol> typeParams, ITypeSymbol[] typeArguments)
    {
        // If the type is a type parameter, substitute it with the corresponding type argument
        if (type is ITypeParameterSymbol typeParam)
        {
            for (var i = 0; i < typeParams.Length; i++)
            {
                if (SymbolEqualityComparer.Default.Equals(typeParams[i], typeParam))
                {
                    return typeArguments[i];
                }
            }
            return type;
        }

        // If the type is a named type with type arguments (e.g., IComparable<T>), substitute recursively
        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            var originalTypeArgs = namedType.TypeArguments;
            var newTypeArgs = new ITypeSymbol[originalTypeArgs.Length];
            var anyChanged = false;

            for (var i = 0; i < originalTypeArgs.Length; i++)
            {
                newTypeArgs[i] = SubstituteTypeParameters(originalTypeArgs[i], typeParams, typeArguments);
                if (!SymbolEqualityComparer.Default.Equals(newTypeArgs[i], originalTypeArgs[i]))
                {
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                return namedType.OriginalDefinition.Construct(newTypeArgs);
            }
        }

        return type;
    }

    private static bool TypeImplementsInterface(ITypeSymbol type, ITypeSymbol interfaceType)
    {
        // Check if the type directly implements the interface
        if (type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceType)))
        {
            return true;
        }

        // For generic interfaces, also check if the type implements a constructed version
        if (interfaceType is INamedTypeSymbol { IsGenericType: true } genericInterface)
        {
            var originalDef = genericInterface.OriginalDefinition;
            return type.AllInterfaces.Any(i =>
                i.IsGenericType &&
                SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, originalDef) &&
                ((IEnumerable<ITypeSymbol>)i.TypeArguments).SequenceEqual(genericInterface.TypeArguments, SymbolEqualityComparer.Default));
        }

        return false;
    }

    private static ITypeSymbol[]? InferClassTypesFromMethodArguments(INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol, AttributeData argAttr, Compilation compilation)
    {
        if (argAttr.ConstructorArguments.Length == 0)
        {
            return null;
        }

        var inferredTypes = new Dictionary<string, ITypeSymbol>();
        var classTypeParameters = classSymbol.TypeParameters;

        // Arguments attribute takes params object?[] so the first constructor argument is an array
        if (argAttr.ConstructorArguments.Length != 1 || argAttr.ConstructorArguments[0].Kind != TypedConstantKind.Array)
        {
            return null;
        }

        var argumentValues = argAttr.ConstructorArguments[0].Values;
        var methodParams = methodSymbol.Parameters;

        // For each value in the params array
        for (var argIndex = 0; argIndex < argumentValues.Length && argIndex < methodParams.Length; argIndex++)
        {
            var methodParam = methodParams[argIndex];
            var argValue = argumentValues[argIndex];

            // Skip if this is a CancellationToken parameter
            if (methodParam.Type.Name == "CancellationToken")
            {
                continue;
            }

            // Check if the method parameter type is a class generic type parameter
            if (methodParam.Type is ITypeParameterSymbol { DeclaringMethod: null } typeParam)
            {
                // This is a class type parameter
                var paramName = typeParam.Name;

                // The argument value's type tells us what the generic type should be
                ITypeSymbol? argType = null;

                if (argValue.Type != null)
                {
                    argType = argValue.Type;
                }
                else if (argValue.Value != null)
                {
                    // For literal values, infer type from the value
                    var value = argValue.Value;
                    argType = InferTypeFromValue(value, compilation);
                }

                if (argType != null && !inferredTypes.ContainsKey(paramName))
                {
                    inferredTypes[paramName] = argType;
                }
            }
            // Also handle generic types that contain class type parameters (e.g., List<T> where T is a class type parameter)
            else if (ContainsClassTypeParameter(methodParam.Type, classSymbol))
            {
                // Extract the concrete type and map it to the type parameter
                if (argValue.Type != null)
                {
                    MapGenericTypeArguments(methodParam.Type, argValue.Type, inferredTypes);
                }
            }
        }

        // Check if we've inferred all required type parameters
        if (inferredTypes.Count == 0)
        {
            return null;
        }

        // Build the result array in the correct order
        var result = new ITypeSymbol[classTypeParameters.Length];
        for (var i = 0; i < classTypeParameters.Length; i++)
        {
            var paramName = classTypeParameters[i].Name;
            if (inferredTypes.TryGetValue(paramName, out var inferredType))
            {
                result[i] = inferredType;
            }
            else
            {
                // Could not infer this type parameter
                return null;
            }
        }

        return result;
    }

    private static bool ContainsClassTypeParameter(ITypeSymbol type, INamedTypeSymbol classSymbol)
    {
        if (type is ITypeParameterSymbol { DeclaringMethod: null })
        {
            return true;
        }

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            return namedType.TypeArguments.Any(ta => ContainsClassTypeParameter(ta, classSymbol));
        }

        return false;
    }

    private static void MapGenericTypeArguments(ITypeSymbol paramType, ITypeSymbol argType, Dictionary<string, ITypeSymbol> inferredTypes)
    {
        if (paramType is ITypeParameterSymbol { DeclaringMethod: null } typeParam)
        {
            if (!inferredTypes.ContainsKey(typeParam.Name))
            {
                inferredTypes[typeParam.Name] = argType;
            }
        }
        else if (paramType is INamedTypeSymbol paramNamedType && argType is INamedTypeSymbol argNamedType &&
                 paramNamedType.IsGenericType && argNamedType.IsGenericType &&
                 paramNamedType.OriginalDefinition.Equals(argNamedType.OriginalDefinition, SymbolEqualityComparer.Default))
        {
            // Map type arguments recursively
            for (var i = 0; i < paramNamedType.TypeArguments.Length && i < argNamedType.TypeArguments.Length; i++)
            {
                MapGenericTypeArguments(paramNamedType.TypeArguments[i], argNamedType.TypeArguments[i], inferredTypes);
            }
        }
    }

    private static ITypeSymbol? InferTypeFromValue(object value, Compilation compilation)
    {
        return value switch
        {
            int => compilation?.GetSpecialType(SpecialType.System_Int32),
            string => compilation?.GetSpecialType(SpecialType.System_String),
            bool => compilation?.GetSpecialType(SpecialType.System_Boolean),
            double => compilation?.GetSpecialType(SpecialType.System_Double),
            float => compilation?.GetSpecialType(SpecialType.System_Single),
            long => compilation?.GetSpecialType(SpecialType.System_Int64),
            byte => compilation?.GetSpecialType(SpecialType.System_Byte),
            char => compilation?.GetSpecialType(SpecialType.System_Char),
            decimal => compilation?.GetSpecialType(SpecialType.System_Decimal),
            ITypeSymbol => compilation?.GetTypeByMetadataName("System.Type"),
            _ => null
        };
    }

    private static ITypeSymbol[]? InferTypesFromClassArgumentsAttribute(INamedTypeSymbol classSymbol, AttributeData argAttr, Compilation compilation)
    {
        if (argAttr.ConstructorArguments.Length == 0)
        {
            return null;
        }

        var inferredTypes = new Dictionary<string, ITypeSymbol>();
        var typeParameters = classSymbol.TypeParameters;

        // Arguments attribute takes params object?[] so the first constructor argument is an array
        if (argAttr.ConstructorArguments.Length != 1 || argAttr.ConstructorArguments[0].Kind != TypedConstantKind.Array)
        {
            return null;
        }

        var argumentValues = argAttr.ConstructorArguments[0].Values;

        // Find the primary constructor
        var primaryConstructor = classSymbol.Constructors
            .FirstOrDefault(c => c.DeclaringSyntaxReferences.Any(sr =>
                sr.GetSyntax() is ConstructorDeclarationSyntax cds &&
                cds.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword))))
            ?? classSymbol.Constructors.FirstOrDefault();

        if (primaryConstructor == null)
        {
            return null;
        }

        var constructorParams = primaryConstructor.Parameters;

        // For each value in the params array
        for (var argIndex = 0; argIndex < argumentValues.Length && argIndex < constructorParams.Length; argIndex++)
        {
            var constructorParam = constructorParams[argIndex];
            var argValue = argumentValues[argIndex];

            // Check if the constructor parameter type is a generic type parameter
            if (constructorParam.Type is ITypeParameterSymbol typeParam)
            {
                // The argument value's type tells us what the generic type should be
                ITypeSymbol? argType = null;

                if (argValue.Type != null)
                {
                    argType = argValue.Type;
                }
                else if (argValue.Value != null)
                {
                    // For literal values, infer type from the value
                    var value = argValue.Value;

                    argType = value switch
                    {
                        int => compilation?.GetSpecialType(SpecialType.System_Int32),
                        string => compilation?.GetSpecialType(SpecialType.System_String),
                        bool => compilation?.GetSpecialType(SpecialType.System_Boolean),
                        double => compilation?.GetSpecialType(SpecialType.System_Double),
                        float => compilation?.GetSpecialType(SpecialType.System_Single),
                        long => compilation?.GetSpecialType(SpecialType.System_Int64),
                        char => compilation?.GetSpecialType(SpecialType.System_Char),
                        byte => compilation?.GetSpecialType(SpecialType.System_Byte),
                        decimal => compilation?.GetSpecialType(SpecialType.System_Decimal),
                        _ => argType
                    };
                }

                if (argType != null)
                {
                    inferredTypes[typeParam.Name] = argType;
                }
            }
        }

        // Build the result array in the correct order
        if (inferredTypes.Count == typeParameters.Length)
        {
            var result = new ITypeSymbol[typeParameters.Length];
            for (var i = 0; i < typeParameters.Length; i++)
            {
                if (inferredTypes.TryGetValue(typeParameters[i].Name, out var type))
                {
                    result[i] = type;
                }
                else
                {
                    return null; // Missing type inference
                }
            }
            return result;
        }

        return null;
    }

    private static ITypeSymbol[]? InferTypesFromArgumentsAttribute(IMethodSymbol method, AttributeData argAttr, Compilation compilation)
    {
        if (argAttr.ConstructorArguments.Length == 0)
        {
            return null;
        }

        var inferredTypes = new Dictionary<string, ITypeSymbol>();
        var typeParameters = method.TypeParameters;

        // Arguments attribute takes params object?[] so the first constructor argument is an array
        if (argAttr.ConstructorArguments.Length != 1 || argAttr.ConstructorArguments[0].Kind != TypedConstantKind.Array)
        {
            return null;
        }

        var argumentValues = argAttr.ConstructorArguments[0].Values;
        var methodParams = method.Parameters;

        // For each value in the params array
        for (var argIndex = 0; argIndex < argumentValues.Length && argIndex < methodParams.Length; argIndex++)
        {
            var methodParam = methodParams[argIndex];
            var argValue = argumentValues[argIndex];

            // Skip if this is a CancellationToken parameter
            if (methodParam.Type.Name == "CancellationToken")
            {
                continue;
            }

            // Check if the method parameter type is a generic type parameter
            if (methodParam.Type is ITypeParameterSymbol typeParam)
            {
                // The argument value's type tells us what the generic type should be
                // For literal values, we need to infer the type from the value itself
                ITypeSymbol? argType = null;

                if (argValue.Type != null)
                {
                    argType = argValue.Type;
                }
                else if (argValue.Value != null)
                {
                    // For literal values, infer type from the value
                    var value = argValue.Value;

                    argType = value switch
                    {
                        int => compilation?.GetSpecialType(SpecialType.System_Int32),
                        string => compilation?.GetSpecialType(SpecialType.System_String),
                        bool => compilation?.GetSpecialType(SpecialType.System_Boolean),
                        double => compilation?.GetSpecialType(SpecialType.System_Double),
                        float => compilation?.GetSpecialType(SpecialType.System_Single),
                        long => compilation?.GetSpecialType(SpecialType.System_Int64),
                        char => compilation?.GetSpecialType(SpecialType.System_Char),
                        byte => compilation?.GetSpecialType(SpecialType.System_Byte),
                        decimal => compilation?.GetSpecialType(SpecialType.System_Decimal),
                        _ => argType
                    };
                }

                if (argType != null)
                {
                    inferredTypes[typeParam.Name] = argType;
                }
            }
        }

        // Build the result array in the correct order
        if (inferredTypes.Count == typeParameters.Length)
        {
            var result = new ITypeSymbol[typeParameters.Length];
            for (var i = 0; i < typeParameters.Length; i++)
            {
                if (inferredTypes.TryGetValue(typeParameters[i].Name, out var type))
                {
                    result[i] = type;
                }
                else
                {
                    return null; // Missing type inference
                }
            }
            return result;
        }

        return null;
    }

    private static ITypeSymbol[]? InferTypesFromDataSourceAttribute(IMethodSymbol method, AttributeData dataSourceAttr)
    {
        var attrClass = dataSourceAttr.AttributeClass;
        if (attrClass == null)
        {
            return null;
        }

        // Check if it's a typed data source by examining its base types
        var baseType = attrClass.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType)
            {
                var baseTypeDef = baseType.OriginalDefinition;
                var baseTypeName = baseTypeDef.Name;
                var baseTypeNamespace = baseTypeDef.ContainingNamespace?.ToDisplayString();

                // Check for typed data source base classes more precisely
                if (baseTypeName is "DataSourceGeneratorAttribute" or "AsyncDataSourceGeneratorAttribute" &&
                    baseTypeNamespace?.Contains("TUnit.Core") == true)
                {
                    // Get the type arguments from the base class
                    var typeArgs = baseType.TypeArguments;

                    // For generic method inference
                    if (method.IsGenericMethod && !method.ContainingType.IsGenericType)
                    {
                        // Single type parameter method
                        if (method.TypeParameters.Length == 1 && typeArgs.Length >= 1)
                        {
                            return new[] { typeArgs[0] };
                        }

                        // Multiple type parameters - check for tuple
                        if (typeArgs is
                            [
                                INamedTypeSymbol { IsTupleType: true } tupleType
                            ])
                        {
                            if (tupleType.TupleElements.Length == method.TypeParameters.Length)
                            {
                                return tupleType.TupleElements.Select(e => e.Type).ToArray();
                            }
                        }

                        // Direct match for multiple type args
                        if (typeArgs.Length == method.TypeParameters.Length)
                        {
                            return typeArgs.ToArray();
                        }
                    }
                    // For generic class inference (non-generic method)
                    else if (!method.IsGenericMethod && method.ContainingType.IsGenericType)
                    {
                        var classTypeParams = method.ContainingType.TypeParameters;

                        // Single type parameter class
                        if (classTypeParams.Length == 1 && typeArgs.Length >= 1)
                        {
                            return new[] { typeArgs[0] };
                        }

                        // Multiple type parameters - check for tuple
                        if (typeArgs is
                            [
                                INamedTypeSymbol { IsTupleType: true } tupleType
                            ])
                        {
                            if (tupleType.TupleElements.Length == classTypeParams.Length)
                            {
                                return tupleType.TupleElements.Select(e => e.Type).ToArray();
                            }
                        }

                        // Direct match for multiple type args
                        if (typeArgs.Length == classTypeParams.Length)
                        {
                            return typeArgs.ToArray();
                        }
                    }
                    // For combined generic class and method
                    else if (method.IsGenericMethod && method.ContainingType.IsGenericType)
                    {
                        var totalGenericParams = method.TypeParameters.Length + method.ContainingType.TypeParameters.Length;

                        // Check if the data source provides types for all parameters
                        if (typeArgs is
                            [
                                INamedTypeSymbol { IsTupleType: true } tupleType
                            ])
                        {
                            if (tupleType.TupleElements.Length == totalGenericParams)
                            {
                                return tupleType.TupleElements.Select(e => e.Type).ToArray();
                            }
                        }

                        // Direct match for multiple type args
                        if (typeArgs.Length == totalGenericParams)
                        {
                            return typeArgs.ToArray();
                        }
                    }
                }
            }
            baseType = baseType.BaseType;
        }

        return null;
    }

    private static ITypeSymbol[]? InferTypesFromMethodDataSource(TestMethodMetadata testMethod, AttributeData mdsAttr)
    {
        if (mdsAttr.ConstructorArguments.Length == 0)
        {
            return null;
        }

        // Get the method name from the attribute
        if (mdsAttr.ConstructorArguments[0].Value is not string methodName)
        {
            return null;
        }

        // Find the method in the test class
        var testClass = testMethod.TypeSymbol;
        var dataMethod = testClass.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.IsStatic && m.Parameters.Length == 0);

        if (dataMethod == null)
        {
            return null;
        }

        // Check if the method returns IEnumerable<Func<T>> where T is a tuple
        var returnType = dataMethod.ReturnType;
        if (returnType is not INamedTypeSymbol namedReturnType)
        {
            return null;
        }

        // Navigate through IEnumerable<Func<...>>
        if (!namedReturnType.IsGenericType || namedReturnType.Name != "IEnumerable")
        {
            return null;
        }

        var funcType = namedReturnType.TypeArguments[0] as INamedTypeSymbol;
        if (funcType == null || funcType.Name != "Func" || funcType.TypeArguments.Length != 1)
        {
            return null;
        }

        var tupleType = funcType.TypeArguments[0] as INamedTypeSymbol;
        if (tupleType == null || !tupleType.IsTupleType)
        {
            return null;
        }

        // Extract the types from the tuple elements that correspond to the generic parameters
        var testMethodParams = testMethod.MethodSymbol.Parameters;
        var genericParams = testMethod.MethodSymbol.TypeParameters;
        var genericParamMap = new Dictionary<string, ITypeSymbol>();

        // Map tuple elements to method parameters to infer types
        var tupleElements = tupleType.TupleElements;
        for (var i = 0; i < testMethodParams.Length && i < tupleElements.Length; i++)
        {
            var paramType = testMethodParams[i].Type;
            var tupleElementType = tupleElements[i].Type;

            // Process the parameter type to find generic references
            ProcessTypeForGenerics(paramType, tupleElementType, genericParams, genericParamMap);
        }

        // Build the result array in the correct order
        var inferredTypes = new ITypeSymbol[genericParams.Length];
        for (var i = 0; i < genericParams.Length; i++)
        {
            if (!genericParamMap.TryGetValue(genericParams[i].Name, out var inferredType))
            {
                return null;
            }
            inferredTypes[i] = inferredType;
        }

        return inferredTypes;
    }

    private static ITypeSymbol[]? InferClassTypesFromMethodDataSource(TestMethodMetadata testMethod, AttributeData mdsAttr)
    {
        if (mdsAttr.ConstructorArguments.Length == 0)
        {
            return null;
        }

        // Get the method name from the attribute
        if (mdsAttr.ConstructorArguments[0].Value is not string methodName)
        {
            return null;
        }

        // Find the method in the test class
        var testClass = testMethod.TypeSymbol;
        var dataMethod = testClass.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.IsStatic && m.Parameters.Length == 0);

        if (dataMethod == null)
        {
            return null;
        }

        // Check if the method returns IEnumerable<Func<T>> where T is a tuple or a single type
        var returnType = dataMethod.ReturnType;
        if (returnType is not INamedTypeSymbol namedReturnType)
        {
            return null;
        }

        // Navigate through IEnumerable<Func<...>> or IEnumerable<...>
        if (!namedReturnType.IsGenericType || namedReturnType.Name != "IEnumerable")
        {
            return null;
        }

        var innerType = namedReturnType.TypeArguments[0];
        INamedTypeSymbol? dataType = null;

        // Check if it's IEnumerable<Func<T>>
        if (innerType is INamedTypeSymbol { Name: "Func", TypeArguments.Length: 1 } funcType)
        {
            dataType = funcType.TypeArguments[0] as INamedTypeSymbol;
        }
        // Or direct IEnumerable<T>
        else if (innerType is INamedTypeSymbol directType)
        {
            dataType = directType;
        }

        if (dataType == null)
        {
            return null;
        }

        // Get class type parameters
        var classTypeParams = testMethod.TypeSymbol.TypeParameters;
        var genericParamMap = new Dictionary<string, ITypeSymbol>();

        // If it's a tuple, map tuple elements to method parameters
        if (dataType.IsTupleType)
        {
            var tupleElements = dataType.TupleElements;
            var testMethodParams = testMethod.MethodSymbol.Parameters;

            for (var i = 0; i < testMethodParams.Length && i < tupleElements.Length; i++)
            {
                var paramType = testMethodParams[i].Type;
                var tupleElementType = tupleElements[i].Type;

                // Process the parameter type to find class generic references
                ProcessTypeForGenerics(paramType, tupleElementType, classTypeParams, genericParamMap);
            }
        }
        // If it's a single type that matches a class type parameter
        else if (testMethod.MethodSymbol.Parameters.Length == 1)
        {
            var paramType = testMethod.MethodSymbol.Parameters[0].Type;
            ProcessTypeForGenerics(paramType, dataType, classTypeParams, genericParamMap);
        }

        // Build the result array in the correct order
        var inferredTypes = new ITypeSymbol[classTypeParams.Length];
        for (var i = 0; i < classTypeParams.Length; i++)
        {
            if (!genericParamMap.TryGetValue(classTypeParams[i].Name, out var inferredType))
            {
                return null;
            }
            inferredTypes[i] = inferredType;
        }

        return inferredTypes;
    }

    private static void ProcessTypeForGenerics(ITypeSymbol paramType, ITypeSymbol actualType, ImmutableArray<ITypeParameterSymbol> genericParams, Dictionary<string, ITypeSymbol> genericParamMap)
    {
        // Direct type parameter match
        if (paramType is ITypeParameterSymbol typeParam && genericParams.Contains(typeParam))
        {
            genericParamMap[typeParam.Name] = actualType;
            return;
        }

        // Check for generic types like IEnumerable<T>, Func<T1, T2>, etc.
        if (paramType is INamedTypeSymbol { IsGenericType: true } namedParamType &&
            actualType is INamedTypeSymbol { IsGenericType: true } namedActualType &&
            namedParamType.OriginalDefinition.Equals(namedActualType.OriginalDefinition, SymbolEqualityComparer.Default))
        {
            // Recursively process type arguments
            for (var i = 0; i < namedParamType.TypeArguments.Length && i < namedActualType.TypeArguments.Length; i++)
            {
                ProcessTypeForGenerics(namedParamType.TypeArguments[i], namedActualType.TypeArguments[i], genericParams, genericParamMap);
            }
        }
    }

    private static bool ValidateTypeConstraints(IMethodSymbol method, ITypeSymbol[] typeArguments)
    {
        // Only validate method type parameters here - class type parameters are validated separately
        // by ValidateClassTypeConstraints in the cartesian product loop
        var methodTypeParams = method.TypeParameters;

        if (methodTypeParams.Length != typeArguments.Length)
        {
            return false;
        }

        return ValidateTypeParameterConstraints(methodTypeParams, typeArguments);
    }

    private static bool ValidateTypeParameterConstraints(ImmutableArray<ITypeParameterSymbol> typeParams, ITypeSymbol[] typeArguments)
    {
        for (var i = 0; i < typeParams.Length; i++)
        {
            var typeParam = typeParams[i];
            var typeArg = typeArguments[i];

            // Check struct constraint
            if (typeParam.HasValueTypeConstraint)
            {
                if (!typeArg.IsValueType || typeArg.IsReferenceType)
                {
                    return false;
                }
            }

            // Check class constraint
            if (typeParam.HasReferenceTypeConstraint)
            {
                if (!typeArg.IsReferenceType)
                {
                    return false;
                }
            }

            // Check interface constraints
            foreach (var constraintType in typeParam.ConstraintTypes)
            {
                // Substitute type parameters in the constraint type with the actual type arguments
                var substitutedConstraint = SubstituteTypeParameters(constraintType, typeParams, typeArguments);

                if (substitutedConstraint.TypeKind == TypeKind.Interface)
                {
                    // Check if the type argument implements the interface
                    if (!TypeImplementsInterface(typeArg, substitutedConstraint))
                    {
                        return false;
                    }
                }
                else if (substitutedConstraint.TypeKind == TypeKind.Class)
                {
                    // Check if the type argument derives from the base class
                    var baseType = typeArg.BaseType;
                    var found = false;
                    while (baseType != null)
                    {
                        if (SymbolEqualityComparer.Default.Equals(baseType, substitutedConstraint))
                        {
                            found = true;
                            break;
                        }
                        baseType = baseType.BaseType;
                    }
                    if (!found)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static void GenerateConcreteTestMetadata(
        CodeWriter writer,
        TestMethodMetadata testMethod,
        string className,
        ITypeSymbol[] typeArguments,
        AttributeData? specificArgumentsAttribute = null)
    {
        var methodName = testMethod.MethodSymbol.Name;

        // Separate class type arguments from method type arguments
        var classTypeArgCount = testMethod.IsGenericType ? testMethod.TypeSymbol.TypeParameters.Length : 0;
        var methodTypeArgCount = testMethod.IsGenericMethod ? testMethod.MethodSymbol.TypeParameters.Length : 0;

        var classTypeArgs = classTypeArgCount > 0 ? typeArguments.Take(classTypeArgCount).ToArray() : Array.Empty<ITypeSymbol>();
        var methodTypeArgs = methodTypeArgCount > 0 ? typeArguments.Skip(classTypeArgCount).ToArray() : Array.Empty<ITypeSymbol>();

        // Build the concrete class name if it's a generic class
        string concreteClassName;
        string openGenericTypeName;
        if (testMethod.IsGenericType)
        {
            openGenericTypeName = GetOpenGenericTypeName(testMethod.TypeSymbol);
            var baseClassName = className.Contains("<") ? className.Substring(0, className.IndexOf('<')) : className;
            concreteClassName = $"{baseClassName}<{string.Join(", ", classTypeArgs.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))}>";
        }
        else
        {
            openGenericTypeName = className;
            concreteClassName = className;
        }

        // Build the test name
        string testName;
        if (methodTypeArgs.Length > 0)
        {
            var methodTypeArgsString = string.Join(", ", methodTypeArgs.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            testName = $"{methodName}<{methodTypeArgsString}>";
        }
        else
        {
            testName = methodName;
        }

        writer.AppendLine($"new global::TUnit.Core.TestMetadata<{concreteClassName}>");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"TestName = \"{testName}\",");
        writer.AppendLine($"TestClassType = typeof({concreteClassName}),");
        writer.AppendLine($"TestMethodName = \"{methodName}\",");

        // Only set GenericMethodTypeArguments if we have method type arguments
        if (methodTypeArgs.Length > 0)
        {
            writer.AppendLine($"GenericMethodTypeArguments = new global::System.Type[] {{ {string.Join(", ", methodTypeArgs.Select(t => $"typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})"))}}},");
        }

        // Generate metadata with filtered data sources for this specific type
        GenerateConcreteMetadataWithFilteredDataSources(writer, testMethod, specificArgumentsAttribute, typeArguments);

        // Generate instance factory
        writer.AppendLine("InstanceFactory = static (typeArgs, args) =>");
        writer.AppendLine("{");
        writer.Indent();

        // Check if the class has a constructor that requires arguments
        var hasParameterizedConstructor = false;
        var constructorParamCount = 0;

        if (testMethod.IsGenericType)
        {
            // Find the primary constructor or first public constructor
            var constructor = testMethod.TypeSymbol.Constructors
                .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
                .OrderByDescending(c => c.Parameters.Length)
                .FirstOrDefault();

            if (constructor is { Parameters.Length: > 0 })
            {
                hasParameterizedConstructor = true;
                constructorParamCount = constructor.Parameters.Length;
            }
        }

        if (hasParameterizedConstructor)
        {
            // For classes with constructor parameters, use the specific constructor arguments from the Arguments attribute
            if (specificArgumentsAttribute is { ConstructorArguments.Length: > 0 } &&
                specificArgumentsAttribute.ConstructorArguments[0].Kind == TypedConstantKind.Array)
            {
                var argumentValues = specificArgumentsAttribute.ConstructorArguments[0].Values;
                var constructorArgs = string.Join(", ", argumentValues.Select(arg => TypedConstantParser.GetRawTypedConstantValue(arg)));

                writer.AppendLine($"return ({concreteClassName})global::System.Activator.CreateInstance(typeof({concreteClassName}), new object[] {{ {constructorArgs} }})!;");
            }
            else
            {
                // Fallback to using args if no specific Arguments attribute
                writer.AppendLine($"return ({concreteClassName})global::System.Activator.CreateInstance(typeof({concreteClassName}), args)!;");
            }
        }
        else
        {
            writer.AppendLine($"return new {concreteClassName}();");
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Generate strongly-typed test invoker
        writer.AppendLine("InvokeTypedTest = static (instance, args, cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();

        // Wrap entire lambda body in try-catch to handle synchronous exceptions
        writer.AppendLine("try");
        writer.AppendLine("{");
        writer.Indent();

        testMethod.MethodSymbol.Parameters.Any(p =>
            p.Type.Name == "CancellationToken" &&
            p.Type.ContainingNamespace?.ToString() == "System.Threading");

        // Generate direct method call with specific types
        writer.AppendLine($"var typedInstance = ({concreteClassName})instance;");

        // Prepare method arguments with proper casting
        var parameterCasts = new List<string>();
        for (var i = 0; i < testMethod.MethodSymbol.Parameters.Length; i++)
        {
            var param = testMethod.MethodSymbol.Parameters[i];
            if (param.Type.Name == "CancellationToken")
            {
                parameterCasts.Add("cancellationToken");
            }
            else
            {
                var paramType = SubstituteTypeParameters(param.Type, testMethod, classTypeArgs, methodTypeArgs);
                parameterCasts.Add($"({paramType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})args[{i}]!");
            }
        }

        // Generate the method call
        if (methodTypeArgs.Length > 0)
        {
            var methodTypeArgsString = string.Join(", ", methodTypeArgs.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            writer.AppendLine($"return global::TUnit.Core.AsyncConvert.Convert(() => typedInstance.{methodName}<{methodTypeArgsString}>({string.Join(", ", parameterCasts)}));");
        }
        else
        {
            writer.AppendLine($"return global::TUnit.Core.AsyncConvert.Convert(() => typedInstance.{methodName}({string.Join(", ", parameterCasts)}));");
        }

        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("catch (global::System.Exception ex)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("return new global::System.Threading.Tasks.ValueTask(global::System.Threading.Tasks.Task.FromException(ex));");
        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("}");
    }

    private static ITypeSymbol SubstituteTypeParameters(
        ITypeSymbol type,
        TestMethodMetadata testMethod,
        ITypeSymbol[] classTypeArgs,
        ITypeSymbol[] methodTypeArgs)
    {
        // Handle direct type parameters
        if (type is ITypeParameterSymbol typeParam)
        {
            // Check if it's a class type parameter
            if (typeParam.ContainingSymbol is INamedTypeSymbol && testMethod.IsGenericType)
            {
                for (var i = 0; i < testMethod.TypeSymbol.TypeParameters.Length; i++)
                {
                    if (testMethod.TypeSymbol.TypeParameters[i].Name == typeParam.Name)
                    {
                        if (i < classTypeArgs.Length)
                        {
                            return classTypeArgs[i];
                        }
                    }
                }
            }

            // Check if it's a method type parameter
            for (var i = 0; i < testMethod.MethodSymbol.TypeParameters.Length; i++)
            {
                if (testMethod.MethodSymbol.TypeParameters[i].Name == typeParam.Name)
                {
                    if (i < methodTypeArgs.Length)
                    {
                        return methodTypeArgs[i];
                    }
                }
            }
        }

        // Handle generic types like IEnumerable<T>, Dictionary<TKey, TValue>, etc.
        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            var substitutedTypeArgs = namedType.TypeArguments
                .Select(arg => SubstituteTypeParameters(arg, testMethod, classTypeArgs, methodTypeArgs))
                .ToArray();

            return namedType.OriginalDefinition.Construct(substitutedTypeArgs);
        }

        // Return the original type if no substitution is needed
        return type;
    }

    private static void GenerateConcreteMetadataWithFilteredDataSources(
        CodeWriter writer,
        TestMethodMetadata testMethod,
        AttributeData? specificArgumentsAttribute,
        ITypeSymbol[] typeArguments)
    {
        var compilation = testMethod.Context!.Value.SemanticModel.Compilation;
        var methodSymbol = testMethod.MethodSymbol;
        var typeSymbol = testMethod.TypeSymbol;

        // For generic classes, construct the closed generic type for data source generation
        // This ensures that static methods are called on the concrete type rather than the open generic
        INamedTypeSymbol concreteTypeSymbol = typeSymbol;
        if (testMethod.IsGenericType && typeArguments.Length > 0)
        {
            var classTypeArgCount = typeSymbol.TypeParameters.Length;
            if (classTypeArgCount > 0 && typeArguments.Length >= classTypeArgCount)
            {
                var classTypeArgs = typeArguments.Take(classTypeArgCount).ToArray();
                concreteTypeSymbol = typeSymbol.Construct(classTypeArgs);
            }
        }

        GenerateDependencies(writer, methodSymbol);

        // Generate attribute factory with filtered attributes
        var filteredAttributes = new List<AttributeData>();

        // Filter method attributes - only filter Arguments attributes when we have a specific one to match
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "ArgumentsAttribute" && specificArgumentsAttribute != null)
            {
                // We have a specific Arguments attribute to match against
                if (AreSameAttribute(attr, specificArgumentsAttribute))
                {
                    filteredAttributes.Add(attr);
                }
                // Skip other Arguments attributes since we're looking for a specific one
            }
            else
            {
                // Include all other attributes (including Arguments when no specific one is provided)
                filteredAttributes.Add(attr);
            }
        }

        // Add all class and assembly attributes (they don't have Arguments attributes)
        filteredAttributes.AddRange(testMethod.TypeSymbol.GetAttributesIncludingBaseTypes());
        filteredAttributes.AddRange(testMethod.TypeSymbol.ContainingAssembly.GetAttributes());

        writer.AppendLine("AttributeFactory = static () =>");
        writer.AppendLine("[");
        writer.Indent();
        testMethod.CompilationContext.AttributeWriter.WriteAttributes(writer, filteredAttributes);
        writer.Unindent();
        writer.AppendLine("],");

        // Extract and emit RepeatCount if present
        var repeatCount = ExtractRepeatCount(methodSymbol, typeSymbol);
        if (repeatCount.HasValue)
        {
            writer.AppendLine($"RepeatCount = {repeatCount.Value},");
        }

        // Filter data sources based on the specific attribute
        List<AttributeData> methodDataSources;
        AttributeData[] classDataSources;

        if (specificArgumentsAttribute != null)
        {
            // For specific data source attributes, include the specific one that matches
            methodDataSources = methodSymbol.GetAttributes()
                .Where(a => AreSameAttribute(a, specificArgumentsAttribute))
                .ToList();

            // For combined generic class + generic method scenarios, also include method-level Arguments
            // that provide method parameters (different from the class-level specificArgumentsAttribute)
            if (testMethod is { IsGenericType: true, IsGenericMethod: true })
            {
                var additionalMethodDataSources = methodSymbol.GetAttributes()
                    .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute" &&
                                !AreSameAttribute(a, specificArgumentsAttribute));
                methodDataSources.AddRange(additionalMethodDataSources);
            }

            classDataSources = typeSymbol.GetAttributesIncludingBaseTypes()
                .Where(a => AreSameAttribute(a, specificArgumentsAttribute))
                .ToArray();
        }
        else
        {
            // For other cases, include all data sources
            methodDataSources = methodSymbol.GetAttributes()
                .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
                .ToList();

            classDataSources = typeSymbol.GetAttributesIncludingBaseTypes()
                .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
                .ToArray();
        }

        // Generate method data sources
        if (methodDataSources.Count == 0)
        {
            writer.AppendLine("DataSources = global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>(),");
        }
        else
        {
            writer.AppendLine("DataSources = new global::TUnit.Core.IDataSourceAttribute[]");
            writer.AppendLine("{");
            writer.Indent();

            foreach (var attr in methodDataSources)
            {
                GenerateDataSourceAttribute(writer, testMethod.CompilationContext, attr, methodSymbol, concreteTypeSymbol);
            }

            writer.Unindent();
            writer.AppendLine("},");
        }

        // Generate class data sources
        if (classDataSources.Length == 0)
        {
            writer.AppendLine("ClassDataSources = global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>(),");
        }
        else
        {
            writer.AppendLine("ClassDataSources = new global::TUnit.Core.IDataSourceAttribute[]");
            writer.AppendLine("{");
            writer.Indent();

            foreach (var attr in classDataSources)
            {
                GenerateDataSourceAttribute(writer, testMethod.CompilationContext, attr, methodSymbol, concreteTypeSymbol);
            }

            writer.Unindent();
            writer.AppendLine("},");
        }

        // Empty property data sources for concrete instantiations
        writer.AppendLine("PropertyDataSources = global::System.Array.Empty<global::TUnit.Core.PropertyDataSource>(),");

        GeneratePropertyInjections(writer, concreteTypeSymbol, concreteTypeSymbol.GloballyQualified());

        // Other metadata
        writer.AppendLine($"FilePath = @\"{(testMethod.FilePath ?? "").Replace("\\", "\\\\")}\",");
        writer.AppendLine($"LineNumber = {testMethod.LineNumber},");
        writer.AppendLine($"InheritanceDepth = {testMethod.InheritanceDepth},");
        writer.AppendLine("TestSessionId = testSessionId,");

        // Method metadata
        writer.Append("MethodMetadata = ");
        SourceInformationWriter.GenerateMethodInformation(writer, compilation, testMethod.TypeSymbol, testMethod.MethodSymbol, null, ',');
    }

    private static bool AreSameAttribute(AttributeData a1, AttributeData a2)
    {
        // Compare attributes by their constructor arguments and attribute class
        if (a1.AttributeClass?.Name != a2.AttributeClass?.Name)
        {
            return false;
        }

        if (a1.ConstructorArguments.Length != a2.ConstructorArguments.Length)
        {
            return false;
        }

        for (var i = 0; i < a1.ConstructorArguments.Length; i++)
        {
            if (!a1.ConstructorArguments[i].Equals(a2.ConstructorArguments[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static ITypeSymbol[]? InferTypesFromTypeInferringAttributes(IMethodSymbol method)
    {
        var inferredTypes = new Dictionary<string, ITypeSymbol>();
        var typeParameters = method.TypeParameters;

        // Look at each parameter to see if it has generic data source attributes we can infer types from
        foreach (var parameter in method.Parameters)
        {
            if (parameter.Type is ITypeParameterSymbol typeParam)
            {
                // Check if this parameter has attributes that implement IInfersType<T>
                foreach (var attr in parameter.GetAttributes())
                {
                    if (attr.AttributeClass != null)
                    {
                        // Look for IInfersType<T> in the attribute's interfaces
                        var infersTypeInterface = attr.AttributeClass.AllInterfaces
                            .FirstOrDefault(i => i.GloballyQualifiedNonGeneric() == "global::TUnit.Core.Interfaces.IInfersType" &&
                                                 i.IsGenericType &&
                                                 i.TypeArguments.Length == 1);

                        if (infersTypeInterface != null)
                        {
                            // Get the type argument from IInfersType<T>
                            var inferredType = infersTypeInterface.TypeArguments[0];

                            // Map this to the method's type parameter
                            inferredTypes[typeParam.Name] = inferredType;
                            break;
                        }
                    }
                }
            }
        }

        // Return null if we didn't infer all type parameters
        if (inferredTypes.Count != typeParameters.Length)
        {
            return null;
        }

        // Build the result array in the correct order
        var result = new ITypeSymbol[typeParameters.Length];
        for (var i = 0; i < typeParameters.Length; i++)
        {
            if (!inferredTypes.TryGetValue(typeParameters[i].Name, out var inferredType))
            {
                return null;
            }
            result[i] = inferredType;
        }

        return result;
    }

    private static ITypeSymbol[]? InferTypesFromTypedDataSourceForClass(INamedTypeSymbol classSymbol, IMethodSymbol method)
    {
        var inferredTypes = new Dictionary<string, ITypeSymbol>();
        var classTypeParameters = classSymbol.TypeParameters;

        // Look at each parameter to see if it has typed data source attributes
        foreach (var parameter in method.Parameters)
        {
            if (parameter.Type is ITypeParameterSymbol typeParam)
            {
                // Check if this is a class type parameter
                var isClassTypeParam = classTypeParameters.Any(ctp => ctp.Name == typeParam.Name);
                if (isClassTypeParam)
                {
                    // Check if this parameter has attributes that implement IInfersType<T>
                    foreach (var attr in parameter.GetAttributes())
                    {
                        if (attr.AttributeClass != null)
                        {
                            // Look for IInfersType<T> in the attribute's interfaces
                            var infersTypeInterface = attr.AttributeClass.AllInterfaces
                                .FirstOrDefault(i => i.GloballyQualifiedNonGeneric() == "global::TUnit.Core.Interfaces.IInfersType" &&
                                                     i.IsGenericType &&
                                                     i.TypeArguments.Length == 1);

                            if (infersTypeInterface != null)
                            {
                                // Get the type argument from IInfersType<T>
                                var inferredType = infersTypeInterface.TypeArguments[0];
                                inferredTypes[typeParam.Name] = inferredType;
                                break;
                            }

                            // Fall back to checking if it's a data source attribute with type info
                            if (attr.AttributeClass.IsOrInherits("global::TUnit.Core.AsyncDataSourceGeneratorAttribute"))
                            {
                                // Try to infer from the base type
                                var baseType = GetTypedDataSourceBase(attr.AttributeClass);
                                if (baseType is { TypeArguments.Length: > 0 })
                                {
                                    var dataSourceType = baseType.TypeArguments[0];
                                    inferredTypes[typeParam.Name] = dataSourceType;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Return null if we didn't infer all class type parameters
        if (inferredTypes.Count != classTypeParameters.Length)
        {
            return null;
        }

        // Build the result array in the correct order
        var result = new ITypeSymbol[classTypeParameters.Length];
        for (var i = 0; i < classTypeParameters.Length; i++)
        {
            if (!inferredTypes.TryGetValue(classTypeParameters[i].Name, out var inferredType))
            {
                return null;
            }
            result[i] = inferredType;
        }

        return result;
    }

    private static INamedTypeSymbol? GetTypedDataSourceBase(INamedTypeSymbol attributeClass)
    {
        var current = attributeClass.BaseType;
        while (current != null)
        {
            if (current.IsGenericType)
            {
                var name = current.Name;
                var namespaceName = current.ContainingNamespace?.ToDisplayString();

                // Check for exact match of the typed base classes
                if (name is "DataSourceGeneratorAttribute" or "AsyncDataSourceGeneratorAttribute" &&
                    namespaceName?.Contains("TUnit.Core") == true)
                {
                    return current;
                }
            }
            current = current.BaseType;
        }
        return null;
    }

    private static void GenerateConcreteTestMetadataForNonGeneric(
        CodeWriter writer,
        TestMethodMetadata testMethod,
        string className,
        AttributeData? classDataSourceAttribute,
        AttributeData? methodDataSourceAttribute)
    {
        var compilation = testMethod.Context!.Value.SemanticModel.Compilation;
        var methodName = testMethod.MethodSymbol.Name;

        writer.AppendLine($"var metadata = new global::TUnit.Core.TestMetadata<{className}>");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"TestName = \"{methodName}\",");
        writer.AppendLine($"TestClassType = typeof({className}),");
        writer.AppendLine($"TestMethodName = \"{methodName}\",");

        // Generate metadata with filtered data sources for this specific combination
        var filteredClassAttributes = new List<AttributeData>();
        var filteredMethodAttributes = new List<AttributeData>();

        // Add the specific data source attributes we're generating for
        if (classDataSourceAttribute != null)
        {
            filteredClassAttributes.Add(classDataSourceAttribute);
        }
        if (methodDataSourceAttribute != null)
        {
            filteredMethodAttributes.Add(methodDataSourceAttribute);
        }

        // Add other non-data-source attributes from class and method
        filteredClassAttributes.AddRange(testMethod.TypeSymbol.GetAttributesIncludingBaseTypes()
            .Where(a => !DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass)));
        filteredMethodAttributes.AddRange(testMethod.MethodSymbol.GetAttributes()
            .Where(a => !DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass)));

        // Generate metadata

        GenerateDependencies(writer, testMethod.MethodSymbol);

        // Generate attribute factory
        writer.AppendLine("AttributeFactory = static () =>");
        writer.AppendLine("[");
        writer.Indent();

        var attributes = filteredMethodAttributes
            .Concat(filteredClassAttributes)
            .Concat(testMethod.TypeSymbol.ContainingAssembly.GetAttributes())
            .ToImmutableArray();

        testMethod.CompilationContext.AttributeWriter.WriteAttributes(writer, attributes);

        writer.Unindent();
        writer.AppendLine("],");

        // Extract and emit RepeatCount if present
        var repeatCount = ExtractRepeatCount(testMethod.MethodSymbol, testMethod.TypeSymbol);
        if (repeatCount.HasValue)
        {
            writer.AppendLine($"RepeatCount = {repeatCount.Value},");
        }

        if (methodDataSourceAttribute == null)
        {
            writer.AppendLine("DataSources = global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>(),");
        }
        else
        {
            writer.AppendLine("DataSources = new global::TUnit.Core.IDataSourceAttribute[]");
            writer.AppendLine("{");
            writer.Indent();
            GenerateDataSourceAttribute(writer, testMethod.CompilationContext, methodDataSourceAttribute, testMethod.MethodSymbol, testMethod.TypeSymbol);
            writer.Unindent();
            writer.AppendLine("},");
        }

        // Generate class data sources
        if (classDataSourceAttribute == null)
        {
            writer.AppendLine("ClassDataSources = global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>(),");
        }
        else
        {
            writer.AppendLine("ClassDataSources = new global::TUnit.Core.IDataSourceAttribute[]");
            writer.AppendLine("{");
            writer.Indent();
            GenerateDataSourceAttribute(writer, testMethod.CompilationContext, classDataSourceAttribute, testMethod.MethodSymbol, testMethod.TypeSymbol);
            writer.Unindent();
            writer.AppendLine("},");
        }

        // Generate property data sources and injections
        GeneratePropertyDataSources(writer, testMethod);
        GeneratePropertyInjections(writer, testMethod.TypeSymbol, className);


        // Method metadata
        writer.Append("MethodMetadata = ");
        SourceInformationWriter.GenerateMethodInformation(writer, compilation, testMethod.TypeSymbol, testMethod.MethodSymbol, null, ',');

        // Generate instance factory
        writer.AppendLine("InstanceFactory = static (typeArgs, args) =>");
        writer.AppendLine("{");
        writer.Indent();

        // Check if the class has a constructor that requires arguments
        var hasParameterizedConstructor = false;
        var constructorParamCount = 0;

        // Find the primary constructor or first public constructor
        var constructor = testMethod.TypeSymbol.Constructors
            .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();

        if (constructor is { Parameters.Length: > 0 })
        {
            hasParameterizedConstructor = true;
            constructorParamCount = constructor.Parameters.Length;
        }

        if (hasParameterizedConstructor)
        {
            // For classes with constructor parameters, check if we have Arguments attribute
            var isArgumentsAttribute = classDataSourceAttribute?.AttributeClass?.Name == "ArgumentsAttribute";

            if (isArgumentsAttribute && classDataSourceAttribute is { ConstructorArguments.Length: > 0 } &&
                classDataSourceAttribute.ConstructorArguments[0].Kind == TypedConstantKind.Array)
            {
                var argumentValues = classDataSourceAttribute.ConstructorArguments[0].Values;
                var constructorArgs = string.Join(", ", argumentValues.Select(arg => TypedConstantParser.GetRawTypedConstantValue(arg)));

                writer.AppendLine($"return new {className}({constructorArgs});");
            }
            else
            {
                // Use the args parameter if no specific arguments are provided
                writer.AppendLine($"if (args.Length >= {constructorParamCount})");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine($"return new {className}({string.Join(", ", Enumerable.Range(0, constructorParamCount).Select(i => $"args[{i}]"))});");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine("throw new global::System.InvalidOperationException(\"Not enough arguments provided for class constructor\");");
            }
        }
        else
        {
            // No constructor parameters needed
            writer.AppendLine($"return new {className}();");
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Generate typed invoker
        GenerateTypedInvokers(writer, testMethod, className);

        writer.AppendLine($"FilePath = @\"{(testMethod.FilePath ?? "").Replace("\\", "\\\\")}\",");
        writer.AppendLine($"LineNumber = {testMethod.LineNumber},");

        writer.Unindent();
        writer.AppendLine("};");

        writer.AppendLine("yield return metadata;");
    }

    private static int? ExtractRepeatCount(IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
    {
        // Check method-level RepeatAttribute first
        var repeatAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RepeatAttribute");

        if (repeatAttribute?.ConstructorArguments.Length > 0
            && repeatAttribute.ConstructorArguments[0].Value is int methodCount)
        {
            return methodCount;
        }

        // Check class-level RepeatAttribute (can be inherited)
        var classRepeatAttr = typeSymbol.GetAttributesIncludingBaseTypes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RepeatAttribute");

        if (classRepeatAttr?.ConstructorArguments.Length > 0
            && classRepeatAttr.ConstructorArguments[0].Value is int classCount)
        {
            return classCount;
        }

        // No repeat attribute found
        return null;
    }
}

public class InheritsTestsClassMetadata
{
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public required ClassDeclarationSyntax ClassSyntax { get; init; }
    public GeneratorAttributeSyntaxContext Context { get; init; }
    public required CompilationContext CompilationContext { get; init; }
}
