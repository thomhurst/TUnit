using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableFieldPropertyAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        Rules.Dispose_Member_In_Cleanup
    ];

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeClass(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        var namedTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        if (namedTypeSymbol == null || !namedTypeSymbol.IsTestClass(context.Compilation))
        {
            return;
        }

        var methods = namedTypeSymbol.GetSelfAndBaseTypes().SelectMany(x => x.GetMembers()).OfType<IMethodSymbol>().ToArray();

        CheckMethods(context, methods, true);
        CheckMethods(context, methods, false);
    }

    private static void CheckMethods(SyntaxNodeAnalysisContext context, IMethodSymbol[] methods, bool isStaticMethod)
    {
        var createdObjects = new ConcurrentDictionary<ISymbol, HookLevel?>(SymbolEqualityComparer.Default);

        var methodSymbols = methods.Where(x => x.IsStatic == isStaticMethod).ToArray();

        foreach (var methodSymbol in methodSymbols)
        {
            CheckSetUps(context, methodSymbol, createdObjects);
        }

        foreach (var methodSymbol in methodSymbols)
        {
            CheckTeardowns(context, methodSymbol, createdObjects);
        }

        foreach (var kvp in createdObjects)
        {
            var createdObject = kvp.Key;

            context.ReportDiagnostic(Diagnostic.Create(Rules.Dispose_Member_In_Cleanup,
                createdObject.Locations.FirstOrDefault(), createdObject.Name));
        }
    }

    private static void CheckSetUps(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol, ConcurrentDictionary<ISymbol, HookLevel?> createdObjects)
    {
        var syntaxNodes = methodSymbol.DeclaringSyntaxReferences
            .SelectMany(x => x.GetSyntax().DescendantNodesAndSelf()).ToArray();

        var isHookMethod = methodSymbol.IsHookMethod(context.Compilation, out _, out var level, out _);

        if (!isHookMethod && methodSymbol.MethodKind != MethodKind.Constructor)
        {
            return;
        }

        if (methodSymbol.MethodKind == MethodKind.Constructor)
        {
            level = HookLevel.Test;
        }

        foreach (var assignment in syntaxNodes
                     .Where(x => x.IsKind(SyntaxKind.SimpleAssignmentExpression)))
        {
            var assignmentOperation = assignment.GetOperation(context.SemanticModel) as IAssignmentOperation;

            if (assignmentOperation?.Target is not IFieldReferenceOperation and not IPropertyReferenceOperation)
            {
                continue;
            }

            if (assignmentOperation
               .Descendants()
               .OfType<IObjectCreationOperation>()
               .Any(x => x.Type?.IsDisposable() is true || x.Type?.IsAsyncDisposable() is true))
            {
                if (assignmentOperation.Target is IFieldReferenceOperation fieldReferenceOperation
                   && context.Compilation.HasImplicitConversion(methodSymbol.ContainingType, fieldReferenceOperation.Field.ContainingType))
                {
                    createdObjects.TryAdd(fieldReferenceOperation.Field, level);
                }

                if (assignmentOperation.Target is IPropertyReferenceOperation propertyReferenceOperation
                   && context.Compilation.HasImplicitConversion(methodSymbol.ContainingType, propertyReferenceOperation.Property.ContainingType))
                {
                    createdObjects.TryAdd(propertyReferenceOperation.Property, level);
                }
            }
        }
    }

    private static void CheckTeardowns(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol, ConcurrentDictionary<ISymbol, HookLevel?> createdObjects)
    {
        var syntaxNodes = methodSymbol.DeclaringSyntaxReferences
            .SelectMany(x => x.GetSyntax().DescendantNodesAndSelf()).ToArray();

        foreach (var assignment in syntaxNodes
                     .Where(x => x.IsKind(SyntaxKind.InvocationExpression)))
        {
            if (assignment.GetOperation(context.SemanticModel) is not IInvocationOperation invocationOperation)
            {
                continue;
            }

            if (!IsDisposeInvocation(context, invocationOperation) || !IsValidTearDownMethod(context, methodSymbol, out var level))
            {
                continue;
            }

            var fieldOrPropertyOperation = GetFieldOrPropertyOperation(invocationOperation);

            if (fieldOrPropertyOperation is IFieldReferenceOperation fieldReferenceOperation && createdObjects.TryGetValue(fieldReferenceOperation.Field, out var createdObjectLevel) && createdObjectLevel == level)
            {
                createdObjects.TryRemove(fieldReferenceOperation.Field, out _);
            }

            if (fieldOrPropertyOperation is IPropertyReferenceOperation propertyReferenceOperation && createdObjects.TryGetValue(propertyReferenceOperation.Property, out createdObjectLevel) && createdObjectLevel == level)
            {
                createdObjects.TryRemove(propertyReferenceOperation.Property, out _);
            }
        }
    }

    private static bool IsValidTearDownMethod(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol, out HookLevel? hookLevel)
    {
        if (!methodSymbol.IsStatic)
        {
            hookLevel = HookLevel.Test;
            return IsValidInstanceTearDownMethod(context, methodSymbol);
        }

        return IsValidStaticTearDownMethod(context, methodSymbol, out hookLevel);
    }

    private static bool IsValidStaticTearDownMethod(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol, out HookLevel? hookLevel)
    {
        return methodSymbol.IsHookMethod(context.Compilation, out var type, out hookLevel, out _)
               && type.Name.StartsWith("After")
               && hookLevel is HookLevel.TestSession or HookLevel.Assembly or HookLevel.Class;
    }

    private static bool IsValidInstanceTearDownMethod(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol)
    {
        if (methodSymbol is { Name: "Dispose", Parameters.IsDefaultOrEmpty: true }
            && methodSymbol.ContainingType.Interfaces.Any(x => x.SpecialType == SpecialType.System_IDisposable))
        {
            return true;
        }

        if (methodSymbol is { Name: "DisposeAsync", Parameters.IsDefaultOrEmpty: true })
        {
            var asyncDisposable = context.Compilation.GetTypeByMetadataName("System.IAsyncDisposable");
            return methodSymbol.ContainingType.Interfaces.Any(x =>
                SymbolEqualityComparer.Default.Equals(x, asyncDisposable));
        }

        if (methodSymbol.IsHookMethod(context.Compilation, out var type, out var level, out _)
            && type.Name.StartsWith("After") && level == HookLevel.Test)
        {
            return true;
        }

        return false;
    }

    private static IOperation? GetFieldOrPropertyOperation(IInvocationOperation invocationOperation)
    {
        var operation = invocationOperation.Instance;

        while (operation is not null)
        {
            if (operation is IConditionalAccessOperation conditionalAccessOperation)
            {
                return conditionalAccessOperation.Operation;
            }

            if (operation is IFieldReferenceOperation or IPropertyReferenceOperation)
            {
                return operation;
            }

            operation = operation.Parent;
        }

        return null;
    }

    private static bool IsDisposeInvocation(SyntaxNodeAnalysisContext context, IInvocationOperation invocationOperation)
    {
        if (invocationOperation.TargetMethod is { Name: "Dispose", Parameters.IsDefaultOrEmpty: true })
        {
            return invocationOperation.Instance?.Type?.AllInterfaces.Any(x => x.SpecialType == SpecialType.System_IDisposable) ==
                   true;
        }

        if (invocationOperation.TargetMethod is { Name: "DisposeAsync", Parameters.IsDefaultOrEmpty: true })
        {
            var asyncDisposable = context.Compilation.GetTypeByMetadataName("System.IAsyncDisposable");
            return invocationOperation.Instance?.Type?.AllInterfaces.Any(x =>
                SymbolEqualityComparer.Default.Equals(x, asyncDisposable)) == true;
        }

        return false;
    }
}
