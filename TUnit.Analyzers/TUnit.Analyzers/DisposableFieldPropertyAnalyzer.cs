﻿using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
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

        if (!IsTestClass(field.ContainingType))
        {
            return;
        }

        var isAsyncDisposable = IsAsyncDisposable(field.Type); 
        
        var isDisposable = IsDisposable(field.Type);

        if (!isAsyncDisposable && !isDisposable)
        {
            return;
        }
        
        var expectedTearDownMethodAttribute = field.IsStatic
            ? WellKnown.AttributeFullyQualifiedClasses.AfterAllTestsInClassAttribute
            : WellKnown.AttributeFullyQualifiedClasses.AfterEachTest;
            
        var methodsRequiringDisposeCall = field.ContainingType.GetMembers()
            .Where(x => x.IsStatic == field.IsStatic)
            .OfType<IMethodSymbol>()
            .Where(x => IsExpectedMethod(x, expectedTearDownMethodAttribute));

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

        if (!IsTestClass(property.ContainingType))
        {
            return;
        }

        
        var isAsyncDisposable = IsAsyncDisposable(property.Type); 
        
        var isDisposable = IsDisposable(property.Type);

        if (!isAsyncDisposable && !isDisposable)
        {
            return;
        }
        
        var expectedTearDownMethodAttribute = property.IsStatic
                ? WellKnown.AttributeFullyQualifiedClasses.AfterAllTestsInClassAttribute
                : WellKnown.AttributeFullyQualifiedClasses.AfterEachTest;
            
        var methodsRequiringDisposeCall = property.ContainingType.GetMembers()
            .Where(x => x.IsStatic == property.IsStatic)
            .OfType<IMethodSymbol>()
            .Where(x => IsExpectedMethod(x, expectedTearDownMethodAttribute));

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

    private bool IsTestClass(INamedTypeSymbol classType)
    {
        return classType.GetMembers().OfType<IMethodSymbol>().Any(x => x.IsTestMethod());
    }

    private static bool IsExpectedMethod(IMethodSymbol method, string expectedTearDownMethodAttribute)
    {
        if (method.Name == "DisposeAsync" && IsAsyncDisposable(method.ContainingType))
        {
            return true;
        }

        if (method.Name == "Dispose" && IsDisposable(method.ContainingType))
        {
            return true;
        }
        
        return method.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) == expectedTearDownMethodAttribute);
    }

    private static bool IsDisposable(ITypeSymbol type)
    {
        return type.AllInterfaces
            .Any(x => x.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) == "global::System.IDisposable");
    }
    
    private static bool IsAsyncDisposable(ITypeSymbol type)
    {
        return type.AllInterfaces
            .Any(x => x.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix) == "global::System.IAsyncDisposable");
    }
}