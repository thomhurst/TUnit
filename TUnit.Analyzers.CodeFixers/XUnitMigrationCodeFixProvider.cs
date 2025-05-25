using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XUnitMigrationCodeFixProvider)), Shared]
public class XUnitMigrationCodeFixProvider : CodeFixProvider
{
    public override sealed ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.XunitMigration.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override sealed Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Rules.XunitMigration.Title.ToString(),
                    createChangedDocument: c => ConvertCodeAsync(context.Document, c),
                    equivalenceKey: Rules.XunitMigration.Title.ToString()),
                diagnostic);
        }
        
        return Task.CompletedTask;
    }

    private static async Task<Document> ConvertCodeAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }
        
        var compilation = await document.Project.GetCompilationAsync(cancellationToken);

        if (compilation is null)
        {
            return document;
        }
        
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        
        if (semanticModel is null)
        {
            return document;
        }
        
        // Convert all attributes in the syntax tree
        var updatedRoot = UpdateClassAttributesAsync(compilation, root);
        
        compilation = compilation.ReplaceSyntaxTree(root.SyntaxTree, updatedRoot.SyntaxTree);
        
        // Remove all xUnit specific interfaces and base classes
        updatedRoot = RemoveInterfacesAndBaseClasses(compilation, updatedRoot);

        // Remove using directives that are no longer needed
        updatedRoot = RemoveUsingDirectives(updatedRoot);
        
        // Apply all changes in one step
        return document.WithSyntaxRoot(updatedRoot);
    }

    private static SyntaxNode RemoveInterfacesAndBaseClasses(Compilation compilation, SyntaxNode root)
    {
        foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            if (classDeclaration.BaseList is null)
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            
            if (semanticModel.GetDeclaredSymbol(classDeclaration) is not { } symbol)
            {
                continue;
            }

            root = root.ReplaceNode(
                classDeclaration,
                new BaseTypeRewriter(symbol).VisitClassDeclaration(classDeclaration)
            );
        }

        return root;
    }

    private static SyntaxNode RemoveUsingDirectives(SyntaxNode updatedRoot)
    {
        return updatedRoot.RemoveNodes(
            updatedRoot.DescendantNodes().OfType<UsingDirectiveSyntax>()
                .Where(x => x.Name?.ToString().StartsWith("Xunit") is true),
            SyntaxRemoveOptions.AddElasticMarker)!;
    }

    private static SyntaxNode UpdateClassAttributesAsync(Compilation compilation, SyntaxNode root)
    {
        var rewriter = new AttributeRewriter(compilation);
        
        return rewriter.Visit(root);
    }

    private static string GetSimpleName(AttributeSyntax attributeSyntax)
    {
        var name = attributeSyntax.Name;

        while (name is not SimpleNameSyntax)
        {
            name = (name as QualifiedNameSyntax)?.Right;
        }

        return name.ToString();
    }

    private static IEnumerable<AttributeSyntax> ConvertTestAttribute(AttributeSyntax attributeSyntax)
    {
        yield return SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Test"));

        if (attributeSyntax.ArgumentList?.Arguments.FirstOrDefault(x => x.NameEquals?.Name.Identifier.ValueText == "Skip") is { } skip)
        {
            yield return SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Skip"))
                .AddArgumentListArguments(SyntaxFactory.AttributeArgument(skip.Expression));
        }
    }

    private static IEnumerable<AttributeSyntax> ConvertCollection(Compilation compilation, AttributeSyntax attributeSyntax)
    {
        var collectionDefinition = GetCollectionAttribute(compilation, attributeSyntax);

        if (collectionDefinition is null)
        {
            return [attributeSyntax];
        }

        var disableParallelism =
            collectionDefinition.ArgumentList?.Arguments.Any(x => x.NameEquals?.Name.Identifier.Text == "DisableParallelization"
                && x.Expression is LiteralExpressionSyntax { Token.ValueText: "true" }) ?? false;

        var attributes = new List<AttributeSyntax>();

        if (disableParallelism)
        {
            attributes.Add(SyntaxFactory.Attribute(SyntaxFactory.ParseName("NotInParallel")));
        }

        var baseListSyntax = collectionDefinition.Parent?.Parent?.ChildNodes().OfType<BaseListSyntax>().FirstOrDefault();

        if (baseListSyntax is null)
        {
            return attributes;
        }

        var collectionFixture = baseListSyntax.Types.Select(x => x.Type).OfType<GenericNameSyntax>().FirstOrDefault(x => x.Identifier.Text == "ICollectionFixture");

        if (collectionFixture is null)
        {
            return attributes;
        }

        var type = collectionFixture.TypeArgumentList.Arguments.FirstOrDefault();

        if (type is null)
        {
            return attributes;
        }

        attributes.Add(SyntaxFactory.Attribute(
            SyntaxFactory.GenericName(SyntaxFactory.Identifier("ClassDataSource"),
                SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(type))),
            SyntaxFactory.AttributeArgumentList()
                .AddArguments(
                    SyntaxFactory.AttributeArgument(
                        nameEquals: SyntaxFactory.NameEquals("Shared"),
                        nameColon: null,
                        expression: SyntaxFactory.ParseExpression("SharedType.Keyed")
                    ),
                    SyntaxFactory.AttributeArgument(
                        nameEquals: SyntaxFactory.NameEquals("Key"),
                        nameColon: null,
                        expression: GetMethodArgumentName(attributeSyntax)
                    )
                )
        ));

        return attributes;
    }

    private static ExpressionSyntax GetMethodArgumentName(AttributeSyntax attributeSyntax)
    {
        var firstToken = attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.GetFirstToken();

        if (!firstToken.HasValue)
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(""));
        }

        return SyntaxFactory.ParseExpression(firstToken.Value.Text);
    }

    private static AttributeSyntax? GetCollectionAttribute(Compilation compilation, AttributeSyntax attributeSyntax)
    {
        var firstToken = attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.GetFirstToken();

        if (!firstToken.HasValue)
        {
            return null;
        }

        var collectionName = firstToken.Value.IsKind(SyntaxKind.NameOfKeyword)
            ? GetNameFromNameOfToken(firstToken.Value)
            : firstToken.Value.ValueText;

        if (collectionName is null)
        {
            return null;
        }

        return compilation.SyntaxTrees
            .Select(x => x.GetRoot())
            .SelectMany(x => x.DescendantNodes().OfType<AttributeSyntax>())
            .Where(attr => attr.Name.ToString() == "CollectionDefinition")
            .FirstOrDefault(x =>
            {
                var syntaxToken = x.ArgumentList?.Arguments.FirstOrDefault()?.GetFirstToken();

                if (!syntaxToken.HasValue)
                {
                    return false;
                }

                var name = syntaxToken.Value.IsKind(SyntaxKind.NameOfKeyword)
                    ? GetNameFromNameOfToken(syntaxToken.Value)
                    : syntaxToken.Value.ValueText;

                return name == collectionName;
            });
    }

    private static string? GetNameFromNameOfToken(SyntaxToken token)
    {
        var expression = SyntaxFactory.ParseExpression(token.Text) as InvocationExpressionSyntax;

        if (expression?.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" } &&
            expression.ArgumentList.Arguments.FirstOrDefault()?.Expression is IdentifierNameSyntax nameOfArgument)
        {
            return nameOfArgument.Identifier.Text;
        }

        return null;
    }

    private class AttributeRewriter(Compilation compilation) : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitAttributeList(AttributeListSyntax node)
        {
            var newAttributes = new List<AttributeSyntax>();

            foreach (var attr in node.Attributes)
            {
                var name = GetSimpleName(attr);

                var converted = name switch
                {
                    "Fact" or "FactAttribute" or "Theory" or "TheoryAttribute" => ConvertTestAttribute(attr),
                    "Trait" or "TraitAttribute" => [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Property"), attr.ArgumentList)],
                    "InlineData" or "InlineDataAttribute" => [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Arguments"), attr.ArgumentList)],
                    "MemberData" or "MemberDataAttribute" => [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("MethodDataSource"), attr.ArgumentList)],
                    "ClassData" or "ClassDataAttribute" =>
                    [
                        SyntaxFactory.Attribute(
                            SyntaxFactory.IdentifierName("MethodDataSource"),
                            (attr.ArgumentList ?? SyntaxFactory.AttributeArgumentList()).AddArguments(
                                SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal("GetEnumerator")))))
                    ],
                    "Collection" or "CollectionAttribute" => ConvertCollection(compilation, attr),
                    _ => [attr]
                };

                newAttributes.AddRange(converted);
            }

            // Preserve original trivia instead of forcing elastic trivia
            return SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(newAttributes))
                .WithLeadingTrivia(node.GetLeadingTrivia())
                .WithTrailingTrivia(node.GetTrailingTrivia());
        }
    }
    
    private class BaseTypeRewriter(INamedTypeSymbol namedTypeSymbol) : CSharpSyntaxRewriter
    {
        
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.BaseList is null)
            {
                return node;
            }

            ITypeSymbol[] types = namedTypeSymbol.BaseType != null && namedTypeSymbol.BaseType.SpecialType != SpecialType.System_Object
                ? [namedTypeSymbol.BaseType, ..namedTypeSymbol.AllInterfaces] 
                : [..namedTypeSymbol.AllInterfaces];
            
            var newBaseList = types.Where(x => !x.ContainingNamespace.Name.StartsWith("Xunit"))
                .Select(x => SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(x.ToDisplayString())))
                .ToList();

            if (newBaseList.Count == 0)
            {
                // Preserve original trivia instead of forcing elastic trivia
                return node.WithBaseList(null)
                    .WithOpenBraceToken(node.OpenBraceToken.WithLeadingTrivia(node.BaseList.GetTrailingTrivia()));
            }

            var baseListSyntax = node.BaseList.WithTypes(SyntaxFactory.SeparatedList<BaseTypeSyntax>(newBaseList));
            
            return node.WithBaseList(baseListSyntax);
        }
    }
}
