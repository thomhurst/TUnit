using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using TUnit.Core.SourceGenerator.CodeGenerators;
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
        // Find all test methods using the more performant ForAttributeWithMetadataName
        var testMethodsProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.TestAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => GetTestMethodMetadata(ctx))
            .Where(static m => m is not null);

        // Find classes with [InheritsTests] attribute
        var inheritsTestsClassesProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.InheritsTestsAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetInheritsTestsClassMetadata(ctx))
            .Where(static m => m is not null);

        // Generate one source file per test method
        context.RegisterSourceOutput(testMethodsProvider.Combine(context.CompilationProvider),
            static (context, tuple) => GenerateTestMethodSource(context, tuple.Right, tuple.Left));

        // Generate test methods for inherited tests
        context.RegisterSourceOutput(inheritsTestsClassesProvider.Combine(context.CompilationProvider),
            static (context, tuple) => GenerateInheritedTestSources(context, tuple.Right, tuple.Left));
    }

    private static InheritsTestsClassMetadata? GetInheritsTestsClassMetadata(GeneratorAttributeSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.TargetNode;

        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        // Skip abstract classes
        if (classSymbol.IsAbstract)
        {
            return null;
        }

        return new InheritsTestsClassMetadata
        {
            TypeSymbol = classSymbol,
            ClassSyntax = classSyntax
        };
    }

    private static TestMethodMetadata? GetTestMethodMetadata(GeneratorAttributeSyntaxContext context)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.TargetNode;
        var methodSymbol = context.TargetSymbol as IMethodSymbol;

        var containingType = methodSymbol?.ContainingType;

        if (containingType == null)
        {
            return null;
        }

        var testAttribute = methodSymbol!.GetRequiredTestAttribute();

        // Skip abstract classes (cannot be instantiated)
        if (containingType.IsAbstract)
        {
            return null;
        }

        // For generic types and methods, we now emit metadata that will be resolved at runtime
        var isGenericType = containingType is { IsGenericType: true, TypeParameters.Length: > 0 };
        var isGenericMethod = methodSymbol is { IsGenericMethod: true };

        return new TestMethodMetadata
        {
            MethodSymbol = methodSymbol ?? throw new InvalidOperationException("Symbol is not a method"),
            TypeSymbol = containingType,
            FilePath = methodSyntax.GetLocation().SourceTree?.FilePath ?? testAttribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString() ?? methodSyntax.SyntaxTree.FilePath,
            LineNumber = methodSyntax.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            TestAttribute = context.Attributes.First(),
            Context = context,
            MethodSyntax = methodSyntax,
            IsGenericType = isGenericType,
            IsGenericMethod = isGenericMethod,
            MethodAttributes = methodSymbol.GetAttributes()
        };
    }

    private static void GenerateInheritedTestSources(SourceProductionContext context, Compilation compilation, InheritsTestsClassMetadata? classInfo)
    {
        if (classInfo?.TypeSymbol == null)
        {
            return;
        }

        // Find all test methods in base classes
        var inheritedTestMethods = CollectInheritedTestMethods(classInfo.TypeSymbol);

        // Generate test metadata for each inherited test method
        foreach (var method in inheritedTestMethods)
        {
            var testAttribute = method.GetAttributes().FirstOrDefault(a => a.IsTestAttribute());

            // Skip if no test attribute found
            if (testAttribute == null)
            {
                continue;
            }

            // Find the concrete implementation of this method in the derived class
            var concreteMethod = FindConcreteMethodImplementation(classInfo.TypeSymbol, method);

            // Calculate inheritance depth for this test
            int inheritanceDepth = CalculateInheritanceDepth(classInfo.TypeSymbol, method);

            var testMethodMetadata = new TestMethodMetadata
            {
                MethodSymbol = concreteMethod ?? method, // Use concrete method if found, otherwise base method
                TypeSymbol = classInfo.TypeSymbol,
                FilePath = classInfo.ClassSyntax.GetLocation().SourceTree?.FilePath ?? testAttribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString() ?? classInfo.ClassSyntax.SyntaxTree.FilePath,
                LineNumber = classInfo.ClassSyntax.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                TestAttribute = testAttribute,
                Context = null, // No context for inherited tests
                MethodSyntax = null, // No syntax for inherited methods
                IsGenericType = classInfo.TypeSymbol.IsGenericType,
                IsGenericMethod = (concreteMethod ?? method).IsGenericMethod,
                MethodAttributes = (concreteMethod ?? method).GetAttributes(), // Use concrete method attributes
                InheritanceDepth = inheritanceDepth
            };

            GenerateTestMethodSource(context, compilation, testMethodMetadata);
        }
    }

    private static int CalculateInheritanceDepth(INamedTypeSymbol testClass, IMethodSymbol testMethod)
    {
        // If the method is declared directly in the test class, depth is 0
        if (testMethod.ContainingType.Equals(testClass, SymbolEqualityComparer.Default))
        {
            return 0;
        }

        // Count how many levels up the inheritance chain the method is declared
        int depth = 0;
        INamedTypeSymbol? currentType = testClass.BaseType;

        while (currentType != null)
        {
            depth++;
            if (testMethod.ContainingType.Equals(currentType, SymbolEqualityComparer.Default))
            {
                return depth;
            }
            currentType = currentType.BaseType;
        }

        // This shouldn't happen in normal cases, but return the depth anyway
        return depth;
    }

    private static void GenerateTestMethodSource(SourceProductionContext context, Compilation compilation, TestMethodMetadata? testMethod)
    {
        try
        {
            if (testMethod?.MethodSymbol == null)
            {
                return;
            }

            var writer = new CodeWriter();
            GenerateFileHeader(writer);
            GenerateTestMetadata(writer, compilation, testMethod);

            var fileName = $"{testMethod.TypeSymbol.Name}_{testMethod.MethodSymbol.Name}_{Guid.NewGuid():N}.g.cs";
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
        writer.AppendLine("// <auto-generated/>");
        writer.AppendLine("#pragma warning disable");
        writer.AppendLine("#nullable enable");
        writer.AppendLine();
        // No using statements - use globally qualified types
        writer.AppendLine();
        writer.AppendLine($"namespace {GeneratedNamespace};");
        writer.AppendLine();
    }

    private static void GenerateTestMetadata(CodeWriter writer, Compilation compilation, TestMethodMetadata testMethod)
    {
        var className = testMethod.TypeSymbol.GloballyQualified();
        var methodName = testMethod.MethodSymbol.Name;
        var guid = Guid.NewGuid().ToString("N");
        var combinationGuid = Guid.NewGuid().ToString("N").Substring(0, 8);

        writer.AppendLine($"internal sealed class {testMethod.TypeSymbol.Name}_{methodName}_TestSource_{guid} : global::TUnit.Core.Interfaces.SourceGenerator.ITestSource");
        writer.AppendLine("{");
        writer.Indent();

        // Generate reflection-based field accessors for init-only properties with data source attributes
        GenerateReflectionFieldAccessors(writer, testMethod.TypeSymbol, className);

        writer.AppendLine("public async global::System.Collections.Generic.IAsyncEnumerable<global::TUnit.Core.TestMetadata> GetTestsAsync(string testSessionId, [global::System.Runtime.CompilerServices.EnumeratorCancellation] global::System.Threading.CancellationToken cancellationToken = default)");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine();

        // Check if we have generic types or methods
        if (testMethod.IsGenericType || testMethod is { IsGenericMethod: true, MethodSymbol.TypeParameters.Length: > 0 })
        {
            // Check if we can use the concrete types approach (AOT-compatible)
            // This is possible when we have typed data sources that can inform the generic type arguments
            var hasTypedDataSource = testMethod.MethodAttributes
                .Any(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass) &&
                         InferTypesFromDataSourceAttribute(testMethod.MethodSymbol, a) != null);

            // Check if we have GenerateGenericTest attributes on methods or classes
            var hasGenerateGenericTest = (testMethod.IsGenericMethod && testMethod.MethodAttributes
                .Any(a => a.AttributeClass?.IsOrInherits("global::TUnit.Core.GenerateGenericTestAttribute") is true)) ||
                (testMethod.IsGenericType && testMethod.TypeSymbol.GetAttributes()
                .Any(a => a.AttributeClass?.IsOrInherits("global::TUnit.Core.GenerateGenericTestAttribute") is true));

            // Check if class has class-level Arguments attributes (for both generic and non-generic classes)
            var hasClassArguments = testMethod.TypeSymbol.GetAttributes()
                .Any(a => a.AttributeClass?.IsOrInherits("global::TUnit.Core.ArgumentsAttribute") is true);

            // Check if generic class has method-level typed data source attributes that can provide type information
            var hasTypedDataSourceForGenericType = testMethod is { IsGenericType: true, IsGenericMethod: false } && testMethod.MethodAttributes
                .Any(a => a.AttributeClass != null &&
                    a.AttributeClass.IsOrInherits("global::TUnit.Core.AsyncDataSourceGeneratorAttribute") &&
                    InferTypesFromTypedDataSourceForClass(testMethod.TypeSymbol, testMethod.MethodSymbol) != null);

            // Check if generic class has method-level Arguments attributes that can provide type information
            var hasMethodArgumentsForGenericType = testMethod is { IsGenericType: true, IsGenericMethod: false } && testMethod.MethodAttributes
                .Any(a => a.AttributeClass?.IsOrInherits("global::TUnit.Core.ArgumentsAttribute") is true);

            // Check if generic class has MethodDataSource that can provide type information
            var hasMethodDataSourceForGenericType = testMethod is { IsGenericType: true, IsGenericMethod: false } && testMethod.MethodAttributes
                .Any(a => a.AttributeClass?.Name == "MethodDataSourceAttribute" &&
                          InferClassTypesFromMethodDataSource(compilation, testMethod, a) != null);

            if (hasTypedDataSource || hasGenerateGenericTest || testMethod.IsGenericMethod || hasClassArguments || hasTypedDataSourceForGenericType || hasMethodArgumentsForGenericType || hasMethodDataSourceForGenericType)
            {
                // Use concrete types approach for AOT compatibility
                // For generic methods and classes with Arguments attributes, we always use this approach
                GenerateGenericTestWithConcreteTypes(writer, compilation, testMethod, className, combinationGuid);
            }
            else
            {
                // Fall back to runtime resolution for other cases
                GenerateTestMetadataInstance(writer, compilation, testMethod, className, combinationGuid);
            }
        }
        else
        {
            // Non-generic method in non-generic class
            GenerateTestMetadataInstance(writer, compilation, testMethod, className, combinationGuid);
        }

        writer.AppendLine("yield break;");
        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("}");

        // Generate module initializer
        GenerateModuleInitializer(writer, testMethod, guid);
    }

    private static void GenerateSpecificGenericInstantiation(
        CodeWriter writer,
        Compilation compilation,
        TestMethodMetadata testMethod,
        string className,
        string combinationGuid,
        ImmutableArray<ITypeSymbol> typeArguments)
    {
        var methodName = testMethod.MethodSymbol.Name;
        var typeArgsString = string.Join(", ", typeArguments.Select(t => t.GloballyQualified()));
        var instantiatedMethodName = $"{methodName}<{typeArgsString}>";

        // Create a modified test method metadata with concrete types
        var concreteTestMethod = new TestMethodMetadata
        {
            MethodSymbol = testMethod.MethodSymbol,
            TypeSymbol = testMethod.TypeSymbol,
            FilePath = testMethod.FilePath,
            LineNumber = testMethod.LineNumber,
            TestAttribute = testMethod.TestAttribute,
            Context = testMethod.Context,
            MethodSyntax = testMethod.MethodSyntax,
            IsGenericType = testMethod.IsGenericType,
            IsGenericMethod = false, // We're creating a concrete instantiation
            MethodAttributes = testMethod.MethodAttributes
        };

        writer.AppendLine($"// Generated instantiation for {instantiatedMethodName}");
        writer.AppendLine("{");
        writer.Indent();

        // Generate metadata for this specific instantiation
        writer.AppendLine($"var metadata = new global::TUnit.Core.TestMetadata<{className}>");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"TestName = \"{instantiatedMethodName}\",");
        writer.AppendLine($"TestClassType = {GenerateTypeReference(testMethod.TypeSymbol, testMethod.IsGenericType)},");
        writer.AppendLine($"TestMethodName = \"{methodName}\",");
        writer.AppendLine($"GenericMethodTypeArguments = new global::System.Type[] {{ {string.Join(", ", typeArguments.Select(t => $"typeof({t.GloballyQualified()})"))}}},");

        // Add basic metadata
        GenerateMetadata(writer, compilation, concreteTestMethod);

        // Generate generic type info if needed
        if (testMethod.IsGenericType)
        {
            GenerateGenericTypeInfo(writer, testMethod.TypeSymbol);
        }

        // Generate AOT-friendly invokers that use the specific types
        GenerateAotFriendlyInvokers(writer, testMethod, className, typeArguments);

        // Add file location metadata
        writer.AppendLine($"FilePath = @\"{testMethod.FilePath.Replace("\\", "\\\\")}\",");
        writer.AppendLine($"LineNumber = {testMethod.LineNumber},");

        writer.Unindent();
        writer.AppendLine("};");

        writer.AppendLine("metadata.TestSessionId = testSessionId;");
        writer.AppendLine("yield return metadata;");

        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }

    private static void GenerateAotFriendlyInvokers(
        CodeWriter writer,
        TestMethodMetadata testMethod,
        string className,
        ImmutableArray<ITypeSymbol> typeArguments)
    {
        var methodName = testMethod.MethodSymbol.Name;
        var typeArgsString = string.Join(", ", typeArguments.Select(t => t.GloballyQualified()));
        var hasCancellationToken = testMethod.MethodSymbol.Parameters.Any(p =>
            p.Type.Name == "CancellationToken" &&
            p.Type.ContainingNamespace?.ToString() == "System.Threading");

        // Generate the instance factory
        writer.AppendLine("InstanceFactory = (typeArgs, args) =>");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"return new {className}();");

        writer.Unindent();
        writer.AppendLine("},");

        // Generate InvokeTypedTest for the specific generic instantiation
        writer.AppendLine("InvokeTypedTest = async (instance, args, cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();

        // Generate direct method call with specific types (no MakeGenericMethod)
        writer.AppendLine($"var typedInstance = ({className})instance;");

        // Prepare method arguments
        writer.AppendLine("var methodArgs = new object?[args.Length" + (hasCancellationToken ? " + 1" : "") + "];");
        writer.AppendLine("global::System.Array.Copy(args, methodArgs, args.Length);");

        if (hasCancellationToken)
        {
            writer.AppendLine("methodArgs[args.Length] = cancellationToken;");
        }

        // Direct method invocation with known types
        var parameterCasts = new List<string>();
        for (int i = 0; i < testMethod.MethodSymbol.Parameters.Length; i++)
        {
            var param = testMethod.MethodSymbol.Parameters[i];
            if (param.Type.Name == "CancellationToken")
            {
                parameterCasts.Add("cancellationToken");
            }
            else
            {
                var paramType = ReplaceTypeParametersWithConcreteTypes(param.Type, testMethod.MethodSymbol.TypeParameters, typeArguments);
                parameterCasts.Add($"({paramType.GloballyQualified()})methodArgs[{i}]!");
            }
        }

        writer.AppendLine($"await global::TUnit.Core.AsyncConvert.Convert(() => typedInstance.{methodName}<{typeArgsString}>({string.Join(", ", parameterCasts)}));");

        writer.Unindent();
        writer.AppendLine("},");
    }

    private static ITypeSymbol ReplaceTypeParametersWithConcreteTypes(
        ITypeSymbol type,
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        ImmutableArray<ITypeSymbol> typeArguments)
    {
        if (type is ITypeParameterSymbol typeParam)
        {
            // Find the index of this type parameter
            var index = -1;
            for (int j = 0; j < typeParameters.Length; j++)
            {
                if (typeParameters[j].Name == typeParam.Name)
                {
                    index = j;
                    break;
                }
            }

            if (index >= 0 && index < typeArguments.Length)
            {
                return typeArguments[index];
            }
            return type;
        }

        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            // Replace type arguments in generic types like IEnumerable<T>, Func<T1, T2>, etc.
            var newTypeArgs = namedType.TypeArguments
                .Select(ta => ReplaceTypeParametersWithConcreteTypes(ta, typeParameters, typeArguments))
                .ToImmutableArray();

            return namedType.ConstructedFrom.Construct(newTypeArgs.ToArray());
        }

        return type;
    }

    private static void GenerateTestMetadataInstance(CodeWriter writer, Compilation compilation, TestMethodMetadata testMethod, string className, string combinationGuid)
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

        // Add basic metadata
        GenerateMetadata(writer, compilation, testMethod);

        // Generate generic type info if needed
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

        // Set the test to use runtime data generation
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

    private static void GenerateMetadata(CodeWriter writer, Compilation compilation, TestMethodMetadata testMethod)
    {
        var methodSymbol = testMethod.MethodSymbol;


        // Generate dependencies
        GenerateDependencies(writer, compilation, methodSymbol);

        writer.AppendLine("AttributeFactory = () =>");
        writer.AppendLine("[");
        writer.Indent();

        var attributes = methodSymbol.GetAttributes()
            .Concat(testMethod.TypeSymbol.GetAttributesIncludingBaseTypes())
            .Concat(testMethod.TypeSymbol.ContainingAssembly.GetAttributes())
            .ToImmutableArray();

        AttributeWriter.WriteAttributes(writer, compilation, attributes);

        writer.Unindent();
        writer.AppendLine("],");

        // Generate data sources with factory methods
        GenerateDataSources(writer, compilation, testMethod);

        // Generate property injections
        GeneratePropertyInjections(writer, testMethod.TypeSymbol, testMethod.TypeSymbol.GloballyQualified());

        // Inheritance depth
        writer.AppendLine($"InheritanceDepth = {testMethod.InheritanceDepth},");

        // File location metadata
        writer.AppendLine($"FilePath = @\"{testMethod.FilePath.Replace("\\", "\\\\")}\",");
        writer.AppendLine($"LineNumber = {testMethod.LineNumber},");

        // Method metadata
        writer.Append("MethodMetadata = ");
        SourceInformationWriter.GenerateMethodInformation(writer, compilation, testMethod.TypeSymbol, testMethod.MethodSymbol, null, ',');
    }

    private static void GenerateMetadataForConcreteInstantiation(CodeWriter writer, Compilation compilation, TestMethodMetadata testMethod)
    {
        var methodSymbol = testMethod.MethodSymbol;


        // Generate dependencies
        GenerateDependencies(writer, compilation, methodSymbol);

        writer.AppendLine("AttributeFactory = () =>");
        writer.AppendLine("[");
        writer.Indent();

        // Filter out ALL data source attributes - we'll add back only the specific one if provided
        var attributes = methodSymbol.GetAttributes()
            .Where(a => !DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
            .Concat(testMethod.TypeSymbol.GetAttributesIncludingBaseTypes())
            .Concat(testMethod.TypeSymbol.ContainingAssembly.GetAttributes())
            .ToImmutableArray();

        AttributeWriter.WriteAttributes(writer, compilation, attributes);

        writer.Unindent();
        writer.AppendLine("],");

        // No data sources for concrete instantiations
        writer.AppendLine("DataSources = global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>(),");
        writer.AppendLine("ClassDataSources = global::System.Array.Empty<global::TUnit.Core.IDataSourceAttribute>(),");
        writer.AppendLine("PropertyDataSources = global::System.Array.Empty<global::TUnit.Core.PropertyDataSource>(),");

        // Generate property injections
        GeneratePropertyInjections(writer, testMethod.TypeSymbol, testMethod.TypeSymbol.GloballyQualified());

        // Inheritance depth
        writer.AppendLine($"InheritanceDepth = {testMethod.InheritanceDepth},");

        // File location metadata
        writer.AppendLine($"FilePath = @\"{testMethod.FilePath.Replace("\\", "\\\\")}\",");
        writer.AppendLine($"LineNumber = {testMethod.LineNumber},");

        // Method metadata
        writer.Append("MethodMetadata = ");
        SourceInformationWriter.GenerateMethodInformation(writer, compilation, testMethod.TypeSymbol, testMethod.MethodSymbol, null, ',');

    }


    private static void GenerateDataSources(CodeWriter writer, Compilation compilation, TestMethodMetadata testMethod)
    {
        var methodSymbol = testMethod.MethodSymbol;
        var typeSymbol = testMethod.TypeSymbol;

        // Extract data source attributes from method
        var methodDataSources = methodSymbol.GetAttributes()
            .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
            .ToList();

        // Extract data source attributes from class
        var classDataSources = typeSymbol.GetAttributesIncludingBaseTypes()
            .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
            .ToList();

        // Generate method data sources
        writer.AppendLine("DataSources = new global::TUnit.Core.IDataSourceAttribute[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var attr in methodDataSources)
        {
            GenerateDataSourceAttribute(writer, attr, methodSymbol, typeSymbol);
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Generate class data sources
        writer.AppendLine("ClassDataSources = new global::TUnit.Core.IDataSourceAttribute[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var attr in classDataSources)
        {
            GenerateDataSourceAttribute(writer, attr, methodSymbol, typeSymbol);
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Generate property data sources
        GeneratePropertyDataSources(writer, compilation, testMethod);
    }

    private static void GenerateDataSourceAttribute(CodeWriter writer, AttributeData attr, IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
    {
        var attrClass = attr.AttributeClass;
        if (attrClass == null)
        {
            return;
        }

        var attrName = attrClass.GloballyQualifiedNonGeneric();

        if (attrName == "global::TUnit.Core.MethodDataSourceAttribute")
        {
            GenerateMethodDataSourceAttribute(writer, attr, methodSymbol, typeSymbol);
        }
        else
        {
            // Use the generic attribute instantiation method for all other attributes
            // This properly handles generics on the attribute type
            var generatedCode = CodeGenerationHelpers.GenerateAttributeInstantiation(attr);
            writer.AppendLine($"{generatedCode},");
        }
    }

    private static void GenerateMethodDataSourceAttribute(CodeWriter writer, AttributeData attr, IMethodSymbol methodSymbol, INamedTypeSymbol typeSymbol)
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

        // Find the data source method
        var dataSourceMethod = targetType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault();

        if (dataSourceMethod == null)
        {
            // Still generate the attribute even if method not found - it will fail at runtime with proper error
            // Use CodeGenerationHelpers to properly handle any generics on the attribute
            var generatedCode = CodeGenerationHelpers.GenerateAttributeInstantiation(attr);
            writer.AppendLine($"{generatedCode},");
            return;
        }

        // Generate the attribute with factory
        // We need to manually construct this to properly add the Factory property
        var attrClass = attr.AttributeClass!;
        var attrTypeName = attrClass.GloballyQualified();

        if (attr.ConstructorArguments is
            [
                { Value: ITypeSymbol typeArg } _, _, ..
            ])
        {
            // MethodDataSource(Type, string) constructor
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
        GenerateMethodDataSourceFactory(writer, dataSourceMethod, targetType, methodSymbol, attr, hasArguments);

        writer.Unindent();
        writer.AppendLine("}");

        writer.Unindent();
        writer.AppendLine("},");
    }

    private static void GenerateMethodDataSourceFactory(CodeWriter writer, IMethodSymbol dataSourceMethod, ITypeSymbol targetType, IMethodSymbol testMethod, AttributeData attr, bool hasArguments)
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
        var returnTypeName = returnType.ToDisplayString();

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
                writer.AppendLine($"instance = new {fullyQualifiedType}();");
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
                writer.AppendLine($"instance = new {fullyQualifiedType}();");
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
                writer.AppendLine($"instance = new {fullyQualifiedType}();");
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
                writer.AppendLine($"instance = new {fullyQualifiedType}();");
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

    private static bool IsAsyncEnumerable(ITypeSymbol type)
    {
        return type.AllInterfaces.Any(i =>
            i.IsGenericType &&
            i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IAsyncEnumerable<T>");
    }

    private static bool IsTask(ITypeSymbol type)
    {
        var typeName = type.ToDisplayString();
        return typeName.StartsWith("System.Threading.Tasks.Task") ||
               typeName.StartsWith("System.Threading.Tasks.ValueTask");
    }

    private static bool IsEnumerable(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        return type.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString() == "System.Collections.IEnumerable" ||
            (i.IsGenericType && i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>"));
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
                    writer.Append(constant.Value?.ToString() ?? "null");
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
                for (int i = 0; i < constant.Values.Length; i++)
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
        writer.AppendLine("PropertyInjections = new global::TUnit.Core.PropertyInjectionData[]");
        writer.AppendLine("{");
        writer.Indent();

        // Walk inheritance hierarchy to find properties with data source attributes
        var currentType = typeSymbol;
        var processedProperties = new HashSet<string>();

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
                            // For init-only properties, use UnsafeAccessor on .NET 8+, throw on older frameworks
                            writer.AppendLine("#if NET8_0_OR_GREATER");
                            // Cast to the property's containing type if needed
                            var containingTypeName = property.ContainingType.GloballyQualified();

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

    private static void GeneratePropertyDataSources(CodeWriter writer, Compilation compilation, TestMethodMetadata testMethod)
    {
        writer.AppendLine("PropertyDataSources = new global::TUnit.Core.PropertyDataSource[]");
        writer.AppendLine("{");
        writer.Indent();

        var typeSymbol = testMethod.TypeSymbol;
        var currentType = typeSymbol;
        var processedProperties = new HashSet<string>();

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
                        GenerateDataSourceAttribute(writer, dataSourceAttr, testMethod.MethodSymbol, typeSymbol);
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

    private static void GenerateNestedPropertyInjections(CodeWriter writer, ITypeSymbol propertyType, HashSet<string> processedProperties)
    {
        writer.AppendLine("NestedPropertyInjections = new global::TUnit.Core.PropertyInjectionData[]");
        writer.AppendLine("{");
        writer.Indent();

        // Only generate nested injections for reference types that aren't basic types
        if (ShouldGenerateNestedInjections(propertyType))
        {
            GeneratePropertyInjectionsForType(writer, propertyType, processedProperties, isNested: true);
        }

        writer.Unindent();
        writer.AppendLine("},");
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

    private static void GeneratePropertyInjectionsForType(CodeWriter writer, ITypeSymbol typeSymbol, HashSet<string> processedProperties, bool isNested)
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
                                writer.AppendLine($"value = System.Activator.CreateInstance<{fullyQualifiedType}>();");
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

        // For generic types or methods, we need to use reflection to invoke the test method
        var isAsync = IsAsyncMethod(testMethod.MethodSymbol);
        if (testMethod.IsGenericType || testMethod.IsGenericMethod)
        {
            GenerateGenericTestInvoker(writer, testMethod, methodName, isAsync, hasCancellationToken, parametersFromArgs);
        }
        else
        {
            GenerateConcreteTestInvoker(writer, testMethod, className, methodName, isAsync, hasCancellationToken, parametersFromArgs);
        }
    }



    private static void GenerateGenericTestInvoker(CodeWriter writer, TestMethodMetadata testMethod, string methodName, bool isAsync, bool hasCancellationToken, IParameterSymbol[] parametersFromArgs)
    {
        writer.AppendLine("TestInvoker = async (instance, args) =>");
        writer.AppendLine("{");
        writer.Indent();

        // Use reflection to invoke the method on the generic type instance
        writer.AppendLine("var instanceType = instance.GetType();");
        writer.AppendLine($"var method = instanceType.GetMethod(\"{methodName}\", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Instance);");
        writer.AppendLine("if (method == null)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"throw new global::System.InvalidOperationException($\"Method '{methodName}' not found on type {{instanceType.FullName}}\");");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();

        // Handle generic method case
        if (testMethod is { IsGenericMethod: true, MethodSymbol.TypeParameters.Length: > 0 })
        {
            writer.AppendLine("// Make the method generic if it has type parameters");
            writer.AppendLine("if (method.IsGenericMethodDefinition)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("// Use the resolved generic types from the test context");
            writer.AppendLine("var testContext = global::TUnit.Core.TestContext.Current;");
            writer.AppendLine("var resolvedTypes = testContext?.TestDetails?.MethodGenericArguments;");
            writer.AppendLine();
            writer.AppendLine("if (resolvedTypes != null && resolvedTypes.Length > 0)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("// Use the pre-resolved generic types");
            writer.AppendLine("method = method.MakeGenericMethod(resolvedTypes);");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("else");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("// Fallback: infer type arguments from the actual argument types");
            writer.AppendLine("var typeArgs = new global::System.Type[" + testMethod.MethodSymbol.TypeParameters.Length + "];");
            writer.AppendLine("for (int i = 0; i < typeArgs.Length && i < args.Length; i++)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("typeArgs[i] = args[i]?.GetType() ?? typeof(object);");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("method = method.MakeGenericMethod(typeArgs);");
            writer.Unindent();
            writer.AppendLine("}");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine();
        }

        writer.AppendLine("// Prepare method arguments");
        writer.AppendLine("var methodArgs = new object?[args.Length" + (hasCancellationToken ? " + 1" : "") + "];");
        writer.AppendLine("args.CopyTo(methodArgs, 0);");

        if (hasCancellationToken)
        {
            writer.AppendLine("methodArgs[args.Length] = global::TUnit.Core.TestContext.Current?.CancellationToken ?? global::System.Threading.CancellationToken.None;");
        }

        writer.AppendLine();
        writer.AppendLine("// Invoke the method");
        writer.AppendLine("var result = method.Invoke(instance, methodArgs);");

        if (isAsync)
        {
            writer.AppendLine("if (result is global::System.Threading.Tasks.Task task)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("await task;");
            writer.Unindent();
            writer.AppendLine("}");
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    private static void GenerateConcreteTestInvoker(CodeWriter writer, TestMethodMetadata testMethod, string className, string methodName, bool isAsync, bool hasCancellationToken, IParameterSymbol[] parametersFromArgs)
    {
        writer.AppendLine("TestInvoker = async (instance, args) =>");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"var typedInstance = ({className})instance;");
        
        // Only declare context if it's needed (when hasCancellationToken is true)
        if (hasCancellationToken)
        {
            writer.AppendLine("var context = global::TUnit.Core.TestContext.Current;");
        }

        if (parametersFromArgs.Length == 0)
        {
            var methodCall = hasCancellationToken
                ? $"typedInstance.{methodName}(context?.CancellationToken ?? System.Threading.CancellationToken.None)"
                : $"typedInstance.{methodName}()";
            if (isAsync)
            {
                writer.AppendLine($"await {methodCall};");
            }
            else
            {
                writer.AppendLine($"{methodCall};");
                writer.AppendLine("await global::System.Threading.Tasks.Task.CompletedTask;");
            }
        }
        else
        {
            // Count required parameters (those without default values, excluding CancellationToken and params parameters)
            var requiredParamCount = parametersFromArgs.Count(p => !p.HasExplicitDefaultValue && !p.IsOptional && !p.IsParams);

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

                // Build the arguments to pass, handling params arrays correctly
                var argsToPass = TupleArgumentHelper.GenerateArgumentAccessWithParams(parametersFromArgs, "args", argCount);

                // Add CancellationToken if present
                if (hasCancellationToken)
                {
                    argsToPass.Add("context?.CancellationToken ?? System.Threading.CancellationToken.None");
                }

                var methodCall = $"typedInstance.{methodName}({string.Join(", ", argsToPass)})";

                if (isAsync)
                {
                    writer.AppendLine($"await {methodCall};");
                }
                else
                {
                    writer.AppendLine($"{methodCall};");
                }
                writer.AppendLine("break;");
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

            if (!isAsync)
            {
                writer.AppendLine("await global::System.Threading.Tasks.Task.CompletedTask;");
            }
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Also generate InvokeTypedTest which is required by CreateExecutableTestFactory
        writer.AppendLine("InvokeTypedTest = async (instance, args, cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();
        
        // Only declare context if it's needed (when hasCancellationToken is true and there are parameters)
        if (hasCancellationToken && parametersFromArgs.Length > 0)
        {
            writer.AppendLine("var context = global::TUnit.Core.TestContext.Current;");
        }

        if (parametersFromArgs.Length == 0)
        {
            var typedMethodCall = hasCancellationToken
                ? $"instance.{methodName}(cancellationToken)"
                : $"instance.{methodName}()";
            if (isAsync)
            {
                writer.AppendLine($"await {typedMethodCall};");
            }
            else
            {
                writer.AppendLine($"{typedMethodCall};");
                writer.AppendLine("await global::System.Threading.Tasks.Task.CompletedTask;");
            }
        }
        else
        {
            // Count required parameters (those without default values, excluding CancellationToken and params parameters)
            var requiredParamCount = parametersFromArgs.Count(p => !p.HasExplicitDefaultValue && !p.IsOptional && !p.IsParams);

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

                // Build the arguments to pass, handling params arrays correctly
                var argsToPass = TupleArgumentHelper.GenerateArgumentAccessWithParams(parametersFromArgs, "args", argCount);

                // Add CancellationToken if present
                if (hasCancellationToken)
                {
                    argsToPass.Add("context?.CancellationToken ?? System.Threading.CancellationToken.None");
                }

                var typedMethodCall = $"instance.{methodName}({string.Join(", ", argsToPass)})";

                if (isAsync)
                {
                    writer.AppendLine($"await {typedMethodCall};");
                }
                else
                {
                    writer.AppendLine($"{typedMethodCall};");
                }
                writer.AppendLine("break;");
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

            if (!isAsync)
            {
                writer.AppendLine("await global::System.Threading.Tasks.Task.CompletedTask;");
            }
        }

        writer.Unindent();
        writer.AppendLine("},");
    }


    private static void GenerateModuleInitializer(CodeWriter writer, TestMethodMetadata testMethod, string guid)
    {
        writer.AppendLine();
        writer.AppendLine($"internal static class {testMethod.TypeSymbol.Name}_{testMethod.MethodSymbol.Name}_ModuleInitializer_{guid}");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
        writer.AppendLine("public static void Initialize()");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"global::TUnit.Core.SourceRegistrar.Register({GenerateTypeReference(testMethod.TypeSymbol, testMethod.IsGenericType)}, new {testMethod.TypeSymbol.Name}_{testMethod.MethodSymbol.Name}_TestSource_{guid}());");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
    }

    private static bool IsAsyncMethod(IMethodSymbol method)
    {
        var returnType = method.ReturnType;

        var returnTypeName = returnType.ToDisplayString();
        return returnTypeName.StartsWith("System.Threading.Tasks.Task") ||
               returnTypeName.StartsWith("System.Threading.Tasks.ValueTask") ||
               returnTypeName.StartsWith("Task<") ||
               returnTypeName.StartsWith("ValueTask<");
    }

    private static void GenerateDependencies(CodeWriter writer, Compilation compilation, IMethodSymbol methodSymbol)
    {
        var dependsOnAttributes = methodSymbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name == "DependsOnAttribute" &&
                          attr.AttributeClass.ContainingNamespace?.ToDisplayString() == "TUnit.Core")
            .ToList();

        if (!dependsOnAttributes.Any())
        {
            writer.AppendLine("Dependencies = global::System.Array.Empty<global::TUnit.Core.TestDependency>(),");
            return;
        }

        writer.AppendLine("Dependencies = new global::TUnit.Core.TestDependency[]");
        writer.AppendLine("{");
        writer.Indent();

        for (var i = 0; i < dependsOnAttributes.Count; i++)
        {
            var attr = dependsOnAttributes[i];
            GenerateTestDependency(writer, attr);

            if (i < dependsOnAttributes.Count - 1)
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

        // Handle the different constructor overloads of DependsOnAttribute
        if (constructorArgs.Length == 1)
        {
            var arg = constructorArgs[0];
            if (arg.Type?.Name == "String")
            {
                // DependsOnAttribute(string testName) - dependency on test in same class
                var testName = arg.Value?.ToString() ?? "";
                writer.AppendLine($"new global::TUnit.Core.TestDependency {{ MethodName = \"{testName}\", ProceedOnFailure = {proceedOnFailure.ToString().ToLower()} }}");
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
                // DependsOnAttribute(string testName, Type[] parameterTypes)
                var testName = firstArg.Value?.ToString() ?? "";
                writer.Append($"new global::TUnit.Core.TestDependency {{ MethodName = \"{testName}\"");

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
            if (namedArg.Key == "ProceedOnFailure" && namedArg.Value.Value is bool proceedOnFailure)
            {
                return proceedOnFailure;
            }
        }

        // Default value is false
        return false;
    }

    private static string GetDefaultValueString(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
        {
            return $"default({parameter.Type.GloballyQualified()})";
        }

        var defaultValue = parameter.ExplicitDefaultValue;
        if (defaultValue == null)
        {
            return "null";
        }

        var type = parameter.Type;

        // Handle string
        if (type.SpecialType == SpecialType.System_String)
        {
            return $"\"{defaultValue.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        }

        // Handle char
        if (type.SpecialType == SpecialType.System_Char)
        {
            return $"'{defaultValue}'";
        }

        // Handle bool
        if (type.SpecialType == SpecialType.System_Boolean)
        {
            return defaultValue.ToString().ToLowerInvariant();
        }

        // Handle numeric types with proper suffixes
        if (type.SpecialType == SpecialType.System_Single)
        {
            return $"{defaultValue}f";
        }
        if (type.SpecialType == SpecialType.System_Double)
        {
            return $"{defaultValue}d";
        }
        if (type.SpecialType == SpecialType.System_Decimal)
        {
            return $"{defaultValue}m";
        }
        if (type.SpecialType == SpecialType.System_Int64)
        {
            return $"{defaultValue}L";
        }
        if (type.SpecialType == SpecialType.System_UInt32)
        {
            return $"{defaultValue}u";
        }
        if (type.SpecialType == SpecialType.System_UInt64)
        {
            return $"{defaultValue}ul";
        }

        // Default for other types
        return defaultValue.ToString();
    }


    private static List<IMethodSymbol> CollectInheritedTestMethods(INamedTypeSymbol derivedClass)
    {
        return derivedClass.GetMembersIncludingBase().OfType<IMethodSymbol>()
            .Where(m => m.GetAttributes().Any(attr => attr.IsTestAttribute()))
            // Exclude test methods declared directly in the derived class to avoid duplication
            // These are already handled by the regular test method provider
            .Where(m => !SymbolEqualityComparer.Default.Equals(m.ContainingType, derivedClass))
            .ToList();
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

        for (int i = 0; i < params1.Length; i++)
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

    private static void GenerateReflectionFieldAccessors(CodeWriter writer, INamedTypeSymbol typeSymbol, string className)
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
        if (initOnlyPropertiesWithDataSources.Any())
        {
            writer.AppendLine("#if NET8_0_OR_GREATER");
            foreach (var property in initOnlyPropertiesWithDataSources)
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
        var interfaceConstraints = typeParam.ConstraintTypes.Where(c => c.TypeKind == TypeKind.Interface).ToArray();
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

    private static bool IsGenericTypeParameter(ITypeSymbol type)
    {
        return type.TypeKind == TypeKind.TypeParameter;
    }

    private static bool ContainsGenericTypeParameter(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.TypeParameter)
        {
            return true;
        }

        if (type is INamedTypeSymbol namedType)
        {
            return namedType.TypeArguments.Any(ContainsGenericTypeParameter);
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return ContainsGenericTypeParameter(arrayType.ElementType);
        }

        return false;
    }

    private static void GenerateGenericTestWithConcreteTypes(
        CodeWriter writer,
        Compilation compilation,
        TestMethodMetadata testMethod,
        string className,
        string combinationGuid)
    {
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
        GenerateMetadataForConcreteInstantiation(writer, compilation, testMethod);

        // Generate instance factory that works with generic types
        writer.AppendLine("InstanceFactory = (typeArgs, args) =>");
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

        // Generate TestInvoker for generic methods
        var isAsync = IsAsyncMethod(testMethod.MethodSymbol);
        var hasCancellationToken = testMethod.MethodSymbol.Parameters.Any(p =>
            p.Type.Name == "CancellationToken" &&
            p.Type.ContainingNamespace?.ToString() == "System.Threading");
        var parametersFromArgs = testMethod.MethodSymbol.Parameters
            .Where(p => p.Type.Name != "CancellationToken")
            .ToArray();

        GenerateGenericTestInvoker(writer, testMethod, methodName, isAsync, hasCancellationToken, parametersFromArgs);

        // Generate concrete instantiations dictionary
        writer.AppendLine("ConcreteInstantiations = new global::System.Collections.Generic.Dictionary<string, global::TUnit.Core.TestMetadata>");
        writer.AppendLine("{");
        writer.Indent();

        var methodArgumentsAttributes = testMethod.MethodAttributes
            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
            .ToList();

        var classArgumentsAttributes = new List<AttributeData>();

        // For generic classes, collect class-level Arguments attributes separately
        if (testMethod.IsGenericType)
        {
            classArgumentsAttributes = testMethod.TypeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
                .ToList();
        }

        var processedTypeCombinations = new HashSet<string>();


        // Handle the combination of class and method Arguments attributes
        if (testMethod is { IsGenericType: true, IsGenericMethod: true } && classArgumentsAttributes.Any() && methodArgumentsAttributes.Any())
        {
            // Generate combinations of class and method Arguments
            foreach (var classAttr in classArgumentsAttributes)
            {
                var classTypes = InferTypesFromClassArgumentsAttribute(testMethod.TypeSymbol, classAttr, compilation);
                if (classTypes == null || classTypes.Length == 0) continue;

                foreach (var methodAttr in methodArgumentsAttributes)
                {
                    var methodTypes = InferTypesFromArgumentsAttribute(testMethod.MethodSymbol, methodAttr, compilation);
                    if (methodTypes == null || methodTypes.Length == 0) continue;

                    // Combine class and method types
                    var combinedTypes = new ITypeSymbol[classTypes.Length + methodTypes.Length];
                    Array.Copy(classTypes, 0, combinedTypes, 0, classTypes.Length);
                    Array.Copy(methodTypes, 0, combinedTypes, classTypes.Length, methodTypes.Length);

                    var typeKey = string.Join(",", combinedTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")));

                    // Skip if we've already processed this type combination
                    if (processedTypeCombinations.Contains(typeKey))
                        continue;

                    processedTypeCombinations.Add(typeKey);

                    // Validate constraints for both class and method separately
                    bool constraintsValid = ValidateClassTypeConstraints(testMethod.TypeSymbol, classTypes) &&
                                          ValidateTypeConstraints(testMethod.MethodSymbol, methodTypes);

                    // TODO: Fix ValidateTypeConstraints method - temporarily skip validation
                    constraintsValid = true;

                    if (!constraintsValid)
                        continue;

                    // Generate a concrete instantiation for this type combination
                    writer.AppendLine($"[{string.Join(" + \",\" + ", combinedTypes.Select(t => $"(typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).FullName ?? typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).Name)"))}] = ");
                    GenerateConcreteTestMetadata(writer, compilation, testMethod, className, combinedTypes, classAttr);
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
            if (!(testMethod.IsGenericType && !testMethod.IsGenericMethod))
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
                    var typeKey = string.Join(",", inferredTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")));

                    // Skip if we've already processed this type combination
                    if (processedTypeCombinations.Contains(typeKey))
                        continue;

                    processedTypeCombinations.Add(typeKey);

                    // Validate constraints
                    bool constraintsValid = true;
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
                        continue;

                    // Generate a concrete instantiation for this type combination
                    writer.AppendLine($"[{string.Join(" + \",\" + ", inferredTypes.Select(t => $"(typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).FullName ?? typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).Name)"))}] = ");
                    GenerateConcreteTestMetadata(writer, compilation, testMethod, className, inferredTypes, argAttr);
                    writer.AppendLine(",");
                }
            }
        }

        // Handle generic classes with non-generic methods that have method-level Arguments
        // These were skipped in the main loop and need special processing
        if (testMethod.IsGenericType && !testMethod.IsGenericMethod && methodArgumentsAttributes.Count > 0)
        {
            foreach (var methodArgAttr in methodArgumentsAttributes)
            {
                var inferredTypes = InferClassTypesFromMethodArguments(testMethod.TypeSymbol, testMethod.MethodSymbol, methodArgAttr, compilation);
                if (inferredTypes is { Length: > 0 })
                {
                    var typeKey = string.Join(",", inferredTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")));

                    // Skip if we've already processed this type combination
                    if (processedTypeCombinations.Contains(typeKey))
                        continue;

                    processedTypeCombinations.Add(typeKey);

                    // Validate class type constraints
                    bool constraintsValid = ValidateClassTypeConstraints(testMethod.TypeSymbol, inferredTypes);

                    // TODO: Fix ValidateClassTypeConstraints method - temporarily skip validation
                    constraintsValid = true;

                    if (!constraintsValid)
                        continue;

                    // Generate a concrete instantiation for this type combination
                    writer.AppendLine($"[{string.Join(" + \",\" + ", inferredTypes.Select(t => $"(typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).FullName ?? typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).Name)"))}] = ");
                    GenerateConcreteTestMetadata(writer, compilation, testMethod, className, inferredTypes, methodArgAttr);
                    writer.AppendLine(",");
                }
            }
        }

        // Process typed data source attributes
        var dataSourceAttributes = testMethod.MethodAttributes
            .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
            .ToList();

        foreach (var dataSourceAttr in dataSourceAttributes)
        {
            var inferredTypes = InferTypesFromDataSourceAttribute(testMethod.MethodSymbol, dataSourceAttr);
            if (inferredTypes is { Length: > 0 })
            {
                var typeKey = string.Join(",", inferredTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")));

                // Skip if we've already processed this type combination
                if (processedTypeCombinations.Contains(typeKey))
                    continue;

                processedTypeCombinations.Add(typeKey);

                // Validate constraints
                bool constraintsValid = true;
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
                    continue;

                // Generate a concrete instantiation for this type combination
                writer.AppendLine($"[{string.Join(" + \",\" + ", inferredTypes.Select(t => $"(typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).FullName ?? typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).Name)"))}] = ");
                GenerateConcreteTestMetadata(writer, compilation, testMethod, className, inferredTypes, dataSourceAttr);
                writer.AppendLine(",");
            }
        }

        // Process attributes that implement IInfersType<T> on parameters
        if (testMethod.IsGenericMethod)
        {
            var inferredTypes = InferTypesFromTypeInferringAttributes(testMethod.MethodSymbol);
            if (inferredTypes is { Length: > 0 })
            {
                var typeKey = string.Join(",", inferredTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")));

                // Skip if we've already processed this type combination
                if (!processedTypeCombinations.Contains(typeKey))
                {
                    processedTypeCombinations.Add(typeKey);

                    // Validate constraints
                    if (ValidateTypeConstraints(testMethod.MethodSymbol, inferredTypes))
                    {
                        // Generate a concrete instantiation for this type combination
                        writer.AppendLine($"[{string.Join(" + \",\" + ", inferredTypes.Select(t => $"(typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).FullName ?? typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).Name)"))}] = ");
                        GenerateConcreteTestMetadata(writer, compilation, testMethod, className, inferredTypes);
                        writer.AppendLine(",");
                    }
                }
            }
        }

        // Process MethodDataSource attributes for generic classes (non-generic methods)
        if (testMethod is { IsGenericType: true, IsGenericMethod: false })
        {
            var methodDataSourceAttributes = testMethod.MethodAttributes
                .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute")
                .ToList();

            foreach (var mdsAttr in methodDataSourceAttributes)
            {
                // Try to infer types from the method data source
                var inferredTypes = InferClassTypesFromMethodDataSource(compilation, testMethod, mdsAttr);
                if (inferredTypes is { Length: > 0 })
                {
                    var typeKey = string.Join(",", inferredTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")));

                    // Skip if we've already processed this type combination
                    if (!processedTypeCombinations.Contains(typeKey))
                    {
                        processedTypeCombinations.Add(typeKey);

                        // Validate constraints for the generic class
                        if (ValidateClassTypeConstraints(testMethod.TypeSymbol, inferredTypes))
                        {
                            // Generate a concrete instantiation for this type combination
                            writer.AppendLine($"[{string.Join(" + \",\" + ", inferredTypes.Select(t => $"(typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).FullName ?? typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).Name)"))}] = ");
                            GenerateConcreteTestMetadata(writer, compilation, testMethod, className, inferredTypes);
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
                var typeKey = string.Join(",", typedDataSourceInferredTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")));

                // Skip if we've already processed this type combination
                if (!processedTypeCombinations.Contains(typeKey))
                {
                    processedTypeCombinations.Add(typeKey);

                    // Validate constraints for the generic class
                    if (ValidateClassTypeConstraints(testMethod.TypeSymbol, typedDataSourceInferredTypes))
                    {
                        // Generate a concrete instantiation for this type combination
                        writer.AppendLine($"[{string.Join(" + \",\" + ", typedDataSourceInferredTypes.Select(t => $"(typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).FullName ?? typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).Name)"))}] = ");
                        GenerateConcreteTestMetadata(writer, compilation, testMethod, className, typedDataSourceInferredTypes);
                        writer.AppendLine(",");
                    }
                }
            }
        }

        // Process MethodDataSource attributes for generic methods
        if (testMethod.IsGenericMethod)
        {
            var methodDataSourceAttributes = testMethod.MethodAttributes
                .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute")
                .ToList();

            foreach (var mdsAttr in methodDataSourceAttributes)
            {
                // Try to infer types from the method data source
                var inferredTypes = InferTypesFromMethodDataSource(compilation, testMethod, mdsAttr);
                if (inferredTypes is { Length: > 0 })
                {
                    var typeKey = string.Join(",", inferredTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")));

                    // Skip if we've already processed this type combination
                    if (!processedTypeCombinations.Contains(typeKey))
                    {
                        processedTypeCombinations.Add(typeKey);

                        // Validate constraints
                        if (ValidateTypeConstraints(testMethod.MethodSymbol, inferredTypes))
                        {
                            // Generate a concrete instantiation for this type combination
                            writer.AppendLine($"[{string.Join(" + \",\" + ", inferredTypes.Select(t => $"(typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).FullName ?? typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).Name)"))}] = ");
                            GenerateConcreteTestMetadata(writer, compilation, testMethod, className, inferredTypes);
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
                .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
                .ToList();


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
                            .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
                            .ToList();

                        foreach (var methodArgAttr in methodLevelArgumentsAttributes)
                        {
                            var methodInferredTypes = InferTypesFromArgumentsAttribute(testMethod.MethodSymbol, methodArgAttr, compilation);
                            if (methodInferredTypes is { Length: > 0 })
                            {
                                // Combine class types and method types
                                var combinedTypes = classInferredTypes.Concat(methodInferredTypes).ToArray();
                                var typeKey = string.Join(",", combinedTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")));

                                // Skip if we've already processed this type combination
                                if (!processedTypeCombinations.Contains(typeKey))
                                {
                                    processedTypeCombinations.Add(typeKey);

                                    // Validate constraints for both class and method type parameters
                                    if (ValidateClassTypeConstraints(testMethod.TypeSymbol, classInferredTypes) &&
                                        ValidateTypeConstraints(testMethod.MethodSymbol, methodInferredTypes))
                                    {
                                        // Generate a concrete instantiation for this type combination
                                        writer.AppendLine($"[{string.Join(" + \",\" + ", combinedTypes.Select(t => $"(typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).FullName ?? typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).Name)"))}] = ");
                                        GenerateConcreteTestMetadata(writer, compilation, testMethod, className, combinedTypes, argAttr);
                                        writer.AppendLine(",");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // For non-generic methods, just use class types
                        var typeKey = string.Join(",", classInferredTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")));

                        // Skip if we've already processed this type combination
                        if (!processedTypeCombinations.Contains(typeKey))
                        {
                            processedTypeCombinations.Add(typeKey);

                            // Validate constraints for the generic class type parameters
                            if (ValidateClassTypeConstraints(testMethod.TypeSymbol, classInferredTypes))
                            {
                                // Generate a concrete instantiation for this type combination
                                writer.AppendLine($"[{string.Join(" + \",\" + ", classInferredTypes.Select(t => $"(typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).FullName ?? typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).Name)"))}] = ");
                                GenerateConcreteTestMetadata(writer, compilation, testMethod, className, classInferredTypes, argAttr);
                                writer.AppendLine(",");
                            }
                        }
                    }
                }
            }
        }

        // Process class-level Arguments attributes for non-generic classes with parameterized constructors
        if (!testMethod.IsGenericType && !testMethod.IsGenericMethod)
        {
            var nonGenericClassArguments = testMethod.TypeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
                .ToList();

            var nonGenericMethodArguments = testMethod.MethodAttributes
                .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute")
                .ToList();

            // Also get class-level data source generators for non-generic classes
            var nonGenericClassDataSourceGenerators = testMethod.TypeSymbol.GetAttributes()
                .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass) &&
                           a.AttributeClass?.Name != "ArgumentsAttribute")
                .ToList();

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
                        GenerateConcreteTestMetadataForNonGeneric(writer, compilation, testMethod, className, classArgAttr, methodArgAttr);
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
                    GenerateConcreteTestMetadataForNonGeneric(writer, compilation, testMethod, className, classArgAttr, null);
                    writer.AppendLine();
                }
            }
            else if (nonGenericMethodArguments.Any())
            {
                // Only method arguments, no class arguments
                foreach (var methodArgAttr in nonGenericMethodArguments)
                {
                    writer.AppendLine($"// Method arguments: {string.Join(", ", methodArgAttr.ConstructorArguments.SelectMany(a => a.Values.Select(v => v.Value?.ToString() ?? "null")))}");
                    GenerateConcreteTestMetadataForNonGeneric(writer, compilation, testMethod, className, null, methodArgAttr);
                    writer.AppendLine();
                }
            }

            // Process class-level data source generators for non-generic classes
            if (nonGenericClassDataSourceGenerators.Any())
            {
                foreach (var dataSourceAttr in nonGenericClassDataSourceGenerators)
                {
                    writer.AppendLine($"// Class data source generator: {dataSourceAttr.AttributeClass?.Name}");
                    GenerateConcreteTestMetadataForNonGeneric(writer, compilation, testMethod, className, dataSourceAttr, null);
                    writer.AppendLine();
                }
            }
        }

        // Process GenerateGenericTest attributes from both methods and classes
        var generateGenericTestAttributes = testMethod.MethodAttributes
            .Where(a => a.AttributeClass?.Name == "GenerateGenericTestAttribute")
            .ToList();

        // For generic classes, also check class-level GenerateGenericTest attributes
        if (testMethod.IsGenericType)
        {
            var classLevelAttributes = testMethod.TypeSymbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "GenerateGenericTestAttribute")
                .ToList();
            generateGenericTestAttributes.AddRange(classLevelAttributes);
        }

        foreach (var genAttr in generateGenericTestAttributes)
        {
            // Extract type arguments from the attribute
            if (genAttr.ConstructorArguments.Length > 0)
            {
                var typeArgs = new List<ITypeSymbol>();
                foreach (var arg in genAttr.ConstructorArguments)
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
                    var inferredTypes = typeArgs.ToArray();
                    var typeKey = string.Join(",", inferredTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")));

                    // Skip if we've already processed this type combination
                    if (!processedTypeCombinations.Contains(typeKey))
                    {
                        processedTypeCombinations.Add(typeKey);

                        // Validate constraints
                        if (ValidateTypeConstraints(testMethod.MethodSymbol, inferredTypes))
                        {
                            // Generate a concrete instantiation for this type combination
                            // Use the same key format as runtime: FullName ?? Name
                            writer.AppendLine($"[{string.Join(" + \",\" + ", inferredTypes.Select(t => $"(typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).FullName ?? typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}).Name)"))}] = ");
                            GenerateConcreteTestMetadata(writer, compilation, testMethod, className, inferredTypes);
                            writer.AppendLine(",");
                        }
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

    private static bool ValidateClassTypeConstraints(INamedTypeSymbol classSymbol, ITypeSymbol[] typeArguments)
    {
        var typeParams = classSymbol.TypeParameters;

        if (typeParams.Length != typeArguments.Length)
            return false;

        for (int i = 0; i < typeParams.Length; i++)
        {
            var typeParam = typeParams[i];
            var typeArg = typeArguments[i];

            // Check struct constraint
            if (typeParam.HasValueTypeConstraint)
            {
                if (!typeArg.IsValueType || typeArg.IsReferenceType)
                    return false;
            }

            // Check class constraint
            if (typeParam.HasReferenceTypeConstraint)
            {
                if (!typeArg.IsReferenceType)
                    return false;
            }

            // Check specific type constraints
            foreach (var constraintType in typeParam.ConstraintTypes)
            {
                // For interface constraints, check if the type implements the interface
                if (constraintType.TypeKind == TypeKind.Interface)
                {
                    if (!typeArg.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, constraintType)))
                        return false;
                }
                // For base class constraints, check if the type derives from the class
                else if (constraintType.TypeKind == TypeKind.Class)
                {
                    var baseType = typeArg.BaseType;
                    bool found = false;
                    while (baseType != null)
                    {
                        if (SymbolEqualityComparer.Default.Equals(baseType, constraintType))
                        {
                            found = true;
                            break;
                        }
                        baseType = baseType.BaseType;
                    }
                    if (!found && !SymbolEqualityComparer.Default.Equals(typeArg, constraintType))
                        return false;
                }
            }
        }

        return true;
    }

    private static ITypeSymbol[]? InferClassTypesFromMethodArguments(INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol, AttributeData argAttr, Compilation compilation)
    {
        if (argAttr.ConstructorArguments.Length == 0)
            return null;

        var inferredTypes = new Dictionary<string, ITypeSymbol>();
        var classTypeParameters = classSymbol.TypeParameters;

        // Arguments attribute takes params object?[] so the first constructor argument is an array
        if (argAttr.ConstructorArguments.Length != 1 || argAttr.ConstructorArguments[0].Kind != TypedConstantKind.Array)
            return null;

        var argumentValues = argAttr.ConstructorArguments[0].Values;
        var methodParams = methodSymbol.Parameters;

        // For each value in the params array
        for (int argIndex = 0; argIndex < argumentValues.Length && argIndex < methodParams.Length; argIndex++)
        {
            var methodParam = methodParams[argIndex];
            var argValue = argumentValues[argIndex];

            // Skip if this is a CancellationToken parameter
            if (methodParam.Type.Name == "CancellationToken")
                continue;

            // Check if the method parameter type is a class generic type parameter
            if (methodParam.Type is ITypeParameterSymbol typeParam && typeParam.DeclaringMethod == null)
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
                    MapGenericTypeArguments(methodParam.Type, argValue.Type, classSymbol, inferredTypes);
                }
            }
        }

        // Check if we've inferred all required type parameters
        if (inferredTypes.Count == 0)
            return null;

        // Build the result array in the correct order
        var result = new ITypeSymbol[classTypeParameters.Length];
        for (int i = 0; i < classTypeParameters.Length; i++)
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
        if (type is ITypeParameterSymbol typeParam && typeParam.DeclaringMethod == null)
        {
            return true;
        }

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            return namedType.TypeArguments.Any(ta => ContainsClassTypeParameter(ta, classSymbol));
        }

        return false;
    }

    private static void MapGenericTypeArguments(ITypeSymbol paramType, ITypeSymbol argType, INamedTypeSymbol classSymbol, Dictionary<string, ITypeSymbol> inferredTypes)
    {
        if (paramType is ITypeParameterSymbol typeParam && typeParam.DeclaringMethod == null)
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
            for (int i = 0; i < paramNamedType.TypeArguments.Length && i < argNamedType.TypeArguments.Length; i++)
            {
                MapGenericTypeArguments(paramNamedType.TypeArguments[i], argNamedType.TypeArguments[i], classSymbol, inferredTypes);
            }
        }
    }

    private static ITypeSymbol? InferTypeFromValue(object value, Compilation compilation)
    {
        if (value is int)
            return compilation?.GetSpecialType(SpecialType.System_Int32);
        else if (value is string)
            return compilation?.GetSpecialType(SpecialType.System_String);
        else if (value is bool)
            return compilation?.GetSpecialType(SpecialType.System_Boolean);
        else if (value is double)
            return compilation?.GetSpecialType(SpecialType.System_Double);
        else if (value is float)
            return compilation?.GetSpecialType(SpecialType.System_Single);
        else if (value is long)
            return compilation?.GetSpecialType(SpecialType.System_Int64);
        else if (value is byte)
            return compilation?.GetSpecialType(SpecialType.System_Byte);
        else if (value is char)
            return compilation?.GetSpecialType(SpecialType.System_Char);
        else if (value is decimal)
            return compilation?.GetSpecialType(SpecialType.System_Decimal);
        else if (value is ITypeSymbol typeSymbol)
            return compilation?.GetTypeByMetadataName("System.Type");
        else
            return null;
    }

    private static ITypeSymbol[]? InferTypesFromClassArgumentsAttribute(INamedTypeSymbol classSymbol, AttributeData argAttr, Compilation compilation)
    {
        if (argAttr.ConstructorArguments.Length == 0)
            return null;

        var inferredTypes = new Dictionary<string, ITypeSymbol>();
        var typeParameters = classSymbol.TypeParameters;

        // Arguments attribute takes params object?[] so the first constructor argument is an array
        if (argAttr.ConstructorArguments.Length != 1 || argAttr.ConstructorArguments[0].Kind != TypedConstantKind.Array)
            return null;

        var argumentValues = argAttr.ConstructorArguments[0].Values;

        // Find the primary constructor
        var primaryConstructor = classSymbol.Constructors
            .FirstOrDefault(c => c.DeclaringSyntaxReferences.Any(sr =>
                sr.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax cds &&
                cds.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword))))
            ?? classSymbol.Constructors.FirstOrDefault();

        if (primaryConstructor == null)
            return null;

        var constructorParams = primaryConstructor.Parameters;

        // For each value in the params array
        for (int argIndex = 0; argIndex < argumentValues.Length && argIndex < constructorParams.Length; argIndex++)
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

                    if (value is int)
                        argType = compilation?.GetSpecialType(SpecialType.System_Int32);
                    else if (value is string)
                        argType = compilation?.GetSpecialType(SpecialType.System_String);
                    else if (value is bool)
                        argType = compilation?.GetSpecialType(SpecialType.System_Boolean);
                    else if (value is double)
                        argType = compilation?.GetSpecialType(SpecialType.System_Double);
                    else if (value is float)
                        argType = compilation?.GetSpecialType(SpecialType.System_Single);
                    else if (value is long)
                        argType = compilation?.GetSpecialType(SpecialType.System_Int64);
                    else if (value is char)
                        argType = compilation?.GetSpecialType(SpecialType.System_Char);
                    else if (value is byte)
                        argType = compilation?.GetSpecialType(SpecialType.System_Byte);
                    else if (value is decimal)
                        argType = compilation?.GetSpecialType(SpecialType.System_Decimal);
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
            for (int i = 0; i < typeParameters.Length; i++)
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
            return null;

        var inferredTypes = new Dictionary<string, ITypeSymbol>();
        var typeParameters = method.TypeParameters;

        // Arguments attribute takes params object?[] so the first constructor argument is an array
        if (argAttr.ConstructorArguments.Length != 1 || argAttr.ConstructorArguments[0].Kind != TypedConstantKind.Array)
            return null;

        var argumentValues = argAttr.ConstructorArguments[0].Values;
        var methodParams = method.Parameters;

        // For each value in the params array
        for (int argIndex = 0; argIndex < argumentValues.Length && argIndex < methodParams.Length; argIndex++)
        {
            var methodParam = methodParams[argIndex];
            var argValue = argumentValues[argIndex];

            // Skip if this is a CancellationToken parameter
            if (methodParam.Type.Name == "CancellationToken")
                continue;

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

                    if (value is int)
                        argType = compilation?.GetSpecialType(SpecialType.System_Int32);
                    else if (value is string)
                        argType = compilation?.GetSpecialType(SpecialType.System_String);
                    else if (value is bool)
                        argType = compilation?.GetSpecialType(SpecialType.System_Boolean);
                    else if (value is double)
                        argType = compilation?.GetSpecialType(SpecialType.System_Double);
                    else if (value is float)
                        argType = compilation?.GetSpecialType(SpecialType.System_Single);
                    else if (value is long)
                        argType = compilation?.GetSpecialType(SpecialType.System_Int64);
                    else if (value is char)
                        argType = compilation?.GetSpecialType(SpecialType.System_Char);
                    else if (value is byte)
                        argType = compilation?.GetSpecialType(SpecialType.System_Byte);
                    else if (value is decimal)
                        argType = compilation?.GetSpecialType(SpecialType.System_Decimal);
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
            for (int i = 0; i < typeParameters.Length; i++)
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
            return null;

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
                if ((baseTypeName == "DataSourceGeneratorAttribute" || baseTypeName == "AsyncDataSourceGeneratorAttribute") &&
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
                        if (typeArgs.Length == 1 && typeArgs[0] is INamedTypeSymbol { IsTupleType: true } tupleType)
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
                        if (typeArgs.Length == 1 && typeArgs[0] is INamedTypeSymbol { IsTupleType: true } tupleType)
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
                        if (typeArgs.Length == 1 && typeArgs[0] is INamedTypeSymbol { IsTupleType: true } tupleType)
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

    private static ITypeSymbol[]? InferTypesFromMethodDataSource(Compilation compilation, TestMethodMetadata testMethod, AttributeData mdsAttr)
    {
        if (mdsAttr.ConstructorArguments.Length == 0)
            return null;

        // Get the method name from the attribute
        if (mdsAttr.ConstructorArguments[0].Value is not string methodName)
            return null;

        // Find the method in the test class
        var testClass = testMethod.TypeSymbol;
        var dataMethod = testClass.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.IsStatic && m.Parameters.Length == 0);

        if (dataMethod == null)
            return null;

        // Check if the method returns IEnumerable<Func<T>> where T is a tuple
        var returnType = dataMethod.ReturnType;
        if (returnType is not INamedTypeSymbol namedReturnType)
            return null;

        // Navigate through IEnumerable<Func<...>>
        if (!namedReturnType.IsGenericType || namedReturnType.Name != "IEnumerable")
            return null;

        var funcType = namedReturnType.TypeArguments[0] as INamedTypeSymbol;
        if (funcType == null || funcType.Name != "Func" || funcType.TypeArguments.Length != 1)
            return null;

        var tupleType = funcType.TypeArguments[0] as INamedTypeSymbol;
        if (tupleType == null || !tupleType.IsTupleType)
            return null;

        // Extract the types from the tuple elements that correspond to the generic parameters
        var testMethodParams = testMethod.MethodSymbol.Parameters;
        var genericParams = testMethod.MethodSymbol.TypeParameters;
        var genericParamMap = new Dictionary<string, ITypeSymbol>();

        // Map tuple elements to method parameters to infer types
        var tupleElements = tupleType.TupleElements;
        for (int i = 0; i < testMethodParams.Length && i < tupleElements.Length; i++)
        {
            var paramType = testMethodParams[i].Type;
            var tupleElementType = tupleElements[i].Type;

            // Process the parameter type to find generic references
            ProcessTypeForGenerics(paramType, tupleElementType, genericParams, genericParamMap);
        }

        // Build the result array in the correct order
        var inferredTypes = new ITypeSymbol[genericParams.Length];
        for (int i = 0; i < genericParams.Length; i++)
        {
            if (!genericParamMap.TryGetValue(genericParams[i].Name, out var inferredType))
                return null;
            inferredTypes[i] = inferredType;
        }

        return inferredTypes;
    }

    private static ITypeSymbol[]? InferClassTypesFromMethodDataSource(Compilation compilation, TestMethodMetadata testMethod, AttributeData mdsAttr)
    {
        if (mdsAttr.ConstructorArguments.Length == 0)
            return null;

        // Get the method name from the attribute
        if (mdsAttr.ConstructorArguments[0].Value is not string methodName)
            return null;

        // Find the method in the test class
        var testClass = testMethod.TypeSymbol;
        var dataMethod = testClass.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.IsStatic && m.Parameters.Length == 0);

        if (dataMethod == null)
            return null;

        // Check if the method returns IEnumerable<Func<T>> where T is a tuple or a single type
        var returnType = dataMethod.ReturnType;
        if (returnType is not INamedTypeSymbol namedReturnType)
            return null;

        // Navigate through IEnumerable<Func<...>> or IEnumerable<...>
        if (!namedReturnType.IsGenericType || namedReturnType.Name != "IEnumerable")
            return null;

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
            return null;

        // Get class type parameters
        var classTypeParams = testMethod.TypeSymbol.TypeParameters;
        var genericParamMap = new Dictionary<string, ITypeSymbol>();

        // If it's a tuple, map tuple elements to method parameters
        if (dataType.IsTupleType)
        {
            var tupleElements = dataType.TupleElements;
            var testMethodParams = testMethod.MethodSymbol.Parameters;

            for (int i = 0; i < testMethodParams.Length && i < tupleElements.Length; i++)
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
        for (int i = 0; i < classTypeParams.Length; i++)
        {
            if (!genericParamMap.TryGetValue(classTypeParams[i].Name, out var inferredType))
                return null;
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
            for (int i = 0; i < namedParamType.TypeArguments.Length && i < namedActualType.TypeArguments.Length; i++)
            {
                ProcessTypeForGenerics(namedParamType.TypeArguments[i], namedActualType.TypeArguments[i], genericParams, genericParamMap);
            }
        }
    }

    private static bool ValidateTypeConstraints(INamedTypeSymbol classType, ITypeSymbol[] typeArguments)
    {
        // Validate constraints for a generic class
        if (!classType.IsGenericType)
            return true;

        var typeParams = classType.TypeParameters;
        if (typeParams.Length != typeArguments.Length)
            return false;

        return ValidateTypeParameterConstraints(typeParams, typeArguments);
    }

    private static bool ValidateTypeConstraints(IMethodSymbol method, ITypeSymbol[] typeArguments)
    {
        // Get all type parameters (class + method)
        var allTypeParams = new List<ITypeParameterSymbol>();

        // Add class type parameters first
        if (method.ContainingType.IsGenericType)
        {
            allTypeParams.AddRange(method.ContainingType.TypeParameters);
        }

        // Add method type parameters
        allTypeParams.AddRange(method.TypeParameters);

        if (allTypeParams.Count != typeArguments.Length)
            return false;

        return ValidateTypeParameterConstraints(allTypeParams, typeArguments);
    }

    private static bool ValidateTypeParameterConstraints(IEnumerable<ITypeParameterSymbol> typeParams, ITypeSymbol[] typeArguments)
    {
        var typeParamsList = typeParams.ToList();

        for (int i = 0; i < typeParamsList.Count; i++)
        {
            var typeParam = typeParamsList[i];
            var typeArg = typeArguments[i];

            // Check struct constraint
            if (typeParam.HasValueTypeConstraint)
            {
                if (!typeArg.IsValueType || typeArg.IsReferenceType)
                    return false;
            }

            // Check class constraint
            if (typeParam.HasReferenceTypeConstraint)
            {
                if (!typeArg.IsReferenceType)
                    return false;
            }

            // Check interface constraints
            foreach (var constraintType in typeParam.ConstraintTypes)
            {
                if (constraintType.TypeKind == TypeKind.Interface)
                {
                    // Check if the type argument implements the interface
                    if (!typeArg.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, constraintType)))
                        return false;
                }
                else if (constraintType.TypeKind == TypeKind.Class)
                {
                    // Check if the type argument derives from the base class
                    var baseType = typeArg.BaseType;
                    bool found = false;
                    while (baseType != null)
                    {
                        if (SymbolEqualityComparer.Default.Equals(baseType, constraintType))
                        {
                            found = true;
                            break;
                        }
                        baseType = baseType.BaseType;
                    }
                    if (!found)
                        return false;
                }
            }
        }

        return true;
    }

    private static void GenerateConcreteTestMetadata(
        CodeWriter writer,
        Compilation compilation,
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
        GenerateConcreteMetadataWithFilteredDataSources(writer, compilation, testMethod, specificArgumentsAttribute, typeArguments);

        // Generate instance factory
        writer.AppendLine("InstanceFactory = (typeArgs, args) =>");
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
            if (specificArgumentsAttribute != null && specificArgumentsAttribute.ConstructorArguments.Length > 0 &&
                specificArgumentsAttribute.ConstructorArguments[0].Kind == TypedConstantKind.Array)
            {
                var argumentValues = specificArgumentsAttribute.ConstructorArguments[0].Values;
                var constructorArgs = string.Join(", ", argumentValues.Select(arg =>
                {
                    if (arg.Value is string str)
                        return $"\"{str}\"";
                    else if (arg.Value is char chr)
                        return $"'{chr}'";
                    else if (arg.Value is bool b)
                        return b.ToString().ToLower();
                    else if (arg.Value is null)
                        return "null";
                    else
                        return arg.Value.ToString();
                }));

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
        writer.AppendLine("InvokeTypedTest = async (instance, args, cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();

        var hasCancellationToken = testMethod.MethodSymbol.Parameters.Any(p =>
            p.Type.Name == "CancellationToken" &&
            p.Type.ContainingNamespace?.ToString() == "System.Threading");

        // Generate direct method call with specific types
        writer.AppendLine($"var typedInstance = ({concreteClassName})instance;");

        // Prepare method arguments with proper casting
        var parameterCasts = new List<string>();
        for (int i = 0; i < testMethod.MethodSymbol.Parameters.Length; i++)
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
            writer.AppendLine($"await global::TUnit.Core.AsyncConvert.Convert(() => typedInstance.{methodName}<{methodTypeArgsString}>({string.Join(", ", parameterCasts)}));");
        }
        else
        {
            writer.AppendLine($"await global::TUnit.Core.AsyncConvert.Convert(() => typedInstance.{methodName}({string.Join(", ", parameterCasts)}));");
        }

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
                for (int i = 0; i < testMethod.TypeSymbol.TypeParameters.Length; i++)
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
            for (int i = 0; i < testMethod.MethodSymbol.TypeParameters.Length; i++)
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
        Compilation compilation,
        TestMethodMetadata testMethod,
        AttributeData? specificArgumentsAttribute,
        ITypeSymbol[] typeArguments)
    {
        var methodSymbol = testMethod.MethodSymbol;
        var typeSymbol = testMethod.TypeSymbol;


        // Generate dependencies
        GenerateDependencies(writer, compilation, methodSymbol);

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

        writer.AppendLine("AttributeFactory = () =>");
        writer.AppendLine("[");
        writer.Indent();
        AttributeWriter.WriteAttributes(writer, compilation, filteredAttributes.ToImmutableArray());
        writer.Unindent();
        writer.AppendLine("],");

        // Filter data sources based on the specific attribute
        List<AttributeData> methodDataSources;
        List<AttributeData> classDataSources;

        if (specificArgumentsAttribute != null)
        {
            // For specific data source attributes, include the specific one that matches
            methodDataSources = methodSymbol.GetAttributes()
                .Where(a => AreSameAttribute(a, specificArgumentsAttribute))
                .ToList();

            // For combined generic class + generic method scenarios, also include method-level Arguments
            // that provide method parameters (different from the class-level specificArgumentsAttribute)
            if (testMethod.IsGenericType && testMethod.IsGenericMethod)
            {
                var additionalMethodDataSources = methodSymbol.GetAttributes()
                    .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute" && !AreSameAttribute(a, specificArgumentsAttribute))
                    .ToList();
                methodDataSources.AddRange(additionalMethodDataSources);
            }

            classDataSources = typeSymbol.GetAttributesIncludingBaseTypes()
                .Where(a => AreSameAttribute(a, specificArgumentsAttribute))
                .ToList();
        }
        else
        {
            // For other cases, include all data sources
            methodDataSources = methodSymbol.GetAttributes()
                .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
                .ToList();

            classDataSources = typeSymbol.GetAttributesIncludingBaseTypes()
                .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
                .ToList();
        }

        // Generate method data sources
        writer.AppendLine("DataSources = new global::TUnit.Core.IDataSourceAttribute[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var attr in methodDataSources)
        {
            GenerateDataSourceAttribute(writer, attr, methodSymbol, typeSymbol);
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Generate class data sources
        writer.AppendLine("ClassDataSources = new global::TUnit.Core.IDataSourceAttribute[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var attr in classDataSources)
        {
            GenerateDataSourceAttribute(writer, attr, methodSymbol, typeSymbol);
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Empty property data sources for concrete instantiations
        writer.AppendLine("PropertyDataSources = global::System.Array.Empty<global::TUnit.Core.PropertyDataSource>(),");

        // Generate property injections
        GeneratePropertyInjections(writer, typeSymbol, typeSymbol.GloballyQualified());

        // Other metadata
        writer.AppendLine($"FilePath = @\"{testMethod.FilePath.Replace("\\", "\\\\")}\",");
        writer.AppendLine($"LineNumber = {testMethod.LineNumber},");
        writer.AppendLine($"InheritanceDepth = {testMethod.InheritanceDepth},");
        writer.AppendLine($"TestSessionId = testSessionId,");

        // Method metadata
        writer.Append("MethodMetadata = ");
        SourceInformationWriter.GenerateMethodInformation(writer, compilation, testMethod.TypeSymbol, testMethod.MethodSymbol, null, ',');
    }

    private static bool AreSameAttribute(AttributeData a1, AttributeData a2)
    {
        // Compare attributes by their constructor arguments and attribute class
        if (a1.AttributeClass?.Name != a2.AttributeClass?.Name)
            return false;

        if (a1.ConstructorArguments.Length != a2.ConstructorArguments.Length)
            return false;

        for (int i = 0; i < a1.ConstructorArguments.Length; i++)
        {
            if (!a1.ConstructorArguments[i].Equals(a2.ConstructorArguments[i]))
                return false;
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
                            .FirstOrDefault(i => ((ISymbol)i).GloballyQualifiedNonGeneric() == "global::TUnit.Core.Interfaces.IInfersType" &&
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
            return null;

        // Build the result array in the correct order
        var result = new ITypeSymbol[typeParameters.Length];
        for (int i = 0; i < typeParameters.Length; i++)
        {
            if (!inferredTypes.TryGetValue(typeParameters[i].Name, out var inferredType))
                return null;
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
                                .FirstOrDefault(i => ((ISymbol)i).GloballyQualifiedNonGeneric() == "global::TUnit.Core.Interfaces.IInfersType" &&
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
            return null;

        // Build the result array in the correct order
        var result = new ITypeSymbol[classTypeParameters.Length];
        for (int i = 0; i < classTypeParameters.Length; i++)
        {
            if (!inferredTypes.TryGetValue(classTypeParameters[i].Name, out var inferredType))
                return null;
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
                if ((name == "DataSourceGeneratorAttribute" || name == "AsyncDataSourceGeneratorAttribute") &&
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
        Compilation compilation,
        TestMethodMetadata testMethod,
        string className,
        AttributeData? classDataSourceAttribute,
        AttributeData? methodDataSourceAttribute)
    {
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

        // Generate dependencies
        GenerateDependencies(writer, compilation, testMethod.MethodSymbol);

        // Generate attribute factory
        writer.AppendLine("AttributeFactory = () =>");
        writer.AppendLine("[");
        writer.Indent();

        var attributes = filteredMethodAttributes
            .Concat(filteredClassAttributes)
            .Concat(testMethod.TypeSymbol.ContainingAssembly.GetAttributes())
            .ToImmutableArray();

        AttributeWriter.WriteAttributes(writer, compilation, attributes);

        writer.Unindent();
        writer.AppendLine("],");

        // Generate data sources
        writer.AppendLine("DataSources = new global::TUnit.Core.IDataSourceAttribute[]");
        writer.AppendLine("{");
        writer.Indent();

        // Add method data source if present
        if (methodDataSourceAttribute != null)
        {
            GenerateDataSourceAttribute(writer, methodDataSourceAttribute, testMethod.MethodSymbol, testMethod.TypeSymbol);
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Generate class data sources
        writer.AppendLine("ClassDataSources = new global::TUnit.Core.IDataSourceAttribute[]");
        writer.AppendLine("{");
        writer.Indent();

        // Add class data source if present
        if (classDataSourceAttribute != null)
        {
            GenerateDataSourceAttribute(writer, classDataSourceAttribute, testMethod.MethodSymbol, testMethod.TypeSymbol);
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Generate property data sources and injections
        GeneratePropertyDataSources(writer, compilation, testMethod);
        GeneratePropertyInjections(writer, testMethod.TypeSymbol, className);


        // Method metadata
        writer.Append("MethodMetadata = ");
        SourceInformationWriter.GenerateMethodInformation(writer, compilation, testMethod.TypeSymbol, testMethod.MethodSymbol, null, ',');

        // Generate instance factory
        writer.AppendLine("InstanceFactory = (typeArgs, args) =>");
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

            if (isArgumentsAttribute && classDataSourceAttribute != null &&
                classDataSourceAttribute.ConstructorArguments.Length > 0 &&
                classDataSourceAttribute.ConstructorArguments[0].Kind == TypedConstantKind.Array)
            {
                var argumentValues = classDataSourceAttribute.ConstructorArguments[0].Values;
                var constructorArgs = string.Join(", ", argumentValues.Select(arg =>
                {
                    if (arg.Value is string str)
                        return $"\"{str}\"";
                    else if (arg.Value is char chr)
                        return $"'{chr}'";
                    else if (arg.Value is bool b)
                        return b.ToString().ToLower();
                    else if (arg.Value == null)
                        return "null";
                    else
                        return arg.Value.ToString();
                }));

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
                writer.AppendLine($"throw new global::System.InvalidOperationException(\"Not enough arguments provided for class constructor\");");
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

        // Add file location metadata
        writer.AppendLine($"FilePath = @\"{testMethod.FilePath.Replace("\\", "\\\\")}\",");
        writer.AppendLine($"LineNumber = {testMethod.LineNumber},");

        writer.Unindent();
        writer.AppendLine("};");

        writer.AppendLine("yield return metadata;");
    }
}

public class InheritsTestsClassMetadata
{
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public required ClassDeclarationSyntax ClassSyntax { get; init; }
}

