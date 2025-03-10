using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XUnitAttributesCodeFixProvider)), Shared]
public class XUnitAttributesCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.XunitAttributes.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Rules.XunitAttributes.Title.ToString(),
                    createChangedDocument: c => ConvertAttributesAsync(context.Document, root?.FindNode(diagnosticSpan), c),
                    equivalenceKey: Rules.XunitAttributes.Title.ToString()),
                diagnostic);
        }
    }
    
    private static async Task<Document> ConvertAttributesAsync(Document document, SyntaxNode? node, CancellationToken cancellationToken)
    {
        if (node is not AttributeSyntax attributeSyntax)
        {
            return document;
        }

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        var newExpression = await GetNewExpression(document, attributeSyntax);

        if (newExpression != null)
        {
            root = root.ReplaceNode(attributeSyntax, newExpression.WithTriviaFrom(attributeSyntax));
        }

        var compilationUnit = root as CompilationUnitSyntax;

        if (compilationUnit is null)
        {
            return document.WithSyntaxRoot(root);
        }
        
        return document.WithSyntaxRoot(root);
    }

    private static async Task<SyntaxNode?> GetNewExpression(Document document, AttributeSyntax attributeSyntax)
    {
        var name = attributeSyntax.Name.ToString();

        return name switch
        {
            "Fact" or "FactAttribute" => SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Test")),

            "Theory" or "TheoryAttribute" => SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Test")),

            "Trait" or "TraitAttribute" => SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Property"),
                attributeSyntax.ArgumentList),

            "InlineData" or "InlineDataAttribute" => SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Arguments"),
                attributeSyntax.ArgumentList),

            "MemberData" or "MemberDataAttribute" => SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("MethodDataSource"), attributeSyntax.ArgumentList),

            "ClassData" or "ClassDataAttribute" => SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("MethodDataSource"),
                (attributeSyntax.ArgumentList ?? SyntaxFactory.AttributeArgumentList()).AddArguments(
                    SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal("GetEnumerator"))))),

            "Collection" or "CollectionAttribute" => SyntaxFactory.Attribute(
                SyntaxFactory.GenericName(SyntaxFactory.Identifier("ClassDataSource"),
                    await GetGenericArguments(document, attributeSyntax)),
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
                            expression: SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.GetFirstToken().ValueText ?? "Name"))
                        )
                    )
            ),

            "CollectionDefinition" or "CollectionDefinitionAttribute" => SyntaxFactory.Attribute(
                SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"),
                    SyntaxFactory.IdentifierName("Obsolete"))),

            _ => null
        };
    }

    private static async Task<TypeArgumentListSyntax> GetGenericArguments(Document document,
        AttributeSyntax attributeSyntax)
    {
        var compilation = await document.Project.GetCompilationAsync();

        if (compilation is null)
        {
            return SyntaxFactory.TypeArgumentList();
        }

        var collectionName = attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.GetFirstToken().ValueText;

        if (collectionName is null)
        {
            return SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                SyntaxFactory.IdentifierName("T")
            ));
        }

        var collectionAttribute = compilation.SyntaxTrees
            .Select(x => x.GetRoot())
            .SelectMany(x => x.DescendantNodes().OfType<AttributeSyntax>())
            .Where(attr => attr.Name.ToString() == "CollectionDefinition")
            .FirstOrDefault(x => x.ArgumentList?.Arguments.FirstOrDefault()?.GetFirstToken().ValueText == collectionName);

        if (collectionAttribute is null)
        {
            return SyntaxFactory.TypeArgumentList();
        }

        var classDeclaration = collectionAttribute.Parent?.Parent;
        
        if (classDeclaration is null)
        {
            return SyntaxFactory.TypeArgumentList();
        }
        
        var classSymbol = compilation.GetSemanticModel(classDeclaration.SyntaxTree).GetDeclaredSymbol(classDeclaration);
        
        if (classSymbol is not ITypeSymbol typeSymbol)
        {
            return SyntaxFactory.TypeArgumentList();
        }
        
        var interfaceType = typeSymbol.AllInterfaces.FirstOrDefault(x => x.Name == "ICollectionFixture");

        if (interfaceType is null)
        {
            return SyntaxFactory.TypeArgumentList();
        }
        
        return SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
            SyntaxFactory.IdentifierName(interfaceType.TypeArguments.First().Name)
        ));
    }
}