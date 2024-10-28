using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using TUnit.Analyzers.Extensions;
using TUnit.Analyzers.Helpers;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DisposableFieldPropertyAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.Dispose_Member_In_Cleanup);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
    }

    private void AnalyzeField(SyntaxNodeAnalysisContext context)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;
        
        var field = (IFieldSymbol) context.SemanticModel.GetDeclaredSymbol(fieldDeclaration.Declaration.Variables[0])!;

        if (!field.ContainingType.IsTestClass(context.Compilation))
        {
            return;
        }

        var isAsyncDisposable = IsAsyncDisposable(field.Type); 
        
        var isDisposable = IsDisposable(field.Type);

        if (!isAsyncDisposable && !isDisposable)
        {
            return;
        }

        if (!field.IsStatic && field.ContainingType.GetMembers().OfType<IMethodSymbol>()
                .Where(x => x.MethodKind == MethodKind.Constructor)
                .SelectMany(x => x.DeclaringSyntaxReferences)
                .SelectMany(x => x.GetSyntax().DescendantNodesAndSelf())
                .Where(x => x.IsKind(SyntaxKind.SimpleAssignmentExpression))
                .Select(x => context.SemanticModel.GetOperation(x))
                .OfType<IAssignmentOperation>()
                .Select(x => x.Target)
                .OfType<IFieldReferenceOperation>()
                .Any(x => x.Field.Name == field.Name))
        {
            return;
        }
        
        var expectedHookType = field.IsStatic
            ? "TUnit.Core.HookType.Class"
            : "TUnit.Core.HookType.Test";
            
        var methodsRequiringDisposeCall = field.ContainingType.GetMembers()
            .Where(x => x.IsStatic == field.IsStatic)
            .OfType<IMethodSymbol>()
            .Where(x => IsExpectedMethod(x, expectedHookType));

        var syntaxesWithinMethods = methodsRequiringDisposeCall
            .SelectMany(x => x.DeclaringSyntaxReferences)
            .Select(x => x.GetSyntax());

        if (!syntaxesWithinMethods
                .Where(x => !x.DescendantTrivia().Any(dt => dt.IsKind(SyntaxKind.SingleLineCommentTrivia)))
                .Where(x => !x.DescendantTrivia().Any(dt => dt.IsKind(SyntaxKind.MultiLineCommentTrivia)))
                .Any(x =>
                x.ToFullString()
                    .Replace("?", "")
                    .Replace("!", "")
                    .Contains(isAsyncDisposable
                    ? $"await {field.Name}.DisposeAsync()"
                    : $"{field.Name}.Dispose()")))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.Dispose_Member_In_Cleanup, context.Node.GetLocation()));
        }
    }

    private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax) context.Node;
        
        var property = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration)!;

        if (!property.ContainingType.IsTestClass(context.Compilation))
        {
            return;
        }
        
        var isAsyncDisposable = IsAsyncDisposable(property.Type); 
        
        var isDisposable = IsDisposable(property.Type);

        if (!isAsyncDisposable && !isDisposable)
        {
            return;
        }
        
        if (!property.IsStatic && property.ContainingType.GetMembers().OfType<IMethodSymbol>()
                .Where(x => x.MethodKind == MethodKind.Constructor)
                .SelectMany(x => x.DeclaringSyntaxReferences)
                .SelectMany(x => x.GetSyntax().DescendantNodesAndSelf())
                .Where(x => x.IsKind(SyntaxKind.SimpleAssignmentExpression))
                .Select(x => context.SemanticModel.GetOperation(x))
                .OfType<IAssignmentOperation>()
                .Select(x => x.Target)
                .OfType<IPropertyReferenceOperation>()
                .Any(x => x.Property.Name == property.Name))
        {
            return;
        }

        if (IsInjectedProperty(property))
        {
            return;
        }
        
        var expectedHookType = property.IsStatic
                ? "Class"
                : "Test";
            
        var methodsRequiringDisposeCall = property.ContainingType.GetMembers()
            .Where(x => x.IsStatic == property.IsStatic)
            .OfType<IMethodSymbol>()
            .Where(x => IsExpectedMethod(x, expectedHookType));

        var syntaxesWithinMethods = methodsRequiringDisposeCall
            .SelectMany(x => x.DeclaringSyntaxReferences)
            .Select(x => x.GetSyntax());

        if (!syntaxesWithinMethods
                .Where(x => !x.DescendantTrivia().Any(dt => dt.IsKind(SyntaxKind.SingleLineCommentTrivia)))
                .Where(x => !x.DescendantTrivia().Any(dt => dt.IsKind(SyntaxKind.MultiLineCommentTrivia)))
                .Any(x =>
                x.ToFullString()
                    .Replace("?", "")
                    .Replace("!", "")
                    .Contains(isAsyncDisposable
                    ? $"await {property.Name}.DisposeAsync()"
                    : $"{property.Name}.Dispose()")))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.Dispose_Member_In_Cleanup, context.Node.GetLocation()));
        }
    }

    private bool IsInjectedProperty(IPropertySymbol property)
    {
        return property.TryGetClassDataAttribute(out var classAttributeData);
    }

    private static bool IsExpectedMethod(IMethodSymbol method, string expectedHookType)
    {
        if (method.Name == "DisposeAsync" && IsAsyncDisposable(method.ContainingType))
        {
            return true;
        }

        if (method.Name == "Dispose" && IsDisposable(method.ContainingType))
        {
            return true;
        }
        
        return method.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) == WellKnown.AttributeFullyQualifiedClasses.AfterAttribute.WithGlobalPrefix && a.GetHookType() == expectedHookType);
    }

    private static bool IsDisposable(ITypeSymbol type)
    {
        return type.AllInterfaces
            .Any(x => x.SpecialType == SpecialType.System_IDisposable);
    }
    
    private static bool IsAsyncDisposable(ITypeSymbol type)
    {
        return type.AllInterfaces
            .Any(x => x.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) == "global::System.IAsyncDisposable");
    }
}