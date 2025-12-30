using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Assertions.SourceGenerator.Generators;

/// <summary>
/// Source generator that creates wrapper type overloads from methods decorated with [GenerateAssertOverloads].
/// Generates:
/// - Wrapper types (FuncXxxAssertion, TaskXxxAssertion, etc.) implementing IAssertionSource&lt;T&gt;
/// - Assert.That() overloads for Func, Task, ValueTask variants
/// </summary>
[Generator]
public sealed class AssertOverloadsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all methods decorated with [GenerateAssertOverloads]
        var methods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Assertions.Attributes.GenerateAssertOverloadsAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, ct) => GetMethodData(ctx, ct))
            .Where(x => x != null);

        // Generate output
        context.RegisterSourceOutput(methods.Collect(), static (context, methods) =>
        {
            GenerateOverloads(context, methods!);
        });
    }

    private static OverloadMethodData? GetMethodData(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        // Extract attribute properties
        int priority = 0;
        bool generateFunc = true;
        bool generateFuncTask = true;
        bool generateFuncValueTask = true;
        bool generateTask = true;
        bool generateValueTask = true;

        var attribute = context.Attributes.FirstOrDefault();
        if (attribute != null)
        {
            foreach (var namedArg in attribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "Priority" when namedArg.Value.Value is int p:
                        priority = p;
                        break;
                    case "Func" when namedArg.Value.Value is bool f:
                        generateFunc = f;
                        break;
                    case "FuncTask" when namedArg.Value.Value is bool ft:
                        generateFuncTask = ft;
                        break;
                    case "FuncValueTask" when namedArg.Value.Value is bool fvt:
                        generateFuncValueTask = fvt;
                        break;
                    case "Task" when namedArg.Value.Value is bool t:
                        generateTask = t;
                        break;
                    case "ValueTask" when namedArg.Value.Value is bool vt:
                        generateValueTask = vt;
                        break;
                }
            }
        }

        return new OverloadMethodData(
            methodSymbol,
            priority,
            generateFunc,
            generateFuncTask,
            generateFuncValueTask,
            generateTask,
            generateValueTask);
    }

    private static void GenerateOverloads(
        SourceProductionContext context,
        ImmutableArray<OverloadMethodData?> methods)
    {
        var validMethods = methods.Where(m => m != null).Select(m => m!).ToList();
        if (validMethods.Count == 0)
        {
            return;
        }

        // Group methods by return type to avoid generating duplicate wrapper types
        // when multiple source types (e.g., IDictionary and IReadOnlyDictionary) share the same assertion type
        var methodsByReturnType = validMethods
            .GroupBy(m => m.Method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
            .ToList();

        foreach (var group in methodsByReturnType)
        {
            GenerateForMethodGroup(context, group.ToList());
        }
    }

    /// <summary>
    /// Generates wrapper types and overloads for a group of methods that share the same return type.
    /// This avoids generating duplicate wrapper types when multiple source types (e.g., IDictionary and IReadOnlyDictionary)
    /// share the same assertion type (e.g., DictionaryAssertion).
    /// </summary>
    private static void GenerateForMethodGroup(
        SourceProductionContext context,
        List<OverloadMethodData> methodGroup)
    {
        if (methodGroup.Count == 0)
        {
            return;
        }

        // Use the first method to get shared info (return type, containing type, namespace, type parameters)
        var firstMethodData = methodGroup[0];
        var firstMethod = firstMethodData.Method;
        var returnType = firstMethod.ReturnType as INamedTypeSymbol;
        if (returnType == null)
        {
            return;
        }

        var firstParam = firstMethod.Parameters.FirstOrDefault();
        if (firstParam == null)
        {
            return;
        }

        var containingType = firstMethod.ContainingType;
        var namespaceName = containingType.ContainingNamespace?.ToDisplayString() ?? "TUnit.Assertions";
        var returnTypeName = returnType.Name;

        // Handle generic methods - extract type parameters from the method
        var methodTypeParameters = firstMethod.TypeParameters;

        // For wrapper types, we need to use the source type from the first parameter
        // This is typically the type that assertions operate on
        var sourceTypeInfo = GetSourceTypeInfo(firstParam.Type);

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using TUnit.Assertions.Conditions;");
        sb.AppendLine("using TUnit.Assertions.Core;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        // Determine which wrapper types to generate (based on first method's settings)
        // All methods in the group should typically have the same generation settings
        // since they share the same return type
        if (firstMethodData.GenerateFunc)
        {
            GenerateFuncWrapperType(sb, returnTypeName, sourceTypeInfo, methodTypeParameters);
        }

        if (firstMethodData.GenerateFuncTask)
        {
            GenerateAsyncFuncWrapperType(sb, returnTypeName, sourceTypeInfo, methodTypeParameters);
        }

        if (firstMethodData.GenerateFuncValueTask)
        {
            GenerateValueTaskAsyncFuncWrapperType(sb, returnTypeName, sourceTypeInfo, methodTypeParameters);
        }

        if (firstMethodData.GenerateTask)
        {
            GenerateTaskWrapperType(sb, returnTypeName, sourceTypeInfo, methodTypeParameters);
        }

        if (firstMethodData.GenerateValueTask)
        {
            GenerateValueTaskWrapperType(sb, returnTypeName, sourceTypeInfo, methodTypeParameters);
        }

        // Generate partial class with overloads for ALL methods in the group
        sb.AppendLine($"public static partial class {containingType.Name}");
        sb.AppendLine("{");

        foreach (var methodData in methodGroup)
        {
            var method = methodData.Method;
            var param = method.Parameters.FirstOrDefault();
            if (param == null)
            {
                continue;
            }

            var paramSourceTypeInfo = GetSourceTypeInfo(param.Type);
            var paramTypeParameters = method.TypeParameters;

            if (methodData.GenerateFunc)
            {
                GenerateFuncOverload(sb, returnTypeName, paramSourceTypeInfo, paramTypeParameters, methodData.Priority);
            }

            if (methodData.GenerateFuncTask)
            {
                GenerateAsyncFuncOverload(sb, returnTypeName, paramSourceTypeInfo, paramTypeParameters, methodData.Priority);
            }

            if (methodData.GenerateFuncValueTask)
            {
                GenerateValueTaskAsyncFuncOverload(sb, returnTypeName, paramSourceTypeInfo, paramTypeParameters, methodData.Priority);
            }

            if (methodData.GenerateTask)
            {
                GenerateTaskOverload(sb, returnTypeName, paramSourceTypeInfo, paramTypeParameters, methodData.Priority);
            }

            if (methodData.GenerateValueTask)
            {
                GenerateValueTaskOverload(sb, returnTypeName, paramSourceTypeInfo, paramTypeParameters, methodData.Priority);
            }
        }

        sb.AppendLine("}");

        // Generate a unique file name based on return type
        var safeReturnTypeName = returnTypeName
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "");
        var fileName = $"{containingType.Name}.{safeReturnTypeName}.Overloads.g.cs";
        context.AddSource(fileName, sb.ToString());
    }

    private static void GenerateForMethod(
        SourceProductionContext context,
        OverloadMethodData methodData)
    {
        var method = methodData.Method;
        var returnType = method.ReturnType as INamedTypeSymbol;
        if (returnType == null)
        {
            return;
        }

        // Get the source type (first parameter type)
        var firstParam = method.Parameters.FirstOrDefault();
        if (firstParam == null)
        {
            return;
        }

        var containingType = method.ContainingType;
        var namespaceName = containingType.ContainingNamespace?.ToDisplayString() ?? "TUnit.Assertions";

        // Extract type information
        var sourceTypeInfo = GetSourceTypeInfo(firstParam.Type);
        var returnTypeName = returnType.Name;

        // Handle generic methods - extract type parameters from the method
        var methodTypeParameters = method.TypeParameters;
        var typeParameterList = "";
        var typeParameterConstraints = "";
        if (methodTypeParameters.Length > 0)
        {
            typeParameterList = "<" + string.Join(", ", methodTypeParameters.Select(tp => tp.Name)) + ">";
            typeParameterConstraints = GetTypeConstraints(methodTypeParameters);
        }

        // Build the wrapper type name suffix (includes type params for generic methods)
        var wrapperTypeSuffix = returnTypeName + typeParameterList;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using TUnit.Assertions.Conditions;");
        sb.AppendLine("using TUnit.Assertions.Core;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        // Generate wrapper types
        if (methodData.GenerateFunc)
        {
            GenerateFuncWrapperType(sb, returnTypeName, sourceTypeInfo, methodTypeParameters);
        }

        if (methodData.GenerateFuncTask)
        {
            GenerateAsyncFuncWrapperType(sb, returnTypeName, sourceTypeInfo, methodTypeParameters);
        }

        if (methodData.GenerateFuncValueTask)
        {
            GenerateValueTaskAsyncFuncWrapperType(sb, returnTypeName, sourceTypeInfo, methodTypeParameters);
        }

        if (methodData.GenerateTask)
        {
            GenerateTaskWrapperType(sb, returnTypeName, sourceTypeInfo, methodTypeParameters);
        }

        if (methodData.GenerateValueTask)
        {
            GenerateValueTaskWrapperType(sb, returnTypeName, sourceTypeInfo, methodTypeParameters);
        }

        // Generate partial class with overloads
        sb.AppendLine($"public static partial class {containingType.Name}");
        sb.AppendLine("{");

        if (methodData.GenerateFunc)
        {
            GenerateFuncOverload(sb, returnTypeName, sourceTypeInfo, methodTypeParameters, methodData.Priority);
        }

        if (methodData.GenerateFuncTask)
        {
            GenerateAsyncFuncOverload(sb, returnTypeName, sourceTypeInfo, methodTypeParameters, methodData.Priority);
        }

        if (methodData.GenerateFuncValueTask)
        {
            GenerateValueTaskAsyncFuncOverload(sb, returnTypeName, sourceTypeInfo, methodTypeParameters, methodData.Priority);
        }

        if (methodData.GenerateTask)
        {
            GenerateTaskOverload(sb, returnTypeName, sourceTypeInfo, methodTypeParameters, methodData.Priority);
        }

        if (methodData.GenerateValueTask)
        {
            GenerateValueTaskOverload(sb, returnTypeName, sourceTypeInfo, methodTypeParameters, methodData.Priority);
        }

        sb.AppendLine("}");

        // Generate a unique file name
        var safeTypeName = sourceTypeInfo.SafeTypeName;
        var fileName = $"{containingType.Name}.{safeTypeName}.Overloads.g.cs";
        context.AddSource(fileName, sb.ToString());
    }

    private static SourceTypeInfo GetSourceTypeInfo(ITypeSymbol typeSymbol)
    {
        // Get display string for use in generated code
        var fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var minimalTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        // Handle nullable reference types
        var isNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
        var underlyingType = typeSymbol;

        if (isNullable && typeSymbol is INamedTypeSymbol namedType)
        {
            underlyingType = namedType.WithNullableAnnotation(NullableAnnotation.None);
        }

        // Create safe file name component
        var safeTypeName = minimalTypeName
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "")
            .Replace("?", "Nullable");

        return new SourceTypeInfo(
            fullTypeName,
            minimalTypeName,
            safeTypeName,
            isNullable,
            underlyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            underlyingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
    }

    private static string GetTypeConstraints(ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        if (typeParameters.Length == 0)
        {
            return "";
        }

        var constraints = new StringBuilder();
        foreach (var tp in typeParameters)
        {
            var constraintParts = new List<string>();

            // Reference type constraint
            if (tp.HasReferenceTypeConstraint)
            {
                constraintParts.Add("class");
            }

            // Value type constraint
            if (tp.HasValueTypeConstraint)
            {
                constraintParts.Add("struct");
            }

            // Unmanaged constraint
            if (tp.HasUnmanagedTypeConstraint)
            {
                constraintParts.Add("unmanaged");
            }

            // notnull constraint
            if (tp.HasNotNullConstraint)
            {
                constraintParts.Add("notnull");
            }

            // Type constraints
            foreach (var constraintType in tp.ConstraintTypes)
            {
                constraintParts.Add(constraintType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            }

            // new() constraint (must be last)
            if (tp.HasConstructorConstraint)
            {
                constraintParts.Add("new()");
            }

            if (constraintParts.Count > 0)
            {
                constraints.AppendLine();
                constraints.Append($"        where {tp.Name} : {string.Join(", ", constraintParts)}");
            }
        }

        return constraints.ToString();
    }

    private static string GetTypeParameterList(ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        if (typeParameters.Length == 0)
        {
            return "";
        }

        return "<" + string.Join(", ", typeParameters.Select(tp => tp.Name)) + ">";
    }

    private static void GenerateFuncWrapperType(
        StringBuilder sb,
        string returnTypeName,
        SourceTypeInfo sourceTypeInfo,
        ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        var typeParamList = GetTypeParameterList(typeParameters);
        var constraints = GetTypeConstraints(typeParameters);
        var wrapperTypeName = $"Func{returnTypeName}{typeParamList}";
        var sourceType = sourceTypeInfo.MinimalTypeName;
        // Ensure nullable type for tuple first element
        var nullableSourceType = sourceTypeInfo.IsNullable ? sourceType : sourceType + "?";

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Func wrapper for {returnTypeName}. Implements IAssertionSource for lazy synchronous evaluation.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public class Func{returnTypeName}{typeParamList} : IAssertionSource<{sourceType}>{constraints}");
        sb.AppendLine("{");
        sb.AppendLine($"    public AssertionContext<{sourceType}> Context {{ get; }}");
        sb.AppendLine();
        sb.AppendLine($"    public Func{returnTypeName}(Func<{sourceType}> func, string? expression)");
        sb.AppendLine("    {");
        sb.AppendLine("        var expressionBuilder = new StringBuilder();");
        sb.AppendLine("        expressionBuilder.Append($\"Assert.That({expression ?? \"?\"})\");");
        sb.AppendLine();
        sb.AppendLine($"        var evaluationContext = new EvaluationContext<{sourceType}>(() =>");
        sb.AppendLine("        {");
        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine("                var result = func();");
        sb.AppendLine($"                return Task.FromResult<({nullableSourceType}, Exception?)>((result, null));");
        sb.AppendLine("            }");
        sb.AppendLine("            catch (Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine($"                return Task.FromResult<({nullableSourceType}, Exception?)>((default, ex));");
        sb.AppendLine("            }");
        sb.AppendLine("        });");
        sb.AppendLine();
        sb.AppendLine($"        Context = new AssertionContext<{sourceType}>(evaluationContext, expressionBuilder);");
        sb.AppendLine("    }");
        sb.AppendLine();
        GenerateIAssertionSourceMethods(sb, sourceType);
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GenerateAsyncFuncWrapperType(
        StringBuilder sb,
        string returnTypeName,
        SourceTypeInfo sourceTypeInfo,
        ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        var typeParamList = GetTypeParameterList(typeParameters);
        var constraints = GetTypeConstraints(typeParameters);
        var sourceType = sourceTypeInfo.MinimalTypeName;
        // Ensure nullable type for tuple first element
        var nullableSourceType = sourceTypeInfo.IsNullable ? sourceType : sourceType + "?";

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Async Func wrapper for {returnTypeName}. Implements IAssertionSource for async factory evaluation.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public class AsyncFunc{returnTypeName}{typeParamList} : IAssertionSource<{sourceType}>{constraints}");
        sb.AppendLine("{");
        sb.AppendLine($"    public AssertionContext<{sourceType}> Context {{ get; }}");
        sb.AppendLine();
        sb.AppendLine($"    public AsyncFunc{returnTypeName}(Func<Task<{sourceType}>> func, string? expression)");
        sb.AppendLine("    {");
        sb.AppendLine("        var expressionBuilder = new StringBuilder();");
        sb.AppendLine("        expressionBuilder.Append($\"Assert.That({expression ?? \"?\"})\");");
        sb.AppendLine();
        sb.AppendLine($"        var evaluationContext = new EvaluationContext<{sourceType}>(async () =>");
        sb.AppendLine("        {");
        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine("                var result = await func().ConfigureAwait(false);");
        sb.AppendLine($"                return (({nullableSourceType})result, (Exception?)null);");
        sb.AppendLine("            }");
        sb.AppendLine("            catch (Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine($"                return (default({nullableSourceType}), ex);");
        sb.AppendLine("            }");
        sb.AppendLine("        });");
        sb.AppendLine();
        sb.AppendLine($"        Context = new AssertionContext<{sourceType}>(evaluationContext, expressionBuilder);");
        sb.AppendLine("    }");
        sb.AppendLine();
        GenerateIAssertionSourceMethods(sb, sourceType);
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GenerateValueTaskAsyncFuncWrapperType(
        StringBuilder sb,
        string returnTypeName,
        SourceTypeInfo sourceTypeInfo,
        ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        var typeParamList = GetTypeParameterList(typeParameters);
        var constraints = GetTypeConstraints(typeParameters);
        var sourceType = sourceTypeInfo.MinimalTypeName;
        // Ensure nullable type for tuple first element
        var nullableSourceType = sourceTypeInfo.IsNullable ? sourceType : sourceType + "?";

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// ValueTask Async Func wrapper for {returnTypeName}. Implements IAssertionSource for ValueTask async factory evaluation.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public class ValueTaskAsyncFunc{returnTypeName}{typeParamList} : IAssertionSource<{sourceType}>{constraints}");
        sb.AppendLine("{");
        sb.AppendLine($"    public AssertionContext<{sourceType}> Context {{ get; }}");
        sb.AppendLine();
        sb.AppendLine($"    public ValueTaskAsyncFunc{returnTypeName}(Func<ValueTask<{sourceType}>> func, string? expression)");
        sb.AppendLine("    {");
        sb.AppendLine("        var expressionBuilder = new StringBuilder();");
        sb.AppendLine("        expressionBuilder.Append($\"Assert.That({expression ?? \"?\"})\");");
        sb.AppendLine();
        sb.AppendLine($"        var evaluationContext = new EvaluationContext<{sourceType}>(async () =>");
        sb.AppendLine("        {");
        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine("                var result = await func().ConfigureAwait(false);");
        sb.AppendLine($"                return (({nullableSourceType})result, (Exception?)null);");
        sb.AppendLine("            }");
        sb.AppendLine("            catch (Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine($"                return (default({nullableSourceType}), ex);");
        sb.AppendLine("            }");
        sb.AppendLine("        });");
        sb.AppendLine();
        sb.AppendLine($"        Context = new AssertionContext<{sourceType}>(evaluationContext, expressionBuilder);");
        sb.AppendLine("    }");
        sb.AppendLine();
        GenerateIAssertionSourceMethods(sb, sourceType);
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GenerateTaskWrapperType(
        StringBuilder sb,
        string returnTypeName,
        SourceTypeInfo sourceTypeInfo,
        ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        var typeParamList = GetTypeParameterList(typeParameters);
        var constraints = GetTypeConstraints(typeParameters);
        var sourceType = sourceTypeInfo.MinimalTypeName;
        // Ensure nullable type for tuple first element
        var nullableSourceType = sourceTypeInfo.IsNullable ? sourceType : sourceType + "?";

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Task wrapper for {returnTypeName}. Implements IAssertionSource for awaiting an already-started task.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public class Task{returnTypeName}{typeParamList} : IAssertionSource<{sourceType}>{constraints}");
        sb.AppendLine("{");
        sb.AppendLine($"    public AssertionContext<{sourceType}> Context {{ get; }}");
        sb.AppendLine();
        sb.AppendLine($"    public Task{returnTypeName}(Task<{sourceType}> task, string? expression)");
        sb.AppendLine("    {");
        sb.AppendLine("        var expressionBuilder = new StringBuilder();");
        sb.AppendLine("        expressionBuilder.Append($\"Assert.That({expression ?? \"?\"})\");");
        sb.AppendLine();
        sb.AppendLine($"        var evaluationContext = new EvaluationContext<{sourceType}>(async () =>");
        sb.AppendLine("        {");
        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine("                var result = await task.ConfigureAwait(false);");
        sb.AppendLine($"                return (({nullableSourceType})result, (Exception?)null);");
        sb.AppendLine("            }");
        sb.AppendLine("            catch (Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine($"                return (default({nullableSourceType}), ex);");
        sb.AppendLine("            }");
        sb.AppendLine("        });");
        sb.AppendLine();
        sb.AppendLine($"        Context = new AssertionContext<{sourceType}>(evaluationContext, expressionBuilder);");
        sb.AppendLine("    }");
        sb.AppendLine();
        GenerateIAssertionSourceMethods(sb, sourceType);
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GenerateValueTaskWrapperType(
        StringBuilder sb,
        string returnTypeName,
        SourceTypeInfo sourceTypeInfo,
        ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        var typeParamList = GetTypeParameterList(typeParameters);
        var constraints = GetTypeConstraints(typeParameters);
        var sourceType = sourceTypeInfo.MinimalTypeName;
        // Ensure nullable type for tuple first element
        var nullableSourceType = sourceTypeInfo.IsNullable ? sourceType : sourceType + "?";

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// ValueTask wrapper for {returnTypeName}. Implements IAssertionSource for awaiting a ValueTask.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public class ValueTask{returnTypeName}{typeParamList} : IAssertionSource<{sourceType}>{constraints}");
        sb.AppendLine("{");
        sb.AppendLine($"    public AssertionContext<{sourceType}> Context {{ get; }}");
        sb.AppendLine();
        sb.AppendLine($"    public ValueTask{returnTypeName}(ValueTask<{sourceType}> valueTask, string? expression)");
        sb.AppendLine("    {");
        sb.AppendLine("        var expressionBuilder = new StringBuilder();");
        sb.AppendLine("        expressionBuilder.Append($\"Assert.That({expression ?? \"?\"})\");");
        sb.AppendLine();
        sb.AppendLine($"        var evaluationContext = new EvaluationContext<{sourceType}>(async () =>");
        sb.AppendLine("        {");
        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine("                var result = await valueTask.ConfigureAwait(false);");
        sb.AppendLine($"                return (({nullableSourceType})result, (Exception?)null);");
        sb.AppendLine("            }");
        sb.AppendLine("            catch (Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine($"                return (default({nullableSourceType}), ex);");
        sb.AppendLine("            }");
        sb.AppendLine("        });");
        sb.AppendLine();
        sb.AppendLine($"        Context = new AssertionContext<{sourceType}>(evaluationContext, expressionBuilder);");
        sb.AppendLine("    }");
        sb.AppendLine();
        GenerateIAssertionSourceMethods(sb, sourceType);
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void GenerateIAssertionSourceMethods(StringBuilder sb, string sourceType)
    {
        // Generate the IAssertionSource<T> interface methods
        sb.AppendLine($"    /// <inheritdoc />");
        sb.AppendLine($"    public TypeOfAssertion<{sourceType}, TExpected> IsTypeOf<TExpected>()");
        sb.AppendLine("    {");
        sb.AppendLine("        Context.ExpressionBuilder.Append($\".IsTypeOf<{typeof(TExpected).Name}>()\");");
        sb.AppendLine($"        return new TypeOfAssertion<{sourceType}, TExpected>(Context);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    /// <inheritdoc />");
        sb.AppendLine($"    public IsNotTypeOfAssertion<{sourceType}, TExpected> IsNotTypeOf<TExpected>()");
        sb.AppendLine("    {");
        sb.AppendLine("        Context.ExpressionBuilder.Append($\".IsNotTypeOf<{typeof(TExpected).Name}>()\");");
        sb.AppendLine($"        return new IsNotTypeOfAssertion<{sourceType}, TExpected>(Context);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    /// <inheritdoc />");
        sb.AppendLine($"    public IsAssignableToAssertion<TTarget, {sourceType}> IsAssignableTo<TTarget>()");
        sb.AppendLine("    {");
        sb.AppendLine("        Context.ExpressionBuilder.Append($\".IsAssignableTo<{typeof(TTarget).Name}>()\");");
        sb.AppendLine($"        return new IsAssignableToAssertion<TTarget, {sourceType}>(Context);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    /// <inheritdoc />");
        sb.AppendLine($"    public IsNotAssignableToAssertion<TTarget, {sourceType}> IsNotAssignableTo<TTarget>()");
        sb.AppendLine("    {");
        sb.AppendLine("        Context.ExpressionBuilder.Append($\".IsNotAssignableTo<{typeof(TTarget).Name}>()\");");
        sb.AppendLine($"        return new IsNotAssignableToAssertion<TTarget, {sourceType}>(Context);");
        sb.AppendLine("    }");
    }

    private static void GenerateFuncOverload(
        StringBuilder sb,
        string returnTypeName,
        SourceTypeInfo sourceTypeInfo,
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        int priority)
    {
        var typeParamList = GetTypeParameterList(typeParameters);
        var constraints = GetTypeConstraints(typeParameters);
        var sourceType = sourceTypeInfo.MinimalTypeName;

        if (priority != 0)
        {
            sb.AppendLine($"    [OverloadResolutionPriority({priority})]");
        }

        sb.AppendLine($"    public static Func{returnTypeName}{typeParamList} That{typeParamList}(");
        sb.AppendLine($"        Func<{sourceType}> func,");
        sb.AppendLine($"        [CallerArgumentExpression(nameof(func))] string? expression = null){constraints}");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new Func{returnTypeName}{typeParamList}(func, expression);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateAsyncFuncOverload(
        StringBuilder sb,
        string returnTypeName,
        SourceTypeInfo sourceTypeInfo,
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        int priority)
    {
        var typeParamList = GetTypeParameterList(typeParameters);
        var constraints = GetTypeConstraints(typeParameters);
        var sourceType = sourceTypeInfo.MinimalTypeName;

        if (priority != 0)
        {
            sb.AppendLine($"    [OverloadResolutionPriority({priority})]");
        }

        sb.AppendLine($"    public static AsyncFunc{returnTypeName}{typeParamList} That{typeParamList}(");
        sb.AppendLine($"        Func<Task<{sourceType}>> func,");
        sb.AppendLine($"        [CallerArgumentExpression(nameof(func))] string? expression = null){constraints}");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new AsyncFunc{returnTypeName}{typeParamList}(func, expression);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateValueTaskAsyncFuncOverload(
        StringBuilder sb,
        string returnTypeName,
        SourceTypeInfo sourceTypeInfo,
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        int priority)
    {
        var typeParamList = GetTypeParameterList(typeParameters);
        var constraints = GetTypeConstraints(typeParameters);
        var sourceType = sourceTypeInfo.MinimalTypeName;

        if (priority != 0)
        {
            sb.AppendLine($"    [OverloadResolutionPriority({priority})]");
        }

        sb.AppendLine($"    public static ValueTaskAsyncFunc{returnTypeName}{typeParamList} That{typeParamList}(");
        sb.AppendLine($"        Func<ValueTask<{sourceType}>> func,");
        sb.AppendLine($"        [CallerArgumentExpression(nameof(func))] string? expression = null){constraints}");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new ValueTaskAsyncFunc{returnTypeName}{typeParamList}(func, expression);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateTaskOverload(
        StringBuilder sb,
        string returnTypeName,
        SourceTypeInfo sourceTypeInfo,
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        int priority)
    {
        var typeParamList = GetTypeParameterList(typeParameters);
        var constraints = GetTypeConstraints(typeParameters);
        var sourceType = sourceTypeInfo.MinimalTypeName;

        if (priority != 0)
        {
            sb.AppendLine($"    [OverloadResolutionPriority({priority})]");
        }

        sb.AppendLine($"    public static Task{returnTypeName}{typeParamList} That{typeParamList}(");
        sb.AppendLine($"        Task<{sourceType}> task,");
        sb.AppendLine($"        [CallerArgumentExpression(nameof(task))] string? expression = null){constraints}");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new Task{returnTypeName}{typeParamList}(task, expression);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateValueTaskOverload(
        StringBuilder sb,
        string returnTypeName,
        SourceTypeInfo sourceTypeInfo,
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        int priority)
    {
        var typeParamList = GetTypeParameterList(typeParameters);
        var constraints = GetTypeConstraints(typeParameters);
        var sourceType = sourceTypeInfo.MinimalTypeName;

        if (priority != 0)
        {
            sb.AppendLine($"    [OverloadResolutionPriority({priority})]");
        }

        sb.AppendLine($"    public static ValueTask{returnTypeName}{typeParamList} That{typeParamList}(");
        sb.AppendLine($"        ValueTask<{sourceType}> valueTask,");
        sb.AppendLine($"        [CallerArgumentExpression(nameof(valueTask))] string? expression = null){constraints}");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new ValueTask{returnTypeName}{typeParamList}(valueTask, expression);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private record OverloadMethodData(
        IMethodSymbol Method,
        int Priority,
        bool GenerateFunc,
        bool GenerateFuncTask,
        bool GenerateFuncValueTask,
        bool GenerateTask,
        bool GenerateValueTask);

    private record SourceTypeInfo(
        string FullTypeName,
        string MinimalTypeName,
        string SafeTypeName,
        bool IsNullable,
        string UnderlyingFullTypeName,
        string UnderlyingMinimalTypeName);
}
