using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

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

        root = root.ReplaceNode(attributeSyntax, newExpression);

        var compilationUnit = root as CompilationUnitSyntax;

        if (compilationUnit is null)
        {
            return document.WithSyntaxRoot(root);
        }
        
        return document.WithSyntaxRoot(root);
    }

    private static async Task<IEnumerable<SyntaxNode>> GetNewExpression(Document document, AttributeSyntax attributeSyntax)
    {
        var name = attributeSyntax.Name.ToString();

        return name switch
        {
            "Fact" or "FactAttribute"
                or "Theory" or "TheoryAttribute" => ConvertTestAttribute(attributeSyntax),
            
            "Trait" or "TraitAttribute" => [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Property"),
                attributeSyntax.ArgumentList)],

            "InlineData" or "InlineDataAttribute" => [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Arguments"),
                attributeSyntax.ArgumentList)],

            "MemberData" or "MemberDataAttribute" => [SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("MethodDataSource"), attributeSyntax.ArgumentList)],

            "ClassData" or "ClassDataAttribute" => [SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("MethodDataSource"),
                (attributeSyntax.ArgumentList ?? SyntaxFactory.AttributeArgumentList()).AddArguments(
                    SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal("GetEnumerator")))))],

            "Collection" or "CollectionAttribute" => await ConvertCollection(document, attributeSyntax),

            "CollectionDefinition" or "CollectionDefinitionAttribute" => [SyntaxFactory.Attribute(
                SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"),
                    SyntaxFactory.IdentifierName("Obsolete")))],

            _ => []
        };
    }

    private static IEnumerable<AttributeSyntax> ConvertTestAttribute(AttributeSyntax attributeSyntax)
    {
        yield return SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Test"));

        if (attributeSyntax.ArgumentList?.Arguments.FirstOrDefault(x => x.NameEquals?.Name.Identifier.ValueText == "Skip") is {} skip)
        {
            yield return SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Skip"))
                .AddArgumentListArguments(SyntaxFactory.AttributeArgument(skip.Expression));
        }
    }

    private static async Task<IEnumerable<AttributeSyntax>> ConvertCollection(Document document, AttributeSyntax attributeSyntax)
    {
        var compilation = await document.Project.GetCompilationAsync();

        if (compilation is null)
        {
            return [];
        }
        
        var collectionDefinition = GetCollectionAttribute(compilation, attributeSyntax);

        if (collectionDefinition is null)
        {
            return [];
        }

        var disableParallelism =
            collectionDefinition.ArgumentList?.Arguments.Any(
                x => x.NameEquals?.Name.Identifier.Text == "DisableParallelization"
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

    private static TypeArgumentListSyntax GetGenericArguments(Compilation compilation, Document document,
        AttributeSyntax attributeSyntax)
    {
        var collectionAttribute = GetCollectionAttribute(compilation, attributeSyntax);

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

    private static AttributeSyntax? GetCollectionAttribute(Compilation compilation, AttributeSyntax attributeSyntax)
    {
        var firstToken = attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.GetFirstToken();

        if (!firstToken.HasValue)
        {
            return null;
        }

        var collectionName = firstToken.Value.IsKind(SyntaxKind.NameOfKeyword)
            ? GetNameFromNameOfToken(firstToken.Value) : firstToken.Value.ValueText;

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
                    ? GetNameFromNameOfToken(syntaxToken.Value) : syntaxToken.Value.ValueText;
                
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
}