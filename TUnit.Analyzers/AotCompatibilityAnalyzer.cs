using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

/// <summary>
/// Analyzer that detects patterns incompatible with AOT compilation and provides actionable error messages
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AotCompatibilityAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            Rules.GenericTestMissingExplicitInstantiation,
            Rules.DynamicDataSourceNotAotCompatible,
            Rules.OpenGenericTypeNotAotCompatible,
            Rules.ExpressionCompileNotAotCompatible,
            Rules.ReflectionWithoutDynamicallyAccessedMembers,
            Rules.TypeGetTypeNotAotCompatible,
            Rules.MakeGenericTypeNotAotCompatible,
            Rules.ActivatorCreateInstanceWithoutAttribution,
            Rules.DynamicCodeGenerationNotAotCompatible,
            Rules.ReflectionEmitNotAotCompatible,
            Rules.AssemblyLoadFromNotAotCompatible
        );

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeGenericTestMethods, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeGenericTestClasses, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeDataSourceAttributes, SyntaxKind.Attribute);
        context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccessExpression, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeGenericTestMethods(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

        if (methodSymbol == null || !IsTestMethod(methodSymbol))
        {
            return;
        }

        // Skip test methods in abstract classes - they can't be instantiated directly
        // and type inference happens when concrete classes inherit from them
        if (methodSymbol.ContainingType.IsAbstract)
        {
            return;
        }

        if (methodSymbol.IsGenericMethod)
        {
            var hasGenerateGenericTestAttribute = HasGenerateGenericTestAttribute(methodSymbol) ||
                                                   HasGenerateGenericTestAttribute(methodSymbol.ContainingType);

            if (!hasGenerateGenericTestAttribute)
            {
                var canInferTypes = CanInferGenericTypes(methodSymbol, context.Compilation);

                if (!canInferTypes)
                {
                    var diagnostic = Diagnostic.Create(
                        Rules.GenericTestMissingExplicitInstantiation,
                        methodDeclaration.Identifier.GetLocation(),
                        methodSymbol.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        if (methodSymbol.ContainingType.IsGenericType && methodSymbol.ContainingType.TypeParameters.Length > 0)
        {
            var hasGenerateGenericTestAttribute = HasGenerateGenericTestAttribute(methodSymbol.ContainingType);

            if (!hasGenerateGenericTestAttribute)
            {
                var canInferTypes = CanInferGenericTypes(methodSymbol, context.Compilation);

                if (!canInferTypes)
                {
                    var diagnostic = Diagnostic.Create(
                        Rules.GenericTestMissingExplicitInstantiation,
                        methodDeclaration.Identifier.GetLocation(),
                        $"{methodSymbol.ContainingType.Name}.{methodSymbol.Name}");

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static void AnalyzeGenericTestClasses(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

        if (classSymbol == null || !classSymbol.IsGenericType)
        {
            return;
        }

        if (classSymbol.IsAbstract)
        {
            return;
        }

        var testMethods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(IsTestMethod)
            .ToList();

        if (!testMethods.Any())
        {
            return;
        }

        if (classSymbol.TypeParameters.Length > 0)
        {
            var hasGenerateGenericTestAttribute = HasGenerateGenericTestAttribute(classSymbol);

            if (!hasGenerateGenericTestAttribute)
            {
                var canInferTypes = testMethods.Any(m => CanInferGenericTypes(m, context.Compilation));

                var isUsedAsInheritsTestsBase = IsUsedAsInheritsTestsBase(classSymbol, context.Compilation);

                if (!canInferTypes && !isUsedAsInheritsTestsBase)
                {
                    var diagnostic = Diagnostic.Create(
                        Rules.GenericTestMissingExplicitInstantiation,
                        classDeclaration.Identifier.GetLocation(),
                        classSymbol.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }


    private static void AnalyzeDataSourceAttributes(SyntaxNodeAnalysisContext context)
    {
        var attribute = (AttributeSyntax)context.Node;
        var attributeSymbol = context.SemanticModel.GetSymbolInfo(attribute).Symbol;

        if (attributeSymbol?.ContainingType == null)
        {
            return;
        }

        var attributeTypeName = attributeSymbol.ContainingType.Name;

        if (attributeTypeName == "MethodDataSourceAttribute")
        {
            if (IsMethodDataSourceDynamic(context, attribute))
            {
                var diagnostic = Diagnostic.Create(
                    Rules.DynamicDataSourceNotAotCompatible,
                    attribute.GetLocation(),
                    attributeTypeName);

                context.ReportDiagnostic(diagnostic);
            }
        }
        else if (attributeTypeName == "ClassDataSourceAttribute")
        {
            if (attributeSymbol.ContainingType.IsGenericType)
            {
                return;
            }

            if (attribute.ArgumentList?.Arguments.Count is >= 1 and <= 5)
            {
                var allArgumentsAreTypeOf = attribute.ArgumentList.Arguments.All(arg =>
                    arg.Expression is TypeOfExpressionSyntax);

                if (allArgumentsAreTypeOf)
                {
                    return;
                }
            }

            var diagnostic = Diagnostic.Create(
                Rules.DynamicDataSourceNotAotCompatible,
                attribute.GetLocation(),
                attributeTypeName);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsTestMethod(IMethodSymbol method)
    {
        return method.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "TestAttribute" ||
            attr.AttributeClass?.Name == "Test");
    }

    private static bool HasGenerateGenericTestAttribute(ISymbol symbol)
    {
        return symbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == "GenerateGenericTestAttribute");
    }

    /// <summary>
    /// Checks if generic types can be inferred from test method data sources
    /// </summary>
    private static bool CanInferGenericTypes(IMethodSymbol method, Compilation compilation)
    {

        // Check for Arguments attributes that could provide type inference
        var argumentsType = compilation.GetTypeByMetadataName("TUnit.Core.ArgumentsAttribute");
        if (argumentsType != null)
        {
            var hasArgumentsAttributes = method.GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, argumentsType));

            if (hasArgumentsAttributes)
            {
                return true; // Arguments attributes can provide type inference
            }
        }

        // Check for MethodDataSource attributes
        var methodDataSourceType = compilation.GetTypeByMetadataName("TUnit.Core.MethodDataSourceAttribute");
        if (methodDataSourceType != null)
        {
            var hasMethodDataSourceAttributes = method.GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, methodDataSourceType));

            if (hasMethodDataSourceAttributes)
            {
                return true; // MethodDataSource attributes can provide type inference
            }
        }

        // Check for AsyncDataSourceGenerator attributes (implementations of IAsyncDataSourceGeneratorAttribute)
        var asyncDataSourceInterface = compilation.GetTypeByMetadataName("TUnit.Core.IAsyncDataSourceGeneratorAttribute");
        if (asyncDataSourceInterface != null)
        {
            var hasAsyncDataSourceGeneratorAttributes = method.GetAttributes()
                .Any(a => a.AttributeClass?.AllInterfaces.Contains(asyncDataSourceInterface, SymbolEqualityComparer.Default) == true);

            if (hasAsyncDataSourceGeneratorAttributes)
            {
                return true; // AsyncDataSourceGenerator attributes can provide type inference
            }
        }

        // For generic classes, also check class-level data source attributes
        if (method.ContainingType.IsGenericType)
        {
            // Check class-level Arguments attributes
            if (argumentsType != null)
            {
                var hasClassLevelArguments = method.ContainingType.GetAttributes()
                    .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, argumentsType));

                if (hasClassLevelArguments)
                {
                    return true; // Class-level Arguments attributes can provide type inference
                }
            }

            // Check class-level MethodDataSource attributes
            if (methodDataSourceType != null)
            {
                var hasClassLevelMethodDataSource = method.ContainingType.GetAttributes()
                    .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, methodDataSourceType));

                if (hasClassLevelMethodDataSource)
                {
                    return true; // Class-level MethodDataSource attributes can provide type inference
                }
            }

            // Check class-level AsyncDataSourceGenerator attributes
            if (asyncDataSourceInterface != null)
            {
                var hasClassLevelAsyncDataSource = method.ContainingType.GetAttributes()
                    .Any(a => a.AttributeClass?.AllInterfaces.Contains(asyncDataSourceInterface, SymbolEqualityComparer.Default) == true);

                if (hasClassLevelAsyncDataSource)
                {
                    return true; // Class-level AsyncDataSourceGenerator attributes can provide type inference
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a generic class is used as a base class for InheritsTests pattern
    /// </summary>
    private static bool IsUsedAsInheritsTestsBase(INamedTypeSymbol genericClass, Compilation compilation)
    {
        // Get all types in the compilation
        var allTypes = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>();

        // Check if any type inherits from this generic class and has InheritsTests attribute
        foreach (var type in allTypes)
        {
            if (type.BaseType != null &&
                SymbolEqualityComparer.Default.Equals(type.BaseType.OriginalDefinition, genericClass) &&
                type.GetAttributes().Any(a => a.AttributeClass?.Name == "InheritsTestsAttribute" ||
                                             a.AttributeClass?.Name == "InheritsTests"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a MethodDataSource attribute uses dynamic resolution patterns
    /// </summary>
    private static bool IsMethodDataSourceDynamic(SyntaxNodeAnalysisContext context, AttributeSyntax attribute)
    {
        if (attribute.ArgumentList?.Arguments.Count == 0)
        {
            return true; // No arguments means it can't be statically resolved
        }

        var firstArgument = attribute.ArgumentList?.Arguments[0];
        if (firstArgument == null)
        {
            return true;
        }

        // Check if first argument is nameof() - this is AOT-compatible
        if (firstArgument.Expression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } })
        {
            return false; // nameof() is AOT-compatible
        }

        // Check if first argument is a string literal - this is AOT-compatible
        if (firstArgument.Expression is LiteralExpressionSyntax literal &&
            literal.Token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralToken))
        {
            return false; // String literal method names are AOT-compatible
        }

        // Check for typeof() with method name - pattern: [MethodDataSource(typeof(SomeType), "MethodName")]
        if (attribute.ArgumentList?.Arguments.Count >= 2)
        {
            var secondArgument = attribute.ArgumentList.Arguments[1];

            // If first arg is typeof() and second is string literal or nameof(), it's AOT-compatible
            if (firstArgument.Expression is TypeOfExpressionSyntax &&
                (secondArgument.Expression is LiteralExpressionSyntax secondLiteral &&
                 secondLiteral.Token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralToken) ||
                 secondArgument.Expression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } }))
            {
                return false; // typeof() + string/nameof() is AOT-compatible
            }
        }

        // If we can't determine the pattern, assume it's dynamic
        return true;
    }

    private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);

        if (symbolInfo.Symbol is not IMethodSymbol method)
        {
            return;
        }

        var containingTypeName = method.ContainingType?.ToDisplayString();
        var methodName = method.Name;

        // Check for Expression.Compile()
        if (IsExpressionCompile(containingTypeName, methodName))
        {
            var diagnostic = Diagnostic.Create(
                Rules.ExpressionCompileNotAotCompatible,
                invocation.GetLocation());
            
            context.ReportDiagnostic(diagnostic);
        }

        // Check for Type.GetType()
        else if (containingTypeName == "System.Type" && methodName == "GetType" && method.Parameters.Length > 0)
        {
            if (invocation.ArgumentList.Arguments.Count > 0)
            {
                var firstArg = invocation.ArgumentList.Arguments[0];
                if (firstArg.Expression is LiteralExpressionSyntax literal && 
                    literal.Token.IsKind(SyntaxKind.StringLiteralToken))
                {
                    var diagnostic = Diagnostic.Create(
                        Rules.TypeGetTypeNotAotCompatible,
                        invocation.GetLocation(),
                        literal.Token.ValueText);
                    
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        // Check for MakeGenericType() and MakeArrayType()
        else if (containingTypeName == "System.Type" && 
                (methodName == "MakeGenericType" || methodName == "MakeArrayType"))
        {
            var diagnostic = Diagnostic.Create(
                Rules.MakeGenericTypeNotAotCompatible,
                invocation.GetLocation(),
                methodName);
            
            context.ReportDiagnostic(diagnostic);
        }

        // Check for Activator.CreateInstance
        else if (containingTypeName == "System.Activator" && methodName == "CreateInstance")
        {
            string typeToCheck = string.Empty;
            
            if (method.TypeParameters.Length > 0)
            {
                typeToCheck = method.TypeParameters[0].Name;
            }
            else if (invocation.ArgumentList.Arguments.Count > 0 && 
                     invocation.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeOfExpr)
            {
                typeToCheck = GetTypeFromTypeOfExpression(context, typeOfExpr);
            }

            if (!string.IsNullOrEmpty(typeToCheck))
            {
                var diagnostic = Diagnostic.Create(
                    Rules.ActivatorCreateInstanceWithoutAttribution,
                    invocation.GetLocation(),
                    typeToCheck);
                
                context.ReportDiagnostic(diagnostic);
            }
        }

        // Check for dynamic code generation methods
        else if (IsDynamicCodeGeneration(containingTypeName, methodName))
        {
            var diagnostic = Diagnostic.Create(
                Rules.DynamicCodeGenerationNotAotCompatible,
                invocation.GetLocation(),
                $"{containingTypeName}.{methodName}");
            
            context.ReportDiagnostic(diagnostic);
        }

        // Check for Reflection.Emit APIs
        else if (IsReflectionEmit(containingTypeName, methodName))
        {
            var diagnostic = Diagnostic.Create(
                Rules.ReflectionEmitNotAotCompatible,
                invocation.GetLocation(),
                $"{containingTypeName}.{methodName}");
            
            context.ReportDiagnostic(diagnostic);
        }

        // Check for dynamic assembly loading
        else if (IsDynamicAssemblyLoading(containingTypeName, methodName))
        {
            var diagnostic = Diagnostic.Create(
                Rules.AssemblyLoadFromNotAotCompatible,
                invocation.GetLocation(),
                $"{containingTypeName}.{methodName}");
            
            context.ReportDiagnostic(diagnostic);
        }

        // Check for reflection methods without proper attribution
        CheckReflectionMethod(context, method, invocation);
    }

    private static void AnalyzeMemberAccessExpression(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);

        if (symbolInfo.Symbol is IMethodSymbol method)
        {
            CheckReflectionMethod(context, method, memberAccess);
        }
    }

    private static bool IsExpressionCompile(string? containingTypeName, string methodName)
    {
        if (string.IsNullOrEmpty(containingTypeName) || methodName != "Compile")
        {
            return false;
        }

        return containingTypeName?.StartsWith("System.Linq.Expressions.Expression") == true ||
               containingTypeName == "System.Linq.Expressions.LambdaExpression";
    }

    private static bool IsDynamicCodeGeneration(string? containingTypeName, string methodName)
    {
        if (string.IsNullOrEmpty(containingTypeName))
        {
            return false;
        }

        var dynamicCodePatterns = new[]
        {
            ("Microsoft.CSharp.CSharpCodeProvider", "CompileAssemblyFromSource"),
            ("Microsoft.CSharp.CSharpCodeProvider", "CompileAssemblyFromFile"),
            ("Microsoft.CSharp.CSharpCodeProvider", "CompileAssemblyFromDom"),
            ("System.CodeDom.Compiler.CodeDomProvider", "CompileAssemblyFromSource"),
            ("System.CodeDom.Compiler.CodeDomProvider", "CompileAssemblyFromFile"),
            ("System.CodeDom.Compiler.CodeDomProvider", "CompileAssemblyFromDom"),
            ("Microsoft.CodeAnalysis.CSharp.CSharpCompilation", "Emit"),
            ("Microsoft.CodeAnalysis.Compilation", "Emit")
        };

        return dynamicCodePatterns.Any(pattern => 
            containingTypeName == pattern.Item1 && methodName == pattern.Item2);
    }

    private static bool IsReflectionEmit(string? containingTypeName, string methodName)
    {
        if (string.IsNullOrEmpty(containingTypeName))
        {
            return false;
        }

        var reflectionEmitNamespaces = new[]
        {
            "System.Reflection.Emit.AssemblyBuilder",
            "System.Reflection.Emit.ModuleBuilder", 
            "System.Reflection.Emit.TypeBuilder",
            "System.Reflection.Emit.MethodBuilder",
            "System.Reflection.Emit.ConstructorBuilder",
            "System.Reflection.Emit.PropertyBuilder",
            "System.Reflection.Emit.FieldBuilder",
            "System.Reflection.Emit.EventBuilder",
            "System.Reflection.Emit.ILGenerator",
            "System.Reflection.Emit.DynamicMethod"
        };

        return reflectionEmitNamespaces.Any(ns => containingTypeName?.StartsWith(ns) == true) &&
               IsEmitMethod(methodName);
    }

    private static bool IsEmitMethod(string methodName)
    {
        var emitMethods = new[]
        {
            "DefineType", "DefineMethod", "DefineConstructor", "DefineProperty", 
            "DefineField", "DefineEvent", "CreateType", "Emit", "EmitCall",
            "EmitCalli", "GetILGenerator", "SetImplementationFlags"
        };

        return emitMethods.Contains(methodName);
    }

    private static bool IsDynamicAssemblyLoading(string? containingTypeName, string methodName)
    {
        if (string.IsNullOrEmpty(containingTypeName))
        {
            return false;
        }

        var assemblyLoadingPatterns = new[]
        {
            ("System.Reflection.Assembly", "LoadFrom"),
            ("System.Reflection.Assembly", "LoadFile"), 
            ("System.Reflection.Assembly", "Load"),
            ("System.AppDomain", "Load"),
            ("System.AppDomain", "ExecuteAssembly"),
            ("System.Runtime.Loader.AssemblyLoadContext", "LoadFromAssemblyPath"),
            ("System.Runtime.Loader.AssemblyLoadContext", "LoadFromAssemblyName"),
            ("System.Runtime.Loader.AssemblyLoadContext", "LoadFromStream")
        };

        return assemblyLoadingPatterns.Any(pattern => 
            containingTypeName == pattern.Item1 && methodName == pattern.Item2);
    }

    private static void CheckReflectionMethod(SyntaxNodeAnalysisContext context, IMethodSymbol method, SyntaxNode node)
    {
        var containingTypeName = method.ContainingType?.ToDisplayString();
        var methodName = method.Name;

        var reflectionMethods = new[]
        {
            ("System.Type", "GetMethods"),
            ("System.Type", "GetMethod"),
            ("System.Type", "GetProperties"),
            ("System.Type", "GetProperty"),
            ("System.Type", "GetFields"),
            ("System.Type", "GetField"),
            ("System.Type", "GetConstructors"),
            ("System.Type", "GetConstructor"),
            ("System.Type", "GetMembers"),
            ("System.Type", "GetMember"),
            ("System.Type", "GetEvents"),
            ("System.Type", "GetEvent"),
            ("System.Type", "GetNestedTypes"),
            ("System.Type", "GetNestedType"),
            ("System.Type", "GetInterfaces"),
            ("System.Reflection.Assembly", "GetTypes"),
            ("System.Reflection.Assembly", "GetType"),
            ("System.Reflection.Module", "GetTypes"),
            ("System.Reflection.Module", "GetType")
        };

        foreach (var (typeName, methodNameToCheck) in reflectionMethods)
        {
            if (containingTypeName == typeName && methodName == methodNameToCheck)
            {
                var diagnostic = Diagnostic.Create(
                    Rules.ReflectionWithoutDynamicallyAccessedMembers,
                    node.GetLocation(),
                    $"{typeName}.{methodName}");
                
                context.ReportDiagnostic(diagnostic);
                break;
            }
        }
    }

    private static string GetTypeFromTypeOfExpression(SyntaxNodeAnalysisContext context, TypeOfExpressionSyntax typeOfExpr)
    {
        var typeInfo = context.SemanticModel.GetTypeInfo(typeOfExpr.Type);
        return typeInfo.Type?.ToDisplayString() ?? typeOfExpr.Type.ToString();
    }

}
