using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using TUnit.Core.SourceGenerator.CodeGenerators;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Simplified test metadata generator that emits one TestMetadata<T> per method
/// with a DataCombinationGenerator delegate for runtime data expansion.
/// </summary>
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

    private static TestMethodMetadata? GetTestMethodMetadata(GeneratorAttributeSyntaxContext context)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.TargetNode;
        var methodSymbol = context.TargetSymbol as IMethodSymbol;

        var containingType = methodSymbol?.ContainingType;

        if (containingType == null)
        {
            return null;
        }

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
            MethodSymbol = methodSymbol ?? throw new global::System.InvalidOperationException("Symbol is not a method"),
            TypeSymbol = containingType,
            FilePath = methodSyntax.SyntaxTree.FilePath,
            LineNumber = methodSyntax.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            TestAttribute = context.Attributes.First(),
            Context = context,
            MethodSyntax = methodSyntax,
            IsGenericType = isGenericType,
            IsGenericMethod = isGenericMethod,
            MethodAttributes = methodSymbol.GetAttributes()
        };
    }

    private static void GenerateTestMethodSource(SourceProductionContext context, Compilation compilation, TestMethodMetadata? testMethod)
    {
        try
        {
            if (testMethod?.MethodSymbol == null || testMethod.TypeSymbol == null)
            {
                return;
            }

            var writer = new CodeWriter();
            GenerateFileHeader(writer);
            GenerateSimplifiedTestMetadata(writer, compilation, testMethod);

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

    private static void GenerateSimplifiedTestMetadata(CodeWriter writer, Compilation compilation, TestMethodMetadata testMethod)
    {
        var className = testMethod.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = testMethod.MethodSymbol.Name;
        var guid = Guid.NewGuid().ToString("N");
        var combinationGuid = Guid.NewGuid().ToString("N").Substring(0, 8);

        writer.AppendLine($"internal sealed class {testMethod.TypeSymbol.Name}_{methodName}_TestSource_{guid} : global::TUnit.Core.Interfaces.SourceGenerator.ITestSource");
        writer.AppendLine("{");
        writer.Indent();

        // Generate reflection-based field accessors for init-only properties with data source attributes
        GenerateReflectionFieldAccessors(writer, testMethod.TypeSymbol, className);

        writer.AppendLine("public async global::System.Threading.Tasks.ValueTask<global::System.Collections.Generic.List<global::TUnit.Core.TestMetadata>> GetTestsAsync(string testSessionId)");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine("var tests = new global::System.Collections.Generic.List<global::TUnit.Core.TestMetadata>();");
        writer.AppendLine();

        // Generate the TestMetadata<T> with DataCombinationGenerator
        GenerateTestMetadataInstance(writer, compilation, testMethod, className, combinationGuid);

        writer.AppendLine("return tests;");
        writer.Unindent();
        writer.AppendLine("}");

        // Generate the data combination method inside the class
        writer.AppendLine();
        DataCombinationGeneratorEmitter.EmitDataCombinationGenerator(writer, testMethod.MethodSymbol, testMethod.TypeSymbol, combinationGuid, testMethod);

        writer.Unindent();
        writer.AppendLine("}");

        // Generate module initializer
        GenerateModuleInitializer(writer, testMethod, guid);
    }

    private static void GenerateTestMetadataInstance(CodeWriter writer, Compilation compilation, TestMethodMetadata testMethod, string className, string combinationGuid)
    {
        var methodName = testMethod.MethodSymbol.Name;

        // For generic types, we need to use object as the type parameter since we can't resolve generics at compile time
        var metadataTypeParameter = testMethod.IsGenericType ? "object" : className;

        writer.AppendLine($"var metadata = new global::TUnit.Core.TestMetadata<{metadataTypeParameter}>");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine($"TestName = \"{methodName}\",");
        writer.AppendLine($"TestClassType = {GenerateTypeReference(testMethod.TypeSymbol, testMethod.IsGenericType)},");
        writer.AppendLine($"TestMethodName = \"{methodName}\",");

        // Add basic metadata
        GenerateBasicMetadata(writer, compilation, testMethod);

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

        // For generic types, also set CreateInstance and InvokeTest
        if (testMethod.IsGenericType || testMethod.IsGenericMethod)
        {
            GenerateGenericDelegates(writer, testMethod);
        }

        writer.Unindent();
        writer.AppendLine("};");

        // Set the DataCombinationGenerator after construction
        writer.AppendLine($"metadata.SetDataCombinationGenerator(() => GenerateCombinations_{combinationGuid}(testSessionId));");

        writer.AppendLine("tests.Add(metadata);");
    }

    private static void GenerateBasicMetadata(CodeWriter writer, Compilation compilation, TestMethodMetadata testMethod)
    {
        var methodSymbol = testMethod.MethodSymbol;

        writer.AppendLine("Categories = global::System.Array.Empty<string>(),");
        writer.AppendLine("TimeoutMs = null,");
        writer.AppendLine("RetryCount = 0,");
        writer.AppendLine("CanRunInParallel = true,");

        // Generate dependencies
        GenerateDependencies(writer, compilation, methodSymbol);

        writer.AppendLine("AttributeFactory = () =>");
        writer.AppendLine("[");
        writer.Indent();

        var attributes = methodSymbol.GetAttributes()
            .Concat(testMethod.TypeSymbol.GetAttributes())
            .Concat(testMethod.TypeSymbol.ContainingAssembly.GetAttributes())
            .ToImmutableArray();

        AttributeWriter.WriteAttributes(writer, compilation, attributes);

        writer.Unindent();
        writer.AppendLine("],");

        // Generate data sources with factory methods
        GenerateDataSources(writer, compilation, testMethod);

        // Generate property injections
        GeneratePropertyInjections(writer, testMethod.TypeSymbol, testMethod.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

        // Parameter types
        writer.AppendLine("ParameterTypes = new global::System.Type[]");
        writer.AppendLine("{");
        writer.Indent();
        foreach (var param in methodSymbol.Parameters)
        {
            var paramType = param.Type;
            if (IsGenericTypeParameter(paramType) || ContainsGenericTypeParameter(paramType))
            {
                // Use object as placeholder for generic type parameters
                writer.AppendLine("typeof(global::System.Object),");
            }
            else
            {
                writer.AppendLine($"typeof({paramType.GloballyQualified()}),");
            }
        }
        writer.Unindent();
        writer.AppendLine("},");

        // String parameter types
        writer.AppendLine("TestMethodParameterTypes = new string[]");
        writer.AppendLine("{");
        writer.Indent();
        foreach (var param in methodSymbol.Parameters)
        {
            var paramType = CodeGenerationHelpers.ContainsTypeParameter(param.Type) ? "object" : param.Type.ToDisplayString();
            writer.AppendLine($"\"{paramType}\",");
        }
        writer.Unindent();
        writer.AppendLine("},");

        // Method metadata
        writer.Append("MethodMetadata = ");
        SourceInformationWriter.GenerateMethodInformation(writer, compilation, testMethod.TypeSymbol, testMethod.MethodSymbol, null, ',');

        // Empty hooks for now
        writer.AppendLine("Hooks = new global::TUnit.Core.TestHooks");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("BeforeClass = global::System.Array.Empty<global::TUnit.Core.HookMetadata>(),");
        writer.AppendLine("AfterClass = global::System.Array.Empty<global::TUnit.Core.HookMetadata>(),");
        writer.AppendLine("BeforeTest = global::System.Array.Empty<global::TUnit.Core.HookMetadata>(),");
        writer.AppendLine("AfterTest = global::System.Array.Empty<global::TUnit.Core.HookMetadata>()");
        writer.Unindent();
        writer.AppendLine("},");
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
        var classDataSources = typeSymbol.GetAttributes()
            .Where(a => DataSourceAttributeHelper.IsDataSourceAttribute(a.AttributeClass))
            .ToList();

        // Generate method data sources
        writer.AppendLine("DataSources = new global::TUnit.Core.Interfaces.IDataSourceAttribute[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var attr in methodDataSources)
        {
            GenerateDataSourceAttribute(writer, attr, methodSymbol, typeSymbol);
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Generate class data sources
        writer.AppendLine("ClassDataSources = new global::TUnit.Core.Interfaces.IDataSourceAttribute[]");
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
        else if (attrName == "global::TUnit.Core.ArgumentsAttribute")
        {
            GenerateArgumentsAttribute(writer, attr);
        }
        else if (attrName == "global::TUnit.Core.ClassDataSourceAttribute" && attrClass.IsGenericType)
        {
            GenerateClassDataSourceAttribute(writer, attr);
        }
        else
        {
            // For other data source attributes, generate using the generic attribute instantiation method
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
            targetType = (ITypeSymbol)attr.ConstructorArguments[0].Value;
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

        // Find the data source method
        var dataSourceMethod = targetType?.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .FirstOrDefault();

        if (dataSourceMethod == null)
        {
            // Still generate the attribute even if method not found - it will fail at runtime with proper error
            if (attr.ConstructorArguments is
                [
                    { Value: ITypeSymbol missingMethodTypeArg } _, _, ..
                ])
            {
                writer.AppendLine($"new global::TUnit.Core.MethodDataSourceAttribute(typeof({missingMethodTypeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}), \"{methodName}\"),");
            }
            else
            {
                writer.AppendLine($"new global::TUnit.Core.MethodDataSourceAttribute(\"{methodName}\"),");
            }
            return;
        }

        // Generate the attribute with factory
        if (attr.ConstructorArguments is
            [
                { Value: ITypeSymbol typeArg } _, _, ..
            ])
        {
            // MethodDataSource(Type, string) constructor
            writer.AppendLine($"new global::TUnit.Core.MethodDataSourceAttribute(typeof({typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}), \"{methodName}\")");
        }
        else
        {
            // MethodDataSource(string) constructor
            writer.AppendLine($"new global::TUnit.Core.MethodDataSourceAttribute(\"{methodName}\")");
        }
        writer.AppendLine("{");
        writer.Indent();

        // Check if the attribute has Arguments property set
        var argumentsProperty = attr.NamedArguments.FirstOrDefault(x => x.Key == "Arguments");
        var hasArguments = argumentsProperty is { Key: not null, Value.IsNull: false };

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
        writer.AppendLine("},");

        // Copy over any Arguments property if present
        if (hasArguments)
        {
            writer.Append("Arguments = ");
            WriteTypedConstant(writer, argumentsProperty.Value);
            writer.AppendLine(",");
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    private static void GenerateMethodDataSourceFactory(CodeWriter writer, IMethodSymbol dataSourceMethod, ITypeSymbol targetType, IMethodSymbol testMethod, AttributeData attr, bool hasArguments)
    {
        var isStatic = dataSourceMethod.IsStatic;
        var returnType = dataSourceMethod.ReturnType;
        var fullyQualifiedType = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Generate async enumerable that yields Func<Task<object?[]?>>
        writer.AppendLine("async IAsyncEnumerable<Func<Task<object?[]?>>> Factory()");
        writer.AppendLine("{");
        writer.Indent();

        // Build the method call with arguments if needed
        string methodCall;
        if (hasArguments && dataSourceMethod.Parameters.Length > 0)
        {
            // Use the captured arguments
            var argsList = string.Join(", ", dataSourceMethod.Parameters.Select((p, i) =>
                $"(({p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})arguments[{i}])"));
            methodCall = $"{dataSourceMethod.Name}({argsList})";
        }
        else
        {
            methodCall = $"{dataSourceMethod.Name}()";
        }

        // Invoke the data source method
        if (isStatic)
        {
            writer.AppendLine($"var result = {fullyQualifiedType}.{methodCall};");
        }
        else
        {
            // For instance methods, check if test instance is available
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

        // Handle different return types
        var returnTypeName = returnType.ToDisplayString();

        if (IsAsyncEnumerable(returnType))
        {
            // IAsyncEnumerable<T>
            writer.AppendLine("await foreach (var item in result)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(ConvertToObjectArray(item));");
            writer.Unindent();
            writer.AppendLine("}");
        }
        else if (IsTask(returnType))
        {
            // Task<T>
            writer.AppendLine("var taskResult = await result;");
            writer.AppendLine("if (taskResult is System.Collections.IEnumerable enumerable && !(taskResult is string))");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("foreach (var item in enumerable)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(ConvertToObjectArray(item));");
            writer.Unindent();
            writer.AppendLine("}");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("else");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(ConvertToObjectArray(taskResult));");
            writer.Unindent();
            writer.AppendLine("}");
        }
        else if (IsEnumerable(returnType))
        {
            // IEnumerable<T>
            writer.AppendLine("if (result is System.Collections.IEnumerable enumerable && !(result is string))");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("foreach (var item in enumerable)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(ConvertToObjectArray(item));");
            writer.Unindent();
            writer.AppendLine("}");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("else");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(ConvertToObjectArray(result));");
            writer.Unindent();
            writer.AppendLine("}");
        }
        else
        {
            // Single value
            writer.AppendLine("yield return () => global::System.Threading.Tasks.Task.FromResult(ConvertToObjectArray(result));");
        }

        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();

        // Generate helper method for converting to object array
        writer.AppendLine("object?[]? ConvertToObjectArray(object? item)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("if (item == null) return new object?[] { null };");
        writer.AppendLine("if (item is object?[] objArray) return objArray;");
        writer.AppendLine();
        writer.AppendLine("// Handle ValueTuples");
        writer.AppendLine("var type = item.GetType();");
        writer.AppendLine("if (type.Name.StartsWith(\"ValueTuple\"))");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// Use reflection to extract tuple fields");
        writer.AppendLine("var fields = type.GetFields();");
        writer.AppendLine("var values = new object?[fields.Length];");
        writer.AppendLine("for (int i = 0; i < fields.Length; i++)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("values[i] = fields[i].GetValue(item);");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("return values;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
        writer.AppendLine("// Handle regular arrays");
        writer.AppendLine("if (type.IsArray)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("var array = (Array)item;");
        writer.AppendLine("var result = new object?[array.Length];");
        writer.AppendLine("array.CopyTo(result, 0);");
        writer.AppendLine("return result;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
        writer.AppendLine("// Single value");
        writer.AppendLine("return new[] { item };");
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

    private static void GenerateArgumentsAttribute(CodeWriter writer, AttributeData attr)
    {
        writer.AppendLine("new global::TUnit.Core.ArgumentsAttribute(");
        writer.Indent();

        // Write constructor arguments
        for (int i = 0; i < attr.ConstructorArguments.Length; i++)
        {
            WriteTypedConstant(writer, attr.ConstructorArguments[i]);
            if (i < attr.ConstructorArguments.Length - 1)
            {
                writer.Append(", ");
            }
        }

        writer.Unindent();
        writer.AppendLine("),");
    }

    private static void GenerateClassDataSourceAttribute(CodeWriter writer, AttributeData attr)
    {
        if (attr.AttributeClass is { IsGenericType: true, TypeArguments.Length: > 0 })
        {
            var genericArg = attr.AttributeClass.TypeArguments[0];
            writer.AppendLine($"new global::TUnit.Core.ClassDataSourceAttribute<{genericArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(),");
        }
        else
        {
            writer.AppendLine("// Unable to generate ClassDataSourceAttribute");
        }
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
                writer.Append($"typeof({type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
                break;

            case TypedConstantKind.Array:
                var elementType = constant.Type is IArrayTypeSymbol arrayType
                    ? arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
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
                        var propertyType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

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
                            writer.AppendLine($"Setter = (instance, value) => Get{property.Name}BackingField(({className})instance) = ({propertyType})value,");
                            writer.AppendLine("#else");
                            writer.AppendLine($"Setter = (instance, value) => throw new global::System.NotSupportedException(\"Setting init-only properties requires .NET 8 or later\"),");
                            writer.AppendLine("#endif");
                        }
                        else
                        {
                            // For regular properties, use normal property assignment
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
                        writer.AppendLine($"PropertyType = typeof({property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}),");
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
                        var propertyType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        var className = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                        writer.AppendLine("new global::TUnit.Core.PropertyInjectionData");
                        writer.AppendLine("{");
                        writer.Indent();
                        writer.AppendLine($"PropertyName = \"{property.Name}\",");
                        writer.AppendLine($"PropertyType = typeof({propertyType}),");

                        // Generate setter
                        if (property.SetMethod.IsInitOnly)
                        {
                            writer.AppendLine("#if NET8_0_OR_GREATER");
                            writer.AppendLine($"Setter = (instance, value) => Get{property.Name}BackingFieldNested(({className})instance) = ({propertyType})value,");
                            writer.AppendLine("#else");
                            writer.AppendLine($"Setter = (instance, value) => throw new global::System.NotSupportedException(\"Setting init-only properties requires .NET 8 or later\"),");
                            writer.AppendLine("#endif");
                        }
                        else
                        {
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
                        var className = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                        writer.AppendLine($"if (obj is {className} typedObj)");
                        writer.AppendLine("{");
                        writer.Indent();
                        writer.AppendLine($"nestedValues[\"{property.Name}\"] = typedObj.{property.Name};");
                        writer.Unindent();
                        writer.AppendLine("}");
                    }
                }
            }
            currentType = currentType.BaseType;
        }
    }

    private static void GenerateTypedInvokers(CodeWriter writer, TestMethodMetadata testMethod, string className)
    {
        var methodName = testMethod.MethodSymbol.Name;
        var parameters = testMethod.MethodSymbol.Parameters;

        // Check if last parameter is CancellationToken (regardless of whether it has a default value)
        var hasCancellationToken = parameters.Length > 0 &&
            parameters.Last().Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Threading.CancellationToken";

        // Parameters that come from args (excluding CancellationToken)
        var parametersFromArgs = hasCancellationToken
            ? parameters.Take(parameters.Length - 1).ToArray()
            : parameters.ToArray();

        // For generic types, we don't generate InstanceFactory or InvokeTypedTest
        // Instead, we rely on CreateInstance and InvokeTest which are set in GenerateGenericDelegates
        if (testMethod.IsGenericType || testMethod.IsGenericMethod)
        {
            // Skip generating typed invokers for generic types
            return;
        }

        // Use centralized instance factory generator for non-generic types
        InstanceFactoryGenerator.GenerateInstanceFactory(writer, testMethod.TypeSymbol);

        // Test invoker for non-generic types
        var isAsync = IsAsyncMethod(testMethod.MethodSymbol);
        GenerateConcreteTestInvoker(writer, testMethod, className, methodName, isAsync, hasCancellationToken, parametersFromArgs);
    }


    private static void GenerateGenericDelegates(CodeWriter writer, TestMethodMetadata testMethod)
    {
        var typeSymbol = testMethod.TypeSymbol;
        var methodSymbol = testMethod.MethodSymbol;

        // Generate CreateInstance delegate for runtime generic type resolution
        writer.AppendLine("CreateInstance = async (context) =>");
        writer.AppendLine("{");
        writer.Indent();

        if (testMethod.IsGenericType)
        {
            writer.AppendLine("// Get resolved generic types from the test context parameter");
            writer.AppendLine("if (context?.TestDetails?.DataCombination?.ResolvedGenericTypes == null)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("throw new global::System.InvalidOperationException(\"No resolved generic types available for generic test instantiation\");");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine();
            writer.AppendLine("var resolvedTypes = context.TestDetails.DataCombination.ResolvedGenericTypes;");
            writer.AppendLine();

            // Generate code to map type parameters to resolved types
            writer.AppendLine("// Map generic type parameters to resolved types");
            writer.AppendLine("var typeArguments = new global::System.Type[]");
            writer.AppendLine("{");
            writer.Indent();
            foreach (var typeParam in typeSymbol.TypeParameters)
            {
                writer.AppendLine($"resolvedTypes[\"{typeParam.Name}\"],");
            }
            writer.Unindent();
            writer.AppendLine("};");
            writer.AppendLine();

            // Generate code to construct the generic type
            writer.AppendLine("// Construct the generic type using typeof for strong typing");
            // Use typeof with the open generic type definition
            var genericTypeExpression = GetGenericTypeExpression(typeSymbol);
            writer.AppendLine($"var genericTypeDef = typeof({genericTypeExpression});");
            writer.AppendLine("var constructedType = genericTypeDef.MakeGenericType(typeArguments);");
            writer.AppendLine("var instance = global::System.Activator.CreateInstance(constructedType);");
            writer.AppendLine("return instance!;");
        }
        else
        {
            // Non-generic type but might have generic method
            writer.AppendLine($"return new {typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}();");
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Generate InvokeTest delegate for runtime generic method invocation
        writer.AppendLine("InvokeTest = async (instance, args, context, cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();

        if (testMethod.IsGenericMethod || testMethod.IsGenericType)
        {
            writer.AppendLine("// Get resolved generic types from the test context parameter");
            writer.AppendLine("var resolvedTypes = context?.TestDetails?.DataCombination?.ResolvedGenericTypes;");
            writer.AppendLine();

            // Get the method (handle both generic methods and methods in generic types)
            writer.AppendLine("// Get the method to invoke");
            writer.AppendLine("var instanceType = instance.GetType();");
            writer.AppendLine($"var methodInfo = instanceType.GetMethod(\"{methodSymbol.Name}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);");
            writer.AppendLine();

            if (testMethod.IsGenericMethod)
            {
                writer.AppendLine("// Construct generic method if needed");
                writer.AppendLine("if (methodInfo.IsGenericMethodDefinition && resolvedTypes != null)");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("var methodTypeArgs = new global::System.Type[]");
                writer.AppendLine("{");
                writer.Indent();
                foreach (var typeParam in methodSymbol.TypeParameters)
                {
                    writer.AppendLine($"resolvedTypes[\"{typeParam.Name}\"],");
                }
                writer.Unindent();
                writer.AppendLine("};");
                writer.AppendLine("methodInfo = methodInfo.MakeGenericMethod(methodTypeArgs);");
                writer.Unindent();
                writer.AppendLine("}");
                writer.AppendLine();
            }

            // Handle parameters
            writer.AppendLine("// Prepare method arguments");
            writer.AppendLine("var methodArgs = args;");

            // Check if method has CancellationToken parameter
            var hasCancellationToken = methodSymbol.Parameters.Any(p => p.Type.ToDisplayString() == "System.Threading.CancellationToken");
            if (hasCancellationToken)
            {
                writer.AppendLine("// Add CancellationToken if needed");
                writer.AppendLine("var argsList = args.ToList();");
                writer.AppendLine("argsList.Add(cancellationToken);");
                writer.AppendLine("methodArgs = argsList.ToArray();");
            }

            writer.AppendLine();
            writer.AppendLine("// Invoke the method");
            writer.AppendLine("var result = methodInfo.Invoke(instance, methodArgs);");

            // Handle async methods
            writer.AppendLine("if (result is Task task)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("await task;");
            writer.Unindent();
            writer.AppendLine("}");
        }
        else
        {
            // This shouldn't happen - non-generic type with non-generic method should use regular invoker
            writer.AppendLine("throw new global::System.InvalidOperationException(\"Non-generic test should not use generic delegates\");");
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
        writer.AppendLine("var context = global::TUnit.Core.TestContext.Current;");

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
                writer.AppendLine("await Task.CompletedTask;");
            }
        }
        else
        {
            // Count required parameters (those without default values, excluding CancellationToken)
            var requiredParamCount = parametersFromArgs.Count(p => !p.HasExplicitDefaultValue && !p.IsOptional);

            // Generate runtime logic to handle variable argument counts
            writer.AppendLine("// Invoke with only the arguments that were provided");
            writer.AppendLine("switch (args.Length)");
            writer.AppendLine("{");
            writer.Indent();

            // Generate cases for each valid argument count (from required params up to total params from args)
            for (var argCount = requiredParamCount; argCount <= parametersFromArgs.Length; argCount++)
            {
                writer.AppendLine($"case {argCount}:");
                writer.Indent();

                // Build the arguments to pass, including default values for optional parameters
                var argsToPass = new List<string>();
                for (var i = 0; i < parametersFromArgs.Length; i++)
                {
                    var param = parametersFromArgs[i];
                    if (i < argCount)
                    {
                        // Use tuple-aware argument access
                        var argumentExpressions = TupleArgumentHelper.GenerateArgumentAccess(param.Type, "args", i);
                        argsToPass.AddRange(argumentExpressions);
                    }
                    else if (param.HasExplicitDefaultValue)
                    {
                        // Use the default value
                        argsToPass.Add(GetDefaultValueString(param));
                    }
                    else
                    {
                        // This shouldn't happen if we set up requiredParamCount correctly
                        argsToPass.Add($"default({param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
                    }
                }

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
            if (requiredParamCount == parametersFromArgs.Length)
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
                writer.AppendLine("await Task.CompletedTask;");
            }
        }

        writer.Unindent();
        writer.AppendLine("},");

        // Also generate InvokeTypedTest which is required by CreateExecutableTestFactory
        writer.AppendLine($"InvokeTypedTest = async (instance, args, cancellationToken) =>");
        writer.AppendLine("{");
        writer.Indent();

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
                writer.AppendLine("await Task.CompletedTask;");
            }
        }
        else
        {
            // Count required parameters (those without default values, excluding CancellationToken)
            var requiredParamCount = parametersFromArgs.Count(p => !p.HasExplicitDefaultValue && !p.IsOptional);

            // Generate runtime logic to handle variable argument counts
            writer.AppendLine("// Invoke with only the arguments that were provided");
            writer.AppendLine("switch (args.Length)");
            writer.AppendLine("{");
            writer.Indent();

            // Generate cases for each valid argument count (from required params up to total params from args)
            for (var argCount = requiredParamCount; argCount <= parametersFromArgs.Length; argCount++)
            {
                writer.AppendLine($"case {argCount}:");
                writer.Indent();

                // Build the arguments to pass, including default values for optional parameters
                var argsToPass = new List<string>();
                for (var i = 0; i < parametersFromArgs.Length; i++)
                {
                    var param = parametersFromArgs[i];
                    if (i < argCount)
                    {
                        // Use tuple-aware argument access
                        var argumentExpressions = TupleArgumentHelper.GenerateArgumentAccess(param.Type, "args", i);
                        argsToPass.AddRange(argumentExpressions);
                    }
                    else if (param.HasExplicitDefaultValue)
                    {
                        // Use the default value
                        argsToPass.Add(GetDefaultValueString(param));
                    }
                    else
                    {
                        // This shouldn't happen if we set up requiredParamCount correctly
                        argsToPass.Add($"default({param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
                    }
                }

                // Add CancellationToken if present
                if (hasCancellationToken)
                {
                    argsToPass.Add("cancellationToken");
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
            if (requiredParamCount == parametersFromArgs.Length)
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
                writer.AppendLine("await Task.CompletedTask;");
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
        writer.AppendLine("[System.Runtime.CompilerServices.ModuleInitializer]");
        writer.AppendLine("public static void Initialize()");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"global::TUnit.Core.SourceRegistrar.Register(new {testMethod.TypeSymbol.Name}_{testMethod.MethodSymbol.Name}_TestSource_{guid}());");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
    }

    private static bool IsAsyncMethod(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        if (returnType == null)
        {
            return false;
        }

        var returnTypeName = returnType.ToDisplayString();
        return returnTypeName.StartsWith("System.Threading.Tasks.Task") ||
               returnTypeName.StartsWith("System.Threading.Tasks.ValueTask") ||
               returnTypeName.StartsWith("Task<") ||
               returnTypeName.StartsWith("ValueTask<");
    }

    private static bool IsNullableValueType(ITypeSymbol type)
    {
        return type is INamedTypeSymbol { IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T };
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
            GenerateTestDependency(writer, compilation, attr);

            if (i < dependsOnAttributes.Count - 1)
            {
                writer.AppendLine(",");
            }
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    private static void GenerateTestDependency(CodeWriter writer, Compilation compilation, AttributeData attributeData)
    {
        var constructorArgs = attributeData.ConstructorArguments;

        // Handle the different constructor overloads of DependsOnAttribute
        if (constructorArgs.Length == 1)
        {
            var arg = constructorArgs[0];
            if (arg.Type?.Name == "String")
            {
                // DependsOnAttribute(string testName) - dependency on test in same class
                var testName = arg.Value?.ToString() ?? "";
                writer.AppendLine($"new global::TUnit.Core.TestDependency {{ MethodName = \"{testName}\" }}");
            }
            else if (arg.Type?.TypeKind == TypeKind.Class || arg.Type?.Name == "Type")
            {
                // DependsOnAttribute(Type testClass) - dependency on all tests in a class
                var classType = arg.Value as ITypeSymbol;
                if (classType != null)
                {
                    var className = classType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var genericArity = classType is INamedTypeSymbol { IsGenericType: true } namedType
                        ? namedType.Arity
                        : 0;
                    writer.AppendLine($"new global::TUnit.Core.TestDependency {{ ClassType = typeof({className}), ClassGenericArity = {genericArity} }}");
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
                            writer.Append($"typeof({paramType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
                            if (i < secondArg.Values.Length - 1)
                            {
                                writer.Append(", ");
                            }
                        }
                    }
                    writer.AppendLine(" }");
                }

                writer.AppendLine(" }");
            }
            else if (firstArg.Type?.TypeKind == TypeKind.Class || firstArg.Type?.Name == "Type")
            {
                // DependsOnAttribute(Type testClass, string testName)
                var classType = firstArg.Value as ITypeSymbol;
                var testName = secondArg.Value?.ToString() ?? "";

                if (classType != null)
                {
                    var className = classType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var genericArity = classType is INamedTypeSymbol { IsGenericType: true } namedType
                        ? namedType.Arity
                        : 0;
                    writer.AppendLine($"new global::TUnit.Core.TestDependency {{ ClassType = typeof({className}), ClassGenericArity = {genericArity}, MethodName = \"{testName}\" }}");
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
                var className = classType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
                            writer.Append($"typeof({paramType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})");
                            if (i < paramTypesArg.Values.Length - 1)
                            {
                                writer.Append(", ");
                            }
                        }
                    }
                    writer.AppendLine(" }");
                }

                writer.AppendLine(" }");
            }
        }
    }

    private static string GetDefaultValueString(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
        {
            return $"default({parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
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

    private static InheritsTestsClassMetadata? GetInheritsTestsClassMetadata(GeneratorAttributeSyntaxContext context)
    {
        var classSyntax = (ClassDeclarationSyntax)context.TargetNode;
        var classSymbol = context.TargetSymbol as INamedTypeSymbol;

        if (classSymbol == null)
        {
            return null;
        }

        // Skip abstract classes
        if (classSymbol.IsAbstract)
        {
            return null;
        }

        // Skip generic types without explicit instantiation
        if (classSymbol is { IsGenericType: true, TypeParameters.Length: > 0 })
        {
            return null;
        }

        return new InheritsTestsClassMetadata
        {
            TypeSymbol = classSymbol,
            ClassSyntax = classSyntax
        };
    }

    private static void GenerateInheritedTestSources(SourceProductionContext context, Compilation compilation, InheritsTestsClassMetadata? classInfo)
    {
        if (classInfo?.TypeSymbol == null)
        {
            return;
        }

        // Find all test methods in base classes
        var inheritedTestMethods = new List<IMethodSymbol>();
        CollectInheritedTestMethods(classInfo.TypeSymbol, inheritedTestMethods);

        // Generate test metadata for each inherited test method
        foreach (var method in inheritedTestMethods)
        {
            var testAttribute = method.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "TestAttribute");

            var testMethodMetadata = new TestMethodMetadata
            {
                MethodSymbol = method,
                TypeSymbol = classInfo.TypeSymbol,
                FilePath = "inherited", // No file path for inherited methods
                LineNumber = 0, // No line number for inherited methods
                TestAttribute = testAttribute!,
                Context = null, // No context for inherited tests
                MethodSyntax = null!, // We don't have the syntax for inherited methods
                IsGenericType = classInfo.TypeSymbol.IsGenericType,
                IsGenericMethod = method.IsGenericMethod,
                MethodAttributes = method.GetAttributes()
            };

            GenerateTestMethodSource(context, compilation, testMethodMetadata);
        }
    }

    private static void CollectInheritedTestMethods(INamedTypeSymbol derivedClass, List<IMethodSymbol> testMethods)
    {
        var currentType = derivedClass.BaseType;

        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            // Skip if base type is an open generic type
            if (currentType.IsUnboundGenericType ||
                (currentType.IsGenericType && currentType.TypeArguments.Any(arg => arg.TypeKind == TypeKind.TypeParameter)))
            {
                currentType = currentType.BaseType;
                continue;
            }

            // Get all methods from the base class
            var methods = currentType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => !m.IsStatic && m.MethodKind == MethodKind.Ordinary);

            foreach (var method in methods)
            {
                // Skip generic methods - they can't be instantiated without concrete type arguments
                if (method.IsGenericMethod)
                {
                    continue;
                }

                // Check if method has Test attribute
                var hasTestAttribute = method.GetAttributes()
                    .Any(attr => attr.AttributeClass?.Name == "TestAttribute" &&
                                attr.AttributeClass.ContainingNamespace?.ToDisplayString() == "TUnit.Core");

                if (hasTestAttribute)
                {
                    testMethods.Add(method);
                }
            }

            currentType = currentType.BaseType;
        }
    }

    /// <summary>
    /// Generates field accessors for init-only properties with data source attributes
    /// </summary>
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
                var propertyType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                writer.AppendLine($"/// <summary>");
                writer.AppendLine($"/// UnsafeAccessor for init-only property {property.Name} backing field");
                writer.AppendLine($"/// </summary>");
                writer.AppendLine($"[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = \"{backingFieldName}\")]");
                writer.AppendLine($"private static extern ref {propertyType} Get{property.Name}BackingField({className} instance);");
                writer.AppendLine();
            }
            writer.AppendLine("#endif");
        }
    }

    private static string GetGenericTypeExpression(INamedTypeSymbol typeSymbol)
    {
        // For generic types, we need to generate the open generic type definition
        // e.g., MyClass<T> becomes MyClass<>
        var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

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
            // For generic types, use typeof(object) as placeholder since we can't resolve generics at compile time
            return "typeof(object)";
        }

        var fullyQualifiedName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
        // TODO: Implement parameter position mapping for type inference
        writer.AppendLine("// TODO: Map type parameters to method argument positions");
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
            writer.AppendLine($"BaseTypeConstraint = typeof({baseTypeConstraint.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}),");
        }

        // Generate interface constraints
        var interfaceConstraints = typeParam.ConstraintTypes.Where(c => c.TypeKind == TypeKind.Interface).ToArray();
        writer.AppendLine("InterfaceConstraints = new global::System.Type[]");
        writer.AppendLine("{");
        writer.Indent();
        foreach (var constraintType in interfaceConstraints)
        {
            var constraintTypeName = constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
}

/// <summary>
/// Metadata for a class with [InheritsTests] attribute
/// </summary>
public class InheritsTestsClassMetadata
{
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public required ClassDeclarationSyntax ClassSyntax { get; init; }
}
